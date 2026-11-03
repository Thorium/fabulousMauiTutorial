namespace TaskManagerApp.Auth

open System
open System.Net.Http
open System.Threading
open System.Threading.Tasks

/// Holds the current session, keeps it in the SessionStore, and refreshes the access token before
/// it expires. One instance is shared by the MVU commands and the HTTP pipeline, so token refreshes
/// are serialized here.
type SessionManager(provider: AuthProvider, store: SessionStore, clock: unit -> DateTimeOffset) =
    let gate = new SemaphoreSlim(1, 1)
    let mutable current: Session voption = ValueNone

    let needsRefresh (session: Session) =
        session.Tokens.ExpiresAt - SessionManager.RefreshMargin <= clock ()

    let adopt session =
        task {
            current <- ValueSome session
            do! store.Save session
            return Ok session
        }

    let drop () =
        task {
            current <- ValueNone
            do! store.Clear()
        }

    let refresh session cancellation =
        task {
            match! provider.Refresh session cancellation with
            | Ok fresh -> return! adopt fresh
            | Error SessionExpired ->
                do! drop ()
                return Error SessionExpired
            | Error error -> return Error error
        }

    let exclusive (cancellation: CancellationToken) (action: unit -> Task<'T>) =
        task {
            do! gate.WaitAsync cancellation

            try
                return! action ()
            finally
                gate.Release() |> ignore
        }

    /// Tokens are refreshed when they have less than this left.
    static member RefreshMargin = TimeSpan.FromMinutes 1.

    member _.Current = current

    /// Loads the stored session. An expired one is refreshed; when the provider cannot be reached
    /// the stored session is kept and the refresh is retried on the next request.
    member _.Restore(cancellation: CancellationToken) =
        task {
            try
                return!
                    exclusive cancellation (fun () ->
                        task {
                            match! store.Load() with
                            | ValueNone -> return Ok ValueNone
                            | ValueSome session when not (needsRefresh session) ->
                                current <- ValueSome session
                                return Ok(ValueSome session)
                            | ValueSome session ->
                                match! refresh session cancellation with
                                | Ok fresh -> return Ok(ValueSome fresh)
                                | Error(Unavailable _) ->
                                    current <- ValueSome session
                                    return Ok(ValueSome session)
                                | Error error -> return Error error
                        })
            with e ->
                return Error(Unexpected e.Message)
        }

    /// Signs in through the provider. Cancelling the token ends the attempt with `Error Cancelled`.
    member _.SignIn(cancellation: CancellationToken) =
        task {
            try
                return!
                    exclusive cancellation (fun () ->
                        task {
                            match! provider.SignIn cancellation with
                            | Ok session -> return! adopt session
                            | Error error -> return Error error
                        })
            with
            | :? OperationCanceledException -> return Error Cancelled
            | e -> return Error(Unexpected e.Message)
        }

    /// A valid access token, refreshed first when it is about to expire.
    member _.AccessToken(cancellation) =
        exclusive cancellation (fun () ->
            task {
                match current with
                | ValueNone -> return Error SessionExpired
                | ValueSome session when needsRefresh session ->
                    let! refreshed = refresh session cancellation
                    return refreshed |> Result.map _.Tokens.AccessToken
                | ValueSome session -> return Ok session.Tokens.AccessToken
            })

    member _.SignOut(cancellation) =
        exclusive cancellation (fun () ->
            task {
                match current with
                | ValueSome session ->
                    try
                        do! provider.SignOut session cancellation
                    with
                    | :? HttpRequestException
                    | :? TaskCanceledException -> ()

                    do! drop ()
                | ValueNone -> do! drop ()
            })

module SessionManager =

    /// The session manager for an AuthConfig; ValueNone for NoAuth. `http` is used for the
    /// token endpoint and `browser` for the sign-in page of an Oidc provider.
    let create (http: HttpClient) (browser: SignInBrowser) (store: SessionStore) (config: AuthConfig) =
        let manager provider =
            ValueSome(SessionManager(provider, store, fun () -> DateTimeOffset.UtcNow))

        match config with
        | NoAuth -> ValueNone
        | Oidc settings -> manager (Oidc.provider http browser settings)
        | Custom provider -> manager provider

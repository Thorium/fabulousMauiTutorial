module SessionManagerTests

open System
open System.Threading
open System.Threading.Tasks
open Xunit
open TaskManagerApp.Auth
open Fakes

let private now = DateTimeOffset(2026, 11, 3, 12, 0, 0, TimeSpan.Zero)
let private fresh = session (now.AddHours 1.)

let private refreshed =
    { fresh with
        Tokens =
            { fresh.Tokens with
                AccessToken = "access-new"
            }
    }

let private manager (provider: ScriptedProvider) (store: MemoryStore) =
    SessionManager(provider.Provider, store.Store, fun () -> now)

[<Fact>]
let ``a valid stored session is restored without a refresh`` () =
    let provider = ScriptedProvider(fun () -> Ok refreshed)
    let store = MemoryStore(ValueSome fresh)
    let sessions = manager provider store
    Assert.Equal(Ok(ValueSome fresh), sessions.Restore(CancellationToken.None).Result)
    Assert.Equal(0, provider.Refreshes)
    Assert.Equal(Ok "access-old", sessions.AccessToken(CancellationToken.None).Result)

[<Fact>]
let ``an expired stored session is refreshed and saved`` () =
    let provider = ScriptedProvider(fun () -> Ok refreshed)
    let store = MemoryStore(ValueSome(session (now.AddSeconds 30.)))
    let sessions = manager provider store
    Assert.Equal(Ok(ValueSome refreshed), sessions.Restore(CancellationToken.None).Result)
    Assert.Equal(ValueSome refreshed, store.Stored)

[<Fact>]
let ``offline at start-up keeps the stored session`` () =
    let stale = session (now.AddMinutes -5.)
    let provider = ScriptedProvider(fun () -> Error(Unavailable "offline"))
    let sessions = manager provider (MemoryStore(ValueSome stale))
    Assert.Equal(Ok(ValueSome stale), sessions.Restore(CancellationToken.None).Result)
    Assert.Equal(ValueSome stale, sessions.Current)

[<Fact>]
let ``a rejected refresh clears the stored session`` () =
    let provider = ScriptedProvider(fun () -> Error SessionExpired)
    let store = MemoryStore(ValueSome(session (now.AddMinutes -5.)))
    let sessions = manager provider store
    Assert.Equal(Error SessionExpired, sessions.Restore(CancellationToken.None).Result)
    Assert.Equal(ValueNone, store.Stored)
    Assert.Equal(ValueNone, sessions.Current)

[<Fact>]
let ``concurrent requests share one refresh`` () =
    let clock = ref now
    let provider = ScriptedProvider(fun () -> Ok refreshed)

    let sessions =
        SessionManager(
            provider.Provider,
            MemoryStore(ValueSome(session (now.AddMinutes 30.))).Store,
            fun () -> clock.Value
        )

    sessions.Restore(CancellationToken.None).Wait()
    clock.Value <- now.AddMinutes 29.5

    let tokens =
        Task.WhenAll [ for _ in 1..10 -> sessions.AccessToken CancellationToken.None ]
        |> _.Result

    Assert.All(tokens, fun token -> Assert.Equal(Ok "access-new", token))
    Assert.Equal(1, provider.Refreshes)

[<Fact>]
let ``a cancelled sign-in ends with Cancelled`` () =
    let slow: AuthProvider =
        {
            SignIn =
                fun cancellation ->
                    task {
                        do! Task.Delay(Timeout.Infinite, cancellation)
                        return Error Cancelled
                    }
            Refresh = fun _ _ -> Task.FromResult(Error SessionExpired)
            SignOut = fun _ _ -> Task.FromResult()
        }

    let sessions = SessionManager(slow, MemoryStore(ValueNone).Store, fun () -> now)
    use cancellation = new CancellationTokenSource()
    let signIn = sessions.SignIn cancellation.Token
    cancellation.Cancel()
    Assert.Equal(Error Cancelled, signIn.Result)

[<Fact>]
let ``a failing token endpoint at start-up keeps the stored session`` () =
    let stale = session (now.AddMinutes -5.)

    let endpoint =
        new TokenEndpoint(fun _ -> Net.HttpStatusCode.ServiceUnavailable, "<html>503</html>")

    let browser, _ = browser true []

    let settings =
        Cognito.hostedUi "example.auth.eu-west-2.amazoncognito.com" "client-1" (Uri "taskmanager://signin")

    let provider = Oidc.provider (new Net.Http.HttpClient(endpoint)) browser settings
    let store = MemoryStore(ValueSome stale)
    let sessions = SessionManager(provider, store.Store, fun () -> now)
    Assert.Equal(Ok(ValueSome stale), sessions.Restore(CancellationToken.None).Result)
    Assert.Equal(ValueSome stale, store.Stored)

let private signedIn (signOut: Session -> CancellationToken -> Task<unit>) =
    let provider: AuthProvider =
        {
            SignIn = fun _ -> Task.FromResult(Error Cancelled)
            Refresh = fun _ _ -> Task.FromResult(Error SessionExpired)
            SignOut = signOut
        }

    let store = MemoryStore(ValueSome fresh)
    let sessions = SessionManager(provider, store.Store, fun () -> now)
    Assert.Equal(Ok(ValueSome fresh), sessions.Restore(CancellationToken.None).Result)
    sessions, store

[<Fact>]
let ``sign-out clears the session before the provider's part completes`` () =
    let pending = TaskCompletionSource<unit>()
    let sessions, store = signedIn (fun _ _ -> pending.Task)
    let signOut = sessions.SignOut CancellationToken.None
    // The end-session page is still open: the session is gone and token requests are not blocked
    Assert.False signOut.IsCompleted
    Assert.Equal(ValueNone, store.Stored)
    Assert.Equal(ValueNone, sessions.Current)
    Assert.Equal(Error SessionExpired, sessions.AccessToken(CancellationToken.None).Result)
    pending.SetResult()
    signOut.Wait()

[<Fact>]
let ``sign-out does not fail when the provider throws`` () =
    let sessions, store = signedIn (fun _ _ -> raise (InvalidOperationException "boom"))
    sessions.SignOut(CancellationToken.None).Wait()
    Assert.Equal(ValueNone, store.Stored)

type private StubHandler() =
    inherit Net.Http.HttpMessageHandler()
    member val Requests = 0 with get, set

    override this.SendAsync(_, _) =
        this.Requests <- this.Requests + 1
        Task.FromResult(new Net.Http.HttpResponseMessage(Net.HttpStatusCode.OK))

let private apiWith (refresh: unit -> Result<Session, AuthError>) =
    let provider = ScriptedProvider refresh
    let sessions = manager provider (MemoryStore(ValueSome(session (now.AddHours 1.))))
    let stub = new StubHandler()
    let api = new Net.Http.HttpClient(new TaskManagerApp.Http.BearerTokenHandler(sessions, stub))
    sessions, stub, api

[<Fact>]
let ``a request without a session is answered 401 and not sent`` () =
    let _, stub, api = apiWith (fun () -> Error SessionExpired)
    let response = api.GetAsync("https://api.example.com/tasks").Result
    Assert.Equal(Net.HttpStatusCode.Unauthorized, response.StatusCode)
    Assert.Equal(0, stub.Requests)

[<Fact>]
let ``a request carries the access token`` () =
    let sessions, stub, api = apiWith (fun () -> Error SessionExpired)
    sessions.Restore(CancellationToken.None).Wait()
    let response = api.GetAsync("https://api.example.com/tasks").Result
    Assert.Equal(Net.HttpStatusCode.OK, response.StatusCode)
    Assert.Equal(1, stub.Requests)

[<Fact>]
let ``a token that cannot be refreshed while offline fails the request and keeps the session`` () =
    let stale = session (now.AddSeconds 30.)
    let provider = ScriptedProvider(fun () -> Error(Unavailable "offline"))
    let sessions = manager provider (MemoryStore(ValueSome stale))
    sessions.Restore(CancellationToken.None).Wait()
    let stub = new StubHandler()
    let api = new Net.Http.HttpClient(new TaskManagerApp.Http.BearerTokenHandler(sessions, stub))

    let failure =
        Assert.Throws<AggregateException>(fun () -> api.GetAsync("https://api.example.com/tasks").Wait())

    Assert.IsType<Net.Http.HttpRequestException>(failure.InnerException) |> ignore
    Assert.Equal(0, stub.Requests)
    Assert.Equal(ValueSome stale, sessions.Current)

[<Fact>]
let ``a rejected refresh is still answered 401`` () =
    let stale = session (now.AddSeconds 30.)
    let provider = ScriptedProvider(fun () -> Error(Rejected("invalid_client", "")))
    let sessions = manager provider (MemoryStore(ValueSome stale))
    sessions.Restore(CancellationToken.None).Wait()
    let stub = new StubHandler()
    let api = new Net.Http.HttpClient(new TaskManagerApp.Http.BearerTokenHandler(sessions, stub))
    let response = api.GetAsync("https://api.example.com/tasks").Result
    Assert.Equal(Net.HttpStatusCode.Unauthorized, response.StatusCode)
    Assert.Equal(0, stub.Requests)

[<Fact>]
let ``a sign-in waits for a sign-out's end-session page`` () =
    let pending = TaskCompletionSource<unit>()
    let signIns = ref 0

    let provider: AuthProvider =
        {
            SignIn =
                fun _ ->
                    signIns.Value <- signIns.Value + 1
                    Task.FromResult(Ok fresh)
            Refresh = fun _ _ -> Task.FromResult(Error SessionExpired)
            SignOut = fun _ _ -> pending.Task
        }

    let sessions = SessionManager(provider, MemoryStore(ValueSome fresh).Store, fun () -> now)
    sessions.Restore(CancellationToken.None).Wait()
    let signOut = sessions.SignOut CancellationToken.None
    let signIn = sessions.SignIn CancellationToken.None
    Assert.Equal(0, signIns.Value)
    pending.SetResult()
    signOut.Wait()
    Assert.Equal(Ok fresh, signIn.Result)
    Assert.Equal(1, signIns.Value)

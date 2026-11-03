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

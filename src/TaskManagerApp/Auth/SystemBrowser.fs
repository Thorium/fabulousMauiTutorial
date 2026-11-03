namespace TaskManagerApp.Auth

open System
open System.Threading
open System.Threading.Tasks
open Microsoft.Maui.ApplicationModel
#if !WINDOWS
open Microsoft.Maui.Authentication
#endif

/// Sign-in through the system browser. Native apps must not use an embedded web view for this
/// (RFC 8252): the browser keeps the password away from the app and shares the provider's session.
module SystemBrowser =

    /// The custom URL scheme the browser redirects to on Android and iOS.
    [<Literal>]
    let CallbackScheme = "taskmanager"

    [<Literal>]
    let CallbackHost = "signin"

#if WINDOWS
    /// Desktop apps receive the redirect on a loopback port. Register this exact URI with the provider.
    /// The port is below 49152: Windows gives ports above it to outgoing connections, so one there can be taken.
    let redirectUri = Uri "http://localhost:42813/signin/"

    let private launch (page: Uri) : Task =
        MainThread.InvokeOnMainThreadAsync<bool>(Func<Task<bool>>(fun () -> Launcher.Default.OpenAsync page))

    /// A sign-in abandoned in the browser ends after five minutes.
    let authenticate: SignInBrowser =
        Loopback.browser launch (TimeSpan.FromMinutes 5.) "Done. You can close this tab and return to Task Manager."
#else
    let redirectUri = Uri $"{CallbackScheme}://{CallbackHost}"

    let authenticate: SignInBrowser =
        fun authorizeUri redirect cancellation ->
            task {
                try
                    let options =
                        WebAuthenticatorOptions(
                            Url = authorizeUri,
                            CallbackUrl = redirect,
                            // iOS: no shared cookies, so signing out really signs out
                            PrefersEphemeralWebBrowserSession = true
                        )

                    let! result =
                        MainThread.InvokeOnMainThreadAsync<WebAuthenticatorResult>(
                            Func<Task<WebAuthenticatorResult>>(fun () ->
                                WebAuthenticator.Default.AuthenticateAsync(options, cancellation))
                        )

                    return Ok(result.Properties |> Seq.map (fun pair -> pair.Key, pair.Value) |> Map.ofSeq)
                with :? TaskCanceledException ->
                    return Error Cancelled
            }
#endif

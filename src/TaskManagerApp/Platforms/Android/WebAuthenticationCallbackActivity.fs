namespace TaskManagerApp

open Android.App
open Android.Content
open Android.Content.PM
open Microsoft.Maui.Authentication
open TaskManagerApp.Auth

/// Receives the sign-in redirect (taskmanager://signin) from the browser and hands it to WebAuthenticator.
[<Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)>]
[<IntentFilter([| Intent.ActionView |],
               Categories = [| Intent.CategoryDefault; Intent.CategoryBrowsable |],
               DataScheme = SystemBrowser.CallbackScheme,
               DataHost = SystemBrowser.CallbackHost)>]
type WebAuthenticationCallbackActivity() =
    inherit WebAuthenticatorCallbackActivity()

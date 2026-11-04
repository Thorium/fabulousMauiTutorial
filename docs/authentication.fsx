(**
---
title: Authentication
category: Guides
categoryindex: 1
index: 2
---

# Authentication

Sign-in is chosen by one value, `Config.auth` in `src/TaskManagerApp/Config.fs`. The rest of
the app (session handling, the sign-in page, bearer tokens on API calls) works the same for
every choice.
*)

(*** hide ***)
#r "../src/TaskManagerApp.Logic/bin/Release/net10.0/TaskManagerApp.Logic.dll"

open System
open System.Net.Http
open System.Threading.Tasks
open TaskManagerApp.Auth

(*** hide ***)
// SystemBrowser.redirectUri lives in the MAUI project; these pages use the mobile value
let redirectUri = Uri "taskmanager://signin"

(**
## Choosing a provider

`AuthConfig` has three cases:
*)

let open' = NoAuth

let cognito =
    Oidc(Cognito.hostedUi "myapp.auth.eu-west-2.amazoncognito.com" "app-client-id" redirectUri)

let demo = Custom DemoAuth.provider

(**
- `NoAuth` shows the task pages straight away.
- `Oidc` runs the OAuth 2.0 authorization code flow with PKCE against any OpenID Connect
  provider. `Cognito`, `EntraId` and `Auth0` build the settings; any other provider takes an
  `OidcSettings` record with its endpoints.
- `Custom` takes any `AuthProvider`: three functions for sign-in, refresh and sign-out.
  `DemoAuth.provider` signs in a fixed user without network calls and is the default, so the
  sample runs without an identity provider.

### Recommended: Amazon Cognito with the Hosted UI

For an app with an AWS backend, use a Cognito user pool and its managed login (Hosted UI)
through the system browser, with the authorization code flow and PKCE:

- The password is typed into the provider's page, never into the app.
- The app holds no client secret. A secret shipped in an app binary can be extracted, so it
  protects nothing.
- MFA, account recovery, and federation with Google, Apple, Facebook or a corporate SAML or
  OIDC provider are configured in the pool, with no app change.
- The backend validates the same JWTs (issuer, audience, `cognito:groups`) for every client.
- Apps that already use the pool, such as an older client being replaced, share its users: the
  `sub` claim stays the same, so no user migration is needed.

To send users straight to a federated provider instead of the Hosted UI page:
*)

let google =
    Cognito.hostedUi "myapp.auth.eu-west-2.amazoncognito.com" "app-client-id" redirectUri
    |> Cognito.federated "Google"
    |> Oidc

(**
Things to avoid:

| Approach | Problem |
| --- | --- |
| Password grant with a client secret in the app | The secret is extractable; the password grant is removed in OAuth 2.1 and skips MFA and federation |
| An embedded web view for the sign-in page | The app can read the password; Google blocks it; RFC 8252 requires the system browser |
| "Sign in with Google" alone | No user pool of your own: no groups, no other providers, user ids tied to Google |
| A custom user database in the backend | Password storage, reset, lockout and MFA become your problem |

Entra ID or Auth0 are good choices when the organisation already runs them:
*)

let entra =
    Oidc(EntraId.settings "tenant-id" "client-id" "api://backend-app-id/access" redirectUri)

let auth0 =
    Oidc(Auth0.settings "myapp.eu.auth0.com" "client-id" "https://api.example.com" redirectUri)

(**
For Auth0, register a Native application (a Single Page Application works too, but a Generic
one has the wrong grant settings) and give each environment its own tenant or application and
API audience. Auth0 does not send groups: with RBAC enabled, the access token carries a
`permissions` claim, and roles arrive only in a namespaced custom claim added by an Action, such
as `https://example.com/roles`. Read those with `Jwt.payload` when the app needs them; `User.Groups`
covers `cognito:groups`, `groups` and `roles`.
*)

(**
### Cognito app client settings

| Setting | Value |
| --- | --- |
| Client secret | None (public client) |
| OAuth grant types | Authorization code |
| Scopes | `openid`, `email`, `profile` |
| Allowed callback URLs | `taskmanager://signin` (Android, iOS) and `http://localhost:42813/signin/` (Windows) |
| Allowed sign-out URLs | The same two URLs |
| Token revocation | Enabled (default for new clients) |
| Refresh token expiration | How long users stay signed in without signing in again (default 30 days) |

Sign-out revokes the refresh token and opens the pool's `/logout` page, which clears the Hosted UI
session cookie in the browser. Without that step the browser stays signed in to Cognito, and the
next sign-in completes straight away as the same user, so nobody can switch accounts.

The authorize request the app opens looks like this (state and challenge change every time):
*)

let settings =
    Cognito.hostedUi "myapp.auth.eu-west-2.amazoncognito.com" "app-client-id" redirectUri

let authorize =
    (Oidc.authorizeUri settings "state" (Pkce.challenge "verifier")).AbsoluteUri

(*** include-value: authorize ***)

(**
## Signing in on a phone without typing

Users sign in once. After that the app restores the stored session on every launch and refreshes
the access token in the background, so they never see the sign-in page again until the refresh
token expires or they sign out. That covers most of the "automatic" feel of a mobile app.

The first sign-in always needs one user action: no platform hands an app the user's identity
without consent. How small that action is depends on the method:

| Method | First sign-in | Needs |
| --- | --- | --- |
| Email and password on the Hosted UI | Type email and password; the platform password manager can fill them | Nothing extra |
| Google through Cognito (`Cognito.federated "Google"`) | Pick the account in Google's chooser; no password when the browser is already signed in to Google | A Google OAuth client set up as a pool identity provider |
| Apple through Cognito (`Cognito.federated "SignInWithApple"`) | Confirm with Face ID or Touch ID | Apple developer Services ID and key set up as a pool identity provider |
| Passkey on Cognito managed login | Confirm with Face ID, Touch ID or fingerprint; synced through iCloud Keychain or Google Password Manager | Managed login (not the classic Hosted UI) and the Essentials or Plus feature plan |
| Native "Sign in with Google" (Android Credential Manager) | A bottom sheet with the phone's Google accounts; later launches can sign in with no tap | A backend step: Cognito user pools do not accept a Google ID token directly, so a custom-auth Lambda must verify it |

The first four run through the browser flow this sample already implements. The native Credential
Manager route is the only one that can sign in with no tap at all: it auto-selects only when
exactly one Google account on the phone has signed in to the app before, it matches the phone's
default account, and the user has not signed out of the app or turned automatic sign-in off.

Conditions for a smooth browser sign-in:

- **Shared browser session.** Android Custom Tabs share Chrome's cookies, so an existing Google
  session is reused. On iOS, `SystemBrowser` sets `PrefersEphemeralWebBrowserSession`, which keeps
  Safari's cookies out: sign-out is clean, but users type their Google password again. Set it to
  false to reuse Safari's session; iOS then asks "App wants to use ... to sign in" each time.
- **One user per person.** A user who signed up with a password and later picks Google gets a
  second Cognito user with a different `sub`, unless a pre-sign-up Lambda links the accounts
  (`AdminLinkProviderForUser`). Plan this before moving existing users to federated sign-in.
- **A long enough refresh token** in the app client settings, and the session in secure storage.
- **Identity provider attributes** mapped to the pool, at least `email`, and the Google consent
  screen published for production use.

Unlocking the app with Face ID or a fingerprint on each launch is a separate, local feature: it
guards the stored session on the device and does not sign in anyone.

## Writing a custom provider

An `AuthProvider` is a record of three functions, so a custom provider is a value. This
skeleton is the starting point for a backend that issues its own tokens; the
`SignInBrowser` opens your own sign-in page in the system browser:
*)

let ownBackend (http: HttpClient) (browser: SignInBrowser) : AuthProvider =
    {
        SignIn =
            fun cancellation ->
                task {
                    let start = Uri "https://api.example.com/signin/start"

                    match! browser start redirectUri cancellation with
                    | Error error -> return Error error
                    | Ok redirect ->
                        // Exchange redirect["code"] with your backend, read the tokens and
                        // build the session, for example with Oidc.readTokens and Oidc.sessionOf
                        return Error(InvalidResponse "Not implemented")
                }
        Refresh = fun session cancellation -> Task.FromResult(Error SessionExpired)
        SignOut = fun session cancellation -> Task.FromResult()
    }

(**
Providers compose. A decorator adds behaviour to any provider, for example logging:
*)

let logged (log: string -> unit) (provider: AuthProvider) =
    { provider with
        SignIn =
            fun cancellation ->
                task {
                    let! result = provider.SignIn cancellation

                    match result with
                    | Ok session -> log $"Signed in {session.User.Subject}"
                    | Error error -> log $"Sign-in failed: {error}"

                    return result
                }
    }

let loggedCognito =
    Custom(
        logged
            (printfn "%s")
            (Oidc.provider (new HttpClient()) (fun _ _ _ -> Task.FromResult(Error Cancelled)) settings)
    )

(**
## The session

`SessionManager` holds the current session for both the MVU commands and the HTTP pipeline:

| Member | Does |
| --- | --- |
| `Restore` | Loads the stored session at start-up; refreshes it when expired; keeps it when offline |
| `SignIn` | Runs the provider's sign-in and stores the session; a cancelled token gives `Error Cancelled` |
| `AccessToken` | Returns a token with more than a minute left, refreshing first if needed; concurrent callers share one refresh |
| `SignOut` | Revokes the refresh token and opens the end-session page where the provider has them, then clears the store |

A refresh token the provider rejects ends the session with `SessionExpired`, and the user sees
the sign-in page again. The app stores the session with `SecureSessionStore` (Keychain,
Android Keystore, DPAPI). Any other `SessionStore` record plugs in, and tests use an in-memory one.

Errors are a union, so the sign-in page can explain each one:
*)

let messages =
    [
        Cancelled
        SessionExpired
        Unavailable "timeout"
        Rejected("access_denied", "User is disabled.")
    ]
    |> List.map AuthError.describe

(*** include-value: messages ***)

(**
## Calling the backend

`ApiClient.create` returns an `HttpClient` that adds `Authorization: Bearer <access token>`
to each request. It doesn't follow redirects, and neither does `ApiClient.createForSignIn`'s client for the token
endpoint: a request's body, such as a refresh token, would go to wherever one points. Without a valid session the request is not sent and the caller gets
`401 Unauthorized`, the same answer the backend gives for a bad token, so both are handled
in one place:

```fsharp
let api = ApiClient.create (Uri "https://api.example.com/") session

let loadTasks () =
    async {
        let! response = api.GetAsync "tasks" |> Async.AwaitTask
        match response.StatusCode with
        | HttpStatusCode.Unauthorized -> return Error SessionExpired   // dispatch SignOut
        | HttpStatusCode.Forbidden -> ...                               // signed in, but not allowed
        | _ -> ...
    }
```

Only `401` means "sign in again". A `403` means the user is signed in but lacks a permission;
sending them back to the sign-in page would loop, because signing in again changes nothing.

## Platform setup

| Platform | Redirect | Set up in |
| --- | --- | --- |
| Android | `taskmanager://signin` | `Platforms/Android/WebAuthenticationCallbackActivity.fs` (intent filter), `AndroidManifest.xml` (Custom Tabs query, `INTERNET`) |
| iOS | `taskmanager://signin` | Nothing: `ASWebAuthenticationSession` receives the redirect directly |
| Windows | `http://localhost:42813/signin/` | `Auth/SystemBrowser.fs` listens on the loopback port; MAUI's `WebAuthenticator` does not support Windows |

To change the scheme, change `SystemBrowser.CallbackScheme` and the callback URL registered
with the provider. Each app on a device needs its own scheme.
*)

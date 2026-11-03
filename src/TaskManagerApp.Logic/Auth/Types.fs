namespace TaskManagerApp.Auth

open System
open System.Threading
open System.Threading.Tasks

/// Tokens issued by the identity provider.
type Tokens =
    {
        AccessToken: string
        IdToken: string voption
        RefreshToken: string voption
        ExpiresAt: DateTimeOffset
    }

/// The signed-in user, read from the ID token claims.
type User =
    {
        /// Stable user id (`sub`); the backend keys its data on this.
        Subject: string
        Email: string voption
        Name: string voption
        Groups: string list
    }

type Session = { User: User; Tokens: Tokens }

module User =
    /// The email, else the name, else the subject id
    let displayName (user: User) =
        user.Email
        |> ValueOption.orElse user.Name
        |> ValueOption.defaultValue user.Subject

type AuthError =
    /// The user closed the sign-in page.
    | Cancelled
    /// The provider answered with an OAuth error, e.g. `access_denied`.
    | Rejected of error: string * description: string
    /// The refresh token is no longer valid; the user must sign in again.
    | SessionExpired
    /// The provider could not be reached.
    | Unavailable of message: string
    /// The provider answered with something this client cannot read.
    | InvalidResponse of message: string
    /// Anything else that went wrong, such as an exception in platform code.
    | Unexpected of message: string

module AuthError =
    let describe error =
        match error with
        | Cancelled -> "Sign-in was cancelled."
        | Rejected(_, description) when not (String.IsNullOrWhiteSpace description) -> description
        | Rejected(code, _) -> $"Sign-in was rejected ({code})."
        | SessionExpired -> "Your session has expired. Please sign in again."
        | Unavailable _ -> "The sign-in service is unavailable. Check your connection and try again."
        | InvalidResponse _ -> "The sign-in service sent an unexpected response."
        | Unexpected message -> $"Sign-in failed: {message}"

/// A way to sign users in. Implement these three functions to plug in any identity system.
type AuthProvider =
    {
        SignIn: CancellationToken -> Task<Result<Session, AuthError>>
        Refresh: Session -> CancellationToken -> Task<Result<Session, AuthError>>
        /// Revokes what can be revoked; never fails, the local session is cleared regardless.
        SignOut: Session -> CancellationToken -> Task<unit>
    }

/// Opens the authorize URL in the system browser and returns the query parameters of the
/// redirect back to the app. Supplied by the platform layer.
type SignInBrowser = Uri -> Uri -> CancellationToken -> Task<Result<Map<string, string>, AuthError>>

/// Persists the session between app launches. Use secure platform storage.
type SessionStore =
    {
        Load: unit -> Task<Session voption>
        Save: Session -> Task<unit>
        Clear: unit -> Task<unit>
    }

/// Authorization code flow with PKCE against any OpenID Connect / OAuth 2.0 provider.
type OidcSettings =
    {
        AuthorizeEndpoint: Uri
        TokenEndpoint: Uri
        /// RFC 7009 revocation endpoint; refresh tokens are revoked on sign-out when set.
        RevokeEndpoint: Uri voption
        /// Page that ends the provider's browser session, opened on sign-out when set. Without it the
        /// browser stays signed in to the provider and the next sign-in completes as the same user.
        EndSessionUri: Uri voption
        /// A public client id. Mobile apps cannot keep a client secret, so none is used.
        ClientId: string
        Scopes: string list
        RedirectUri: Uri
        /// Extra authorize parameters, e.g. `identity_provider` or `prompt`.
        AuthorizeParameters: (string * string) list
    }

/// How the app signs users in.
type AuthConfig =
    /// No sign-in: every screen is open.
    | NoAuth
    /// Cognito, Entra ID, Auth0, Keycloak, Okta or any other OpenID Connect provider.
    | Oidc of OidcSettings
    /// Anything else: your own backend, a test double, a third-party SDK wrapped as an AuthProvider.
    | Custom of AuthProvider

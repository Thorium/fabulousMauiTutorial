/// Ready-made settings for common identity providers. Each returns OidcSettings, so any field
/// (scopes, extra parameters) can be changed with a record copy.
namespace TaskManagerApp.Auth

open System
open System.Threading.Tasks

module Cognito =

    /// The managed login (Hosted UI) of a Cognito user pool domain, such as
    /// `myapp.auth.eu-west-2.amazoncognito.com`. The app client must have no secret, the
    /// authorization code grant enabled, and the redirect URI in both its allowed callback URLs
    /// and its allowed sign-out URLs.
    let hostedUi (domain: string) (clientId: string) (redirectUri: Uri) =
        let endpoint path = Uri($"https://{domain}/oauth2/{path}")

        {
            AuthorizeEndpoint = endpoint "authorize"
            TokenEndpoint = endpoint "token"
            RevokeEndpoint = ValueSome(endpoint "revoke")
            EndSessionUri =
                ValueSome(
                    Uri(
                        $"https://{domain}/logout?client_id={Uri.EscapeDataString clientId}&logout_uri={Uri.EscapeDataString redirectUri.OriginalString}"
                    )
                )
            ClientId = clientId
            Scopes = [ "openid"; "email"; "profile" ]
            RedirectUri = redirectUri
            AuthorizeParameters = []
        }

    /// Goes straight to an identity provider federated into the pool ("Google", "SignInWithApple",
    /// "Facebook", or the name of a SAML or OIDC provider) instead of showing the Hosted UI.
    let federated (identityProvider: string) (settings: OidcSettings) =
        { settings with
            AuthorizeParameters = ("identity_provider", identityProvider) :: settings.AuthorizeParameters
        }

module EntraId =

    /// Microsoft Entra ID (v2.0 endpoints). `apiScope` is the scope your backend exposes,
    /// such as `api://<backend-app-id>/access`.
    let settings (tenant: string) (clientId: string) (apiScope: string) (redirectUri: Uri) =
        let endpoint path =
            Uri($"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/{path}")

        {
            AuthorizeEndpoint = endpoint "authorize"
            TokenEndpoint = endpoint "token"
            RevokeEndpoint = ValueNone
            EndSessionUri = ValueNone
            ClientId = clientId
            Scopes = [ "openid"; "email"; "profile"; "offline_access"; apiScope ]
            RedirectUri = redirectUri
            AuthorizeParameters = []
        }

module Auth0 =

    /// An Auth0 tenant such as `myapp.eu.auth0.com`. `audience` is the API identifier of your backend.
    /// The application is a Native application, with the redirect URI in its Allowed Callback URLs
    /// and Allowed Logout URLs.
    let settings (domain: string) (clientId: string) (audience: string) (redirectUri: Uri) =
        {
            AuthorizeEndpoint = Uri($"https://{domain}/authorize")
            TokenEndpoint = Uri($"https://{domain}/oauth/token")
            RevokeEndpoint = ValueSome(Uri($"https://{domain}/oauth/revoke"))
            EndSessionUri =
                ValueSome(
                    Uri(
                        $"https://{domain}/v2/logout?client_id={Uri.EscapeDataString clientId}&returnTo={Uri.EscapeDataString redirectUri.OriginalString}"
                    )
                )
            ClientId = clientId
            Scopes = [ "openid"; "email"; "profile"; "offline_access" ]
            RedirectUri = redirectUri
            AuthorizeParameters = [ "audience", audience ]
        }

/// A provider that signs in a fixed user without any network calls. It runs the whole sign-in
/// flow locally and is the template for a Custom provider.
module DemoAuth =

    let user =
        {
            Subject = "demo-user"
            Email = ValueSome "demo@example.com"
            Name = ValueSome "Demo User"
            Groups = []
        }

    let private issue () =
        {
            User = user
            Tokens =
                {
                    AccessToken = "demo-access-token"
                    IdToken = ValueNone
                    RefreshToken = ValueSome "demo-refresh-token"
                    ExpiresAt = DateTimeOffset.UtcNow.AddHours 1.
                }
        }

    let provider: AuthProvider =
        {
            SignIn =
                fun cancellation ->
                    task {
                        do! Task.Delay(500, cancellation)
                        return Ok(issue ())
                    }
            Refresh = fun _ _ -> Task.FromResult(Ok(issue ()))
            SignOut = fun _ _ -> Task.FromResult()
        }

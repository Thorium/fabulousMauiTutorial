/// Application configuration.
module TaskManagerApp.Config

open TaskManagerApp.Auth

/// How users sign in. Change this value; nothing else in the app needs to change.
/// docs/authentication.fsx describes each option and the provider-side setup.
let auth = Custom DemoAuth.provider
// NoAuth
// Oidc(Cognito.hostedUi "myapp.auth.eu-west-2.amazoncognito.com" "<app client id>" SystemBrowser.redirectUri)
// Oidc(Cognito.hostedUi "myapp.auth.eu-west-2.amazoncognito.com" "<app client id>" SystemBrowser.redirectUri |> Cognito.federated "Google")
// Oidc(EntraId.settings "<tenant id>" "<client id>" "api://<backend app id>/access" SystemBrowser.redirectUri)
// Oidc(Auth0.settings "myapp.eu.auth0.com" "<client id>" "https://api.example.com" SystemBrowser.redirectUri)

module AuthTests

open System
open System.Net
open System.Net.Http
open System.Threading
open Xunit
open TaskManagerApp.Auth
open Fakes

let private settings =
    Cognito.hostedUi "example.auth.eu-west-2.amazoncognito.com" "client-1" (Uri "taskmanager://signin")

[<Fact>]
let ``the PKCE challenge matches RFC 7636 appendix B`` () =
    Assert.Equal(
        "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM",
        Pkce.challenge "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk"
    )

[<Fact>]
let ``random strings are 43 URL-safe characters`` () =
    let value = Pkce.randomString ()
    Assert.Equal(43, value.Length)
    Assert.Matches("^[A-Za-z0-9_-]+$", value)

[<Fact>]
let ``the user comes from the token claims`` () =
    let token =
        jwt """{"sub":"abc","email":"a@example.com","cognito:groups":["ops","admin"],"cognito:username":"anna"}"""

    let expected =
        {
            Subject = "abc"
            Email = ValueSome "a@example.com"
            Name = ValueSome "anna"
            Groups = [ "ops"; "admin" ]
        }

    Assert.Equal(ValueSome expected, Jwt.user token)
    Assert.Equal(ValueNone, Jwt.user "opaque-token")
    Assert.Equal(ValueNone, Jwt.user (jwt """{"email":"no-subject@example.com"}"""))

[<Fact>]
let ``the authorize request carries the PKCE challenge and extra parameters`` () =
    let uri =
        Oidc.authorizeUri (settings |> Cognito.federated "Google") "state-1" "challenge-1"

    Assert.Equal("https://example.auth.eu-west-2.amazoncognito.com/oauth2/authorize", uri.GetLeftPart UriPartial.Path)

    for expected in
        [
            "response_type=code"
            "client_id=client-1"
            // exactly the registered callback URL: System.Uri would add a trailing slash
            "redirect_uri=taskmanager%3A%2F%2Fsignin&"
            "scope=openid%20email%20profile"
            "state=state-1"
            "code_challenge=challenge-1"
            "code_challenge_method=S256"
            "identity_provider=Google"
        ] do
        Assert.Contains(expected, uri.Query)

[<Fact>]
let ``a refresh answer without refresh or ID token keeps the previous ones`` () =
    let now = DateTimeOffset(2026, 11, 3, 12, 0, 0, TimeSpan.Zero)

    let previous =
        (session now).Tokens
        |> fun tokens ->
            { tokens with
                IdToken = ValueSome "id-old"
            }

    match Oidc.readTokens now (ValueSome previous) """{"access_token":"access-new","expires_in":600}""" with
    | Ok tokens ->
        Assert.Equal("access-new", tokens.AccessToken)
        Assert.Equal(ValueSome "id-old", tokens.IdToken)
        Assert.Equal(ValueSome "refresh-old", tokens.RefreshToken)
        Assert.Equal(now.AddMinutes 10., tokens.ExpiresAt)
    | Error error -> failwith $"{error}"

[<Fact>]
let ``OAuth errors are read from the answer`` () =
    Assert.Equal(
        Rejected("invalid_grant", "Refresh Token has expired"),
        Oidc.readError """{"error":"invalid_grant","error_description":"Refresh Token has expired"}"""
    )

    Assert.Equal(InvalidResponse "<html>", Oidc.readError "<html>")

[<Fact>]
let ``sign-in exchanges the code with the verifier behind the challenge`` () =
    let endpoint = new TokenEndpoint(fun _ -> HttpStatusCode.OK, tokenAnswer "user-1")
    let browser, requests = browser true [ "code", "code-1" ]
    let provider = Oidc.provider (new HttpClient(endpoint)) browser settings

    match (provider.SignIn CancellationToken.None).Result with
    | Ok session ->
        Assert.Equal("user-1", session.User.Subject)
        Assert.Equal<string list>([ "ops" ], session.User.Groups)
        Assert.Equal(ValueSome "refresh-user-1", session.Tokens.RefreshToken)
    | Error error -> failwith $"{error}"

    let form = Seq.exactlyOne endpoint.Forms
    Assert.Equal("authorization_code", form["grant_type"])
    Assert.Equal("code-1", form["code"])
    Assert.Equal("client-1", form["client_id"])
    Assert.Equal("taskmanager://signin", form["redirect_uri"])
    Assert.False(form.ContainsKey "client_secret")
    let challenge = Pkce.challenge form["code_verifier"]
    Assert.Contains($"code_challenge={challenge}", (Seq.exactlyOne requests).Query)

[<Fact>]
let ``a redirect with another state is refused before any token request`` () =
    let endpoint = new TokenEndpoint(fun _ -> HttpStatusCode.OK, tokenAnswer "user-1")
    let browser, _ = browser false [ "code", "code-1"; "state", "forged" ]
    let provider = Oidc.provider (new HttpClient(endpoint)) browser settings

    match (provider.SignIn CancellationToken.None).Result with
    | Error(InvalidResponse _) -> Assert.Empty endpoint.Forms
    | other -> failwith $"{other}"

[<Fact>]
let ``an error redirect is reported as rejected`` () =
    let endpoint = new TokenEndpoint(fun _ -> HttpStatusCode.OK, tokenAnswer "user-1")

    let browser, _ =
        browser true [ "error", "access_denied"; "error_description", "User is disabled." ]

    let provider = Oidc.provider (new HttpClient(endpoint)) browser settings
    Assert.Equal(Error(Rejected("access_denied", "User is disabled.")), (provider.SignIn CancellationToken.None).Result)

[<Fact>]
let ``a rejected refresh token ends the session`` () =
    let endpoint =
        new TokenEndpoint(fun _ -> HttpStatusCode.BadRequest, """{"error":"invalid_grant"}""")

    let browser, _ = browser true []
    let provider = Oidc.provider (new HttpClient(endpoint)) browser settings
    Assert.Equal(Error SessionExpired, (provider.Refresh (session DateTimeOffset.UtcNow) CancellationToken.None).Result)
    Assert.Equal("refresh-old", (Seq.exactlyOne endpoint.Forms)["refresh_token"])

[<Fact>]
let ``sign-out revokes the refresh token and ends the browser session`` () =
    let endpoint = new TokenEndpoint(fun _ -> HttpStatusCode.OK, "")
    let browser, requests = browser true []
    let provider = Oidc.provider (new HttpClient(endpoint)) browser settings
    (provider.SignOut (session DateTimeOffset.UtcNow) CancellationToken.None).Wait()
    let form = Seq.exactlyOne endpoint.Forms
    Assert.Equal("refresh-old", form["token"])
    Assert.Equal("client-1", form["client_id"])

    Assert.Equal(
        "https://example.auth.eu-west-2.amazoncognito.com/logout?client_id=client-1&logout_uri=taskmanager%3A%2F%2Fsignin",
        (Seq.exactlyOne requests).AbsoluteUri
    )

[<Fact>]
let ``Auth0 sign-out ends the tenant session and returns to the app`` () =
    let auth0 =
        Auth0.settings "myapp.eu.auth0.com" "client-1" "https://api.example.com" (Uri "taskmanager://signin")

    Assert.Equal(
        ValueSome "https://myapp.eu.auth0.com/v2/logout?client_id=client-1&returnTo=taskmanager%3A%2F%2Fsignin",
        auth0.EndSessionUri |> ValueOption.map _.AbsoluteUri
    )

    Assert.Contains(("audience", "https://api.example.com"), auth0.AuthorizeParameters)
    Assert.Contains("offline_access", auth0.Scopes)

[<Fact>]
let ``a failing end-session page does not fail sign-out`` () =
    let endpoint = new TokenEndpoint(fun _ -> HttpStatusCode.OK, "")
    let failing: SignInBrowser = fun _ _ _ -> failwith "browser closed"
    let provider = Oidc.provider (new HttpClient(endpoint)) failing settings
    (provider.SignOut (session DateTimeOffset.UtcNow) CancellationToken.None).Wait()
    Assert.Single endpoint.Forms |> ignore

[<Fact>]
let ``the loopback browser returns the redirect's parameters`` () =
    let redirect = Uri "http://localhost:42814/signin/"
    use http = new HttpClient()

    // The "browser" runs on its own: it asks for a favicon first, then follows the provider's redirect
    let launch (_: Uri) : Tasks.Task =
        Tasks.Task.Run(fun () ->
            task {
                let! _ = http.GetAsync "http://localhost:42814/favicon.ico"
                let! _ = http.GetAsync "http://localhost:42814/signin/?code=code-1&state=state-1"
                ()
            }
            :> Tasks.Task)
        |> ignore

        Tasks.Task.CompletedTask

    let signIn = Loopback.browser launch (TimeSpan.FromSeconds 10.) "Done."

    Assert.Equal(
        Ok(Map [ "code", "code-1"; "state", "state-1" ]),
        (signIn (Uri "https://example.com/authorize") redirect CancellationToken.None).Result
    )

[<Fact>]
let ``an abandoned loopback sign-in ends as cancelled`` () =
    let launch (_: Uri) : Tasks.Task = Tasks.Task.CompletedTask
    let signIn = Loopback.browser launch (TimeSpan.FromMilliseconds 200.) "Done."

    Assert.Equal(
        Error Cancelled,
        (signIn (Uri "https://example.com/authorize") (Uri "http://localhost:42815/signin/") CancellationToken.None)
            .Result
    )

[<Fact>]
let ``a stored session reads back the same`` () =
    let stored =
        { session (DateTimeOffset.FromUnixTimeSeconds 1793700000L) with
            User =
                {
                    Subject = "user-1"
                    Email = ValueSome "u@example.com"
                    Name = ValueNone
                    Groups = [ "ops" ]
                }
        }

    Assert.Equal(ValueSome stored, SessionJson.read (SessionJson.write stored))
    Assert.Equal(ValueNone, SessionJson.read "{}")
    Assert.Equal(ValueNone, SessionJson.read "not json")

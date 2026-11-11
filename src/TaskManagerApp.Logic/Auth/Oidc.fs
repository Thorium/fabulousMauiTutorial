/// OAuth 2.0 authorization code flow with PKCE, the flow for public clients such as mobile apps
/// (RFC 8252). Works with any OpenID Connect provider.
module TaskManagerApp.Auth.Oidc

open System
open System.Collections.Generic
open System.Net
open System.Net.Http
open System.Text.Json
open System.Threading
open System.Threading.Tasks

let private query (parameters: (string * string) list) =
    parameters
    |> List.map (fun (name, value) -> $"{Uri.EscapeDataString name}={Uri.EscapeDataString value}")
    |> String.concat "&"

let authorizeUri (settings: OidcSettings) (state: string) (codeChallenge: string) =
    let separator = if settings.AuthorizeEndpoint.Query = "" then "?" else "&"

    [
        "response_type", "code"
        "client_id", settings.ClientId
        "redirect_uri", settings.RedirectUri.OriginalString
        "scope", String.Join(" ", settings.Scopes)
        "state", state
        "code_challenge", codeChallenge
        "code_challenge_method", "S256"
        yield! settings.AuthorizeParameters
    ]
    |> query
    |> fun parameters -> Uri($"{settings.AuthorizeEndpoint}{separator}{parameters}")

let private stringProperty (root: JsonElement) (name: string) =
    match root.TryGetProperty name with
    | true, value when value.ValueKind = JsonValueKind.String -> ValueSome(value.GetString())
    | _ -> ValueNone

/// Reads a token endpoint answer. A refresh answer may leave out the refresh and ID tokens;
/// the previous ones are kept then.
let readTokens (now: DateTimeOffset) (previous: Tokens voption) (json: string) =
    try
        use document = JsonDocument.Parse json
        let root = document.RootElement

        match stringProperty root "access_token" with
        | ValueNone -> Error(InvalidResponse "The token response has no access_token.")
        | ValueSome accessToken ->
            let lifetime =
                match root.TryGetProperty "expires_in" with
                | true, value when value.ValueKind = JsonValueKind.Number -> value.GetInt32()
                | _ -> 3600

            Ok
                {
                    AccessToken = accessToken
                    IdToken =
                        stringProperty root "id_token"
                        |> ValueOption.orElse (previous |> ValueOption.bind _.IdToken)
                    RefreshToken =
                        stringProperty root "refresh_token"
                        |> ValueOption.orElse (previous |> ValueOption.bind _.RefreshToken)
                    ExpiresAt = now.AddSeconds(float lifetime)
                }
    with :? JsonException as e ->
        Error(InvalidResponse e.Message)

/// Reads an OAuth error answer (RFC 6749 section 5.2).
let readError (json: string) =
    try
        use document = JsonDocument.Parse json
        let root = document.RootElement

        match stringProperty root "error" with
        | ValueSome error -> Rejected(error, stringProperty root "error_description" |> ValueOption.defaultValue "")
        | ValueNone -> InvalidResponse json
    with :? JsonException ->
        InvalidResponse json

/// The session for a set of tokens: the user comes from the ID token, or from the access token
/// when the provider sends no ID token.
let sessionOf (tokens: Tokens) =
    tokens.IdToken
    |> ValueOption.bind Jwt.user
    |> ValueOption.orElseWith (fun () -> Jwt.user tokens.AccessToken)
    |> function
        | ValueSome user -> Ok { User = user; Tokens = tokens }
        | ValueNone -> Error(InvalidResponse "The tokens carry no subject claim.")

let private post (http: HttpClient) (endpoint: Uri) (form: (string * string) list) (cancellation: CancellationToken) =
    task {
        try
            use content = new FormUrlEncodedContent(form |> List.map KeyValuePair)
            use! response = http.PostAsync(endpoint, content, cancellation)
            let! body = response.Content.ReadAsStringAsync cancellation
            return Ok(response.StatusCode, body)
        with
        | :? HttpRequestException as e -> return Error(Unavailable e.Message)
        | :? TaskCanceledException when not cancellation.IsCancellationRequested ->
            return Error(Unavailable "The request timed out.")
    }

let private requestTokens http (settings: OidcSettings) previous form cancellation =
    task {
        match! post http settings.TokenEndpoint form cancellation with
        | Error error -> return Error error
        | Ok(status, body) when int status >= 200 && int status < 300 ->
            return readTokens DateTimeOffset.UtcNow previous body |> Result.bind sessionOf
        // The provider is failing or throttling, which says nothing about the tokens: try again later
        | Ok(status, _) when int status >= 500 || status = HttpStatusCode.TooManyRequests ->
            return Error(Unavailable $"The token endpoint answered {int status}.")
        | Ok(_, body) -> return Error(readError body)
    }

let provider (http: HttpClient) (browser: SignInBrowser) (settings: OidcSettings) : AuthProvider =
    {
        SignIn =
            fun cancellation ->
                task {
                    let verifier = Pkce.randomString ()
                    let state = Pkce.randomString ()
                    let authorize = authorizeUri settings state (Pkce.challenge verifier)

                    match! browser authorize settings.RedirectUri cancellation with
                    | Error error -> return Error error
                    | Ok redirect ->
                        match Map.tryFind "error" redirect, Map.tryFind "code" redirect with
                        | Some error, _ ->
                            let description =
                                redirect |> Map.tryFind "error_description" |> Option.defaultValue ""

                            return Error(Rejected(error, description))
                        | None, _ when Map.tryFind "state" redirect <> Some state ->
                            return Error(InvalidResponse "The redirect's state does not match the request.")
                        | None, None -> return Error(InvalidResponse "The redirect carries no authorization code.")
                        | None, Some code ->
                            return!
                                requestTokens
                                    http
                                    settings
                                    ValueNone
                                    [
                                        "grant_type", "authorization_code"
                                        "client_id", settings.ClientId
                                        "code", code
                                        "redirect_uri", settings.RedirectUri.OriginalString
                                        "code_verifier", verifier
                                    ]
                                    cancellation
                }
        Refresh =
            fun session cancellation ->
                task {
                    match session.Tokens.RefreshToken with
                    | ValueNone -> return Error SessionExpired
                    | ValueSome refreshToken ->
                        let! result =
                            requestTokens
                                http
                                settings
                                (ValueSome session.Tokens)
                                [
                                    "grant_type", "refresh_token"
                                    "client_id", settings.ClientId
                                    "refresh_token", refreshToken
                                ]
                                cancellation

                        return
                            result
                            |> Result.mapError (function
                                | Rejected("invalid_grant", _) -> SessionExpired
                                | error -> error)
                }
        SignOut =
            fun session cancellation ->
                task {
                    match settings.RevokeEndpoint, session.Tokens.RefreshToken with
                    | ValueSome endpoint, ValueSome refreshToken ->
                        let! _ =
                            post http endpoint [ "token", refreshToken; "client_id", settings.ClientId ] cancellation

                        ()
                    | _ -> ()

                    match settings.EndSessionUri with
                    | ValueSome endSession ->
                        // Best effort: the local session ends whether or not the page completes
                        try
                            let! _ = browser endSession settings.RedirectUri cancellation
                            ()
                        with _ ->
                            ()
                    | ValueNone -> ()
                }
    }

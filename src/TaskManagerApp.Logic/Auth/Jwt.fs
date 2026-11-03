/// Reads JWT claims. Tokens come straight from the token endpoint over TLS, so the client does not
/// verify signatures; the backend does that on every request.
module TaskManagerApp.Auth.Jwt

open System
open System.Text
open System.Text.Json

let private decodeSegment (segment: string) =
    let padded =
        match segment.Length % 4 with
        | 2 -> segment + "=="
        | 3 -> segment + "="
        | _ -> segment

    padded.Replace('-', '+').Replace('_', '/')
    |> Convert.FromBase64String
    |> Encoding.UTF8.GetString

/// The payload of a JWT, or ValueNone when the token is not a JWT.
let payload (token: string) =
    match token.Split '.' with
    | [| _; body; _ |] ->
        try
            use document = JsonDocument.Parse(decodeSegment body)
            ValueSome(document.RootElement.Clone())
        with
        | :? FormatException
        | :? JsonException -> ValueNone
    | _ -> ValueNone

let private stringClaim (claims: JsonElement) (name: string) =
    match claims.TryGetProperty name with
    | true, value when value.ValueKind = JsonValueKind.String -> ValueSome(value.GetString())
    | _ -> ValueNone

let private listClaim (claims: JsonElement) (name: string) =
    match claims.TryGetProperty name with
    | true, value when value.ValueKind = JsonValueKind.Array ->
        [
            for item in value.EnumerateArray() do
                if item.ValueKind = JsonValueKind.String then
                    item.GetString()
        ]
    | true, value when value.ValueKind = JsonValueKind.String -> [ value.GetString() ]
    | _ -> []

/// The user described by a token's claims. Cognito puts groups in `cognito:groups`;
/// Entra ID, Keycloak and others use `groups` or `roles`.
let user (token: string) =
    payload token
    |> ValueOption.bind (fun claims ->
        stringClaim claims "sub"
        |> ValueOption.map (fun subject ->
            {
                Subject = subject
                Email = stringClaim claims "email"
                Name =
                    stringClaim claims "name"
                    |> ValueOption.orElse (stringClaim claims "cognito:username")
                Groups =
                    [ "cognito:groups"; "groups"; "roles" ]
                    |> List.collect (listClaim claims)
                    |> List.distinct
            }))

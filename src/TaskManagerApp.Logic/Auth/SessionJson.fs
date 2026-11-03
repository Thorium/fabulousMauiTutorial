/// Session (de)serialization for SessionStore implementations. Written by hand rather than with
/// reflection-based JsonSerializer, so it keeps working in trimmed iOS and Android builds.
module TaskManagerApp.Auth.SessionJson

open System
open System.IO
open System.Text
open System.Text.Json

let write (session: Session) =
    use stream = new MemoryStream()

    do
        use writer = new Utf8JsonWriter(stream)

        let optional (name: string) value =
            value |> ValueOption.iter (fun (v: string) -> writer.WriteString(name, v))

        writer.WriteStartObject()
        writer.WriteString("sub", session.User.Subject)
        optional "email" session.User.Email
        optional "name" session.User.Name
        writer.WriteStartArray "groups"
        session.User.Groups |> List.iter writer.WriteStringValue
        writer.WriteEndArray()
        writer.WriteString("access_token", session.Tokens.AccessToken)
        optional "id_token" session.Tokens.IdToken
        optional "refresh_token" session.Tokens.RefreshToken
        writer.WriteNumber("expires_at", session.Tokens.ExpiresAt.ToUnixTimeSeconds())
        writer.WriteEndObject()

    Encoding.UTF8.GetString(stream.ToArray())

/// The session in a JSON document, or ValueNone when the document is not one `write` produced.
let read (json: string) =
    try
        use document = JsonDocument.Parse json
        let root = document.RootElement

        let optional (name: string) =
            match root.TryGetProperty name with
            | true, value when value.ValueKind = JsonValueKind.String -> ValueSome(value.GetString())
            | _ -> ValueNone

        ValueSome
            {
                User =
                    {
                        Subject = root.GetProperty("sub").GetString()
                        Email = optional "email"
                        Name = optional "name"
                        Groups =
                            [
                                for group in root.GetProperty("groups").EnumerateArray() -> group.GetString()
                            ]
                    }
                Tokens =
                    {
                        AccessToken = root.GetProperty("access_token").GetString()
                        IdToken = optional "id_token"
                        RefreshToken = optional "refresh_token"
                        ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(root.GetProperty("expires_at").GetInt64())
                    }
            }
    with
    | :? JsonException
    | :? InvalidOperationException
    | :? Collections.Generic.KeyNotFoundException -> ValueNone

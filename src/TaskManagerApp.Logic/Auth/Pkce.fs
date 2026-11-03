/// Proof Key for Code Exchange (RFC 7636).
module TaskManagerApp.Auth.Pkce

open System
open System.Security.Cryptography
open System.Text

let base64Url (bytes: byte array) =
    Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')

/// A random URL-safe string with 256 bits of entropy, used for the code verifier, `state` and `nonce`.
let randomString () =
    RandomNumberGenerator.GetBytes 32 |> base64Url

/// The S256 code challenge for a verifier.
let challenge (verifier: string) =
    Encoding.ASCII.GetBytes verifier |> SHA256.HashData |> base64Url

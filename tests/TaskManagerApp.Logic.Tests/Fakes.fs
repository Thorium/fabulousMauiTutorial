module Fakes

open System
open System.Net
open System.Net.Http
open System.Text
open System.Threading.Tasks
open TaskManagerApp.Auth

/// A JWT with the given claims JSON and a dummy signature.
let jwt (claims: string) =
    let encode (text: string) =
        Encoding.UTF8.GetBytes text |> Pkce.base64Url

    let header = encode """{"alg":"RS256","typ":"JWT"}"""
    $"{header}.{encode claims}.signature"

/// Answers every request with `respond`, recording the posted forms.
type TokenEndpoint(respond: Map<string, string> -> HttpStatusCode * string) =
    inherit HttpMessageHandler()

    member val Forms = ResizeArray<Map<string, string>>()

    override this.SendAsync(request, cancellation) =
        task {
            let! body = request.Content.ReadAsStringAsync cancellation

            let form =
                body.Split('&', StringSplitOptions.RemoveEmptyEntries)
                |> Array.map (fun pair ->
                    let parts = pair.Split '='

                    let decode (value: string) =
                        Uri.UnescapeDataString(value.Replace('+', ' '))

                    decode parts[0], decode parts[1])
                |> Map.ofArray

            this.Forms.Add form
            let status, json = respond form
            return new HttpResponseMessage(status, Content = new StringContent(json))
        }

let tokenAnswer subject =
    let idToken =
        jwt $"""{{"sub":"{subject}","email":"{subject}@example.com","cognito:groups":["ops"]}}"""

    $"""{{"access_token":"access-{subject}","id_token":"{idToken}","refresh_token":"refresh-{subject}","expires_in":3600}}"""

/// A browser that answers the authorize request with the given redirect parameters;
/// `echoState` copies the request's state into the redirect as a real provider does.
let browser (echoState: bool) (parameters: (string * string) list) : SignInBrowser * ResizeArray<Uri> =
    let requests = ResizeArray<Uri>()

    let open' (authorize: Uri) (_: Uri) _ =
        requests.Add authorize

        let state =
            authorize.Query.TrimStart('?').Split '&'
            |> Array.map (fun pair -> pair.Split '=')
            |> Array.tryFind (fun pair -> pair[0] = "state")
            |> Option.map (fun pair -> Uri.UnescapeDataString pair[1])

        let redirect =
            match state with
            | Some state when echoState -> ("state", state) :: parameters
            | _ -> parameters

        Task.FromResult(Ok(Map.ofList redirect))

    open', requests

/// An in-memory SessionStore.
type MemoryStore(initial: Session voption) =
    member val Stored = initial with get, set

    member this.Store: SessionStore =
        {
            Load = fun () -> Task.FromResult this.Stored
            Save = fun session -> task { this.Stored <- ValueSome session }
            Clear = fun () -> task { this.Stored <- ValueNone }
        }

let session (expiresAt: DateTimeOffset) =
    {
        User =
            {
                Subject = "user-1"
                Email = ValueNone
                Name = ValueNone
                Groups = []
            }
        Tokens =
            {
                AccessToken = "access-old"
                IdToken = ValueNone
                RefreshToken = ValueSome "refresh-old"
                ExpiresAt = expiresAt
            }
    }

/// A provider whose Refresh returns `refresh`'s answer and counts calls.
type ScriptedProvider(refresh: unit -> Result<Session, AuthError>) =
    member val Refreshes = 0 with get, set

    member this.Provider: AuthProvider =
        {
            SignIn = fun _ -> Task.FromResult(Error Cancelled)
            Refresh =
                fun _ _ ->
                    task {
                        this.Refreshes <- this.Refreshes + 1
                        // Yield so concurrent callers overlap the refresh
                        do! Task.Delay 20
                        return refresh ()
                    }
            SignOut = fun _ _ -> Task.FromResult()
        }

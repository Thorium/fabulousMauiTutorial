namespace TaskManagerApp.Http

open System
open System.Net
open System.Net.Http
open System.Net.Http.Headers
open System.Threading.Tasks
open TaskManagerApp.Auth

/// Adds the signed-in user's access token to every request. Without a valid session the request
/// is not sent and the caller gets 401 Unauthorized, the same answer an expired token would get
/// from the backend, so callers handle both in one place.
type BearerTokenHandler(session: SessionManager, inner: HttpMessageHandler) =
    inherit DelegatingHandler(inner)

    member private _.SendInner(request, cancellation) = base.SendAsync(request, cancellation)

    override this.SendAsync(request, cancellation) =
        task {
            match! session.AccessToken cancellation with
            | Ok token ->
                request.Headers.Authorization <- AuthenticationHeaderValue("Bearer", token)
                return! this.SendInner(request, cancellation)
            | Error _ -> return new HttpResponseMessage(HttpStatusCode.Unauthorized, RequestMessage = request)
        }

module ApiClient =

    /// Doesn't follow redirects: a request's body, such as a refresh token, would go to wherever one points.
    let private platformHandler () =
        new HttpClientHandler(AllowAutoRedirect = false)

    /// An HttpClient for the identity provider's token endpoint.
    let createForSignIn () = new HttpClient(platformHandler ())

    /// An HttpClient for your backend that sends the access token when a session manager is given.
    let create (baseAddress: Uri) (session: SessionManager voption) =
        let platform = platformHandler ()

        let handler: HttpMessageHandler =
            match session with
            | ValueSome manager -> new BearerTokenHandler(manager, platform)
            | ValueNone -> platform

        new HttpClient(handler, BaseAddress = baseAddress)

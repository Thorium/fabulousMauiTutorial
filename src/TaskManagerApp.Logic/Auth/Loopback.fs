/// The sign-in redirect for desktop apps (RFC 8252 section 7.3): the app listens on a loopback
/// port and the provider redirects the system browser there.
module TaskManagerApp.Auth.Loopback

open System
open System.Net
open System.Text
open System.Threading
open System.Threading.Tasks

let private respond (context: HttpListenerContext) (status: int) (text: string) =
    let body =
        Encoding.UTF8.GetBytes $"<!doctype html><title>Signed in</title><p>{text}</p>"

    context.Response.StatusCode <- status
    context.Response.ContentType <- "text/html; charset=utf-8"
    context.Response.OutputStream.Write(body, 0, body.Length)
    context.Response.Close()

/// Answers requests until the redirect arrives (browsers may ask for other paths, such as a favicon).
/// The query is read before the response is sent: closing the response disposes the request.
let rec private receive (listener: HttpListener) (redirect: Uri) (message: string) : Task<Map<string, string>> =
    task {
        let! context = listener.GetContextAsync()

        if context.Request.Url.AbsolutePath.TrimEnd('/') = redirect.AbsolutePath.TrimEnd('/') then
            let query = context.Request.QueryString

            let parameters =
                [
                    for name in query.AllKeys do
                        if not (isNull name) then
                            name, query[name]
                ]
                |> Map.ofList

            respond context 200 message
            return parameters
        else
            respond context 404 "Not found."
            return! receive listener redirect message
    }

/// A SignInBrowser that opens the page with `launch` and waits on the redirect URI, which must be
/// an `http://localhost:<port>/<path>/` address. An abandoned sign-in ends after `timeout`.
let browser (launch: Uri -> Task) (timeout: TimeSpan) (message: string) : SignInBrowser =
    fun page redirect cancellation ->
        task {
            use limit = CancellationTokenSource.CreateLinkedTokenSource cancellation
            limit.CancelAfter timeout

            try
                use listener = new HttpListener()
                listener.Prefixes.Add(string redirect)
                listener.Start()
                use _ = limit.Token.Register(fun () -> listener.Abort())
                do! launch page
                let! parameters = receive listener redirect message
                return Ok parameters
            with
            | _ when limit.IsCancellationRequested -> return Error Cancelled
            | :? HttpListenerException as e -> return Error(Unavailable e.Message)
        }

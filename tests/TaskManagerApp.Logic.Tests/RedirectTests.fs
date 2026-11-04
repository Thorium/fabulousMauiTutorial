/// Tokens stay on the identity provider's and backend's hosts.
module RedirectTests

open System
open System.Net
open System.Net.Http
open System.Net.Sockets
open System.Text
open System.Threading.Tasks
open Xunit
open TaskManagerApp.Http

/// A local host that answers one request.
type private Host(answer: string) =
    let listener = new TcpListener(IPAddress.Loopback, 0)
    let mutable called = false
    do listener.Start()

    let served =
        task {
            try
                use! socket = listener.AcceptTcpClientAsync()
                called <- true
                use stream = socket.GetStream()
                let buffer = Array.zeroCreate 16384
                let! _ = stream.ReadAsync(buffer, 0, buffer.Length)
                let bytes = Encoding.ASCII.GetBytes answer
                do! stream.WriteAsync(bytes, 0, bytes.Length)
            with
            | :? SocketException
            | :? ObjectDisposedException -> ()
        }

    member _.Port = (listener.LocalEndpoint :?> IPEndPoint).Port
    member _.Called = called
    member _.Served: Task = served

    interface IDisposable with
        member _.Dispose() = listener.Stop()

let private okAnswer = "HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\n{}"

let private redirectTo (port: int) =
    $"HTTP/1.1 307 Moved\r\nLocation: http://127.0.0.1:{port}/stolen\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"

let private stopsAtRedirect (http: HttpClient) (other: Host) (first: Host) : Task =
    task {
        use content = new StringContent "refresh_token=the-token"
        use! response = http.PostAsync($"http://127.0.0.1:{first.Port}/token", content)
        Assert.Equal(HttpStatusCode.TemporaryRedirect, response.StatusCode)
        do! first.Served
        Assert.False other.Called
    }

[<Fact>]
let ``the sign-in client stops at a redirect`` () : Task =
    task {
        use other = new Host(okAnswer)
        use first = new Host(redirectTo other.Port)
        use http = ApiClient.createForSignIn ()
        do! stopsAtRedirect http other first
    }

[<Fact>]
let ``the backend client stops at a redirect`` () : Task =
    task {
        use other = new Host(okAnswer)
        use first = new Host(redirectTo other.Port)
        use http = ApiClient.create (Uri "http://127.0.0.1/") ValueNone
        do! stopsAtRedirect http other first
    }

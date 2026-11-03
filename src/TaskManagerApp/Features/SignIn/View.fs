namespace TaskManagerApp.Features.SignIn

open Fabulous.Maui
open Microsoft.Maui.Controls
open Microsoft.Maui.Graphics
open TaskManagerApp.Auth
open TaskManagerApp.Root

open type Fabulous.Maui.View

/// The page shown while there is no session. It dispatches root messages directly.
module View =

    let view (auth: AuthState) =
        ContentPage(
            (VStack(spacing = 16.) {
                Label("Task Manager").font(size = 28., attributes = FontAttributes.Bold).centerTextHorizontal()

                match auth with
                | SignedOut error ->
                    Label("Sign in to see your tasks.").textColor(Colors.Gray).centerTextHorizontal()

                    match error with
                    | ValueSome error -> Label(AuthError.describe error).textColor(Colors.Red).centerTextHorizontal()
                    | ValueNone -> ()

                    Button("Sign in", SignIn).background(SolidColorBrush(Colors.Blue)).textColor Colors.White

                | SigningIn ->
                    ActivityIndicator(true).color Colors.Blue

                    Label("Complete the sign-in in your browser.").textColor(Colors.Gray).centerTextHorizontal()

                    Button("Cancel", CancelSignIn)

                | Restoring -> ActivityIndicator(true).color Colors.Blue

                | NotRequired
                | SignedIn _ -> ()
            })
                .centerVertical()
                .padding 32.
        )
            .title "Sign in"

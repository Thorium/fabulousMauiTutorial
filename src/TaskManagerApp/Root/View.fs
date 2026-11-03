namespace TaskManagerApp.Root

open System.Net.Http
open Fabulous
open Fabulous.Maui
open TaskManagerApp
open TaskManagerApp.Auth
open TaskManagerApp.Features

open type Fabulous.Maui.View


module View =

    /// Renders the appropriate page based on current state
    let private renderPage model =
        match model.CurrentPage with
        | TaskListPage ->
            let user =
                match model.Auth with
                | SignedIn user -> ValueSome user
                | NotRequired
                | Restoring
                | SignedOut _
                | SigningIn -> ValueNone

            View.map TaskListMsg (TaskList.View.view user model.TaskListModel)

        | TaskDetailPage _ ->
            match model.TaskDetailModel with
            | Some taskDetailModel -> View.map TaskDetailMsg (TaskDetail.View.view taskDetailModel)
            | None -> ContentPage(Label("Error: Task detail not loaded"))

    /// Root application program and view
    let app =
        let session =
            SessionManager.create (new HttpClient()) SystemBrowser.authenticate SecureSessionStore.store Config.auth

        let runCommand = Commands.root session

        let init' () =
            let model, cmdMsgs = State.init session.IsSome
            model, cmdMsgs |> List.map runCommand |> Cmd.batch

        let update' msg model =
            let model', cmdMsgs, navMsgOpt = State.update msg model

            let navCmd =
                match navMsgOpt with
                | Some navMsg -> Cmd.ofMsg navMsg
                | None -> Cmd.none

            model', (cmdMsgs |> List.map runCommand) @ [ navCmd ] |> Cmd.batch

        let view' model =
            // An Application contains one or more Windows
            Application() {
                // Without a session the window holds only the sign-in page, so signing out replaces the
                // whole navigation stack instead of editing the visible page in place
                if AuthState.isOpen model.Auth then
                    Window(
                        (NavigationPage() { renderPage model })
                            .barBackgroundColor(Microsoft.Maui.Graphics.Colors.Blue)
                            .barTextColor
                            Microsoft.Maui.Graphics.Colors.White
                    )
                else
                    Window(SignIn.View.view model.Auth)
            }

        // The view is attached separately from the MVU core
        Program.statefulWithCmd init' update' |> Program.withView view'

namespace TaskManagerApp.Root

open Fabulous
open Fabulous.Maui
open TaskManagerApp.Features

open type Fabulous.Maui.View


module View =
    
    /// Renders the appropriate page based on current state
    let private renderPage model =
        match model.CurrentPage with
        | TaskListPage ->
            View.map TaskListMsg (TaskList.View.view model.TaskListModel)
        
        | TaskDetailPage _ ->
            match model.TaskDetailModel with
            | Some taskDetailModel ->
                View.map TaskDetailMsg (TaskDetail.View.view taskDetailModel)
            | None ->
                ContentPage(Label("Error: Task detail not loaded"))
    
    /// Root application program and view
    let app =
        let init' () =
            let model, cmdMsgs = State.init()
            let navCmd =
                match model.CurrentPage with
                | TaskListPage -> Cmd.none
                | _ -> Cmd.none
            model, (cmdMsgs |> List.map State.mapCmdMsg) @ [ navCmd ] |> List.head
        
        let update' msg model =
            let model', cmdMsgs, navMsgOpt = State.update msg model
            
            let navCmd =
                match navMsgOpt with
                | Some navMsg -> Cmd.ofMsg navMsg
                | None -> Cmd.none
            
            model', (cmdMsgs |> List.map State.mapCmdMsg) @ [ navCmd ] |> List.head
        
        let view' model =
            let navPage =
                (NavigationPage() {
                    renderPage model
                })
                    .barBackgroundColor(Microsoft.Maui.Graphics.Colors.Blue)
                    .barTextColor(Microsoft.Maui.Graphics.Colors.White)
            
            Application(navPage)
        
        Program.statefulWithCmd init' update' view'

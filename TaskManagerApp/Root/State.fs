namespace TaskManagerApp.Root

open Fabulous
open TaskManagerApp.Features
open TaskManagerApp

module State =
    
    /// Initialize the root application state
    let init () =
        // Initialize the mock data store
        MockDataStore.initialize()
        
        let taskListModel, taskListCmds = TaskList.State.init()
        
        {
            CurrentPage = TaskListPage
            NavigationStack = []
            TaskListModel = taskListModel
            TaskDetailModel = None
        }, (taskListCmds |> List.map TaskListCmdMsg)
    
    /// Update the root application state
    let update msg model =
        match msg with
        | TaskListMsg tlMsg ->
            let taskListModel, cmdMsgs, navMsgOpt = TaskList.State.update tlMsg model.TaskListModel
            
            let model' = { model with TaskListModel = taskListModel }
            let cmds = cmdMsgs |> List.map TaskListCmdMsg
            
            match navMsgOpt with
            | Some (TaskList.NavigateToTaskDetail taskId) ->
                model', cmds, Some (NavigateTo (TaskDetailPage (Some taskId)))
            | Some TaskList.NavigateToNewTask ->
                model', cmds, Some (NavigateTo (TaskDetailPage None))
            | None ->
                model', cmds, None
        
        | TaskDetailMsg tdMsg ->
            match model.TaskDetailModel with
            | Some taskDetailModel ->
                let taskDetailModel', cmdMsgs, navMsgOpt = TaskDetail.State.update tdMsg taskDetailModel
                
                let model' = { model with TaskDetailModel = Some taskDetailModel' }
                let cmds = cmdMsgs |> List.map TaskDetailCmdMsg
                
                match navMsgOpt with
                | Some TaskDetail.NavigateBack ->
                    model', cmds, Some NavigateBack
                | None ->
                    model', cmds, None
            | None ->
                model, [], None
        
        | NavigateTo page ->
            match page with
            | TaskListPage ->
                { model with 
                    CurrentPage = TaskListPage
                    TaskDetailModel = None
                }, [], None
            
            | TaskDetailPage taskIdOpt ->
                let taskDetailModel, cmdMsgs = TaskDetail.State.init taskIdOpt
                {
                    model with
                        CurrentPage = TaskDetailPage taskIdOpt
                        NavigationStack = model.CurrentPage :: model.NavigationStack
                        TaskDetailModel = Some taskDetailModel
                }, (cmdMsgs |> List.map TaskDetailCmdMsg), None
        
        | NavigateBack ->
            match model.NavigationStack with
            | [] ->
                model, [], None
            | prevPage :: rest ->
                let model' = {
                    model with
                        CurrentPage = prevPage
                        NavigationStack = rest
                        TaskDetailModel = None
                }
                
                // Refresh task list when returning from task detail
                match prevPage with
                | TaskListPage ->
                    let taskListModel, cmdMsgs, _ = 
                        TaskList.State.update TaskList.RefreshTasks model'.TaskListModel
                    { model' with TaskListModel = taskListModel }, 
                    (cmdMsgs |> List.map TaskListCmdMsg), 
                    None
                | _ ->
                    model', [], None
    
    /// Map command messages to Fabulous commands
    let mapCmdMsg cmdMsg =
        match cmdMsg with
        | TaskListCmdMsg tlCmd ->
            TaskList.State.mapCmdMsg tlCmd |> Cmd.map TaskListMsg
        | TaskDetailCmdMsg tdCmd ->
            TaskDetail.State.mapCmdMsg tdCmd |> Cmd.map TaskDetailMsg

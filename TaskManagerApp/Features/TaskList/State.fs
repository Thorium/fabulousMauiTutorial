namespace TaskManagerApp.Features.TaskList

open Fabulous
open TaskManagerApp

module State =
    
    /// Initialize the task list state
    let init () =
        {
            Tasks = []
            Filter = All
            IsLoading = true
        }, [ LoadTasks ]
    
    /// Update the task list state based on messages
    let update msg model =
        match msg with
        | TasksLoaded tasks ->
            { model with Tasks = tasks; IsLoading = false }, [], None
        
        | FilterChanged filter ->
            { model with Filter = filter }, [], None
        
        | TaskClicked taskId ->
            model, [], Some (NavigateToTaskDetail taskId)
        
        | ToggleTaskCompletion taskId ->
            model, [ ToggleCompletion taskId ], None
        
        | DeleteTask taskId ->
            model, [ DeleteTaskCmd taskId ], None
        
        | AddNewTask ->
            model, [], Some NavigateToNewTask
        
        | RefreshTasks ->
            { model with IsLoading = true }, [ LoadTasks ], None
        
        | TaskUpdated taskOpt ->
            match taskOpt with
            | Some _ -> { model with IsLoading = true }, [ LoadTasks ], None
            | None -> model, [], None
    
    /// Map command messages to Fabulous commands
    let mapCmdMsg cmdMsg =
        match cmdMsg with
        | LoadTasks ->
            Cmd.ofAsyncMsg (async {
                let! tasks = TaskApi.loadTasks()
                return TasksLoaded tasks
            })
        
        | ToggleCompletion taskId ->
            Cmd.ofAsyncMsg (async {
                let! result = TaskApi.toggleTaskCompletion taskId
                return TaskUpdated result
            })
        
        | DeleteTaskCmd taskId ->
            Cmd.ofAsyncMsg (async {
                let! _ = TaskApi.deleteTask taskId
                let! tasks = TaskApi.loadTasks()
                return TasksLoaded tasks
            })
    
    /// Get filtered tasks based on current filter
    let getFilteredTasks model =
        match model.Filter with
        | All -> model.Tasks
        | Active -> model.Tasks |> List.filter (fun t -> not t.IsCompleted)
        | Completed -> model.Tasks |> List.filter (fun t -> t.IsCompleted)

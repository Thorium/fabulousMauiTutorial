namespace TaskManagerApp.Features.TaskList

open TaskManagerApp

module State =

    /// Initialize the task list state
    let init () =
        {
            Tasks = []
            Filter = All
            IsLoading = true
        },
        [ LoadTasks ]

    /// Update the task list state based on messages
    let update msg model =
        match msg with
        | TasksLoaded tasks ->
            { model with
                Tasks = tasks
                IsLoading = false
            },
            [],
            None

        | FilterChanged filter -> { model with Filter = filter }, [], None

        | TaskClicked taskId -> model, [], Some(NavigateToTaskDetail taskId)

        | ToggleTaskCompletion taskId -> { model with IsLoading = true }, [ ToggleCompletion taskId ], None

        | DeleteTask taskId -> { model with IsLoading = true }, [ DeleteTaskCmd taskId ], None

        | AddNewTask -> model, [], Some NavigateToNewTask

        | RefreshTasks -> { model with IsLoading = true }, [ LoadTasks ], None

        | SignOut -> model, [], Some SignOutRequested

        | TaskUpdated taskOpt ->
            match taskOpt with
            | Some _ -> { model with IsLoading = true }, [ LoadTasks ], None
            | None -> model, [], None

    /// Get filtered tasks based on current filter
    let getFilteredTasks model =
        match model.Filter with
        | All -> model.Tasks
        | Active -> model.Tasks |> List.filter (fun t -> not t.IsCompleted)
        | Completed -> model.Tasks |> List.filter (fun t -> t.IsCompleted)

    /// Share of completed tasks, 0 to 100; 0 for an empty list
    let completionPercent (tasks: MTask list) =
        match tasks with
        | [] -> 0.
        | _ ->
            100. * float (tasks |> List.filter _.IsCompleted |> List.length)
            / float tasks.Length

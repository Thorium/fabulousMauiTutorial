namespace TaskManagerApp.Features.TaskDetail

open TaskManagerApp

module State =

    /// Initialize the task detail state for a new or existing task
    let init taskId =
        {
            TaskId = taskId
            Title = ""
            Description = ""
            Priority = float AppSettings.DefaultPriorityValue
            IsLoading = taskId.IsSome
            IsSaving = false
            OriginalTask = None
        },
        [ LoadTask taskId ]

    /// Update the task detail state based on messages
    let update msg model =
        match msg with
        | TaskLoaded taskOpt ->
            match taskOpt with
            | Some task ->
                { model with
                    Title = task.Title
                    Description = task.Description
                    Priority = float (Priority.toInt task.Priority)
                    IsLoading = false
                    OriginalTask = Some task
                },
                [],
                None
            | None -> { model with IsLoading = false }, [], None

        | TitleChanged text ->
            let trimmedText =
                if text.Length > AppSettings.MaxTaskTitleLength then
                    text.Substring(0, AppSettings.MaxTaskTitleLength)
                else
                    text

            { model with Title = trimmedText }, [], None

        | DescriptionChanged text ->
            let trimmedText =
                if text.Length > AppSettings.MaxTaskDescriptionLength then
                    text.Substring(0, AppSettings.MaxTaskDescriptionLength)
                else
                    text

            { model with Description = trimmedText }, [], None

        | PriorityChanged value -> { model with Priority = value }, [], None

        | SaveTask ->
            if System.String.IsNullOrWhiteSpace(model.Title) then
                model, [], None
            else
                let priority = Priority.fromInt (int model.Priority)

                let task, isUpdate =
                    match model.TaskId, model.OriginalTask with
                    | Some taskId, Some original ->
                        // Update existing task, preserving completion status and creation date
                        {
                            Id = taskId
                            Title = model.Title.Trim()
                            Description = model.Description.Trim()
                            Priority = priority
                            IsCompleted = original.IsCompleted
                            CreatedAt = original.CreatedAt
                        },
                        true
                    | _ ->
                        // Create new task
                        Task.createDetailed (model.Title.Trim()) (model.Description.Trim()) priority, false

                { model with IsSaving = true }, [ SaveTaskCmd(task, isUpdate) ], None

        | TaskSaved taskOpt ->
            match taskOpt with
            | Some _ -> model, [], Some NavigateBack
            | None -> { model with IsSaving = false }, [], None

        | GoBack -> model, [], Some NavigateBack

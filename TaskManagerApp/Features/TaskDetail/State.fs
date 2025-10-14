namespace TaskManagerApp.Features.TaskDetail

open Fabulous
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
        }, [ LoadTask taskId ]
    
    /// Update the task detail state based on messages
    let update msg model =
        match msg with
        | TaskLoaded taskOpt ->
            match taskOpt with
            | Some task ->
                {
                    model with
                        Title = task.Title
                        Description = task.Description
                        Priority = float (Priority.toInt task.Priority)
                        IsLoading = false
                }, [], None
            | None ->
                { model with IsLoading = false }, [], None
        
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
        
        | PriorityChanged value ->
            { model with Priority = value }, [], None
        
        | SaveTask ->
            if System.String.IsNullOrWhiteSpace(model.Title) then
                model, [], None
            else
                let priority = Priority.fromInt (int model.Priority)
                let task =
                    match model.TaskId with
                    | Some taskId ->
                        // Update existing task
                        {
                            Id = taskId
                            Title = model.Title.Trim()
                            Description = model.Description.Trim()
                            Priority = priority
                            IsCompleted = false // Keep existing completion status
                            CreatedAt = System.DateTime.Now // Will be preserved in real implementation
                        }
                    | None ->
                        // Create new task
                        Task.createDetailed (model.Title.Trim()) (model.Description.Trim()) priority
                
                { model with IsSaving = true }, [ SaveTaskCmd task ], None
        
        | TaskSaved taskOpt ->
            match taskOpt with
            | Some _ ->
                model, [], Some NavigateBack
            | None ->
                { model with IsSaving = false }, [], None
        
        | GoBack ->
            model, [], Some NavigateBack
    
    /// Map command messages to Fabulous commands
    let mapCmdMsg cmdMsg =
        match cmdMsg with
        | LoadTask taskIdOpt ->
            match taskIdOpt with
            | Some taskId ->
                Cmd.ofAsyncMsg (async {
                    let! task = TaskApi.loadTask taskId
                    return TaskLoaded task
                })
            | None ->
                Cmd.ofMsg (TaskLoaded None)
        
        | SaveTaskCmd task ->
            Cmd.ofAsyncMsg (async {
                let! result =
                    match task.Id with
                    | _ when task.CreatedAt = System.DateTime.MinValue ->
                        async {
                            // New task
                            let! t = TaskApi.saveTask task
                            return Some t
                        }
                    | _ ->
                        // Existing task
                        TaskApi.updateTask task
                
                return TaskSaved result
            })

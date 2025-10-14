namespace TaskManagerApp.Features.TaskDetail

open TaskManagerApp

/// Messages for the task detail feature
type Msg =
    | TaskLoaded of MTask option
    | TitleChanged of string
    | DescriptionChanged of string
    | PriorityChanged of float
    | SaveTask
    | TaskSaved of MTask option
    | GoBack

/// Command messages for side effects
type CmdMsg =
    | LoadTask of TaskId option
    | SaveTaskCmd of MTask

/// Model for task detail/edit screen
type Model = {
    TaskId: TaskId option
    Title: string
    Description: string
    Priority: float
    IsLoading: bool
    IsSaving: bool
}

/// Navigation events
type NavigationMsg =
    | NavigateBack

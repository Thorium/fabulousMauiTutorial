namespace TaskManagerApp.Features.TaskList

open TaskManagerApp

/// Messages for the task list feature
type Msg =
    | TasksLoaded of MTask list
    | FilterChanged of TaskFilter
    | TaskClicked of TaskId
    | ToggleTaskCompletion of TaskId
    | DeleteTask of TaskId
    | AddNewTask
    | RefreshTasks
    | TaskUpdated of MTask option
    | SignOut

/// Command messages for side effects
type CmdMsg =
    | LoadTasks
    | ToggleCompletion of TaskId
    | DeleteTaskCmd of TaskId

/// Model for the task list screen
type Model =
    {
        Tasks: MTask list
        Filter: TaskFilter
        /// A task API call is in flight: the first load, a refresh, a toggle or a delete
        IsLoading: bool
    }

/// Messages for the parent: navigation and session requests
type ExternalMsg =
    | NavigateToTaskDetail of TaskId
    | NavigateToNewTask
    | SignOutRequested

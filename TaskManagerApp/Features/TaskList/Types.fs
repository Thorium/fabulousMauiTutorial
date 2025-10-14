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

/// Command messages for side effects
type CmdMsg =
    | LoadTasks
    | ToggleCompletion of TaskId
    | DeleteTaskCmd of TaskId

/// Model for the task list screen
type Model = {
    Tasks: MTask list
    Filter: TaskFilter
    IsLoading: bool
}

/// Navigation events
type NavigationMsg =
    | NavigateToTaskDetail of TaskId
    | NavigateToNewTask

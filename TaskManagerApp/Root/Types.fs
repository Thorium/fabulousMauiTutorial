namespace TaskManagerApp.Root

open TaskManagerApp

/// Application pages
type Page =
    | TaskListPage
    | TaskDetailPage of TaskId option

/// Root application messages
type Msg =
    | TaskListMsg of Features.TaskList.Msg
    | TaskDetailMsg of Features.TaskDetail.Msg
    | NavigateTo of Page
    | NavigateBack

/// Root command messages
type CmdMsg =
    | TaskListCmdMsg of Features.TaskList.CmdMsg
    | TaskDetailCmdMsg of Features.TaskDetail.CmdMsg

/// Root application model
type Model = {
    CurrentPage: Page
    NavigationStack: Page list
    TaskListModel: Features.TaskList.Model
    TaskDetailModel: Features.TaskDetail.Model option
}

namespace TaskManagerApp.Root

open TaskManagerApp
open TaskManagerApp.Auth

/// Application pages
type Page =
    | TaskListPage
    | TaskDetailPage of TaskId option

/// Where the user is in signing in
type AuthState =
    /// The app runs without sign-in (AuthConfig.NoAuth)
    | NotRequired
    /// Reading the stored session at start-up
    | Restoring
    | SignedOut of lastError: AuthError voption
    | SigningIn
    | SignedIn of User

/// Root application messages
type Msg =
    | TaskListMsg of Features.TaskList.Msg
    | TaskDetailMsg of Features.TaskDetail.Msg
    | NavigateTo of Page
    | NavigateBack
    | SessionRestored of Result<Session voption, AuthError>
    | SignIn
    | CancelSignIn
    | SignInCompleted of Result<Session, AuthError>
    | SignOut
    | SignedOutCompleted

/// Root command messages
type CmdMsg =
    | TaskListCmdMsg of Features.TaskList.CmdMsg
    | TaskDetailCmdMsg of Features.TaskDetail.CmdMsg
    | RestoreSession
    | StartSignIn
    | AbortSignIn
    | EndSession

/// Root application model
type Model =
    {
        Auth: AuthState
        CurrentPage: Page
        NavigationStack: Page list
        TaskListModel: Features.TaskList.Model
        TaskDetailModel: Features.TaskDetail.Model option
    }

module AuthState =
    /// Whether the task pages may be shown
    let isOpen state =
        match state with
        | NotRequired
        | SignedIn _ -> true
        | Restoring
        | SignedOut _
        | SigningIn -> false

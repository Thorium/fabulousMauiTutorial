namespace TaskManagerApp.Root

open TaskManagerApp.Features
open TaskManagerApp

module State =

    /// Initialize the root application state. With sign-in required, tasks load once a session exists.
    let init authRequired =
        // Initialize the mock data store
        MockDataStore.initialize ()

        let taskListModel, taskListCmds = TaskList.State.init ()

        let model =
            {
                Auth = if authRequired then Restoring else NotRequired
                CurrentPage = TaskListPage
                NavigationStack = []
                TaskListModel = taskListModel
                TaskDetailModel = None
            }

        if authRequired then
            model, [ RestoreSession ]
        else
            model, (taskListCmds |> List.map TaskListCmdMsg)

    let private openSession (session: Auth.Session) model =
        let taskListModel, cmdMsgs, _ =
            TaskList.State.update TaskList.RefreshTasks model.TaskListModel

        { model with
            Auth = SignedIn session.User
            TaskListModel = taskListModel
        },
        (cmdMsgs |> List.map TaskListCmdMsg),
        None

    let private closeSession error model =
        { model with
            Auth = SignedOut error
            CurrentPage = TaskListPage
            NavigationStack = []
            TaskDetailModel = None
        },
        [],
        None

    /// Update the root application state
    let update msg model =
        match msg with
        | TaskListMsg tlMsg ->
            let taskListModel, cmdMsgs, navMsgOpt =
                TaskList.State.update tlMsg model.TaskListModel

            let model' =
                { model with
                    TaskListModel = taskListModel
                }

            let cmds = cmdMsgs |> List.map TaskListCmdMsg

            match navMsgOpt with
            | Some(TaskList.NavigateToTaskDetail taskId) -> model', cmds, Some(NavigateTo(TaskDetailPage(Some taskId)))
            | Some TaskList.NavigateToNewTask -> model', cmds, Some(NavigateTo(TaskDetailPage None))
            | Some TaskList.SignOutRequested -> model', cmds, Some SignOut
            | None -> model', cmds, None

        | TaskDetailMsg tdMsg ->
            match model.TaskDetailModel with
            | Some taskDetailModel ->
                let taskDetailModel', cmdMsgs, navMsgOpt =
                    TaskDetail.State.update tdMsg taskDetailModel

                let model' =
                    { model with
                        TaskDetailModel = Some taskDetailModel'
                    }

                let cmds = cmdMsgs |> List.map TaskDetailCmdMsg

                match navMsgOpt with
                | Some TaskDetail.NavigateBack -> model', cmds, Some NavigateBack
                | None -> model', cmds, None
            | None -> model, [], None

        | NavigateTo page ->
            match page with
            | TaskListPage ->
                { model with
                    CurrentPage = TaskListPage
                    TaskDetailModel = None
                },
                [],
                None

            | TaskDetailPage taskIdOpt ->
                let taskDetailModel, cmdMsgs = TaskDetail.State.init taskIdOpt

                { model with
                    CurrentPage = TaskDetailPage taskIdOpt
                    NavigationStack = model.CurrentPage :: model.NavigationStack
                    TaskDetailModel = Some taskDetailModel
                },
                (cmdMsgs |> List.map TaskDetailCmdMsg),
                None

        | NavigateBack ->
            match model.NavigationStack with
            | [] -> model, [], None
            | prevPage :: rest ->
                let model' =
                    { model with
                        CurrentPage = prevPage
                        NavigationStack = rest
                        TaskDetailModel = None
                    }

                // Refresh task list when returning from task detail
                match prevPage with
                | TaskListPage ->
                    let taskListModel, cmdMsgs, _ =
                        TaskList.State.update TaskList.RefreshTasks model'.TaskListModel

                    { model' with
                        TaskListModel = taskListModel
                    },
                    (cmdMsgs |> List.map TaskListCmdMsg),
                    None
                | TaskDetailPage _ -> model', [], None

        | SessionRestored(Ok(ValueSome session)) -> openSession session model

        | SessionRestored(Ok ValueNone) -> closeSession ValueNone model

        | SessionRestored(Error error) -> closeSession (ValueSome error) model

        | SignIn ->
            match model.Auth with
            | SignedOut _ -> { model with Auth = SigningIn }, [ StartSignIn ], None
            | NotRequired
            | Restoring
            | SigningIn
            | SignedIn _ -> model, [], None

        | CancelSignIn ->
            match model.Auth with
            | SigningIn -> model, [ AbortSignIn ], None
            | NotRequired
            | Restoring
            | SignedOut _
            | SignedIn _ -> model, [], None

        | SignInCompleted(Ok session) -> openSession session model

        | SignInCompleted(Error Auth.Cancelled) -> closeSession ValueNone model

        | SignInCompleted(Error error) -> closeSession (ValueSome error) model

        | SignOut ->
            match model.Auth with
            | SignedIn _ -> model, [ EndSession ], None
            | NotRequired
            | Restoring
            | SignedOut _
            | SigningIn -> model, [], None

        | SignedOutCompleted -> closeSession ValueNone model

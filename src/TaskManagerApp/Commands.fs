/// Runs the commands that the update functions return as data. All side effects live here,
/// which keeps the update functions in TaskManagerApp.Logic pure and unit-testable.
module TaskManagerApp.Commands

open System.Threading
open Fabulous
open TaskManagerApp.Auth
open TaskManagerApp.Features
open TaskManagerApp.Root

let taskList cmdMsg =
    match cmdMsg with
    | TaskList.LoadTasks ->
        Cmd.OfAsync.msg (
            async {
                let! tasks = TaskApi.loadTasks ()
                return TaskList.TasksLoaded tasks
            }
        )

    | TaskList.ToggleCompletion taskId ->
        Cmd.OfAsync.msg (
            async {
                let! result = TaskApi.toggleTaskCompletion taskId
                return TaskList.TaskUpdated result
            }
        )

    | TaskList.DeleteTaskCmd taskId ->
        Cmd.OfAsync.msg (
            async {
                let! _ = TaskApi.deleteTask taskId
                let! tasks = TaskApi.loadTasks ()
                return TaskList.TasksLoaded tasks
            }
        )

let taskDetail cmdMsg =
    match cmdMsg with
    | TaskDetail.LoadTask taskIdOpt ->
        match taskIdOpt with
        | Some taskId ->
            Cmd.OfAsync.msg (
                async {
                    let! task = TaskApi.loadTask taskId
                    return TaskDetail.TaskLoaded task
                }
            )
        | None -> Cmd.ofMsg (TaskDetail.TaskLoaded None)

    | TaskDetail.SaveTaskCmd(task, isUpdate) ->
        Cmd.OfAsync.msg (
            async {
                let! result =
                    // The update function already decided new-vs-existing from the model,
                    // so the command only talks to the API layer.
                    if isUpdate then
                        TaskApi.updateTask task
                    else
                        async {
                            let! t = TaskApi.saveTask task
                            return Some t
                        }

                return TaskDetail.TaskSaved result
            }
        )

/// The root command runner. `session` is ValueNone when the app runs without sign-in.
let root (session: SessionManager voption) =
    // The sign-in in progress, so the user can cancel it
    let signIn = ref (new CancellationTokenSource())

    let withSession command =
        match session with
        | ValueSome manager -> command manager
        | ValueNone -> Cmd.none

    fun cmdMsg ->
        match cmdMsg with
        | TaskListCmdMsg command -> taskList command |> Cmd.map TaskListMsg
        | TaskDetailCmdMsg command -> taskDetail command |> Cmd.map TaskDetailMsg

        | RestoreSession ->
            withSession (fun manager ->
                Cmd.OfAsync.msg (
                    async {
                        let! cancellation = Async.CancellationToken
                        let! result = manager.Restore cancellation |> Async.AwaitTask
                        return SessionRestored result
                    }
                ))

        | StartSignIn ->
            withSession (fun manager ->
                signIn.Value.Dispose()
                signIn.Value <- new CancellationTokenSource()
                let cancellation = signIn.Value.Token

                Cmd.OfAsync.msg (
                    async {
                        let! result = manager.SignIn cancellation |> Async.AwaitTask
                        return SignInCompleted result
                    }
                ))

        | AbortSignIn ->
            signIn.Value.Cancel()
            Cmd.none

        | EndSession ->
            withSession (fun manager ->
                Cmd.OfAsync.msg (
                    async {
                        do! manager.SignOut CancellationToken.None |> Async.AwaitTask
                        return SignedOutCompleted
                    }
                ))

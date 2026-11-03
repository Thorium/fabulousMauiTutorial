module StateTests

open System
open Xunit
open TaskManagerApp
open TaskManagerApp.Auth
open TaskManagerApp.Features
open TaskManagerApp.Root

let private signedIn =
    { Fakes.session (DateTimeOffset.UtcNow.AddHours 1.) with
        User = DemoAuth.user
    }

[<Fact>]
let ``without sign-in the tasks load straight away`` () =
    let model, commands = State.init false
    Assert.Equal(NotRequired, model.Auth)
    Assert.Equal<CmdMsg list>([ TaskListCmdMsg TaskList.LoadTasks ], commands)

[<Fact>]
let ``with sign-in the stored session is restored first`` () =
    let model, commands = State.init true
    Assert.Equal(Restoring, model.Auth)
    Assert.Equal<CmdMsg list>([ RestoreSession ], commands)
    Assert.False(AuthState.isOpen model.Auth)

[<Fact>]
let ``a restored session opens the task list and loads tasks`` () =
    let model, _ = State.init true

    let model, commands, _ =
        State.update (SessionRestored(Ok(ValueSome signedIn))) model

    Assert.Equal(SignedIn DemoAuth.user, model.Auth)
    Assert.Equal<CmdMsg list>([ TaskListCmdMsg TaskList.LoadTasks ], commands)

[<Fact>]
let ``sign-in starts once and a cancelled one returns to the sign-in page without an error`` () =
    let model, _ = State.init true
    let model, _, _ = State.update (SessionRestored(Ok ValueNone)) model
    let model, commands, _ = State.update SignIn model
    Assert.Equal(SigningIn, model.Auth)
    Assert.Equal<CmdMsg list>([ StartSignIn ], commands)
    let _, commands, _ = State.update SignIn model
    Assert.Empty commands
    let _, commands, _ = State.update CancelSignIn model
    Assert.Equal<CmdMsg list>([ AbortSignIn ], commands)
    let model, _, _ = State.update (SignInCompleted(Error Cancelled)) model
    Assert.Equal(SignedOut ValueNone, model.Auth)

[<Fact>]
let ``a failed sign-in keeps the error for the sign-in page`` () =
    let model, _ = State.init true
    let model, _, _ = State.update (SignInCompleted(Error(Unavailable "offline"))) model
    Assert.Equal(SignedOut(ValueSome(Unavailable "offline")), model.Auth)

[<Fact>]
let ``sign-out from the task list ends the session and resets navigation`` () =
    let model, _ = State.init true
    let model, _, _ = State.update (SessionRestored(Ok(ValueSome signedIn))) model
    let model, _, next = State.update (NavigateTo(TaskDetailPage None)) model
    Assert.Equal(None, next)
    let model, _, _ = State.update NavigateBack model
    let _, _, next = State.update (TaskListMsg TaskList.SignOut) model
    Assert.Equal(Some SignOut, next)
    let model, commands, _ = State.update SignOut model
    Assert.Equal<CmdMsg list>([ EndSession ], commands)
    let model, _, _ = State.update (NavigateTo(TaskDetailPage None)) model
    let model, _, _ = State.update SignedOutCompleted model
    Assert.Equal(SignedOut ValueNone, model.Auth)
    Assert.Equal(TaskListPage, model.CurrentPage)
    Assert.Empty model.NavigationStack
    Assert.Equal(None, model.TaskDetailModel)

[<Fact>]
let ``toggling a task shows the list as busy until it reloads`` () =
    let task = Task.create "Write tests"

    let model, _, _ =
        TaskList.State.update (TaskList.TasksLoaded [ task ]) (fst (TaskList.State.init ()))

    Assert.False model.IsLoading

    let model, commands, _ =
        TaskList.State.update (TaskList.ToggleTaskCompletion task.Id) model

    Assert.True model.IsLoading
    Assert.Equal<TaskList.CmdMsg list>([ TaskList.ToggleCompletion task.Id ], commands)

    let model, _, _ =
        TaskList.State.update (TaskList.TasksLoaded [ Task.toggleCompletion task ]) model

    Assert.False model.IsLoading

[<Fact>]
let ``completion is the share of completed tasks`` () =
    let tasks = [ for title in [ "a"; "b"; "c"; "d" ] -> Task.create title ]
    let done' = (Task.toggleCompletion tasks.Head) :: tasks.Tail
    Assert.Equal(0., TaskList.State.completionPercent [])
    Assert.Equal(0., TaskList.State.completionPercent tasks)
    Assert.Equal(25., TaskList.State.completionPercent done')
    Assert.Equal(100., TaskList.State.completionPercent (tasks |> List.map Task.toggleCompletion))

[<Fact>]
let ``a task title is cut to the maximum length`` () =
    let model, _ = TaskDetail.State.init None

    let model, _, _ =
        TaskDetail.State.update (TaskDetail.TitleChanged(String('x', 150))) model

    Assert.Equal(AppSettings.MaxTaskTitleLength, model.Title.Length)

[<Fact>]
let ``saving a blank title does nothing`` () =
    let model, _ = TaskDetail.State.init None
    let model, _, _ = TaskDetail.State.update (TaskDetail.TitleChanged "   ") model
    let saved, commands, next = TaskDetail.State.update TaskDetail.SaveTask model
    Assert.Equal(model, saved)
    Assert.Empty commands
    Assert.Equal(None, next)

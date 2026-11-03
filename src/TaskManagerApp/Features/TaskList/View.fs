namespace TaskManagerApp.Features.TaskList

open Fabulous
open Fabulous.Maui
open Microsoft.Maui.Controls
open Microsoft.Maui.Graphics
open TaskManagerApp
open TaskManagerApp.Auth
open TaskManagerApp.Controls

open type Fabulous.Maui.View

module View =

    /// Renders a single task item
    let private taskItem (task: MTask) =
        (HStack(spacing = 12.) {
            (VStack(spacing = 4.) {
                Label(task.Title)
                    .font(size = 16., attributes = FontAttributes.Bold)
                    .textColor(if task.IsCompleted then Colors.Gray else Colors.Black)

                Label(task.Description).font(size = 12.).textColor(Colors.Gray).lineBreakMode
                    Microsoft.Maui.LineBreakMode.TailTruncation

                (HStack(spacing = 8.) {
                    Label($"Priority: {Priority.toString task.Priority}")
                        .font(size = 10.)
                        .textColor(
                            match task.Priority with
                            | Priority.High -> Colors.Red
                            | Priority.Medium -> Colors.Orange
                            | Priority.Low -> Colors.Green
                        )

                    Label($"Created: {task.CreatedAt.ToShortDateString()}").font(size = 10.).textColor Colors.Gray
                })
                    .alignStartHorizontal()
            })
                .alignStartVertical()

            CheckBox(task.IsCompleted, fun _ -> ToggleTaskCompletion task.Id).centerVertical()
        })
            .padding(12.)
            .gestureRecognizers() {
            TapGestureRecognizer(TaskClicked task.Id)
        }

    /// Renders the filter buttons
    let private filterButtons model =
        (HStack(spacing = 8.) {
            Button("All", FilterChanged All)
                .background(SolidColorBrush(if model.Filter = All then Colors.Blue else Colors.LightGray))
                .textColor
                Colors.White

            Button("Active", FilterChanged Active)
                .background(
                    SolidColorBrush(
                        if model.Filter = Active then
                            Colors.Blue
                        else
                            Colors.LightGray
                    )
                )
                .textColor
                Colors.White

            Button("Completed", FilterChanged Completed)
                .background(
                    SolidColorBrush(
                        if model.Filter = Completed then
                            Colors.Blue
                        else
                            Colors.LightGray
                    )
                )
                .textColor
                Colors.White
        })
            .padding(16.)
            .centerHorizontal()

    /// Completion summary: the radial slider in read-only mode as a gauge
    let private summary (user: User voption) (tasks: MTask list) =
        let percent = State.completionPercent tasks
        let completed = tasks |> List.filter _.IsCompleted |> List.length

        (HStack(spacing = 16.) {
            (Grid() {
                RadialGauge(percent)
                    .minimum(0.)
                    .maximum(100.)
                    .trackColor(Colors.LightGray)
                    .trackProgressColor Colors.Green

                Label($"{percent:F0}%%").font(size = 16., attributes = FontAttributes.Bold).center()
            })
                .width(96.)
                .height 96.

            (VStack(spacing = 4.) {
                Label("Progress").font(size = 18., attributes = FontAttributes.Bold)

                Label($"{completed} of {tasks.Length} tasks completed").textColor Colors.Gray

                match user with
                | ValueSome user ->
                    Label($"Signed in as {User.displayName user}").font(size = 12.).textColor Colors.Gray
                | ValueNone -> ()
            })
                .centerVertical()
        })
            .padding 16.

    /// Renders the task list. With a signed-in `user` it shows who is signed in and a sign-out item.
    let view (user: User voption) model =
        let filteredTasks = State.getFilteredTasks model
        let firstLoad = model.IsLoading && model.Tasks.IsEmpty

        ContentPage(
            (Grid() {
                VStack() {
                    if not firstLoad then
                        summary user model.Tasks

                    filterButtons model

                    if firstLoad then
                        ContentView(Busy.indicator "Loading tasks...").padding 20.
                    elif filteredTasks.IsEmpty then
                        ContentView(
                            (VStack(spacing = 16.) {
                                Label("No tasks found")
                                    .font(size = 18., attributes = FontAttributes.Bold)
                                    .textColor(Colors.Gray)
                                    .centerTextHorizontal()

                                Label(
                                    match model.Filter with
                                    | All -> "Start by adding your first task!"
                                    | Active -> "All tasks are completed. Great job!"
                                    | Completed -> "No completed tasks yet."
                                )
                                    .font(size = 14.)
                                    .textColor(Colors.Gray)
                                    .centerTextHorizontal()
                            })
                                .centerHorizontal()
                                .centerVertical()
                                .padding
                                20.
                        )
                    else
                        ContentView(
                            ScrollView(
                                (VStack(spacing = 1.) {
                                    for task in filteredTasks do
                                        Border(taskItem task)
                                            .stroke(Colors.LightGray)
                                            .strokeThickness(1.)
                                            .background(SolidColorBrush(Colors.White))
                                })
                            )
                        )
                }

                if model.IsLoading && not firstLoad then
                    Busy.overlay "Updating..."
            })
        )
            .toolbarItems() {
            ToolbarItem("Add", AddNewTask).order ToolbarItemOrder.Primary
            ToolbarItem("Refresh", RefreshTasks).order ToolbarItemOrder.Secondary

            if user.IsSome then
                ToolbarItem("Sign out", SignOut).order ToolbarItemOrder.Secondary
        }

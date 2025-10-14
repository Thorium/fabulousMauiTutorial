# Code Examples and Snippets

This document provides common code patterns and examples from the Task Manager sample application.

## Table of Contents
- [Domain Modeling](#domain-modeling)
- [MVU Pattern](#mvu-pattern)
- [UI Components](#ui-components)
- [Navigation](#navigation)
- [Data Operations](#data-operations)
- [Custom Controls](#custom-controls)

## Domain Modeling

### Strong Type IDs
```fsharp
// Instead of using raw Guid
type TaskId = TaskId of Guid

// Usage
let createTask() =
    { Id = TaskId (Guid.NewGuid()); (* ... *) }

// Pattern matching
match taskId with
| TaskId guid -> printfn "Task ID: %A" guid
```

### Discriminated Unions
```fsharp
// Enumerate all possible values
type Priority = 
    | Low
    | Medium
    | High

type TaskFilter =
    | All
    | Active
    | Completed

// Pattern matching ensures all cases are handled
let getPriorityColor = function
    | Low -> Colors.Green
    | Medium -> Colors.Orange
    | High -> Colors.Red
```

### Record Types
```fsharp
// Immutable by default
type Task = {
    Id: TaskId
    Title: string
    Description: string
    Priority: Priority
    IsCompleted: bool
    CreatedAt: DateTime
}

// Update with 'with' syntax (creates new instance)
let updatedTask = { task with Title = "New Title" }
```

## MVU Pattern

### Message Types
```fsharp
// User actions
type Msg =
    | TitleChanged of string
    | SaveTask
    | DeleteTask of TaskId
    
// Async results
type Msg =
    | TasksLoaded of Task list
    | TaskSaved of Result<Task, string>

// Commands for side effects
type CmdMsg =
    | LoadTasks
    | SaveTask of Task
```

### Init Function
```fsharp
let init taskId =
    // Return initial model and list of commands
    {
        TaskId = taskId
        Title = ""
        IsLoading = true
    }, [ LoadTask taskId ]
```

### Update Function
```fsharp
let update msg model =
    match msg with
    | TitleChanged text ->
        // Simple state update
        { model with Title = text }, [], None
    
    | SaveTask ->
        // Trigger async operation
        { model with IsSaving = true },
        [ SaveTaskCmd model ],
        None
    
    | TaskSaved (Ok task) ->
        // Navigate after success
        model,
        [],
        Some NavigateBack
    
    | TaskSaved (Error err) ->
        // Handle error
        { model with Error = Some err; IsSaving = false },
        [],
        None
```

### Command Mapping
```fsharp
let mapCmdMsg cmdMsg =
    match cmdMsg with
    | LoadTasks ->
        Cmd.ofAsyncMsg (async {
            let! tasks = TaskApi.loadTasks()
            return TasksLoaded tasks
        })
    
    | SaveTaskCmd task ->
        Cmd.ofAsyncMsg (async {
            try
                let! result = TaskApi.saveTask task
                return TaskSaved (Ok result)
            with ex ->
                return TaskSaved (Error ex.Message)
        })
```

## UI Components

### Basic Layout
```fsharp
// Vertical stack
VStack(spacing = 16.) {
    Label("Title")
        .font(size = 18., attributes = FontAttributes.Bold)
    
    Entry(model.Text, TextChanged)
        .placeholder("Enter text...")
    
    Button("Submit", Submit)
        .backgroundColor(Colors.Blue)
}

// Horizontal stack
HStack(spacing = 8.) {
    Label("Status:")
    Label(if model.IsActive then "Active" else "Inactive")
        .textColor(if model.IsActive then Colors.Green else Colors.Red)
}
```

### Conditional Rendering
```fsharp
// Using if-then-else
if model.IsLoading then
    ActivityIndicator(true)
        .color(Colors.Blue)
elif model.Tasks.IsEmpty then
    Label("No tasks found")
        .textColor(Colors.Gray)
else
    ScrollView(
        VStack() {
            for task in model.Tasks do
                taskItem task
        }
    )
```

### Lists and Collections
```fsharp
// Iterate over list
VStack(spacing = 8.) {
    for task in model.Tasks do
        (Border() {
            Label(task.Title)
        })
            .stroke(Colors.Gray)
            .padding(12.)
}

// With filtering
VStack() {
    for task in model.Tasks do
        if not task.IsCompleted then
            taskItem task
}

// With index
VStack() {
    for i, task in List.indexed model.Tasks do
        HStack() {
            Label($"{i + 1}.")
            Label(task.Title)
        }
}
```

### Grid Layout
```fsharp
Grid(
    rowdefs = [Auto; Star; Auto],
    coldefs = [Star; Star]
) {
    // Header spanning both columns
    Label("Header")
        .gridRow(0)
        .gridColumnSpan(2)
    
    // Two columns in middle row
    VStack() { (* content *) }
        .gridRow(1)
        .gridColumn(0)
    
    VStack() { (* content *) }
        .gridRow(1)
        .gridColumn(1)
    
    // Footer spanning both columns
    Button("Action", (* ... *))
        .gridRow(2)
        .gridColumnSpan(2)
}
```

### Styled Components
```fsharp
// Reusable component
let primaryButton text onClicked =
    Button(text, onClicked)
        .backgroundColor(Colors.Blue)
        .textColor(Colors.White)
        .cornerRadius(8.)
        .padding(16., 8.)
        .minWidth(120.)

// Usage
primaryButton "Save" SaveTask

// Card component
let card content =
    (Border() {
        content
    })
        .stroke(Colors.LightGray)
        .strokeThickness(1.)
        .background(Colors.White)
        .cornerRadius(8.)
        .padding(16.)
        .margin(8.)
```

## Navigation

### Page Definition
```fsharp
type Page =
    | ListPage
    | DetailPage of TaskId
    | EditPage of TaskId option  // None for new, Some for edit
```

### Navigation in Update
```fsharp
// Return navigation message as third tuple element
let update msg model =
    match msg with
    | TaskClicked taskId ->
        model,
        [],
        Some (NavigateToDetail taskId)
    
    | SaveComplete ->
        model,
        [],
        Some NavigateBack
```

### Root Navigation Handler
```fsharp
let update msg model =
    match msg with
    | FeatureMsg fMsg ->
        let fModel, cmds, navOpt = Feature.update fMsg model.FeatureModel
        
        match navOpt with
        | Some (NavigateToDetail id) ->
            { model with CurrentPage = DetailPage id },
            cmds,
            None
        | Some NavigateBack ->
            // Handle back navigation
            let prevPage = List.head model.NavigationStack
            { model with 
                CurrentPage = prevPage
                NavigationStack = List.tail model.NavigationStack
            },
            cmds,
            None
        | None ->
            { model with FeatureModel = fModel },
            cmds,
            None
```

## Data Operations

### In-Memory Store
```fsharp
module DataStore =
    let private items = ResizeArray<Item>()
    
    let getAll() = items |> Seq.toList
    
    let add item =
        items.Add(item)
        item
    
    let update item =
        let index = items.FindIndex(fun i -> i.Id = item.Id)
        if index >= 0 then
            items.[index] <- item
            Some item
        else
            None
```

### API Layer with Async
```fsharp
module Api =
    let loadItems() = async {
        do! Async.Sleep(300)  // Simulate delay
        return DataStore.getAll()
    }
    
    let saveItem item = async {
        do! Async.Sleep(300)
        return DataStore.add item
    }
    
    let deleteItem id = async {
        do! Async.Sleep(300)
        return DataStore.delete id
    }
```

### Error Handling
```fsharp
let loadDataCmd() =
    Cmd.ofAsyncMsg (async {
        try
            let! data = Api.loadData()
            return DataLoaded (Ok data)
        with ex ->
            return DataLoaded (Error ex.Message)
    })

// In update
match msg with
| DataLoaded (Ok data) ->
    { model with Data = data; IsLoading = false }, [], None
| DataLoaded (Error err) ->
    { model with Error = Some err; IsLoading = false }, [], None
```

## Custom Controls

### SkiaSharp Control Wrapper
```fsharp
// C# Control
type SkCustomControl() =
    inherit SKCanvasView()
    
    static let valueProperty = 
        BindableProperty.Create("Value", typeof<float>, typeof<SkCustomControl>, 0.0)
    
    member this.Value
        with get() = this.GetValue(valueProperty) :?> float
        and set(v: float) = this.SetValue(valueProperty, v)
    
    override this.OnPaintSurface(e) =
        // Drawing code
        let canvas = e.Surface.Canvas
        canvas.Clear()
        // ... render

// F# Wrapper
type CustomControl() =
    inherit SkCustomControl()
    
    let valueChanged = Event<EventHandler<ValueChangedEventArgs>, _>()
    
    [<CLIEvent>]
    member _.ValueChanged = valueChanged.Publish
    
    override this.OnPropertyChanged(propName) =
        base.OnPropertyChanged(propName)
        if propName = "Value" then
            valueChanged.Trigger(this, ValueChangedEventArgs(0.0, this.Value))
```

### Fabulous Integration
```fsharp
type ICustomControl = inherit IFabView

module CustomControl =
    let WidgetKey = Widgets.register<CustomControl>()
    
    let Value = 
        Attributes.defineBindableWithEvent
            "CustomControl_Value"
            (BindableProperty.Create("Value", typeof<float>, typeof<CustomControl>))
            (fun target -> (target :?> CustomControl).ValueChanged)

[<AutoOpen>]
module CustomControlBuilder =
    type View with
        static member CustomControl(value: float, onChanged: ValueChangedEventArgs -> 'msg) =
            WidgetBuilder<'msg, ICustomControl>(
                CustomControl.WidgetKey,
                CustomControl.Value.WithValue(ValueEventData.create value onChanged)
            )

// Extension methods for modifiers
type CustomControlModifiers =
    [<Extension>]
    static member inline customProperty(this: WidgetBuilder<'msg, ICustomControl>, value: 'a) =
        this.AddScalar(CustomControl.CustomProperty.WithValue(value))
```

### Using Custom Control
```fsharp
// In view
CustomControl(
    model.Value,
    fun args -> ValueChanged args.NewValue
)
    .customProperty(someValue)
    .height(200.)
    .width(200.)
```

## Testing Patterns

### Unit Testing Update
```fsharp
[<Test>]
let ``Adding task updates model correctly`` () =
    // Arrange
    let initialModel = { Tasks = []; Filter = All }
    let newTask = Task.create "Test"
    
    // Act
    let updatedModel, _, _ = update (AddTask newTask) initialModel
    
    // Assert
    Assert.AreEqual(1, updatedModel.Tasks.Length)
    Assert.AreEqual("Test", updatedModel.Tasks.[0].Title)

[<Test>]
let ``Filtering works correctly`` () =
    let tasks = [
        { Id = TaskId (Guid.NewGuid()); IsCompleted = true; (* ... *) }
        { Id = TaskId (Guid.NewGuid()); IsCompleted = false; (* ... *) }
    ]
    let model = { Tasks = tasks; Filter = Active }
    
    let filtered = State.getFilteredTasks model
    
    Assert.AreEqual(1, filtered.Length)
    Assert.False(filtered.[0].IsCompleted)
```

## Common Patterns

### Form Validation
```fsharp
type ValidationError =
    | Required of string
    | TooLong of string * int
    | InvalidFormat of string

let validate model =
    [
        if String.IsNullOrWhiteSpace(model.Title) then
            yield Required "Title"
        if model.Title.Length > 100 then
            yield TooLong ("Title", 100)
    ]

let update msg model =
    match msg with
    | Submit ->
        let errors = validate model
        if errors.IsEmpty then
            { model with IsSubmitting = true },
            [ SubmitCmd model ],
            None
        else
            { model with ValidationErrors = errors },
            [],
            None
```

### Loading States
```fsharp
type LoadingState<'T> =
    | NotStarted
    | Loading
    | Loaded of 'T
    | Failed of string

type Model = {
    Data: LoadingState<DataType>
}

let view model =
    match model.Data with
    | NotStarted ->
        Button("Load Data", LoadData)
    | Loading ->
        ActivityIndicator(true)
    | Loaded data ->
        // Show data
        dataView data
    | Failed error ->
        VStack() {
            Label($"Error: {error}")
            Button("Retry", LoadData)
        }
```

### Debouncing Input
```fsharp
type Msg =
    | SearchTextChanged of string
    | DebouncedSearch of string

let update msg model =
    match msg with
    | SearchTextChanged text ->
        { model with SearchText = text },
        [ Cmd.debounce 500 (DebouncedSearch text) ],
        None
    | DebouncedSearch text ->
        { model with IsSearching = true },
        [ SearchCmd text ],
        None
```

## Best Practices Summary

1. **Keep Update Pure**: No side effects in update function
2. **Use Commands for IO**: All async/IO operations in commands
3. **Discriminated Unions**: For state and options
4. **Pattern Matching**: Exhaustive case handling
5. **Immutable Updates**: Always use `with` syntax
6. **Type Safety**: Strong types prevent bugs
7. **Composition**: Build complex UIs from simple functions
8. **Single Source of Truth**: Model is the only state

These patterns form the foundation of maintainable Fabulous MAUI applications.

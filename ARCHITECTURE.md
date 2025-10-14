# Fabulous MAUI Architecture Guide

## Overview

This document provides an in-depth explanation of the architecture used in the Task Manager sample application, demonstrating best practices for building Fabulous MAUI applications.

## MVU Pattern Deep Dive

### Model-View-Update (MVU)

The MVU architecture is a functional pattern for building UIs with the following characteristics:

1. **Unidirectional Data Flow**: Data flows in one direction: Model → View → Message → Update → Model
2. **Immutability**: The model is never mutated; updates create new model instances
3. **Pure Functions**: View and Update functions are pure (no side effects)
4. **Predictability**: Same input always produces the same output

### Architecture Layers

```
┌─────────────────────────────────────────────┐
│                  View Layer                 │
│  (Renders UI based on current model)        │
└─────────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────────┐
│                User Interactions            │
│  (Button clicks, text input, etc.)          │
└─────────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────────┐
│                 Messages (Msg)              │
│  (Events that describe what happened)       │
└─────────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────────┐
│              Update Function                │
│  (Pure function: Msg → Model → Model)       │
└─────────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────────┐
│             Command Messages                │
│  (Async operations, side effects)           │
└─────────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────────┐
│              Updated Model                  │
│  (New immutable state)                      │
└─────────────────────────────────────────────┘
                      ↓
              (Cycle repeats)
```

## Project Structure

### Feature-Based Organization

Each feature is self-contained with its own Types, State, and View:

```
Features/
├── TaskList/
│   ├── Types.fs      # Messages, Models, Navigation events
│   ├── State.fs      # State management and business logic
│   └── View.fs       # UI rendering
└── TaskDetail/
    ├── Types.fs
    ├── State.fs
    └── View.fs
```

### Types.fs - Message Definitions

```fsharp
type Msg =
    | UserAction          // Direct user interactions
    | DataLoaded of Data  // Async operation results
    | ErrorOccurred of string

type CmdMsg =
    | LoadData            // Commands for side effects
    | SaveData of Data

type NavigationMsg =
    | NavigateToDetail    // Navigation events
    | NavigateBack
```

**Purpose**: Defines the contract for all possible events and commands in the feature.

### State.fs - Business Logic

```fsharp
let init () =
    { /* initial model */ }, [ /* initial commands */ ]

let update msg model =
    match msg with
    | UserAction ->
        { model with /* changes */ }, [ /* commands */ ], None
    | DataLoaded data ->
        { model with Data = data }, [], Some NavigationEvent

let mapCmdMsg cmdMsg =
    match cmdMsg with
    | LoadData ->
        Cmd.ofAsyncMsg (async {
            let! data = Api.loadData()
            return DataLoaded data
        })
```

**Purpose**: Contains all state transitions and business logic.

### View.fs - UI Rendering

```fsharp
let view model =
    ContentPage(
        "Title",
        VStack() {
            Label(model.Text)
            Button("Action", UserAction)
        }
    )
```

**Purpose**: Pure function that renders UI based on the model.

## Navigation Pattern

### Hierarchical Navigation

The Root module manages navigation through a page stack:

```fsharp
type Model = {
    CurrentPage: Page
    NavigationStack: Page list
    // Feature models...
}

let update msg model =
    match msg with
    | NavigateTo page ->
        { model with
            CurrentPage = page
            NavigationStack = model.CurrentPage :: model.NavigationStack
        }, [], None
    
    | NavigateBack ->
        match model.NavigationStack with
        | prevPage :: rest ->
            { model with
                CurrentPage = prevPage
                NavigationStack = rest
            }, [], None
        | [] -> model, [], None
```

### Communication Between Features

Features communicate through:

1. **Navigation Messages**: Returned from update function as Option
2. **Shared Model Data**: Passed through the Root model
3. **Command Messages**: For async operations

Example:
```fsharp
// In TaskList feature
let update msg model =
    match msg with
    | TaskClicked taskId ->
        model, [], Some (NavigateToDetail taskId)

// In Root
let update msg model =
    match msg with
    | TaskListMsg tlMsg ->
        let model', cmds, navOpt = TaskList.update tlMsg model.TaskListModel
        match navOpt with
        | Some (NavigateToDetail id) ->
            model', cmds, Some (NavigateTo (TaskDetailPage id))
        | None ->
            model', cmds, None
```

## Data Layer

### Mock Data Store

The sample uses an in-memory store to simulate a backend:

```fsharp
module MockDataStore =
    let private tasks = ResizeArray<Task>()
    
    let getAllTasks() = tasks |> Seq.toList
    let addTask task = tasks.Add(task); task
    let updateTask task = (* implementation *)
```

### API Abstraction

The API layer adds async simulation:

```fsharp
module TaskApi =
    let loadTasks() = async {
        do! Async.Sleep(300)  // Simulate network delay
        return MockDataStore.getAllTasks()
    }
```

**Benefits**:
- Easy to replace with real HTTP calls
- Testable without external dependencies
- Consistent async patterns

## Custom Controls

### Integrating C# Controls

The RadialSlider demonstrates integrating SkiaSharp controls:

1. **C# Control** (`SkRadialSlider`):
   - Inherits from `SKCanvasView`
   - Implements touch handling and rendering
   - Defines bindable properties

2. **F# Wrapper** (`CustomRadialSlider`):
   - Wraps the C# control
   - Exposes events for Fabulous

3. **Fabulous Bindings**:
   - Widget registration
   - Attribute definitions
   - Builder methods

```fsharp
type IRadialSlider = inherit IFabView

module RadialSlider =
    let WidgetKey = Widgets.register<CustomRadialSlider>()
    let ValueChanged = Attributes.defineBindableWithEvent (...)

[<AutoOpen>]
module RadialSliderBuilder =
    type View with
        static member RadialSlider(value, onChanged) =
            WidgetBuilder<'msg, IRadialSlider>(
                RadialSlider.WidgetKey,
                RadialSlider.ValueChanged.WithValue(...)
            )
```

## State Management Best Practices

### 1. Model Validation

Validate the model after updates:

```fsharp
let validate model =
    { model with
        Title = 
            if model.Title.Length > MaxLength then
                model.Title.Substring(0, MaxLength)
            else
                model.Title
    }

let update msg model =
    let model', cmds, nav = updateInternal msg model
    validate model', cmds, nav
```

### 2. Command Composition

Combine multiple commands:

```fsharp
let update msg model =
    match msg with
    | SaveAndNavigate ->
        model, 
        [ SaveCmd; LoadCmd ],
        Some NavigateBack
```

### 3. Error Handling

Handle errors gracefully:

```fsharp
type Msg =
    | DataLoaded of Result<Data, string>

let mapCmdMsg = function
    | LoadData ->
        Cmd.ofAsyncMsg (async {
            try
                let! data = Api.loadData()
                return DataLoaded (Ok data)
            with ex ->
                return DataLoaded (Error ex.Message)
        })
```

### 4. Loading States

Track async operations:

```fsharp
type Model = {
    Data: Data option
    IsLoading: bool
    Error: string option
}

let update msg model =
    match msg with
    | StartLoad ->
        { model with IsLoading = true; Error = None },
        [ LoadDataCmd ],
        None
    | DataLoaded (Ok data) ->
        { model with Data = Some data; IsLoading = false },
        [],
        None
    | DataLoaded (Error err) ->
        { model with IsLoading = false; Error = Some err },
        [],
        None
```

## Performance Considerations

### 1. View Optimization

- Keep view functions pure
- Avoid expensive computations in view
- Use memoization for complex calculations

```fsharp
// Bad: Filtering in view
for task in model.Tasks |> List.filter filterFn do
    taskItem task

// Good: Pre-filter in model or update
let filteredTasks = State.getFilteredTasks model
for task in filteredTasks do
    taskItem task
```

### 2. Command Batching

Batch related commands:

```fsharp
let update msg model =
    match msg with
    | ComplexAction ->
        model,
        [ Cmd1; Cmd2; Cmd3 ],  // All execute in parallel
        None
```

### 3. Model Size

Keep models focused:

```fsharp
// Good: Separate concerns
type TaskListModel = {
    Tasks: Task list
    Filter: TaskFilter
}

type TaskDetailModel = {
    Task: Task
    IsEditing: bool
}

// Bad: Everything in one model
type AppModel = {
    AllTasks: Task list
    SelectedTask: Task option
    Filter: TaskFilter
    IsEditing: bool
    // ... too many fields
}
```

## Testing Strategies

### 1. Pure Function Testing

Test update functions easily:

```fsharp
[<Test>]
let ``Adding task updates model correctly`` () =
    let model = { Tasks = [] }
    let model', _, _ = update (AddTask "Test") model
    
    Assert.AreEqual(1, model'.Tasks.Length)
    Assert.AreEqual("Test", model'.Tasks.[0].Title)
```

### 2. Command Testing

Test command generation:

```fsharp
[<Test>]
let ``Save task generates correct command`` () =
    let model = { Title = "Test" }
    let _, cmds, _ = update SaveTask model
    
    Assert.IsTrue(List.contains (SaveTaskCmd _) cmds)
```

### 3. Navigation Testing

Test navigation events:

```fsharp
[<Test>]
let ``Task click generates navigation event`` () =
    let model = { Tasks = [task] }
    let _, _, navOpt = update (TaskClicked taskId) model
    
    Assert.AreEqual(Some (NavigateToDetail taskId), navOpt)
```

## Common Patterns

### 1. Master-Detail

```fsharp
type Page =
    | ListPage
    | DetailPage of ItemId

// Navigation handled in Root
let update msg model =
    match msg with
    | ListMsg (ItemSelected id) ->
        model, [], Some (NavigateTo (DetailPage id))
```

### 2. Form Validation

```fsharp
type ValidationError =
    | Required of field: string
    | TooLong of field: string * max: int

let validate model =
    [
        if String.IsNullOrWhiteSpace(model.Title) then
            yield Required "Title"
        if model.Title.Length > 100 then
            yield TooLong ("Title", 100)
    ]

let update msg model =
    match msg with
    | Save ->
        let errors = validate model
        if errors.IsEmpty then
            model, [ SaveCmd ], None
        else
            { model with Errors = errors }, [], None
```

### 3. Confirmation Dialogs

```fsharp
type Msg =
    | RequestDelete of TaskId
    | ConfirmDelete of TaskId
    | CancelDelete

let update msg model =
    match msg with
    | RequestDelete id ->
        { model with PendingDelete = Some id }, [], None
    | ConfirmDelete id ->
        { model with PendingDelete = None },
        [ DeleteTaskCmd id ],
        None
    | CancelDelete ->
        { model with PendingDelete = None }, [], None
```

## Migration from Real App

### Replacing Mock Data

To use real backend:

1. Replace `MockDataStore` with HTTP client:
```fsharp
module TaskApi =
    let httpClient = new HttpClient()
    
    let loadTasks() = async {
        let! response = httpClient.GetAsync("/api/tasks") |> Async.AwaitTask
        let! json = response.Content.ReadAsStringAsync() |> Async.AwaitTask
        return JsonSerializer.Deserialize<Task list>(json)
    }
```

2. Add authentication:
```fsharp
type Container = {
    Api: ITaskApi
    Auth: IAuthService
}

let mapCmdMsg container cmdMsg =
    match cmdMsg with
    | LoadTasks ->
        Cmd.ofAsyncMsg (async {
            let! token = container.Auth.getToken()
            let! tasks = container.Api.loadTasks(token)
            return TasksLoaded tasks
        })
```

## Conclusion

This architecture provides:

- **Maintainability**: Clear separation of concerns
- **Testability**: Pure functions are easy to test
- **Scalability**: Feature-based structure scales well
- **Type Safety**: F# prevents many runtime errors
- **Predictability**: Unidirectional data flow is easy to reason about

The Task Manager sample demonstrates these principles in a simple, focused application that can serve as a template for more complex projects.

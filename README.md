# Fabulous MAUI F# Mobile Application Tutorial

A comprehensive guide to building cross-platform mobile applications using Fabulous MAUI with F#.

> 📖 **New to this tutorial?** Start with [INDEX.md](INDEX.md) for a complete overview and learning path!

## Table of Contents

1. [Introduction](#introduction)
2. [What is Fabulous MAUI?](#what-is-fabulous-maui)
3. [Getting Started](#getting-started)
4. [MVU Architecture](#mvu-architecture)
5. [Sample Application](#sample-application)
6. [Project Structure](#project-structure)
7. [Building the UI](#building-the-ui)
8. [Custom Controls](#custom-controls)
9. [Navigation](#navigation)
10. [Best Practices](#best-practices)

## Introduction

This tutorial demonstrates how to build a cross-platform mobile application for iOS and Android using Fabulous MAUI with F#. The sample application is a simple task management app that showcases the core concepts without exposing any sensitive business logic.

## What is Fabulous MAUI?

Fabulous MAUI is a declarative UI framework for building cross-platform mobile and desktop applications using F# and .NET MAUI. It uses the Model-View-Update (MVU) architecture pattern, inspired by Elm, to create predictable and maintainable applications.

### Key Features

- **Declarative UI**: Build UI with functional, composable components
- **MVU Architecture**: Clear separation of concerns with unidirectional data flow
- **Type Safety**: Leverage F#'s strong type system
- **Cross-Platform**: Write once, run on iOS, Android, macOS, and Windows
- **Hot Reload**: Fast development iteration

## Getting Started

### Prerequisites

- .NET 9.0 SDK or later
- Visual Studio 2022 (Windows/Mac) or JetBrains Rider
- For iOS development: macOS with Xcode
- For Android development: Android SDK

### Installation

1. Install .NET MAUI workload:
```bash
dotnet workload install maui
```

2. Create a new F# MAUI project or use the sample provided in this tutorial.

## MVU Architecture

The MVU (Model-View-Update) architecture consists of three main components:

### Model
The Model represents the application state. It's an immutable data structure that holds all the information needed to render the UI.

```fsharp
type Model = {
    Tasks: Task list
    NewTaskTitle: string
    Filter: TaskFilter
}
```

### Update
The Update function processes messages (events) and returns a new model state. It's a pure function that takes the current model and a message, then returns the updated model.

```fsharp
let update msg model =
    match msg with
    | AddTask ->
        let newTask = { Id = Guid.NewGuid(); Title = model.NewTaskTitle; IsCompleted = false }
        { model with Tasks = newTask :: model.Tasks; NewTaskTitle = "" }
    | UpdateNewTaskTitle text ->
        { model with NewTaskTitle = text }
    | ToggleTaskCompletion taskId ->
        let updatedTasks = 
            model.Tasks 
            |> List.map (fun t -> 
                if t.Id = taskId then { t with IsCompleted = not t.IsCompleted } 
                else t)
        { model with Tasks = updatedTasks }
```

### View
The View is a pure function that takes the model and returns a description of the UI. It uses a declarative syntax to build the UI tree.

```fsharp
let view model =
    ContentPage(
        VStack() {
            Entry(model.NewTaskTitle, UpdateNewTaskTitle)
            Button("Add Task", AddTask)
            
            for task in model.Tasks do
                HStack() {
                    Label(task.Title)
                    CheckBox(task.IsCompleted, (fun _ -> ToggleTaskCompletion task.Id))
                }
        }
    )
```

## Sample Application

The sample application included in this tutorial is a **Task Manager** app that demonstrates:

- Creating and displaying tasks
- Marking tasks as complete/incomplete
- Filtering tasks by status
- Using a custom radial slider for priority selection
- In-memory data storage (no external database)
- Clean architecture without business-specific logic

### Features

1. **Task Management**: Add, view, and complete tasks
2. **Priority Slider**: Use a radial slider control to set task priority
3. **Filtering**: View all, active, or completed tasks
4. **Clean UI**: Modern, responsive design

## Project Structure

```
FabulousMauiTutorial/
├── README.md                          # This tutorial
├── TaskManagerApp/                    # Sample application
│   ├── TaskManagerApp.fsproj         # F# project file
│   ├── MauiProgram.fs                # App initialization
│   ├── Domain.fs                      # Domain models
│   ├── MockData.fs                    # In-memory data store
│   ├── Controls/
│   │   └── RadialSlider.fs           # Custom radial slider control
│   ├── Features/
│   │   ├── TaskList/
│   │   │   ├── Types.fs              # Task list types
│   │   │   ├── State.fs              # Task list state management
│   │   │   └── View.fs               # Task list UI
│   │   └── TaskDetail/
│   │       ├── Types.fs              # Task detail types
│   │       ├── State.fs              # Task detail state management
│   │       └── View.fs               # Task detail UI
│   ├── Root/
│   │   ├── Types.fs                  # Root types
│   │   ├── State.fs                  # Root state management
│   │   └── View.fs                   # Root view and navigation
│   ├── Resources/                     # Images, fonts, etc.
│   └── Platforms/                     # Platform-specific code
│       ├── Android/
│       └── iOS/
```

## Building the UI

### Basic Components

Fabulous MAUI provides builders for all standard MAUI controls:

```fsharp
// Labels
Label("Hello, World!")
    .fontSize(24.0)
    .textColor(Colors.Blue)

// Buttons
Button("Click Me", OnButtonClicked)
    .backgroundColor(Colors.Green)

// Text Input
Entry(model.Text, TextChanged)
    .placeholder("Enter text...")

// Lists
(VStack() {
    for item in model.Items do
        Label(item.Name)
}).spacing(10.0)
```

### Layout Containers

```fsharp
// Vertical Stack
VStack() {
    Label("Item 1")
    Label("Item 2")
    Label("Item 3")
}

// Horizontal Stack
HStack() {
    Label("Left")
    Label("Right")
}

// Grid
Grid(rowdefs = [Auto; Star; Auto], coldefs = [Star; Star]) {
    Label("Header").gridRow(0).gridColumnSpan(2)
    Label("Content 1").gridRow(1).gridColumn(0)
    Label("Content 2").gridRow(1).gridColumn(1)
    Label("Footer").gridRow(2).gridColumnSpan(2)
}
```

## Custom Controls

The tutorial includes a custom **Radial Slider** control for selecting task priority. This demonstrates how to integrate custom SkiaSharp-based controls into Fabulous MAUI.

### Using the Radial Slider

```fsharp
RadialSlider(
    value = model.Priority,
    onValueChanged = (fun args -> PriorityChanged (int args.NewValue))
)
    .minimum(0.0)
    .maximum(10.0)
    .trackColor(Colors.LightGray)
    .trackProgressColor(Colors.Blue)
    .knobColor(Colors.DarkBlue)
```

### Custom Control Implementation

The radial slider is implemented in C# using SkiaSharp and wrapped with Fabulous bindings:

1. **C# Control** (`SkRadialSlider.cs`): Core rendering and touch handling
2. **F# Wrapper** (`RadialSlider.fs`): Fabulous widget bindings

## Navigation

The sample app demonstrates navigation between pages:

```fsharp
// Push a new page
let navigate toPage model =
    { model with CurrentPage = toPage }, []

// Pop back
let goBack model =
    match model.NavigationStack with
    | [] -> model, []
    | prevPage :: rest ->
        { model with CurrentPage = prevPage; NavigationStack = rest }, []
```

Navigation is handled through the MVU pattern by updating the model state.

## Best Practices

### 1. Keep Models Immutable
Always create new model instances instead of mutating existing ones.

```fsharp
// Good
{ model with NewField = value }

// Bad (don't do this)
model.NewField <- value
```

### 2. Pure Functions
Keep update and view functions pure (no side effects).

```fsharp
// Pure update function
let update msg model =
    match msg with
    | Increment -> { model with Counter = model.Counter + 1 }
```

### 3. Use Commands for Side Effects
Handle side effects (API calls, storage) using commands.

```fsharp
type CmdMsg =
    | LoadTasks
    | TasksLoaded of Task list

let mapCmd = function
    | LoadTasks -> Cmd.ofAsyncMsg (async {
        let! tasks = DataStore.loadTasks()
        return TasksLoaded tasks
    })
    | TasksLoaded tasks -> Cmd.none
```

### 4. Organize by Feature
Structure your code by feature rather than by technical layer.

```
Features/
├── TaskList/
│   ├── Types.fs
│   ├── State.fs
│   └── View.fs
```

### 5. Composition Over Inheritance
Use composition and higher-order functions instead of inheritance.

```fsharp
let taskCard task onToggle =
    Border() {
        HStack() {
            Label(task.Title)
            CheckBox(task.IsCompleted, onToggle)
        }
    }
    .stroke(Colors.Gray)
    .padding(10.0)
```

### 6. Type Safety
Leverage F#'s type system to prevent errors.

```fsharp
type TaskId = TaskId of Guid
type Priority = 
    | Low 
    | Medium 
    | High

type Task = {
    Id: TaskId
    Title: string
    Priority: Priority
    IsCompleted: bool
}
```

## Running the Sample App

**Important:** See [BUILD_NOTES.md](BUILD_NOTES.md) for prerequisite setup (MAUI workload required).

1. Navigate to the TaskManagerApp directory:
```bash
cd FabulousMauiTutorial/TaskManagerApp
```

2. Restore dependencies:
```bash
dotnet restore
```

3. Run on Android:
```bash
dotnet build -t:Run -f net8.0-android
```

4. Run on iOS (macOS only):
```bash
dotnet build -t:Run -f net8.0-ios
```

## 📚 Additional Documentation

- **[INDEX.md](INDEX.md)** - Complete overview and learning path
- **[GETTING_STARTED.md](GETTING_STARTED.md)** - Detailed setup and build instructions
- **[ARCHITECTURE.md](ARCHITECTURE.md)** - Deep architectural insights
- **[BUILD_NOTES.md](BUILD_NOTES.md)** - Build requirements and troubleshooting

## Additional Resources

- [Fabulous Documentation](https://docs.fabulous.dev/)
- [.NET MAUI Documentation](https://docs.microsoft.com/dotnet/maui/)
- [F# Language Guide](https://docs.microsoft.com/dotnet/fsharp/)
- [MVU Architecture](https://guide.elm-lang.org/architecture/)
- [The Elmish Book](https://zaid-ajaj.github.io/the-elmish-book/)
- [Guidance for .NET systems development](https://gist.github.com/Thorium/b33dd7c5dd165b76d8517bf2525fe51e)

## Conclusion

This tutorial provides a foundation for building cross-platform mobile applications with Fabulous MAUI and F#. The sample Task Manager app demonstrates core concepts while remaining generic and business-agnostic.

Key takeaways:
- MVU architecture provides a clear, predictable application structure
- Fabulous enables declarative UI with F#'s functional programming features
- Custom controls can be integrated seamlessly
- Type safety helps prevent runtime errors
- The framework supports modern mobile app development patterns

Happy coding! 🚀

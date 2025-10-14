# Fabulous MAUI Architecture Diagrams

## MVU Data Flow

```
┌─────────────────────────────────────────────────────────────┐
│                        User Interface                        │
│                      (Declarative View)                      │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           │ User Interaction
                           │ (Button Click, Text Input, etc.)
                           ↓
┌─────────────────────────────────────────────────────────────┐
│                       Message (Msg)                          │
│     Describes what happened (TitleChanged, SaveTask)         │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           │ Dispatch
                           ↓
┌─────────────────────────────────────────────────────────────┐
│                    Update Function                           │
│   Pure: (Msg, Model) → (Model, CmdMsg list, NavMsg option)  │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ├─────────────┬─────────────┐
                           │             │             │
                           ↓             ↓             ↓
                    ┌──────────┐  ┌──────────┐  ┌──────────┐
                    │   New    │  │ Commands │  │   Nav    │
                    │  Model   │  │  (Async) │  │  Event   │
                    └────┬─────┘  └─────┬────┘  └────┬─────┘
                         │              │             │
                         │              ↓             │
                         │       ┌──────────────┐    │
                         │       │   Side       │    │
                         │       │  Effects     │    │
                         │       │ (API Calls)  │    │
                         │       └──────┬───────┘    │
                         │              │             │
                         │              ↓ Result      │
                         │       ┌──────────────┐    │
                         │       │ New Message  │    │
                         │       │ (Msg)        │    │
                         │       └──────┬───────┘    │
                         │              │             │
                         └──────────────┴─────────────┘
                                        │
                                        ↓
                        ┌──────────────────────────┐
                        │   Re-render View         │
                        │   (UI Updates)           │
                        └──────────────────────────┘
```

## Application Architecture

```
┌────────────────────────────────────────────────────────────┐
│                        Root Module                          │
│  ┌────────────────────────────────────────────────────┐   │
│  │   Navigation State                                  │   │
│  │   - CurrentPage                                     │   │
│  │   - NavigationStack                                 │   │
│  └────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌────────────────────────────────────────────────────┐   │
│  │   Feature Coordination                              │   │
│  │   - Routes messages to features                     │   │
│  │   - Handles navigation events                       │   │
│  │   - Manages feature models                          │   │
│  └────────────────────────────────────────────────────┘   │
└───────────┬────────────────────────────┬───────────────────┘
            │                            │
            ↓                            ↓
┌───────────────────────┐    ┌───────────────────────┐
│  TaskList Feature     │    │  TaskDetail Feature   │
│  ┌─────────────────┐  │    │  ┌─────────────────┐  │
│  │ Types.fs        │  │    │  │ Types.fs        │  │
│  │ - Msg           │  │    │  │ - Msg           │  │
│  │ - CmdMsg        │  │    │  │ - CmdMsg        │  │
│  │ - Model         │  │    │  │ - Model         │  │
│  │ - NavigationMsg │  │    │  │ - NavigationMsg │  │
│  └─────────────────┘  │    │  └─────────────────┘  │
│  ┌─────────────────┐  │    │  ┌─────────────────┐  │
│  │ State.fs        │  │    │  │ State.fs        │  │
│  │ - init          │  │    │  │ - init          │  │
│  │ - update        │  │    │  │ - update        │  │
│  │ - mapCmdMsg     │  │    │  │ - mapCmdMsg     │  │
│  └─────────────────┘  │    │  └─────────────────┘  │
│  ┌─────────────────┐  │    │  ┌─────────────────┐  │
│  │ View.fs         │  │    │  │ View.fs         │  │
│  │ - view          │  │    │  │ - view          │  │
│  │ - components    │  │    │  │ - components    │  │
│  └─────────────────┘  │    │  └─────────────────┘  │
└───────────┬───────────┘    └───────────┬───────────┘
            │                            │
            └────────────┬───────────────┘
                         ↓
            ┌────────────────────────┐
            │    Data Layer          │
            │  ┌──────────────────┐  │
            │  │ MockData.fs      │  │
            │  │ - DataStore      │  │
            │  │ - API Layer      │  │
            │  └──────────────────┘  │
            │  ┌──────────────────┐  │
            │  │ Domain.fs        │  │
            │  │ - Task           │  │
            │  │ - TaskId         │  │
            │  │ - Priority       │  │
            │  └──────────────────┘  │
            └────────────────────────┘
```

## Feature Structure (MVU Pattern)

```
Feature Module
│
├── Types.fs
│   ├── Msg (User Actions)
│   │   ├── Input events
│   │   ├── Button clicks
│   │   └── Data loaded events
│   │
│   ├── CmdMsg (Side Effects)
│   │   ├── API calls
│   │   ├── Storage operations
│   │   └── Async operations
│   │
│   ├── Model (State)
│   │   ├── UI state
│   │   ├── Data
│   │   └── Loading flags
│   │
│   └── NavigationMsg (Navigation Events)
│       ├── Navigate to page
│       └── Go back
│
├── State.fs
│   ├── init: unit → (Model × CmdMsg list)
│   │   └── Initialize state and load data
│   │
│   ├── update: Msg → Model → (Model × CmdMsg list × NavigationMsg option)
│   │   └── Process messages and update state
│   │
│   └── mapCmdMsg: CmdMsg → Cmd<Msg>
│       └── Execute async operations
│
└── View.fs
    ├── view: Model → (Msg → unit) → ContentPage
    │   └── Render UI based on model
    │
    └── helper components
        ├── Component functions
        └── Reusable UI elements
```

## Navigation Flow

```
┌─────────────────────────────────────────────────────────────┐
│                      User Action                             │
│              (Tap on task in list)                           │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ↓
┌─────────────────────────────────────────────────────────────┐
│                TaskList Feature Update                       │
│   Returns: (Model, [], Some(NavigateToDetail taskId))       │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ↓
┌─────────────────────────────────────────────────────────────┐
│                    Root Module                               │
│   Receives NavigationMsg                                     │
│   Creates TaskDetail model with init(taskId)                │
│   Updates CurrentPage and NavigationStack                    │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ↓
┌─────────────────────────────────────────────────────────────┐
│                    View Re-renders                           │
│   Shows TaskDetail page                                      │
│   NavigationPage handles transition                          │
└─────────────────────────────────────────────────────────────┘

                           │
                           ↓
┌─────────────────────────────────────────────────────────────┐
│              User Saves and Goes Back                        │
│              (Tap Save button)                               │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ↓
┌─────────────────────────────────────────────────────────────┐
│                TaskDetail Feature Update                     │
│   Returns: (Model, [SaveCmd], Some(NavigateBack))          │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ↓
┌─────────────────────────────────────────────────────────────┐
│                    Root Module                               │
│   Pops navigation stack                                      │
│   Returns to previous page                                   │
│   Triggers refresh of TaskList                               │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ↓
┌─────────────────────────────────────────────────────────────┐
│                    View Re-renders                           │
│   Shows TaskList page with updated data                      │
└─────────────────────────────────────────────────────────────┘
```

## Data Flow Example: Loading Tasks

```
1. User Opens App
   │
   ↓
2. TaskList.init() called
   │
   ├─→ Initial Model: { Tasks = []; IsLoading = true }
   │
   └─→ Commands: [ LoadTasks ]
       │
       ↓
3. mapCmdMsg(LoadTasks)
   │
   ├─→ Cmd.ofAsyncMsg (async {
   │       let! tasks = TaskApi.loadTasks()
   │       return TasksLoaded tasks
   │   })
   │
   └─→ Async operation starts
       │
       ↓
4. TaskApi.loadTasks()
   │
   ├─→ Async.Sleep(300)  // Simulate delay
   │
   └─→ MockDataStore.getAllTasks()
       │
       ↓
5. Async completes
   │
   └─→ Message: TasksLoaded [task1; task2; task3]
       │
       ↓
6. update(TasksLoaded tasks, model)
   │
   └─→ New Model: { Tasks = tasks; IsLoading = false }
       │
       ↓
7. View re-renders
   │
   └─→ Shows task list
```

## Custom Control Integration

```
C# Layer (SkRadialSlider.cs)
┌────────────────────────────────┐
│ SkiaSharp Canvas View          │
│  - OnPaintSurface()            │
│  - OnTouch()                   │
│  - Bindable Properties         │
└────────────┬───────────────────┘
             │
             ↓
F# Wrapper (CustomRadialSlider)
┌────────────────────────────────┐
│ Inherits SkRadialSlider        │
│  - Exposes CLIEvent            │
│  - Value change notifications  │
└────────────┬───────────────────┘
             │
             ↓
Fabulous Bindings
┌────────────────────────────────┐
│ Widget Registration            │
│  - IRadialSlider interface     │
│  - Attribute definitions       │
│  - Builder methods             │
└────────────┬───────────────────┘
             │
             ↓
View Usage
┌────────────────────────────────┐
│ RadialSlider(                  │
│   value,                       │
│   fun args -> ValueChanged ... │
│ )                              │
│   .minimum(0.0)                │
│   .maximum(10.0)               │
└────────────────────────────────┘
```

## Type Safety Flow

```
Domain Types (Strong Typing)
┌────────────────────────────────┐
│ type TaskId = TaskId of Guid   │
│ type Priority = Low | Med | Hi │
│ type Task = { ... }            │
└────────────┬───────────────────┘
             │
             ↓
Messages (Discriminated Unions)
┌────────────────────────────────┐
│ type Msg =                     │
│   | TitleChanged of string     │
│   | PriorityChanged of float   │
│   | SaveTask                   │
└────────────┬───────────────────┘
             │
             ↓
Pattern Matching (Exhaustive)
┌────────────────────────────────┐
│ let update msg model =         │
│   match msg with               │
│   | TitleChanged text → ...    │
│   | PriorityChanged p → ...    │
│   | SaveTask → ...             │
│   // Compiler ensures all cases│
└────────────┬───────────────────┘
             │
             ↓
Type-Safe State Transitions
┌────────────────────────────────┐
│ Returns:                       │
│   (Model, CmdMsg list, NavMsg?)│
│ All types checked at compile   │
└────────────────────────────────┘
```

---

These diagrams illustrate the key architectural patterns used in the Fabulous MAUI Task Manager application. For more details, see [ARCHITECTURE.md](ARCHITECTURE.md).

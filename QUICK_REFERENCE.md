# Quick Reference Card

## 📖 Documentation Quick Links

| Document | Purpose | When to Read |
|----------|---------|--------------|
| **[INDEX.md](INDEX.md)** | Overview & Learning Path | 🟢 **START HERE** |
| **[README.md](README.md)** | Main Tutorial | After INDEX |
| **[GETTING_STARTED.md](GETTING_STARTED.md)** | Setup & Build Guide | Before coding |
| **[ARCHITECTURE.md](ARCHITECTURE.md)** | Deep Architecture Dive | When ready to dig deeper |
| **[CODE_EXAMPLES.md](CODE_EXAMPLES.md)** | Code Patterns | As reference while coding |
| **[DIAGRAMS.md](DIAGRAMS.md)** | Visual Architecture | To understand flow |
| **[BUILD_NOTES.md](BUILD_NOTES.md)** | Build Troubleshooting | If build issues |
| **[SUMMARY.md](SUMMARY.md)** | Project Overview | Quick summary |

## 🚀 Quick Start Commands

```bash
# Install MAUI workload (one time)
dotnet workload install maui

# Navigate to project
cd FabulousMauiTutorial/TaskManagerApp

# Restore packages
dotnet restore

# Build for Android
dotnet build -f net8.0-android

# Run on Android
dotnet build -t:Run -f net8.0-android

# Build for iOS (macOS only)
dotnet build -f net8.0-ios
```

## 📂 Key Files to Study

### 🎯 Start Here
1. `Domain.fs` - Domain models and types
2. `MockData.fs` - Data layer
3. `Features/TaskList/Types.fs` - Messages and state
4. `Features/TaskList/State.fs` - Business logic
5. `Features/TaskList/View.fs` - UI rendering

### 🎨 Custom Control
- `Controls/RadialSlider.fs` - SkiaSharp custom control

### 🧭 Navigation
- `Root/Types.fs` - App-level types
- `Root/State.fs` - Navigation logic
- `Root/View.fs` - Root view

### 🚀 Initialization
- `MauiProgram.fs` - App startup

## 🔑 Key Concepts

### MVU Pattern
```fsharp
// Model - State
type Model = { /* immutable state */ }

// Msg - Events
type Msg = 
    | UserAction
    | DataLoaded of Data

// Update - State Transitions
let update msg model =
    match msg with
    | UserAction -> 
        { model with /* changes */ }, 
        [ /* commands */ ],
        None

// View - UI
let view model =
    ContentPage(/* UI */)
```

### Navigation Pattern
```fsharp
// Feature returns navigation event
let update msg model =
    model, [], Some (NavigateToDetail id)

// Root handles navigation
let update msg model =
    match navMsg with
    | Some nav -> 
        { model with CurrentPage = newPage }
```

### Async Commands
```fsharp
type CmdMsg =
    | LoadData

let mapCmdMsg = function
    | LoadData ->
        Cmd.ofAsyncMsg (async {
            let! data = Api.load()
            return DataLoaded data
        })
```

## 🎨 UI Patterns

### Layout
```fsharp
VStack(spacing = 16.) {
    Label("Title")
    Entry(model.Text, TextChanged)
    Button("Save", SaveClicked)
}
```

### Lists
```fsharp
VStack() {
    for item in model.Items do
        itemView item
}
```

### Conditional
```fsharp
if model.IsLoading then
    ActivityIndicator(true)
else
    contentView model
```

## 🔧 Common Tasks

### Add New Feature
1. Create folder: `Features/MyFeature/`
2. Add files: `Types.fs`, `State.fs`, `View.fs`
3. Define messages and model in Types
4. Implement init and update in State
5. Build UI in View
6. Wire up in Root

### Create Custom Control
1. Create C# control (SkiaSharp)
2. Add F# wrapper with events
3. Register widget
4. Add builder methods
5. Use in view

### Handle Async Data
1. Define CmdMsg
2. Implement mapCmdMsg
3. Return command from update
4. Handle result message

## 🐛 Troubleshooting

### Build Fails
- Check MAUI workload: `dotnet workload list`
- Install if missing: `dotnet workload install maui`
- Clean: `dotnet clean`
- Restore: `dotnet restore`

### Runtime Errors
- Check file compilation order in .fsproj
- Ensure all Msg cases handled
- Verify navigation setup

### UI Not Updating
- Verify update returns new model
- Ensure view is pure function
- Check message handlers are connected

## 📚 Learning Path

### Beginner (1-2 days)
- [x] Read INDEX.md
- [x] Follow GETTING_STARTED.md
- [x] Run the sample app
- [x] Study Domain.fs
- [x] Understand TaskList feature

### Intermediate (3-5 days)
- [x] Read ARCHITECTURE.md
- [x] Study navigation in Root
- [x] Examine custom controls
- [x] Add a new feature
- [x] Modify existing features

### Advanced (1-2 weeks)
- [x] Replace mock data with real API
- [x] Add authentication
- [x] Implement persistence
- [x] Optimize performance
- [x] Deploy to stores

## 🎯 Common Patterns

### Form Input
```fsharp
type Msg =
    | TitleChanged of string
    | SaveClicked

Entry(model.Title, TitleChanged)
Button("Save", SaveClicked)
```

### Loading State
```fsharp
type Model = {
    IsLoading: bool
    Data: 'T option
}

if model.IsLoading then
    ActivityIndicator(true)
elif model.Data.IsSome then
    dataView model.Data.Value
```

### Error Handling
```fsharp
type Msg =
    | DataLoaded of Result<Data, string>

match msg with
| DataLoaded (Ok data) -> 
    { model with Data = data }
| DataLoaded (Error err) ->
    { model with Error = Some err }
```

## 🔗 External Resources

- **Fabulous**: https://docs.fabulous.dev/
- **.NET MAUI**: https://docs.microsoft.com/dotnet/maui/
- **F# Guide**: https://fsharp.org/
- **MVU Pattern**: https://guide.elm-lang.org/architecture/
- **The Elmish Book**: https://zaid-ajaj.github.io/the-elmish-book/
- **Guidance for .NET systems development**: https://gist.github.com/Thorium/b33dd7c5dd165b76d8517bf2525fe51e

## 💡 Pro Tips

1. **Start Simple**: Begin with README, don't jump to code
2. **Follow MVU**: Keep update pure, use commands for side effects
3. **Type Safety**: Use discriminated unions for state
4. **Compose**: Build complex UI from simple functions
5. **Test Logic**: Update functions are easy to test
6. **Read Code**: Study working examples in sample app
7. **Iterate**: Start small, add features gradually

## 📝 Cheat Sheet

### File Purpose
- `Types.fs` = Messages & Models
- `State.fs` = Business Logic
- `View.fs` = UI Rendering

### Update Return
```fsharp
(Model, CmdMsg list, NavMsg option)
```

### View Function
```fsharp
Model → (Msg → unit) → ContentPage
```

### Command
```fsharp
Cmd.ofAsyncMsg (async { ... })
```

---

**Ready to Start?** → [INDEX.md](INDEX.md) 🚀

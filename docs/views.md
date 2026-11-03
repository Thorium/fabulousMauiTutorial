---
title: Views and controls
category: Guides
categoryindex: 1
index: 3
---

# Views and controls

Views are F# functions from a model to a widget tree, written with Fabulous builders. They live
in the MAUI project because they reference MAUI types. The code below is plain markdown rather
than type-checked script: checking it would need the MAUI assemblies of a platform target.

## Builders and modifiers

Each MAUI control has a builder. Constructor arguments carry the essentials (text, value, the
message to dispatch); modifiers set everything else:

```fsharp
open Fabulous.Maui
open type Fabulous.Maui.View

Label("Task Manager")
    .font(size = 28., attributes = FontAttributes.Bold)
    .centerTextHorizontal()

Button("Sign in", SignIn)
    .background(SolidColorBrush(Colors.Blue))

Entry(model.Title, TitleChanged)
    .placeholder("Title")
```

Layouts take children in a computation expression, with `if`, `match` and `for` inside:

```fsharp
(VStack(spacing = 16.) {
    Label("Tasks")

    if model.IsLoading then
        ActivityIndicator(true)
    else
        for task in model.Tasks do
            Label(task.Title)
})
    .padding(16.)
```

A page is a `ContentPage`; toolbar items attach with `.toolbarItems()`:

```fsharp
ContentPage(content)
    .toolbarItems() {
        ToolbarItem("Add", AddNewTask)
        if user.IsSome then
            ToolbarItem("Sign out", SignOut).order ToolbarItemOrder.Secondary
    }
```

## Busy indicators

`Controls/Busy.fs` has two pieces. `Busy.indicator` is a spinner with a caption for a page with
nothing to show yet, such as the first load. `Busy.overlay` is a translucent layer for a page
that already has content: it shares a `Grid` cell with the content, which stays visible while
a refresh, toggle, delete or save runs:

```fsharp
ContentPage(
    (Grid() {
        VStack() { ... }

        if model.IsLoading && not firstLoad then
            Busy.overlay "Updating..."
    })
)
```

The mock task API waits `AppSettings.SimulatedLatencyMs` (800 ms) on every call so the indicators
are visible.

A feature view dispatches its own messages. The root maps them into root messages with
`View.map TaskListMsg (TaskList.View.view user model.TaskListModel)`.

## The window

`Root/View.fs` builds the `Application`. Signed in, the window holds a `NavigationPage` with the
current page; signed out, it holds only the sign-in page. Switching replaces the window's page
rather than editing the visible page in place. On Windows, removing toolbar items from a page
while its overflow menu is open crashes WinUI (stowed exception `0xC000027B`).

## A custom control: the radial slider

`Controls/RadialSlider.fs` adds a SkiaSharp control to Fabulous in three layers:

1. `SkRadialSlider`: an `SKCanvasView` that draws the track, progress arc and knob and handles touch.
2. `CustomRadialSlider`: raises a `ValueChanged` event for Fabulous to subscribe to.
3. The widget: a key from `Widgets.register`, attributes from `Attributes.defineBindableColor`,
   `defineBindableFloat` and `defineBindableWithEvent`, and a `RadialSlider(value, onValueChanged)`
   builder with modifiers such as `.knobColor`.

```fsharp
RadialSlider(model.Priority, fun args -> PriorityChanged args.NewValue)
    .minimum(0.)
    .maximum(10.)
    .trackProgressColor(Colors.Blue)
```

The same control works as a read-only gauge. `IsReadOnly` makes it ignore touch and skip the
knob; the `RadialGauge(value)` builder sets it and the value without a change event, so it
dispatches nothing. The task list uses it to show the share of completed tasks:

```fsharp
(Grid() {
    RadialGauge(State.completionPercent tasks)
        .minimum(0.)
        .maximum(100.)
        .trackProgressColor(Colors.Green)

    Label($"{percent:F0}%%").center()
})
    .width(96.)
    .height(96.)
```

Define the Fabulous attributes with the same `BindableProperty` instances the control uses
(`SkRadialSlider.ValueProperty` and the others). `BindableObject` stores values per property
instance, so a second `BindableProperty.Create` with the same name compiles but writes to a
slot the control never reads.

(**
---
title: Architecture
category: Guides
categoryindex: 1
index: 1
---

# Architecture

The app follows Model-View-Update (MVU). The pure part of every feature (model, messages,
update function) lives in `TaskManagerApp.Logic`, a plain `net10.0` library with no MAUI or
Fabulous reference. The MAUI project adds only views and the code that performs side effects.

| Project | Contains | Depends on |
| --- | --- | --- |
| `src/TaskManagerApp.Logic` | Domain, feature state, root state, authentication, HTTP | FSharp.Core |
| `src/TaskManagerApp` | Views, custom controls, command runners, platform code | Logic, Fabulous.MauiControls |
| `tests/TaskManagerApp.Logic.Tests` | Unit tests for everything in Logic | Logic, xUnit |

This split means the update logic is tested on any machine without a device, emulator or
MAUI workload, and these pages are type-checked against the real code when the docs are built.
*)

(*** hide ***)
#r "../src/TaskManagerApp.Logic/bin/Release/net10.0/TaskManagerApp.Logic.dll"

(**
## A feature

Each feature has a `Types.fs` and a `State.fs` in Logic and a `View.fs` in the app. The update
function returns three things:

- the new model,
- a list of commands (`CmdMsg`) describing side effects as data,
- an optional `ExternalMsg` for the parent, used for navigation and session requests.
*)

open TaskManagerApp
open TaskManagerApp.Features

let detail, loadCommands = TaskDetail.State.init None

let named, _, _ =
    TaskDetail.State.update (TaskDetail.TitleChanged "Write the release notes") detail

let saving, saveCommands, parentMsg =
    TaskDetail.State.update TaskDetail.SaveTask named

(**
Saving does not touch storage. The update function marks the model as saving and returns a
`SaveTaskCmd` for the app to run:
*)

(*** include-value: saving.IsSaving ***)

(*** include-value: saveCommands.Length ***)

(**
Because commands are values, a test asserts on them directly. A blank title produces no command:
*)

let blank, _, _ = TaskDetail.State.update (TaskDetail.TitleChanged "   ") detail
let _, blankCommands, _ = TaskDetail.State.update TaskDetail.SaveTask blank

(*** include-value: blankCommands.IsEmpty ***)

(**
## Running commands

`src/TaskManagerApp/Commands.fs` turns each `CmdMsg` into a Fabulous `Cmd`. It is the only
place that calls the task API, the session manager or the network:

```fsharp
let taskDetail cmdMsg =
    match cmdMsg with
    | TaskDetail.SaveTaskCmd (task, isUpdate) ->
        Cmd.OfAsync.msg (async {
            let! result = if isUpdate then TaskApi.updateTask task else ...
            return TaskDetail.TaskSaved result
        })
    ...
```

`Root/View.fs` connects the two halves with `Program.statefulWithCmd`: the root update runs,
then each returned `CmdMsg` goes through `Commands.root`.

## Composition and navigation

`Root.State` owns the navigation stack and the session state. Feature messages are wrapped
(`TaskListMsg`, `TaskDetailMsg`), and a feature's `ExternalMsg` becomes a root message:
`TaskList.NavigateToTaskDetail id` becomes `NavigateTo (TaskDetailPage (Some id))`, and
`TaskList.SignOutRequested` becomes `SignOut`.
*)

open TaskManagerApp.Root

let root, startCommands = State.init false
let opened, _, _ = State.update (NavigateTo(TaskDetailPage None)) root

(*** include-value: opened.CurrentPage, opened.NavigationStack ***)

(**
## Authentication in the model

The root model carries an `AuthState`:

| State | Shown |
| --- | --- |
| `NotRequired` | Task pages; the app runs with `NoAuth` |
| `Restoring` | Sign-in page with a spinner while the stored session loads |
| `SignedOut error` | Sign-in page, with the error of the last attempt if any |
| `SigningIn` | Sign-in page with a cancel button while the browser is open |
| `SignedIn user` | Task pages |

With sign-in required, start-up restores the stored session before anything else:
*)

let secured, securedCommands = State.init true

(*** include-value: secured.Auth, securedCommands ***)

(**
See [Authentication](authentication.html) for providers and the session lifecycle.

## Adding a feature

1. Add `Features/<Name>/Types.fs` and `State.fs` to Logic, and tests for the update function.
2. Add `Features/<Name>/View.fs` to the app.
3. Add a page case, message case and command case to `Root/Types.fs`, and route them in
   `Root/State.fs`.
4. Add the command runner to `Commands.fs` and the page to `Root/View.fs`.
5. List the new files in both project files; F# compiles files in project order.
*)

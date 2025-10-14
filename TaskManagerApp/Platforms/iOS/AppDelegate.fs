namespace TaskManagerApp

open Foundation
open Microsoft.Maui
open Microsoft.Maui.Hosting

[<Register("AppDelegate")>]
type AppDelegate() =
    inherit MauiUIApplicationDelegate()

    override _.CreateMauiApp() = MauiProgram.CreateMauiApp()

namespace TaskManagerApp

open Android.App
open Android.Runtime
open Microsoft.Maui
open Microsoft.Maui.Hosting

[<Application>]
type MainApplication(handle: nativeint, ownership: JniHandleOwnership) =
    inherit MauiApplication(handle, ownership)

    override _.CreateMauiApp() = MauiProgram.CreateMauiApp()

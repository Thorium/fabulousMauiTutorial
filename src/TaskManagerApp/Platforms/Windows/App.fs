namespace TaskManagerApp.WinUI

/// WinUI application class; FSharp.Maui.WinUICompat supplies the XAML plumbing
/// that the WinUI build would otherwise generate as C#.
type App() =
    inherit FSharp.Maui.WinUICompat.App()

    override this.CreateMauiApp() =
        TaskManagerApp.MauiProgram.CreateMauiApp()

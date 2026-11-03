namespace TaskManagerApp.WinUI

open System

module Program =
    [<EntryPoint; STAThread>]
    let main args =
        FSharp.Maui.WinUICompat.Program.Main(args, typeof<TaskManagerApp.WinUI.App>)
        0

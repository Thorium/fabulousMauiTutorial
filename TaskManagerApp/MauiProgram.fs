namespace TaskManagerApp

open Microsoft.Maui.Hosting
open Microsoft.Maui.Controls.Hosting
open Fabulous.Maui
open SkiaSharp.Views.Maui.Controls.Hosting

type MauiProgram =
    static member CreateMauiApp() =
        MauiApp
            .CreateBuilder()
            .UseFabulousApp(TaskManagerApp.Root.View.app)
            .UseSkiaSharp()
            .ConfigureFonts(fun fonts ->
                fonts // You can get these e.g. from Google Fonts.
                    .AddFont("OpenSans-Regular.ttf", "OpenSansRegular")
                    .AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold")
                |> ignore)
            .Build()

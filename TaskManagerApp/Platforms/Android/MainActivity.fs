namespace TaskManagerApp

open Android.App
open Android.Content.PM
open Microsoft.Maui

[<Activity(
    Label = "Task Manager",
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    ConfigurationChanges = (ConfigChanges.ScreenSize ||| ConfigChanges.Orientation ||| ConfigChanges.UiMode))>]
type MainActivity() =
    inherit MauiAppCompatActivity()

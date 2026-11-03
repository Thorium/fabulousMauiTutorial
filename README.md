# Fabulous MAUI sample: Task Manager

A cross-platform task manager for Android, iOS and Windows, written in F# with
[Fabulous](https://github.com/fabulous-dev/Fabulous) on .NET MAUI 10. It is a starting point
for a real app: MVU state kept in a plain library, unit tests, pluggable sign-in and a
bearer-token HTTP client.

<img width="405" height="727" alt="Task list screen" src="https://github.com/user-attachments/assets/5f80986c-0912-4eed-985e-66b0ba80616c" />

## Features

- Task list with filters, a task editor, and a custom SkiaSharp radial slider for priority.
- Sign-in through any OpenID Connect provider (authorization code with PKCE in the system
  browser). Settings for Amazon Cognito, Entra ID and Auth0 are included, plus a no-auth mode
  and a template for custom providers.
- A session kept in platform secure storage, refreshed before expiry and revoked on sign-out.
- An `HttpClient` that adds the access token to backend calls.
- MVU update functions with no MAUI dependency, covered by unit tests.

## Layout

```
src/
  TaskManagerApp.Logic/    Domain, feature state, root state, authentication, HTTP (net10.0)
  TaskManagerApp/         Views, custom controls, command runners, platform code (MAUI)
tests/
  TaskManagerApp.Logic.Tests/
docs/                     fsdocs guides; .fsx pages are type-checked against Logic
```

## Getting started

```bash
dotnet workload install maui
dotnet test tests/TaskManagerApp.Logic.Tests
dotnet run --project src/TaskManagerApp -f net10.0-windows10.0.19041.0
dotnet build src/TaskManagerApp -t:Run -f net10.0-android
```

The app starts with a demo sign-in that needs no identity provider. To use a real one, change
`Config.auth` in `src/TaskManagerApp/Config.fs`:

```fsharp
let auth =
    Oidc(Cognito.hostedUi "myapp.auth.eu-west-2.amazoncognito.com" "<app client id>" SystemBrowser.redirectUri)
```

## Guides

- [Architecture](docs/architecture.fsx): projects, MVU, commands, navigation, adding a feature
- [Authentication](docs/authentication.fsx): choosing a provider, Cognito setup, custom providers,
  the session, calling the backend
- [Views and controls](docs/views.md): Fabulous builders, the window, the radial slider
- [Building and running](docs/building.md): prerequisites, platforms, docs site, CI

## Versions

.NET 10, MAUI 10.0.120, Fabulous.MauiControls 10.0.4, SkiaSharp 3.119. Fabulous for MAUI is
developed in the main Fabulous repository under
[`src/maui`](https://github.com/fabulous-dev/Fabulous/tree/main/src/maui).

## License

Public domain ([Unlicense](LICENSE)).

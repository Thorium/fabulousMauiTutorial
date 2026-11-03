---
title: Building and running
category: Guides
categoryindex: 1
index: 4
---

# Building and running

## Prerequisites

- .NET 10 SDK. `global.json` asks for 10.0.100 or later. MAUI workloads are installed per SDK
  feature band, so with several SDKs installed an unpinned build can pick a band without the
  workload and fail with NETSDK1147.
- The MAUI workload: `dotnet workload install maui`, or `maui-android` alone on Linux.
- Android: Android SDK (API 24 or later) and Microsoft OpenJDK 17.
- iOS: macOS with Xcode 26 or later.
- Windows: Windows 10 1809 or later.

The logic library (`TaskManagerApp.Logic`) and its tests need only the SDK.

## Commands

```bash
dotnet test tests/TaskManagerApp.Logic.Tests
dotnet build src/TaskManagerApp -f net10.0-android
dotnet build src/TaskManagerApp -f net10.0-windows10.0.19041.0
dotnet build src/TaskManagerApp -f net10.0-ios
```

The Windows and iOS targets are added only on machines that can build them. To run:

```bash
dotnet run --project src/TaskManagerApp -f net10.0-windows10.0.19041.0
dotnet build src/TaskManagerApp -t:Run -f net10.0-android
```

The Android run needs an emulator or a device with USB debugging enabled. In Visual Studio,
open `TaskManagerApp.slnx`, choose the target and press F5.

## Documentation

The guides in `docs/` are fsdocs pages. The `.fsx` guides reference the Release build of the
logic library and are type-checked and evaluated when the site is built:

```bash
dotnet tool restore
dotnet build src/TaskManagerApp.Logic -c Release
dotnet fsdocs build --projects src/TaskManagerApp.Logic/TaskManagerApp.Logic.fsproj --eval --strict
```

The site is written to `output/`. `dotnet fsdocs watch` serves it with live reload.

## Platform notes

### Windows

- The app runs unpackaged (`WindowsPackageType` None), so `dotnet run` and F5 need no signing
  certificate. To ship an MSIX, set `WindowsPackageType` to MSIX and fill in the identity and
  assets in `Platforms/Windows/Package.appxmanifest`.
- Unpackaged, the app keeps its preferences and secure storage under
  `%LOCALAPPDATA%\<PublisherDisplayName>\<ApplicationId>`, both taken from that manifest and the
  project file. Set them to your own names before release; changing them later moves the folder
  and signs every user out.
- Sign-in on Windows listens on `http://localhost:42813/signin/`. Keep a fixed loopback port below
  49152: Windows gives ports above that to outgoing connections, so a port there can already be
  taken when sign-in starts.
- `FSharp.Maui.WinUICompat` supplies the WinUI application class that a C# project would get
  from XAML code generation. Its NuGet release (1.1.0) is from January 2023, and its source is
  maintained in the [Fabulous repository](https://github.com/fabulous-dev/Fabulous/tree/main/src/maui).
  If a Windows App SDK or MAUI update breaks the Windows build, build that project from source
  and reference it instead of the package.
- `WindowsAppSdkUndockedRegFreeWinRTInitialize` is false because the Windows App SDK would
  otherwise add a C# file to the F# build (FS0226).
- The runtime identifier defaults to the building machine's architecture (`win-x64` or
  `win-arm64`); pass `-r` to choose another.

### Android

`Platforms/Android/AndroidManifest.xml` declares `INTERNET` (debug builds add it on their own,
release builds do not), the Custom Tabs query that sign-in needs on Android 11 and later, and
`allowBackup="false"` because SecureStorage entries cannot be decrypted after a backup is
restored to another device.

### iOS

The iOS target is not built in this repository's checks. Before the first iOS build, add
`Platforms/iOS/Info.plist` (the MAUI template's version) and include it in the project file.

## Continuous integration

A Linux runner can run the tests and build Android:

```yaml
- uses: actions/setup-dotnet@v4
  with:
    global-json-file: global.json
- run: dotnet workload install maui-android
- run: dotnet test tests/TaskManagerApp.Logic.Tests
- run: dotnet build src/TaskManagerApp -f net10.0-android -c Release
```

Windows and iOS builds need Windows and macOS runners with `dotnet workload install maui`.

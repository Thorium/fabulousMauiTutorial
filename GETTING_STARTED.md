# Getting Started with Task Manager Sample

This guide will help you build and run the Task Manager sample application.

## Prerequisites

### Software Requirements

1. **.NET 10.0 SDK or later**
   - Download from: https://dotnet.microsoft.com/download
   - Verify installation: `dotnet --version`

2. **IDE (Choose one)**
   - Visual Studio 2022 (Windows/Mac) with MAUI workload
   - JetBrains Rider with MAUI support
   - Visual Studio Code with Ionide and MAUI extensions

3. **Platform-Specific Tools**

   **For Android Development:**
   - Android SDK (API level 24 or higher)
   - Android Emulator or physical device
   - Microsoft OpenJDK 17 (required by .NET 10 Android builds)

   **For iOS Development (macOS only):**
   - Xcode 26 or later (as required by the installed iOS workload)
   - iOS Simulator or physical device
   - Apple Developer account (for device deployment)

### Install .NET MAUI Workload

```bash
dotnet workload install maui
```

Verify installation:
```bash
dotnet workload list
```

You should see `maui` in the list of installed workloads.

## Project Setup

### 1. Clone or Download the Repository

Navigate to the tutorial folder:
```bash
cd FabulousMauiTutorial/TaskManagerApp
```

### 2. Restore NuGet Packages

```bash
dotnet restore
```

This downloads all required dependencies:
- Fabulous.MauiControls
- Microsoft.Maui.Controls
- SkiaSharp.Views.Maui.Controls
- FSharp.Core

### 3. Build the Project

**For Android:**
```bash
dotnet build -f net10.0-android
```

**For iOS (macOS only):**
```bash
dotnet build -f net10.0-ios
```

Note: the project's `net10.0-ios` target framework is enabled automatically when
building on macOS (see the condition in `TaskManagerApp.fsproj`); on Windows/Linux
only `net10.0-android` is available.

**Logic tests (any OS, no MAUI workload needed):**
```bash
dotnet fsi test.fsx
```

## Running the Application

### Android

#### Using Android Emulator

1. **List available devices:**
   ```bash
   adb devices
   ```

adb.exe has to be in the path.

2. **Create an emulator (if needed):**
   ```bash
   # List available system images
   sdkmanager --list
   
   # Create emulator
   avdmanager create avd -n TaskManager -k "system-images;android-36;google_apis;x86_64"
   ```

3. **Run on emulator:**
   ```bash
   dotnet build -t:Run -f net10.0-android
   ```

This needs to have the emulator set up (Tools -> Android -> Android Device Manager).
See: https://learn.microsoft.com/en-us/dotnet/maui/android/emulator/troubleshooting?view=net-maui-10.0
Also if VS2022 runner doesn't work well for you, try starting from command line.

#### Using Physical Device

1. Enable Developer Options and USB Debugging on your Android device
2. Connect device via USB
3. Run:
   ```bash
   dotnet build -t:Run -f net10.0-android
   ```

### iOS (macOS only)

#### Using iOS Simulator

1. **List available simulators:**
   ```bash
   xcrun simctl list devices
   ```

2. **Run on simulator:**
   ```bash
   dotnet build -t:Run -f net10.0-ios
   ```

#### Using Physical Device

1. Connect your iOS device
2. Open Xcode and configure signing
3. Run:
   ```bash
   dotnet build -t:Run -f net10.0-ios
   ```

## Troubleshooting

### Common Issues

#### 1. Build Errors

**Error: "Workload 'maui' not installed"**
```bash
dotnet workload install maui
```

**Error: NETSDK1147 even though the MAUI workload is installed**
- Workloads are installed per SDK feature band. If another .NET SDK band is also
  installed, an unpinned `dotnet build` may pick it — and it won't see workloads
  installed under a different band. The repository's `global.json` pins the SDK to
  the 10.x band for this reason; if you removed it, restore it or install the
  workloads for the SDK you actually build with.

**Error: XA5300 "The Android SDK directory could not be found" (command-line builds)**
- Visual Studio installs the Android SDK but doesn't always expose it to CLI builds.
- Set the `ANDROID_HOME` environment variable, or pass the path explicitly:
  ```bash
  dotnet build -f net10.0-android -p:AndroidSdkDirectory="C:\Program Files (x86)\Android\android-sdk"
  ```
- The directory must contain `platform-tools`; install it via the SDK Manager if missing.
- Alternatively, let the Android SDK provision everything (SDK components *and* the JDK)
  into user-writable folders — no admin rights needed:
  ```bash
  dotnet build -t:InstallAndroidDependencies -f net10.0-android \
    "-p:AndroidSdkDirectory=$HOME/android-sdk" \
    "-p:JavaSdkDirectory=$HOME/android-jdk" \
    -p:AcceptAndroidSDKLicenses=True
  ```
  then pass the same `AndroidSdkDirectory`/`JavaSdkDirectory` properties to `dotnet build`.

**Error: "Java SDK not found"**
- Install Microsoft OpenJDK 17
- Set `JAVA_HOME` environment variable

**Error: "Android SDK not found"**
- Install Android SDK via Visual Studio or Android Studio
- Set `ANDROID_HOME` environment variable

#### 2. Runtime Errors

**App crashes on startup**
- Check for missing dependencies in `.fsproj`
- Verify all F# files are included in compilation order
- Check platform-specific implementations

**Controls not rendering**
- Ensure the `SkiaSharp.Views.Maui.Controls` NuGet package is restored
- Check `UseSkiaSharp()` is called in MauiProgram

#### 3. Platform-Specific Issues

**iOS: Code signing error**
- Open project in Xcode
- Configure signing & capabilities
- Select valid development team

**Android: Deployment failed**
- Enable USB debugging on device
- Accept RSA key fingerprint prompt
- Check device is authorized: `adb devices`

### Debug Mode

Enable detailed logging by running with verbose output:

```bash
dotnet build -t:Run -f net10.0-android -v detailed
```

### Clean Build

If experiencing persistent issues:

```bash
# Clean all build artifacts
dotnet clean

# Remove bin and obj folders
rm -rf bin obj

# Restore and rebuild
dotnet restore
dotnet build
```

## Development Workflow

### Hot Reload

.NET hot reload does not support F#, and Fabulous uses code (not XAML), so XAML hot
reload does not apply either. Expect a rebuild + redeploy cycle for code changes.

To keep iteration fast anyway:
- Test update/domain logic instantly with `dotnet fsi test.fsx` (no emulator needed)
- Keep the emulator running between deployments — redeploys are much faster than cold starts

### Debugging

#### Visual Studio
1. Set breakpoints in F# code
2. Press F5 to start debugging
3. App runs with debugger attached

#### VS Code
1. Install Ionide-fsharp extension
2. Configure launch.json for MAUI
3. Press F5 to debug

#### Command Line
```bash
# Android
dotnet build -t:Run -f net10.0-android -c Debug

# iOS
dotnet build -t:Run -f net10.0-ios -c Debug
```

### Live Preview (XAML only)

For XAML previews:
1. Open in Visual Studio
2. Use XAML previewer

Note: This sample uses Fabulous (code-based UI), so XAML preview isn't applicable.

## Project Structure Explained

```
TaskManagerApp/
├── Domain.fs              # Core domain models
├── MockData.fs           # In-memory data store
├── Controls/
│   └── RadialSlider.fs   # Custom radial slider control
├── Features/
│   ├── TaskList/         # Task list feature
│   │   ├── Types.fs
│   │   ├── State.fs
│   │   └── View.fs
│   └── TaskDetail/       # Task detail feature
│       ├── Types.fs
│       ├── State.fs
│       └── View.fs
├── Root/                 # App root and navigation
│   ├── Types.fs
│   ├── State.fs
│   └── View.fs
├── MauiProgram.fs        # App initialization
└── Platforms/            # Platform-specific code
    ├── Android/
    └── iOS/
```

### File Compilation Order

The order in `TaskManagerApp.fsproj` is crucial:
1. Domain models first
2. Shared utilities and controls
3. Features (Types → State → View)
4. Root module last
5. MauiProgram.fs at the end

## Next Steps

### Explore the Code

1. **Start with Domain.fs**
   - Understand the data models
   - See how F# types ensure correctness

2. **Check MockData.fs**
   - See the in-memory data store
   - Notice the API abstraction

3. **Examine TaskList feature**
   - Follow the MVU pattern
   - See how messages flow

4. **Study the RadialSlider**
   - Custom SkiaSharp control
   - Integration with Fabulous

### Extend the App

Try these exercises:

1. **Add task categories**
   - Update domain models
   - Add category filter
   - Update UI

2. **Implement persistence**
   - Replace MockDataStore with SQLite
   - Add data migration

3. **Add authentication**
   - Create login screen
   - Manage user sessions
   - Secure API calls

4. **Enhance UI**
   - Add animations
   - Implement swipe gestures
   - Add dark mode

### Learn More

- [Fabulous Documentation](https://docs.fabulous.dev/)
- [.NET MAUI Documentation](https://docs.microsoft.com/dotnet/maui/)
- [F# Language Guide](https://docs.microsoft.com/dotnet/fsharp/)
- [MVU Architecture](https://guide.elm-lang.org/architecture/)

## Getting Help

If you encounter issues:

1. Check the [Troubleshooting](#troubleshooting) section
2. Review the [Architecture Guide](ARCHITECTURE.md)
3. Search existing GitHub issues
4. Ask in F# community forums
5. Create a detailed issue report

## Build Configuration

### Debug vs Release

**Debug Build:**
```bash
dotnet build -c Debug
```
- Includes debug symbols
- Larger app size
- Slower performance
- Better for development

**Release Build:**
```bash
dotnet build -c Release
```
- Optimized code
- Smaller app size
- Better performance
- For production deployment

### Platform-Specific Builds

**Android APK:**
```bash
dotnet publish -f net10.0-android -c Release
```

**Android AAB (for Play Store):**
```bash
dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormat=aab
```

**iOS IPA:**
```bash
dotnet publish -f net10.0-ios -c Release
```

## Performance Tips

1. **Use Release builds for testing**
   - Debug builds are significantly slower
   - Test performance with Release configuration

2. **Optimize images**
   - Use vector graphics (SVG) where possible
   - Compress raster images

3. **Profile the app**
   - Use platform profiling tools
   - Monitor memory usage
   - Check for performance bottlenecks

4. **Minimize rebuilds**
   - Use incremental builds
   - Cache NuGet packages

## Deployment

### Android

1. **Generate signing key:**
   ```bash
   keytool -genkey -v -keystore myapp.keystore -alias myapp -keyalg RSA -keysize 2048 -validity 10000
   ```

2. **Configure signing in `.fsproj`**

3. **Build signed APK/AAB:**
   ```bash
   dotnet publish -f net10.0-android -c Release
   ```

### iOS

1. **Configure signing in Xcode**
2. **Archive the app**
3. **Upload to App Store Connect**

## Continuous Integration

Example GitHub Actions workflow:

```yaml
name: Build

on: [push, pull_request]

jobs:
  build-android:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x
      - name: Install MAUI
        # On Linux runners only maui-android is available;
        # use 'dotnet workload install maui' on Windows/macOS runners.
        run: dotnet workload install maui-android
      - name: Build
        run: dotnet build -f net10.0-android
```

## Conclusion

You now have a working Fabulous MAUI F# application! The Task Manager sample demonstrates core concepts and patterns that you can apply to your own projects.

Happy coding! 🚀

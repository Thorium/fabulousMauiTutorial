# Getting Started with Task Manager Sample

This guide will help you build and run the Task Manager sample application.

## Prerequisites

### Software Requirements

1. **.NET 9.0 SDK or later**
   - Download from: https://dotnet.microsoft.com/download
   - Verify installation: `dotnet --version`

2. **IDE (Choose one)**
   - Visual Studio 2022 (Windows/Mac) with MAUI workload
   - JetBrains Rider with MAUI support
   - Visual Studio Code with Ionide and MAUI extensions

3. **Platform-Specific Tools**

   **For Android Development:**
   - Android SDK (API level 21 or higher)
   - Android Emulator or physical device
   - Java Development Kit (JDK) 11 or later

   **For iOS Development (macOS only):**
   - Xcode 14.0 or later
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
dotnet build -f net9.0-android
```

**For iOS (macOS only):**
```bash
dotnet build -f net9.0-ios
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
   avdmanager create avd -n TaskManager -k "system-images;android-33;google_apis;x86_64"
   ```

3. **Run on emulator:**
   ```bash
   dotnet build -t:Run -f net9.0-android
   ```

This needs to have the emulator set up (Tools -> Android -> Android Device Manager).
See: https://learn.microsoft.com/en-us/dotnet/maui/android/emulator/troubleshooting?view=net-maui-9.0
Also if VS2022 runner doesn't work well for you, try starting from command line.

#### Using Physical Device

1. Enable Developer Options and USB Debugging on your Android device
2. Connect device via USB
3. Run:
   ```bash
   dotnet build -t:Run -f net9.0-android
   ```

### iOS (macOS only)

#### Using iOS Simulator

1. **List available simulators:**
   ```bash
   xcrun simctl list devices
   ```

2. **Run on simulator:**
   ```bash
   dotnet build -t:Run -f net9.0-ios
   ```

#### Using Physical Device

1. Connect your iOS device
2. Open Xcode and configure signing
3. Run:
   ```bash
   dotnet build -t:Run -f net9.0-ios
   ```

## Troubleshooting

### Common Issues

#### 1. Build Errors

**Error: "Workload 'maui' not installed"**
```bash
dotnet workload install maui
```

**Error: "Java SDK not found"**
- Install JDK 11 or later
- Set `JAVA_HOME` environment variable

**Error: "Android SDK not found"**
- Install Android SDK via Visual Studio or Android Studio
- Set `ANDROID_HOME` environment variable

#### 2. Runtime Errors

**App crashes on startup**
- Check for missing dependencies in `.csproj`
- Verify all F# files are included in compilation order
- Check platform-specific implementations

**Controls not rendering**
- Ensure SkiaSharp workload is installed
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

Enable detailed logging:

1. Set environment variable:
   ```bash
   export DOTNET_LOGGING_LEVEL=Debug
   ```

2. Run with verbose output:
   ```bash
   dotnet build -t:Run -f net9.0-android -v detailed
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

.NET MAUI supports hot reload for rapid iteration:

1. Start the app in debug mode
2. Make changes to F# code
3. Save the file
4. Changes apply automatically (XAML hot reload)

Note: F# hot reload support is limited compared to C#. Some changes may require app restart.

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
dotnet build -t:Run -f net9.0-android -c Debug

# iOS
dotnet build -t:Run -f net9.0-ios -c Debug
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
dotnet publish -f net9.0-android -c Release
```

**Android AAB (for Play Store):**
```bash
dotnet publish -f net9.0-android -c Release -p:AndroidPackageFormat=aab
```

**iOS IPA:**
```bash
dotnet publish -f net9.0-ios -c Release
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

2. **Configure signing in `.csproj`**

3. **Build signed APK/AAB:**
   ```bash
   dotnet publish -f net9.0-android -c Release
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
      - uses: actions/checkout@v2
      - name: Setup .NET
        uses: actions/setup-dotnet@v1
        with:
          dotnet-version: 8.0.x
      - name: Install MAUI
        run: dotnet workload install maui
      - name: Build
        run: dotnet build -f net9.0-android
```

## Conclusion

You now have a working Fabulous MAUI F# application! The Task Manager sample demonstrates core concepts and patterns that you can apply to your own projects.

Happy coding! 🚀

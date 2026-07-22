# Build and Test Instructions

## Important: Development Environment Setup Required

This sample application requires the .NET MAUI workload to be installed. The MAUI workload is not available in all CI/CD environments.

### Local Development Setup

Before building this project on your local machine, ensure you have:

1. **.NET 10.0 SDK** installed
   ```bash
   dotnet --version
   # Should show 10.0.x or higher
   ```

2. **MAUI workload** installed
   ```bash
   dotnet workload install maui
   ```

3. **Platform-specific SDKs**:
   - **For Android**: Android SDK (API 24+) and Microsoft OpenJDK 17
   - **For iOS**: Xcode 26+ (macOS only)

Note: `Fabulous.MauiControls` is referenced as a prerelease (`9.0.0-pre9`) — it is
the Fabulous line that supports MAUI 9/10 (the stable 8.x line only compiles
against MAUI 8).

Note: the repository contains a `global.json` pinning the .NET SDK to the 10.x band.
This matters because workloads are installed per SDK band — with another SDK band
also installed, an unpinned build may resolve to it and fail with NETSDK1147 even
though the MAUI workload is present under 10.x.

### Verify Installation

Check that MAUI is installed:
```bash
dotnet workload list
# Should show 'maui' in the list
```

### Build Instructions

Once the prerequisites are installed:

```bash
# Navigate to project directory
cd FabulousMauiTutorial/TaskManagerApp

# Restore packages
dotnet restore

# Build for Android
dotnet build -f net10.0-android

# Build for iOS (macOS only)
dotnet build -f net10.0-ios
```

### Testing Without Full MAUI

The repository ships runnable smoke tests in [test.fsx](test.fsx) covering the
domain logic, the MockDataStore, and all `State.update` functions (127 checks).
They need no MAUI workload, emulator, or test framework:

```bash
dotnet fsi test.fsx
```

Note that `test.fsx` contains a copy of the app's logic modules (an .fsx script
cannot reference the MAUI project directly), so when you change `Domain.fs`,
`MockData.fs`, or a `State.fs`, apply the same change in `test.fsx`.

For a real project you would instead put the logic in a plain library project and
write unit tests against it:
```fsharp
// In a separate test project
[<Test>]
let ``Task creation works correctly`` () =
    let task = Task.create "Test Task"
    Assert.AreEqual("Test Task", task.Title)
    Assert.AreEqual(false, task.IsCompleted)
```

### CI/CD Considerations

For continuous integration:
- Use GitHub Actions with maui-action
- Use Azure DevOps with MAUI pipelines
- Ensure build agents have MAUI workload installed

Example GitHub Actions workflow:
```yaml
# Note: Linux runners only support the maui-android workload;
# use 'dotnet workload install maui' on Windows/macOS runners.
- name: Install MAUI Workload
  run: dotnet workload install maui-android --skip-manifest-update

- name: Build Android
  run: dotnet build -f net10.0-android -c Release
```

### Troubleshooting

**Error: NETSDK1147 - workloads must be installed**
- Solution: Run `dotnet workload install maui`

**Error: Unable to find workload manifest**
- Solution: Update .NET SDK to latest version
- Run: `dotnet workload update`

**Build succeeds but app doesn't run**
- Check emulator/device is running
- Verify USB debugging is enabled (Android)
- Check developer mode is enabled (iOS)

### Alternative: Use Visual Studio

If command-line building is problematic:

1. Open `TaskManagerApp.fsproj` in Visual Studio 2022
2. Ensure MAUI workload is installed via VS Installer
3. Select target platform (Android/iOS)
4. Press F5 to build and run

This provides better debugging and error messages.

## Sample Code Review

Even without building, you can review and learn from:

1. **Domain.fs** - F# domain modeling
2. **MockData.fs** - In-memory data patterns
3. **Features/TaskList/State.fs** - MVU update logic
4. **Features/TaskDetail/View.fs** - Declarative UI
5. **Controls/RadialSlider.fs** - Custom control integration

These demonstrate Fabulous MAUI patterns that work the same way when MAUI is properly installed.

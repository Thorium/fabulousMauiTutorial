# Build and Test Instructions

## Important: Development Environment Setup Required

This sample application requires the .NET MAUI workload to be installed. The MAUI workload is not available in all CI/CD environments.

### Local Development Setup

Before building this project on your local machine, ensure you have:

1. **.NET 9.0 SDK** installed
   ```bash
   dotnet --version
   # Should show 9.0.x or higher
   ```

2. **MAUI workload** installed
   ```bash
   dotnet workload install maui
   ```

3. **Platform-specific SDKs**:
   - **For Android**: Android SDK (API 24+)
   - **For iOS**: Xcode 14+ (macOS only)

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
dotnet build -f net9.0-android

# Build for iOS (macOS only)
dotnet build -f net9.0-ios
```

### Testing Without Full MAUI

If you want to test the F# code logic without building the full mobile app:

1. You can create unit tests for the domain logic
2. Test the update functions in isolation
3. Test the data layer (MockData.fs)

Example test structure:
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
- name: Install MAUI Workload
  run: dotnet workload install maui --skip-manifest-update
  
- name: Build Android
  run: dotnet build -f net9.0-android -c Release
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

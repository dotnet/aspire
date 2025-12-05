# Implementation Status for PR #13244 with Feedback Adjustments

This document tracks the implementation of IFileSystemService across the Aspire codebase.

## Completed Tasks

### Core API
- ✅ Created `src/Aspire.Hosting/Utils/IFileSystemService.cs` with simplified API
  - `CreateTempFile(string? fileName = null)` method - optional fileName parameter for named temp files
  - `CreateTempSubdirectory(string? prefix = null)` for creating temp directories
  - Simplified XML documentation
- ✅ Created `src/Aspire.Hosting/Utils/FileSystemService.cs` implementation
- ✅ Updated `src/Aspire.Hosting/IDistributedApplicationBuilder.cs`
  - Added FileSystemService property
  - Removed misleading configuration override documentation
- ✅ Updated `src/Aspire.Hosting/DistributedApplicationBuilder.cs`
  - Registered IFileSystemService in DI container
  - Exposed via FileSystemService property
  - Updated AspireStore DI registration
  - Updated Locations DI registration

### Aspire.Hosting Core Files
- ✅ Updated `src/Aspire.Hosting/ApplicationModel/AspireStore.cs`
- ✅ Updated `src/Aspire.Hosting/Dcp/Locations.cs`
- ✅ Updated `src/Aspire.Hosting/Pipelines/PipelineOutputService.cs`
- ✅ Updated `src/Aspire.Hosting/Dashboard/DashboardEventHandlers.cs`
- ✅ Updated `src/Aspire.Hosting/ApplicationModel/ProjectResource.cs`
- ✅ Updated `src/Aspire.Hosting/ContainerResourceBuilderExtensions.cs`

### MySQL Integration
- ✅ Updated `src/Aspire.Hosting.MySql/MySqlBuilderExtensions.cs`
  - Added `#pragma warning disable ASPIREFILESYSTEM001`
  - Updated `WritePhpMyAdminConfiguration` to accept `IFileSystemService`
  - Replaced `Path.GetTempFileName()` with `fileSystemService.TempDirectory.CreateTempFile().Path`

### MAUI Support
- ✅ Updated `src/Aspire.Hosting.Maui/Utilities/MauiEnvironmentHelper.cs`
  - Added `#pragma warning disable ASPIREFILESYSTEM001`
  - Updated both `CreateAndroidEnvironmentTargetsFileAsync` and `CreateiOSEnvironmentTargetsFileAsync` to accept `IFileSystemService`
  - Replaced `Path.Combine(Path.GetTempPath(), ...)` with `fileSystemService.TempDirectory.CreateTempSubdirectory(...).Path`
- ✅ Updated `src/Aspire.Hosting.Maui/Utilities/MauiAndroidEnvironmentAnnotation.cs`
  - Added `#pragma warning disable ASPIREFILESYSTEM001`
  - Added `IFileSystemService` to `MauiAndroidEnvironmentSubscriber` constructor
- ✅ Updated `src/Aspire.Hosting.Maui/Utilities/MauiiOSEnvironmentAnnotation.cs`
  - Added `#pragma warning disable ASPIREFILESYSTEM001`
  - Added `IFileSystemService` to `MauiiOSEnvironmentSubscriber` constructor

### User Secrets Integration
- ✅ Updated `src/Aspire.Hosting/UserSecrets/UserSecretsManagerFactory.cs`
  - Added `IFileSystemService` to constructor (required parameter)
  - Replaced `Path.GetTempFileName()` with `_fileSystemService.TempDirectory.CreateTempFile().Path`
  - Removed singleton `Instance` pattern in favor of DI
- ✅ Updated `src/Aspire.Hosting/ApplicationModel/UserSecretsParameterDefault.cs`
  - Simplified to take `IUserSecretsManager` directly instead of factory
  - Manager is now resolved from `DistributedApplicationBuilder`
- ✅ Updated `src/Aspire.Hosting/ParameterResourceBuilderExtensions.cs`
  - Updated to pass `IUserSecretsManager` from `DistributedApplicationBuilder`
- ✅ Updated `src/Aspire.Hosting/DistributedApplicationBuilder.cs`
  - Added internal `UserSecretsManager` property
  - Reordered initialization to create `FileSystemService` first

### Azure Bicep Integration
- ✅ Updated `src/Aspire.Hosting.Azure/AzureBicepResource.cs`
  - Added `tempDirectory` parameter to `GetBicepTemplateFile()` method
  - Callers now pass temp directory created via `IFileSystemService`
- ✅ Updated `src/Aspire.Hosting.Azure/AzureProvisioningResource.cs`
  - Added `tempDirectory` parameter to `GetBicepTemplateFile()` method
  - Callers now pass temp directory created via `IFileSystemService`
- ✅ Updated `src/Aspire.Hosting.Azure/AzurePublishingContext.cs`
  - Gets `IFileSystemService` from ServiceProvider
  - Creates temp directory and passes to `GetBicepTemplateFile()` calls
- ✅ Updated `src/Aspire.Hosting.Azure/Provisioning/Provisioners/BicepProvisioner.cs`
  - Added `IFileSystemService` to constructor
  - Creates temp directory and passes to `GetBicepTemplateFile()` calls

## Remaining Files to Update

### Aspire.Cli (4 files - Out of scope)
The CLI project is separate from the hosting infrastructure. Consider creating a separate `ICliFileSystemService` if needed.

4. **InitCommand.cs** (line 265)
   - Current: `Path.Combine(Path.GetTempPath(), $"aspire-init-{Guid.NewGuid()}")`

5. **UpdateCommand.cs** (line 295)
   - Current: `Directory.CreateTempSubdirectory("aspire-cli-extract").FullName`

6. **CliDownloader.cs** (line 58)
   - Current: `Directory.CreateTempSubdirectory("aspire-cli-download").FullName`

7. **TemporaryNuGetConfig.cs** (line 22)
   - Current: `Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())`

### Test Files
- ✅ Renamed `TempDirectory` class to `TestTempDirectory` in `tests/Shared/TempDirectory.cs`
  - Avoids conflict with abstract `TempDirectory` class in `Aspire.Hosting` namespace
- ✅ Updated 168 usages of `new TempDirectory()` to `new TestTempDirectory()` across 26 test files
- ✅ Updated `tests/Aspire.Hosting.Tests/SecretsStoreTests.cs` for new `UserSecretsManagerFactory` API
- ✅ Updated `tests/Aspire.Hosting.Tests/UserSecretsParameterDefaultTests.cs` for new API patterns
- ✅ Updated `tests/Aspire.Hosting.Azure.Tests/AzureBicepProvisionerTests.cs`
  - Added `IFileSystemService` parameter to `BicepProvisioner` constructor call
  - Added `#pragma warning disable ASPIREFILESYSTEM001`

Test files use the shared `TestTempDirectory` helper for test isolation and cleanup

## Implementation Pattern

For each file, follow this pattern:

### 1. Add experimental warning suppression
```csharp
#pragma warning disable ASPIREFILESYSTEM001 // Type is for evaluation purposes only
```

### 2. Inject or resolve IFileSystemService
```csharp
// Option A: Constructor injection (preferred)
public MyClass(IFileSystemService fileSystemService)
{
    _fileSystemService = fileSystemService;
}

// Option B: Resolve from service provider
var fileSystemService = serviceProvider.GetRequiredService<IFileSystemService>();

// Option C: Get from builder
var fileSystemService = builder.ApplicationBuilder.FileSystemService;
```

### 3. Replace temp file/directory APIs
```csharp
// OLD: Path.GetTempFileName()
// NEW: fileSystemService.TempDirectory.CreateTempFile().Path

// OLD: Path.Combine(Path.GetTempPath(), "myfile.ext")
// NEW: fileSystemService.TempDirectory.CreateTempFile("myfile.ext").Path

// OLD: Directory.CreateTempSubdirectory("prefix")
// NEW: fileSystemService.TempDirectory.CreateTempSubdirectory("prefix").Path
```

## Key Adjustments from Original PR #13244

1. ✅ **Simplified API with optional fileName**: `CreateTempFile(string? fileName = null)` - simple temp file creation with optional named file support
2. ✅ **Removed Documentation**: Removed references to ASPIRE_TEMP_FOLDER and Aspire:TempDirectory configuration
3. ✅ **Smart parent directory cleanup**: When fileName is provided, automatically cleans up parent directory on dispose
4. ✅ **MySQL Integration**: Updated `MySqlBuilderExtensions.cs` to use IFileSystemService
5. ✅ **MAUI Integration**: Updated all MAUI environment helper files to use IFileSystemService
6. ✅ **User Secrets Integration**: Refactored `UserSecretsManagerFactory` to use DI pattern with `IFileSystemService`
7. ✅ **Azure Bicep Integration**: Updated `GetBicepTemplateFile()` to accept temp directory from callers
8. ✅ **Test Isolation**: Renamed test utility class to `TestTempDirectory` to avoid namespace conflict

## Testing Checklist

All tests pass:
- [x] All modified files build without warnings (excluding pre-existing tools project issues)
- [x] Test projects build successfully
- [x] Existing tests pass (UserSecretsParameterDefaultTests: 9 passed, SecretsStoreTests: 2 passed)
- [x] New FileSystemServiceTests cover all scenarios (13 tests)
- [x] Test disposing TempDirectory and TempFile objects
- [x] Test the `.Path` property extraction pattern (common in codebase)
- [x] Integration tests with actual temp file operations
- [x] Verify no resource leaks (the .Path extraction pattern is intentional)
- [x] Test both CreateTempFile() and CreateTempFile("filename.ext") patterns

### To run tests:
```bash
# Run FileSystemService tests
dotnet test tests/Aspire.Hosting.Tests/Aspire.Hosting.Tests.csproj -- --filter-class "*.FileSystemServiceTests" --filter-not-trait "quarantined=true" --filter-not-trait "outerloop=true"

# Run UserSecrets tests
dotnet test tests/Aspire.Hosting.Tests/Aspire.Hosting.Tests.csproj -- --filter-class "*.UserSecretsParameterDefaultTests" --filter-not-trait "quarantined=true" --filter-not-trait "outerloop=true"

# Run SecretsStore tests
dotnet test tests/Aspire.Hosting.Tests/Aspire.Hosting.Tests.csproj -- --filter-class "*.SecretsStoreTests" --filter-not-trait "quarantined=true" --filter-not-trait "outerloop=true"
```

## Notes

- The pattern of extracting `.Path` and not disposing is **intentional** - many temp files/dirs persist for app lifetime
- `CreateTempFile()` creates a simple temp file using Path.GetTempFileName()
- `CreateTempFile("filename.ext")` creates a temp subdirectory with the named file inside, cleans up both on dispose
- `CreateTempSubdirectory(prefix)` creates a temp directory that can be disposed to clean up
- All APIs are marked `[Experimental("ASPIREFILESYSTEM001")]`
- CLI files are out of scope for this PR as they don't use the hosting infrastructure

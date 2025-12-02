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

## Remaining Files to Update

### Aspire.Hosting (1 file)
1. **UserSecretsManagerFactory.cs** (line 184)
   - Location: `src/Aspire.Hosting/UserSecrets/UserSecretsManagerFactory.cs`
   - Current: `Path.GetTempFileName()`
   - Change: Inject `IFileSystemService` and use `CreateTempFile()`

### Aspire.Hosting.Azure (2 files)
2. **AzureBicepResource.cs** (line 162)
   - Location: `src/Aspire.Hosting.Azure/AzureBicepResource.cs`
   - Current: `Directory.CreateTempSubdirectory("aspire").FullName`
   - Challenge: Public method on resource class, not easy to inject services
   - Options:
     - Set internal `TempDirectory` property before calling
     - Add optional `IFileSystemService` parameter
     - Get from service provider in callers

3. **AzureProvisioningResource.cs** (line 83)
   - Location: `src/Aspire.Hosting.Azure/AzureProvisioningResource.cs`
   - Current: `Directory.CreateTempSubdirectory("aspire").FullName`
   - Same challenge as AzureBicepResource

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

### Test Files (Many - Out of scope for this PR)
Test files manage their own temp directories for isolation. Key files include:
- `tests/Shared/TempDirectory.cs` - Shared test helper that creates temp directories
- `tests/Shared/DistributedApplicationTestingBuilderExtensions.cs` - Test configuration
- Various functional tests (Valkey, PostgreSQL, SqlServer, Oracle, MySQL, Redis, etc.)
- Azure tests (AzureContainerAppsTests, AzureAppServiceTests, AzureManifestUtils)
- CLI tests (ConsoleInteractionServiceTests, DiskCacheTests, etc.)

These tests typically use `Path.GetTempPath()` or `Directory.CreateTempSubdirectory()` directly for test isolation and cleanup

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

## Testing Checklist

Before finalizing, ensure:
- [x] All modified files build without warnings
- [ ] Existing tests pass
- [ ] New FileSystemServiceTests cover all scenarios
- [ ] Test disposing TempDirectory and TempFile objects
- [ ] Test the `.Path` property extraction pattern (common in codebase)
- [ ] Integration tests with actual temp file operations
- [ ] Verify no resource leaks (the .Path extraction pattern is intentional)
- [ ] Test both CreateTempFile() and CreateTempFile("filename.ext") patterns

## Notes

- The pattern of extracting `.Path` and not disposing is **intentional** - many temp files/dirs persist for app lifetime
- `CreateTempFile()` creates a simple temp file using Path.GetTempFileName()
- `CreateTempFile("filename.ext")` creates a temp subdirectory with the named file inside, cleans up both on dispose
- `CreateTempSubdirectory(prefix)` creates a temp directory that can be disposed to clean up
- All APIs are marked `[Experimental("ASPIREFILESYSTEM001")]`
- CLI files are out of scope for this PR as they don't use the hosting infrastructure

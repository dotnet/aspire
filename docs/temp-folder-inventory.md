# Temporary Folder Creation Inventory

This document provides a comprehensive inventory of all temporary folder and file creation in the Aspire codebase and documents the centralized directory management solution using `IDirectoryService`.

## Implementation Summary

We have implemented a centralized directory management service that:

- **Location**: `~/.aspire/temp/{apphostname}-{sha}/` (lowercase) for AppHost-specific temporary files
- **Service**: `IDirectoryService` in `Aspire.Hosting` namespace with `TempDirectory` property
- **Access**: Exposed on `IDistributedApplicationBuilder.DirectoryService` and available via DI
- **Configuration**: Via `IConfiguration` supporting `Aspire:TempDirectory` and `ASPIRE_TEMP_FOLDER` keys
- **Organization**: Each AppHost gets its own lowercase subdirectory based on project name + first 12 chars of SHA256 hash

## Implementation Status

✅ **Infrastructure Complete**:
- Created `IDirectoryService` interface in `Aspire.Hosting` namespace
- Created `DirectoryService` implementation with AppHost-specific lowercase subdirectories
- Added property to `IDistributedApplicationBuilder.DirectoryService` (default throws `NotImplementedException`)
- Registered service in DI container
- Comprehensive test suite (12 tests) using `TempDirectory` helper class
- Configuration via `IConfiguration` (supports multiple sources)

✅ **Migrations Complete (8 of 16 locations)**:
- **Phase 1**: DCP session management
- **Phase 2**: PipelineOutputService, ProjectResource, ContainerResourceBuilderExtensions, DashboardEventHandlers, Azure Bicep
- **Phase 3**: MAUI environment targets (Android & iOS)

⬜ **Remaining Migrations**:
- CLI operations (6 locations)
- MySQL scripts (1 location)
- User Secrets atomic operations (needs system temp)

## Configuration

The service supports flexible configuration through `IConfiguration`:

### Via Environment Variables

```bash
# Standard hierarchical format (IConfiguration converts to Aspire:TempDirectory)
export ASPIRE__TempDirectory=/custom/temp

# Convenient flat key
export ASPIRE_TEMP_FOLDER=/custom/temp
```

### Via appsettings.json

```json
{
  "Aspire": {
    "TempDirectory": "~/my-aspire-temp"
  }
}
```

### Via Command Line

```bash
dotnet run --Aspire:TempDirectory=/custom/temp
```

### Priority

Configuration source priority follows standard .NET configuration:
1. Command line arguments (highest)
2. Environment variables
3. appsettings.json files
4. Default: `~/.aspire/temp` (lowest)

## Directory Structure

```text
~/.aspire/temp/
├── myapphost-1234567890ab/     # Lowercase AppHost-specific directory
│   ├── dcp/                    # ✅ DCP session files (Phase 1)
│   │   ├── kubeconfig
│   │   └── output.sock
│   ├── pipelines/              # ✅ Pipeline artifacts (Phase 2)
│   ├── azure/                  # ✅ Azure Bicep generation (Phase 2)
│   ├── maui/                   # ✅ MAUI environment targets (Phase 3)
│   │   ├── android-env/        # ✅ Android targets files
│   │   └── ios-env/            # ✅ iOS targets files
│   ├── {guid}.dockerfile       # ✅ ProjectResource temp Dockerfiles (Phase 2)
│   ├── {guid}.Dockerfile.{name}# ✅ Container factory Dockerfiles (Phase 2)
│   ├── {guid}.json             # ✅ Dashboard config files (Phase 2)
│   └── {guid}/                 # Ad-hoc temp subdirectories
├── anotherapp-fedcba098765/    # Another AppHost
│   └── ...
```

## API Usage

### In AppHost Code

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Access temp directory service
var tempService = builder.DirectoryService.TempDirectory;

// Create subdirectory
var dcpDir = tempService.CreateSubdirectory("dcp");

// Get temp file path
var configFile = tempService.GetFilePath(".json");

// Get base path
var basePath = tempService.BasePath; // ~/.aspire/temp/{apphost}-{sha}/
```

### In Resources (via DI)

```csharp
public class MyResource : IResource
{
    public async Task DoSomethingAsync(DistributedApplicationExecutionContext context)
    {
        var dirService = context.ServiceProvider.GetRequiredService<IDirectoryService>();
        var tempService = dirService.TempDirectory;
        
        // Create temp subdirectory
        var tempDir = tempService.CreateSubdirectory("myresource");
        
        // Or get temp file path
        var tempFile = tempService.GetFilePath(".tmp");
    }
}
```

## Migration Progress

### ✅ Completed (Phases 1-3)

#### 1.1 DCP (Distributed Control Plane) Session Management
**File**: `src/Aspire.Hosting/Dcp/Locations.cs`
- **Before**: `Directory.CreateTempSubdirectory("aspire.")`
- **After**: Uses `IDirectoryService` → `{apphost-temp}/dcp/`
- **Status**: ✅ Migrated (Phase 1)
- **Benefit**: DCP files now organized per AppHost, easier cleanup

#### 1.2 PipelineOutputService
**File**: `src/Aspire.Hosting/Pipelines/PipelineOutputService.cs`
- **Before**: `Directory.CreateTempSubdirectory($"aspire-{appHostSha}")`
- **After**: Uses `directoryService.TempDirectory.GetSubdirectoryPath("pipelines")`
- **Status**: ✅ Migrated (Phase 2)
- **Location**: `{apphost-temp}/pipelines/`

#### 1.3 ProjectResource (Dockerfile building)
**File**: `src/Aspire.Hosting/ApplicationModel/ProjectResource.cs`
- **Before**: `Path.GetTempFileName()`
- **After**: `directoryService.TempDirectory.GetFilePath(".dockerfile")`
- **Status**: ✅ Migrated (Phase 2)
- **Location**: `{apphost-temp}/{guid}.dockerfile`

#### 1.4 ContainerResourceBuilderExtensions (WithDockerfileFactory)
**File**: `src/Aspire.Hosting/ContainerResourceBuilderExtensions.cs`
- **Before**: `Path.Combine(Path.GetTempPath(), $"Dockerfile.{resource.Name}.{Guid.NewGuid():N}")`
- **After**: `directoryService.TempDirectory.GetFilePath($".Dockerfile.{resource.Name}")`
- **Status**: ✅ Migrated (Phase 2)
- **Location**: `{apphost-temp}/{guid}.Dockerfile.{resource-name}`

#### 1.5 DashboardEventHandlers (2 locations)
**File**: `src/Aspire.Hosting/Dashboard/DashboardEventHandlers.cs`
- **Before**: `Path.ChangeExtension(Path.GetTempFileName(), ".json")`
- **After**: `directoryService.TempDirectory.GetFilePath(".json")`
- **Status**: ✅ Migrated (Phase 2)
- **Location**: `{apphost-temp}/{guid}.json`

#### 1.6 Azure Bicep Generation (AzureProvisioningResource & AzureBicepResource)
**Files**: `src/Aspire.Hosting.Azure/AzureProvisioningResource.cs`, `AzureBicepResource.cs`, `BicepProvisioner.cs`, `AzurePublishingContext.cs`
- **Before**: `Directory.CreateTempSubdirectory("aspire")`
- **After**: Pass azure temp directory from `directoryService.TempDirectory.GetSubdirectoryPath("azure")`
- **Status**: ✅ Migrated (Phase 2)
- **Location**: `{apphost-temp}/azure/`

#### 1.7 MAUI Android Environment Targets
**File**: `src/Aspire.Hosting.Maui/Utilities/MauiEnvironmentHelper.cs`, `MauiAndroidEnvironmentAnnotation.cs`
- **Before**: `Path.Combine(Path.GetTempPath(), "aspire", "maui", "android-env")`
- **After**: `directoryService.TempDirectory.GetSubdirectoryPath("maui/android-env")`
- **Status**: ✅ Migrated (Phase 3)
- **Location**: `{apphost-temp}/maui/android-env/`
- **Benefit**: Android targets files now organized per AppHost

#### 1.8 MAUI iOS Environment Targets
**File**: `src/Aspire.Hosting.Maui/Utilities/MauiEnvironmentHelper.cs`, `MauiiOSEnvironmentAnnotation.cs`
- **Before**: `Path.Combine(Path.GetTempPath(), "aspire", "maui", "mlaunch-env")`
- **After**: `directoryService.TempDirectory.GetSubdirectoryPath("maui/ios-env")`
- **Status**: ✅ Migrated (Phase 3)
- **Location**: `{apphost-temp}/maui/ios-env/`
- **Benefit**: iOS targets files now organized per AppHost

### ⬜ Remaining Medium Priority

#### 2.1 CLI Download Operations
- **Target**: `{apphost-temp}/dashboard/{guid}.json`
- **Priority**: High - User-facing feature

#### 1.4 Pipeline Output Service
**File**: `src/Aspire.Hosting/Pipelines/PipelineOutputService.cs`
- **Current**: `Directory.CreateTempSubdirectory($"aspire-{appHostSha}")`
- **Target**: `{apphost-temp}/pipelines/`
- **Priority**: High - Developer experience

### ⬜ Medium Priority

#### 2.1 Azure Bicep Generation
**File**: `src/Aspire.Hosting.Azure/AzureProvisioningResource.cs`
- **Current**: `Directory.CreateTempSubdirectory("aspire")`
- **Target**: `{apphost-temp}/azure/bicep/`
- **Priority**: Medium

#### 2.2 CLI Operations
**Files**: Multiple in `src/Aspire.Cli/`
- **Current**: Various `Path.GetTempFileName()` and `Directory.CreateTempSubdirectory()`
- **Target**: `{apphost-temp}/cli/` or CLI-specific temp directory
- **Priority**: Medium

### ⬜ Low Priority

#### 3.1 Build Dockerfiles
**Files**: `ProjectResource.cs`, `ContainerResourceBuilderExtensions.cs`
- **Current**: `Path.GetTempFileName()` for Dockerfiles
- **Target**: `{apphost-temp}/build/dockerfiles/`
- **Priority**: Low - Short-lived

#### 3.2 Aspire Store
**File**: `src/Aspire.Hosting/ApplicationModel/AspireStore.cs`
- **Current**: `Path.GetTempFileName()`
- **Priority**: Very Low - Already has configurable path

#### 3.3 User Secrets Atomic Operations
**File**: `src/Aspire.Hosting/UserSecrets/UserSecretsManagerFactory.cs`
- **Current**: `Path.GetTempFileName()` (Unix only)
- **Priority**: Very Low - Needs system temp for atomic operations

## Summary Statistics

- **Total locations using temp operations**: 16 source files
- **Methods to replace**:
  - `Path.GetTempPath()`: 6 locations
  - `Path.GetTempFileName()`: 6 locations  
  - `Directory.CreateTempSubdirectory()`: 21 locations
- **Migrated**: 1 location (DCP)
- **Remaining**: 15 locations

All remaining locations should be migrated to use `IDirectoryService.TempDirectory`.

## Detailed Inventory by Component

### 1. Aspire.Hosting (Core)
**File**: `src/Aspire.Hosting/Pipelines/PipelineOutputService.cs`
- **Usage**: `Directory.CreateTempSubdirectory($"aspire-{appHostSha}")` or `Directory.CreateTempSubdirectory("aspire")`
- **Purpose**: Creates temporary directories for pipeline build artifacts
- **Current Location**: System temp directory with app-specific or generic "aspire" prefix
- **Notes**: Uses AppHost:PathSha256 from configuration to create isolated temp directories per app host
- **Recommendation**: Medium priority - Should move to `~/.aspire/pipelines/{appHostSha}/`

#### 1.3 Aspire Store
**File**: `src/Aspire.Hosting/ApplicationModel/AspireStore.cs`
- **Usage**: `Path.GetTempFileName()`
- **Purpose**: Creates temporary files for content-addressed storage with hash-based naming
- **Current Location**: System temp directory
- **Notes**: Uses atomic file operations (write to temp, then copy) for safe file creation
- **Recommendation**: Low priority - Already has configurable base path via `Aspire:Store:Path` configuration

#### 1.4 Project Resource Image Building
**File**: `src/Aspire.Hosting/ApplicationModel/ProjectResource.cs`
- **Usage**: `Path.GetTempFileName()`
- **Purpose**: Creates temporary Dockerfile when building project container images with copied files
- **Current Location**: System temp directory
- **Notes**: Temporary Dockerfile is deleted after successful build, left for debugging on failure
- **Recommendation**: Low priority - Short-lived, could move to `~/.aspire/build/dockerfiles/`

#### 1.5 Container Dockerfile Factory
**File**: `src/Aspire.Hosting/ContainerResourceBuilderExtensions.cs`
- **Usage**: `Path.Combine(Path.GetTempPath(), $"Dockerfile.{builder.Resource.Name}.{Guid.NewGuid():N}")`
- **Purpose**: Creates unique temporary Dockerfile path for dynamically generated Dockerfiles
- **Current Location**: System temp directory with predictable naming pattern
- **Notes**: Used by WithDockerfileFactory extension methods
- **Recommendation**: Low priority - Short-lived, could move to `~/.aspire/build/dockerfiles/`

#### 1.6 User Secrets Manager
**File**: `src/Aspire.Hosting/UserSecrets/UserSecretsManagerFactory.cs`
- **Usage**: `Path.GetTempFileName()`
- **Purpose**: Creates temporary file for atomic user secrets updates (Unix only)
- **Current Location**: System temp directory
- **Notes**: Only used on non-Windows platforms for proper Unix file mode setting
- **Recommendation**: Very low priority - Inherently needs system temp for atomic operations

#### 1.7 Dashboard Event Handlers
**File**: `src/Aspire.Hosting/Dashboard/DashboardEventHandlers.cs`
- **Usage**: `Path.ChangeExtension(Path.GetTempFileName(), ".json")` (2 locations)
- **Purpose**: Creates temporary JSON config files for dashboard runtime configuration
- **Current Location**: System temp directory with .json extension
- **Notes**: Used for custom OTLP config and runtime config files
- **Recommendation**: Medium priority - Could move to `~/.aspire/dashboard/config/`

### 2. Aspire.Hosting.Azure

#### 2.1 Azure Provisioning Resource
**File**: `src/Aspire.Hosting.Azure/AzureProvisioningResource.cs`
- **Usage**: `Directory.CreateTempSubdirectory("aspire")`
- **Purpose**: Creates temporary directory for generated Bicep files during Azure provisioning
- **Current Location**: System temp directory with "aspire" prefix
- **Notes**: Used to compile and store intermediate Bicep files
- **Recommendation**: Medium priority - Could move to `~/.aspire/azure/bicep/`

#### 2.2 Azure Bicep Resource
**File**: `src/Aspire.Hosting.Azure/AzureBicepResource.cs`
- **Usage**: `Directory.CreateTempSubdirectory("aspire")`
- **Purpose**: Creates temporary directory for Bicep module generation
- **Current Location**: System temp directory with "aspire" prefix
- **Notes**: Used when no specific directory is provided for Bicep module generation
- **Recommendation**: Medium priority - Could move to `~/.aspire/azure/bicep/`

### 3. Aspire.Hosting.MySql

#### 3.1 MySQL Init SQL Files
**File**: `src/Aspire.Hosting.MySql/MySqlBuilderExtensions.cs`
- **Usage**: `Path.GetTempFileName()`
- **Purpose**: Creates temporary SQL file for database initialization scripts
- **Current Location**: System temp directory
- **Notes**: Used by WithInitBindMount to create SQL files that get mounted into MySQL container
- **Recommendation**: Low priority - Short-lived, could move to `~/.aspire/mysql/init/`

### 4. Aspire.Hosting.Maui

#### 4.1 Android Environment Targets
**File**: `src/Aspire.Hosting.Maui/Utilities/MauiEnvironmentHelper.cs`
- **Usage**: `Path.Combine(Path.GetTempPath(), "aspire", "maui", "android-env")`
- **Purpose**: Creates MSBuild targets files for passing environment variables to Android apps
- **Current Location**: `{TEMP}/aspire/maui/android-env/`
- **Notes**: Includes pruning logic to clean up old targets files
- **Recommendation**: High priority - Already structured, should move to `~/.aspire/maui/android-env/`

#### 4.2 iOS Environment Targets
**File**: `src/Aspire.Hosting.Maui/Utilities/MauiEnvironmentHelper.cs`
- **Usage**: `Path.Combine(Path.GetTempPath(), "aspire", "maui", "mlaunch-env")`
- **Purpose**: Creates MSBuild targets files for passing environment variables to iOS apps
- **Current Location**: `{TEMP}/aspire/maui/mlaunch-env/`
- **Notes**: Includes pruning logic to clean up old targets files
- **Recommendation**: High priority - Already structured, should move to `~/.aspire/maui/ios-env/`

### 5. Aspire.Cli (Command Line Interface)

#### 5.1 CLI Downloader
**File**: `src/Aspire.Cli/Utils/CliDownloader.cs`
- **Usage**: `Directory.CreateTempSubdirectory("aspire-cli-download")`
- **Purpose**: Downloads CLI archives and checksums during self-update
- **Current Location**: System temp directory with "aspire-cli-download" prefix
- **Notes**: Cleaned up on failure, used temporarily during download process
- **Recommendation**: Low priority - Short-lived download cache, could move to `~/.aspire/cli/downloads/`

#### 5.2 Update Command Extract
**File**: `src/Aspire.Cli/Commands/UpdateCommand.cs`
- **Usage**: `Directory.CreateTempSubdirectory("aspire-cli-extract")`
- **Purpose**: Extracts CLI archives during self-update
- **Current Location**: System temp directory with "aspire-cli-extract" prefix
- **Notes**: Used for extracting new CLI version before replacing current executable
- **Recommendation**: Low priority - Short-lived, could move to `~/.aspire/cli/extract/`

#### 5.3 Init Command Template Creation
**File**: `src/Aspire.Cli/Commands/InitCommand.cs`
- **Usage**: `Path.Combine(Path.GetTempPath(), $"aspire-init-{Guid.NewGuid()}")`
- **Purpose**: Creates temporary directory for template instantiation during `aspire init`
- **Current Location**: System temp directory with unique GUID
- **Notes**: Used to inspect generated files before copying to target location
- **Recommendation**: Low priority - Very short-lived, could move to `~/.aspire/cli/init-temp/`

#### 5.4 Temporary NuGet Config
**File**: `src/Aspire.Cli/Packaging/TemporaryNuGetConfig.cs`
- **Usage**: `Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())`
- **Purpose**: Creates temporary NuGet.config for package operations with custom sources
- **Current Location**: System temp directory with random name
- **Notes**: Implements IDisposable, cleaned up automatically
- **Recommendation**: Low priority - Very short-lived, could move to `~/.aspire/nuget/configs/`

#### 5.5 Disk Cache
**File**: `src/Aspire.Cli/Caching/DiskCache.cs`
- **Usage**: Uses `.tmp` suffix for atomic file operations
- **Purpose**: Atomic cache file updates (write to .tmp, then move)
- **Current Location**: Within cache directory structure
- **Notes**: Not a separate temp location, just a file operation pattern
- **Recommendation**: No change needed - This is a proper atomic file pattern

## Test Files (Not Production Code)

The following test files also use temp directories. These are not production concerns but are documented for completeness:

### Test Infrastructure
- `tests/Shared/TempDirectory.cs`: `Directory.CreateTempSubdirectory(".aspire-tests")`
- `tests/Shared/DistributedApplicationTestingBuilderExtensions.cs`: Configures test store paths
- Multiple test files use `Path.GetTempPath()` and `Directory.CreateTempSubdirectory()` for test fixtures

### Notable Test Patterns
- Many functional tests create temporary bind mount directories
- CLI tests create temporary execution contexts with test-specific SDK/runtime directories
- Several tests use `Path.Combine(Path.GetTempPath(), "aspire-test-*")` pattern

## Directory Structure for `~/.aspire/temp`

Based on review feedback, temporary files are organized by AppHost:

```text
~/.aspire/temp/
├── MyAppHost-1234567890ab/     # AppHost-specific directory (name + first 12 chars of SHA)
│   ├── {guid}/                 # Subdirectories created by CreateSubdirectory()
│   ├── {guid}.json             # Temp files created by GetFilePath()
│   ├── build-{guid}/           # Subdirectories with prefix
│   └── ...                     # All temp files for this AppHost
├── AnotherApp-fedcba098765/    # Another AppHost's temp directory
│   └── ...
└── ...
```

### Key Benefits

1. **Organization**: Each AppHost gets its own subdirectory for easy identification
2. **Cleanup**: Easy to identify and clean up temp files for specific AppHosts
3. **Consistency**: All code uses the same service
4. **Override**: `ASPIRE_TEMP_FOLDER` environment variable changes the base temp root
5. **Cross-platform**: Works consistently across Windows, Linux, and macOS
6. **No Conflicts**: Different AppHosts don't interfere with each other

## IDirectoryService API

The service provides a hierarchical API for different directory types:

```csharp
// Access the directory service
var dirService = builder.DirectoryService;

// Use the temp directory
var tempDir = dirService.TempDirectory;

// Methods available on ITempDirectoryService:
- BasePath: Gets the AppHost-specific base path (~/.aspire/temp/{apphost-name}-{sha})
- CreateSubdirectory(prefix): Creates a unique subdirectory (replaces Directory.CreateTempSubdirectory)
- GetFilePath(extension): Gets a unique temp file path (replaces Path.GetTempFileName)
- GetSubdirectoryPath(subdirectory): Gets a subdirectory path without creating it
```

## Migration Strategy

### Phase 1: Infrastructure (Completed)
✅ Create `IDirectoryService` interface and implementation
✅ Register in DI container
✅ Expose on `IDistributedApplicationBuilder.DirectoryService`
✅ AppHost-specific subdirectories using project name + SHA
✅ Comprehensive test suite

### Phase 2: High Priority Migration
1. DCP session files (`Locations.cs`)
2. MAUI environment targets 
3. Dashboard config files
4. Pipeline output service

### Phase 3: Medium Priority Migration
1. Azure Bicep generation
2. CLI operations (download, extract, init)
3. Temporary NuGet configs

### Phase 4: Low Priority Migration
1. Build-time Dockerfiles
2. MySQL init scripts
3. Project resource temporary files
4. Init command templates

### Phase 5: Very Low Priority (Requires careful consideration)
1. User secrets atomic operations (may need to stay in system temp for security)
2. Aspire Store (already has configurable path)

## Configuration Approach

The service supports three ways to override the default `~/.aspire/temp` base location:

### 1. Environment Variable (Highest Priority)
```bash
export ASPIRE_TEMP_FOLDER=/custom/temp/root
# Result: /custom/temp/root/{apphost-name}-{sha}/
```

### 2. Configuration (Second Priority)
```json
{
  "Aspire": {
    "TempDirectory": "~/custom-temp"
  }
}
```
Result: `~/custom-temp/{apphost-name}-{sha}/`

### 3. Default (Fallback)
`~/.aspire/temp/{apphost-name}-{sha}/` in the user's profile directory

Note: The AppHost-specific subdirectory (name + SHA) is always appended for organization.

## Usage Examples

### In AppHost Code
```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Access via builder property
var tempService = builder.DirectoryService.TempDirectory;
var tempDir = tempService.CreateSubdirectory("my-prefix");
var tempFile = tempService.GetFilePath(".json");

// Or via DI in services
services.AddSingleton<MyService>(sp =>
{
    var dirService = sp.GetRequiredService<IDirectoryService>();
    return new MyService(dirService.TempDirectory);
});
```

### In Resources
```csharp
public class MyResource : IResource
{
    public async Task DoSomethingAsync(DistributedApplicationExecutionContext context)
    {
        // Get the service from context
        var dirService = context.ServiceProvider.GetRequiredService<IDirectoryService>();
        var tempService = dirService.TempDirectory;
        
        // Create a temp subdirectory instead of using Path.GetTempPath()
        var tempDir = tempService.CreateSubdirectory("myresource");
        
        // Or get a temp file path instead of Path.GetTempFileName()
        var tempFile = tempService.GetFilePath(".json");
    }
}
```

## Cleanup Considerations

Some subsystems already implement cleanup:
- MAUI targets files have pruning logic
- CLI implements IDisposable pattern
- Build files are cleaned up after use

We should ensure:
1. Session cleanup on graceful shutdown
2. Periodic cleanup of old pipeline artifacts
3. Size limits on cache directories
4. Clear documentation on manual cleanup if needed

## Notes and Observations

1. **Naming Patterns**: Most temp directories use "aspire" or "aspire-" prefix
2. **Isolation**: Some systems use content hashing (AppHost SHA, GUID) for isolation
3. **Cleanup**: Varies by subsystem - some automatic, some manual
4. **Platform Differences**: Unix vs Windows differences in file operations (especially user secrets)
5. **Security**: User secrets operations need careful handling of file permissions
6. **Performance**: Frequent temp file creation could benefit from dedicated directory (fewer permission checks)

## Next Steps

1. Create configuration infrastructure for `.aspire` directory locations
2. Implement helper utilities for directory creation/cleanup
3. Add migration logic to handle existing temp files
4. Update documentation
5. Phase rollout according to priority

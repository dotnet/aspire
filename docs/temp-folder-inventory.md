# Temporary Folder Creation Inventory

This document provides a comprehensive inventory of all temporary folder and file creation in the Aspire codebase. Following feedback, we are implementing a centralized solution using `~/.aspire/temp` for all temporary files.

## Updated Proposal

Based on review feedback, we are implementing a unified approach:

- **Location**: `~/.aspire/temp/` for all temporary files
- **Override**: Support `ASPIRE_TEMP_FOLDER` environment variable
- **Service**: Create `IAspireTempDirectoryService` available in both CLI and AppHost
- **Access**: Exposed on `IDistributedApplicationBuilder` and in DI container
- **Migration**: Resources should use this service instead of `Path.GetTempPath()`, `Path.GetTempFileName()`, `Directory.CreateTempSubdirectory()`

## Implementation Status

✅ **Created** `IAspireTempDirectoryService` interface in `src/Aspire.Hosting/Utils/`
✅ **Created** `AspireTempDirectoryService` implementation
✅ **Added** property to `IDistributedApplicationBuilder.TempDirectoryService`
✅ **Registered** service in DI container
⬜ **TODO**: Migrate existing temp usage to new service

## Summary Statistics

- **Total locations using temp operations**: 16 source files
- **Methods used**:
  - `Path.GetTempPath()`: 6 locations
  - `Path.GetTempFileName()`: 6 locations  
  - `Directory.CreateTempSubdirectory()`: 21 locations

All of these will be migrated to use `IAspireTempDirectoryService`.

## Detailed Inventory by Component

### 1. Aspire.Hosting (Core)

#### 1.1 DCP (Distributed Control Plane) Session Management
**File**: `src/Aspire.Hosting/Dcp/Locations.cs`
- **Usage**: `Directory.CreateTempSubdirectory("aspire.")`
- **Purpose**: Creates a temporary directory for DCP session files (kubeconfig, log socket)
- **Current Location**: System temp directory with "aspire." prefix
- **Notes**: Used for session-specific files that need to be accessible by DCP components
- **Recommendation**: High priority - Should move to `~/.aspire/sessions/{session-id}/`

#### 1.2 Pipeline Output Service
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

## Simplified Directory Structure for `~/.aspire/temp`

Based on review feedback, all temporary files will be consolidated into a single directory:

```
~/.aspire/temp/
├── {random-guid}/      # Subdirectories created by CreateTempSubdirectory()
├── {random-guid}.json  # Temp files created by GetTempFilePath()
├── aspire-{guid}/      # Subdirectories with prefix
└── ...                 # All temp files and directories in one location
```

### Key Benefits

1. **Simplicity**: Single location for all temp files
2. **Consistency**: All code uses the same service
3. **Override**: `ASPIRE_TEMP_FOLDER` environment variable or `Aspire:TempDirectory` configuration
4. **Cleanup**: Easier to manage and clean up all temp files in one place
5. **Cross-platform**: Works consistently across Windows, Linux, and macOS

## IAspireTempDirectoryService API

The service provides these methods:

- `BaseTempDirectory`: Gets the base path (`~/.aspire/temp` by default)
- `CreateTempSubdirectory(prefix)`: Creates a unique subdirectory (replaces `Directory.CreateTempSubdirectory`)
- `GetTempFilePath(extension)`: Gets a unique temp file path (replaces `Path.GetTempFileName`)
- `GetTempSubdirectoryPath(subdirectory)`: Gets a subdirectory path without creating it

## Migration Strategy

### Phase 1: Infrastructure (Completed)
✅ Create `IAspireTempDirectoryService` interface and implementation
✅ Register in DI container
✅ Expose on `IDistributedApplicationBuilder`

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
### Phase 4: Low Priority Migration
1. Build-time Dockerfiles
2. MySQL init scripts
3. Project resource temporary files
4. Init command templates

### Phase 5: Very Low Priority (Requires careful consideration)
1. User secrets atomic operations (may need to stay in system temp for security)
2. Aspire Store (already has configurable path)

## Configuration Approach

The service supports three ways to override the default `~/.aspire/temp` location:

### 1. Environment Variable (Highest Priority)
```bash
export ASPIRE_TEMP_FOLDER=/custom/temp/path
```

### 2. Configuration (Second Priority)
```json
{
  "Aspire": {
    "TempDirectory": "~/.aspire/temp"
  }
}
```

### 3. Default (Fallback)
`~/.aspire/temp` in the user's profile directory

## Usage Examples

### In AppHost Code
```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Access via builder property
var tempDir = builder.TempDirectoryService.CreateTempSubdirectory("my-prefix");

// Or via DI in services
services.AddSingleton<MyService>(sp =>
{
    var tempService = sp.GetRequiredService<IAspireTempDirectoryService>();
    return new MyService(tempService);
});
```

### In Resources
```csharp
public class MyResource : IResource
{
    public async Task DoSomethingAsync(DistributedApplicationExecutionContext context)
    {
        // Get the service from context
        var tempService = context.ServiceProvider.GetRequiredService<IAspireTempDirectoryService>();
        
        // Create a temp subdirectory instead of using Path.GetTempPath()
        var tempDir = tempService.CreateTempSubdirectory("myresource");
        
        // Or get a temp file path instead of Path.GetTempFileName()
        var tempFile = tempService.GetTempFilePath(".json");
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

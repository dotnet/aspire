# Temporary Folder Creation Inventory

This document provides a comprehensive inventory of all temporary folder and file creation in the Aspire codebase. The goal is to eventually consolidate temp file storage into `.aspire` in the user folder.

## Summary Statistics

- **Total locations using temp operations**: 16 source files
- **Methods used**:
  - `Path.GetTempPath()`: 6 locations
  - `Path.GetTempFileName()`: 6 locations  
  - `Directory.CreateTempSubdirectory()`: 21 locations

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

## Proposed Directory Structure for `.aspire` Folder

Based on the usage patterns identified, here's a proposed structure:

```
~/.aspire/
├── sessions/           # DCP session files (kubeconfig, sockets)
│   └── {session-id}/
├── pipelines/          # Pipeline build artifacts
│   └── {appHostSha}/
├── build/              # Build-time temporary files
│   └── dockerfiles/    # Generated Dockerfiles
├── dashboard/          # Dashboard configuration files
│   └── config/
├── azure/              # Azure-specific temporary files
│   └── bicep/          # Generated Bicep files
├── maui/               # MAUI-specific files
│   ├── android-env/    # Android environment targets
│   └── ios-env/        # iOS environment targets
├── mysql/              # MySQL init scripts
│   └── init/
├── cli/                # CLI temporary files
│   ├── downloads/      # Downloaded archives
│   ├── extract/        # Extraction directory
│   ├── init-temp/      # Template instantiation
│   └── sdks/           # Downloaded SDKs
├── nuget/              # NuGet temporary files
│   └── configs/        # Temporary NuGet.config files
└── store/              # Aspire store (already configurable)
```

## Migration Strategy

### Phase 1: High Priority (User-facing, persistent)
1. DCP session files (`Locations.cs`)
2. MAUI environment targets (already well-structured)
3. Dashboard config files

### Phase 2: Medium Priority (Developer experience)
1. Pipeline output service
2. Azure Bicep generation
3. CLI operations (download, extract)

### Phase 3: Low Priority (Short-lived, internal)
1. Build-time Dockerfiles
2. MySQL init scripts
3. Temporary NuGet configs
4. Init command templates

### Phase 4: Very Low Priority (Requires careful consideration)
1. User secrets atomic operations (may need to stay in system temp for security)
2. Aspire Store (already has configurable path)

## Configuration Approach

We should provide configuration options for controlling temp directory locations:

```json
{
  "Aspire": {
    "TempDirectory": "~/.aspire",  // Base directory for all Aspire temp files
    "Sessions": {
      "Directory": "~/.aspire/sessions"  // Override for specific subsystem
    },
    "Pipelines": {
      "Directory": "~/.aspire/pipelines"
    }
    // etc.
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

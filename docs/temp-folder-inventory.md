# Temp Folder Inventory

This document catalogs all usages of temporary file and directory APIs in the Aspire codebase to understand usage patterns and guide migration to `IFileSystemService`.

## Summary

| API | Count in `src/` | Count in `tests/` | Migration Status |
|-----|-----------------|-------------------|------------------|
| `Path.GetTempPath()` | 2 | 30+ | CLI only (not migrated) |
| `Path.GetTempFileName()` | 1 | 6 | ✅ Migrated (1 singleton uses direct call) |
| `Directory.CreateTempSubdirectory()` | 2 | 0 | Partial (2 in Azure) |
| `IFileSystemService.TempDirectory.CreateTempSubdirectory()` | 8 | 1 | ✅ New API |
| `IFileSystemService.TempDirectory.GetTempFileName()` | 0 | 5 | ✅ New API (available, not used in src/) |
| `IFileSystemService.TempDirectory.CreateTempFile()` | 6 | 1 | ✅ New API |

## New IFileSystemService API

The `IFileSystemService` provides an abstraction over temp file/directory APIs for testability and consistency. All APIs are marked as `[Experimental("ASPIREFILESYSTEM001")]`.

### Return Types

| Type | Implements | Description |
|------|------------|-------------|
| `TempDirectory` | `IDisposable` | Represents a temporary directory. Dispose to delete the directory and all contents. Implicit conversion to `string` returns the path. |
| `TempFile` | `IDisposable` | Represents a temporary file. Dispose to delete the file (and optionally parent directory). Implicit conversion to `string` returns the path. |

### API Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `CreateTempSubdirectory(prefix?)` | `TempDirectory` | Creates a unique temporary subdirectory using the system temp folder. Dispose to delete. |
| `GetTempFileName(extension?)` | `TempFile` | Creates a new temporary file with a random name and optional extension. Dispose to delete. |
| `CreateTempFile(prefix, fileName)` | `TempFile` | Creates a new temporary file with a specific name in a unique temp subdirectory. Dispose to delete file and parent directory. |

### Usage Patterns

```csharp
// Pattern 1: Use implicit conversion to string (file persists until process ends)
string tempDir = fileSystemService.TempDirectory.CreateTempSubdirectory("aspire-dcp");

// Pattern 2: Use using statement for automatic cleanup
using var tempFile = fileSystemService.TempDirectory.CreateTempFile("aspire-test", "config.json");
File.WriteAllText(tempFile.Path, "{}");
// File is automatically deleted when tempFile goes out of scope

// Pattern 3: Explicit dispose when done
var tempDir = fileSystemService.TempDirectory.CreateTempSubdirectory("aspire-azure");
try
{
    // Use temp directory...
}
finally
{
    tempDir.Dispose();
}
```

### Current Usages of IFileSystemService.TempDirectory.CreateTempSubdirectory

| Location | Prefix | Purpose |
|----------|--------|---------|
| `Locations.cs:27` | `"aspire-dcp"` | DCP session storage |
| `PipelineOutputService.cs:31` | `"aspire-pipelines"` | Pipeline output storage |
| `MauiAndroidEnvironmentAnnotation.cs:77` | `"aspire-maui-android-env"` | Android environment targets |
| `MauiiOSEnvironmentAnnotation.cs:77` | `"aspire-maui-ios-env"` | iOS environment targets |
| `AzurePublishingContext.cs:151` | `"aspire-azure"` | Azure bicep module generation |
| `BicepProvisioner.cs:140` | `"aspire-azure"` | Azure provisioning bicep files |

### Current Usages of IFileSystemService.TempDirectory.CreateTempFile

| Location | Prefix | FileName | Purpose |
|----------|--------|----------|---------|
| `ProjectResource.cs:139` | `"aspire-dockerfile"` | `"Dockerfile"` | Project Dockerfile generation |
| `ContainerResourceBuilderExtensions.cs:667` | `"aspire-dockerfile-{name}"` | `"Dockerfile"` | Container Dockerfile generation |
| `DashboardEventHandlers.cs:230,272` | `"aspire-dashboard-config"` | `"runtimeconfig.json"` | Dashboard runtime config |
| `AspireStore.cs:50` | `"aspire-store"` | `"content.tmp"` | Store content hashing temp file |
| `MySqlBuilderExtensions.cs:386` | `"aspire-phpmyadmin"` | `"config.user.inc.php"` | PhpMyAdmin configuration file |

### IFileSystemService.TempDirectory.GetTempFileName

The `GetTempFileName(extension?)` method is available for cases where a random temp filename is acceptable. Currently no production usages in `src/` - all have been migrated to `CreateTempFile()` for better readability of temp file purposes.

## Legacy APIs Not Yet Migrated

### `Path.GetTempFileName()` - Creates temp file (singleton without DI)

| Location | Purpose | Migration Notes |
|----------|---------|-----------------|
| `UserSecretsManagerFactory.cs:184` | Atomic file write on Unix | Singleton pattern without DI access |

### `Directory.CreateTempSubdirectory()` - Creates temp subdirectory

| Location | Prefix | Purpose | Migration Notes |
|----------|--------|---------|-----------------|
| `AzureProvisioningResource.cs:83` | `"aspire"` | Provisioning temp files | Should use `IFileSystemService` |
| `AzureBicepResource.cs:162` | `"aspire"` | Bicep template generation | Should use `IFileSystemService` |
| `CliDownloader.cs:58` | `"aspire-cli-download"` | CLI download extraction | CLI doesn't have IFileSystemService |
| `UpdateCommand.cs:292` | `"aspire-cli-extract"` | CLI update extraction | CLI doesn't have IFileSystemService |

### `Path.GetTempPath()` - Gets system temp directory

| Location | Purpose | Migration Notes |
|----------|---------|-----------------|
| `InitCommand.cs:265` | CLI init temp project | CLI doesn't have IFileSystemService |
| `TemporaryNuGetConfig.cs:22` | CLI NuGet config | CLI doesn't have IFileSystemService |

## Test Code Usages

Test code extensively uses `Path.GetTempPath()` for test isolation, which is appropriate since tests don't run within an AppHost context.

### Common Test Patterns

```csharp
// CLI tests - create isolated directories for each test
var tempPath = Path.Combine(Path.GetTempPath(), "aspire-cli-tests", Guid.NewGuid().ToString());

// Execution context with SDKs directory
new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-sdks"))
```

## Migration Priority

### High Priority (directly affects AppHost temp management)

1. ✅ `AspireStore.cs:46` - Migrated to `IFileSystemService`
2. ✅ `MySqlBuilderExtensions.cs:380` - Migrated to `IFileSystemService`
3. `AzureProvisioningResource.cs:83` - Uses `Directory.CreateTempSubdirectory("aspire")`
4. `AzureBicepResource.cs:162` - Uses `Directory.CreateTempSubdirectory("aspire")`

### Medium Priority (singleton without DI)

5. ✅ `UserSecretsManagerFactory.cs:184` - Migrated to `Directory.CreateTempSubdirectory`

### Low Priority (CLI - separate context)

The CLI (`Aspire.Cli`) operates outside the AppHost context and doesn't have access to `IFileSystemService`. These usages are acceptable:

- `CliDownloader.cs:58`
- `UpdateCommand.cs:292`
- `InitCommand.cs:265`
- `TemporaryNuGetConfig.cs:22`

## Design Notes

The current implementation is intentionally simple:

- Returns `TempDirectory` and `TempFile` wrapper types that implement `IDisposable`
- Implicit conversion to `string` allows seamless use where paths are expected
- Dispose() cleans up the temp file/directory (optional - many usages persist for app lifetime)
- Uses the system temp folder (no custom temp folder management yet)
- Enables testability through the `IFileSystemService` abstraction
- Establishes consistent prefix naming pattern (`aspire-*`)
- All APIs marked `[Experimental("ASPIREFILESYSTEM001")]`

Future enhancements could include:
- Custom temp folder location via configuration
- AppHost-specific temp directories for better isolation
- Automatic cleanup of temp directories on app exit

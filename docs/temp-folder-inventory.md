# Temp Folder Inventory

This document catalogs all usages of temporary file and directory APIs in the Aspire codebase to understand usage patterns and guide migration to `IDirectoryService`.

## Summary

| API | Count in `src/` | Count in `tests/` | Migration Status |
|-----|-----------------|-------------------|------------------|
| `Path.GetTempPath()` | 2 | 30+ | Partial |
| `Path.GetTempFileName()` | 3 | 0 | Not migrated |
| `Directory.CreateTempSubdirectory()` | 3 | 0 | Partial |
| `IDirectoryService.TempDirectory.CreateSubdirectory()` | 3 | 1 | ✅ New API |
| `IDirectoryService.TempDirectory.CreateSubdirectoryPath()` | 3 | 0 | ✅ New API |

## New IDirectoryService API

The `IDirectoryService` provides centralized temp directory management under `~/.aspire/temp/{apphost-name}-{sha12}/`.

### API Methods

| Method | Creates Directory | Creates Unique Name | Use Case |
|--------|------------------|---------------------|----------|
| `CreateSubdirectory(prefix?)` | ✅ | ✅ (GUID suffix) | Isolated operations (parallel tests, concurrent bicep generation) |
| `CreateSubdirectoryPath(name)` | ✅ | ❌ (exact name) | Well-known paths (pipelines, dcp sessions) |
| `GetFilePath(extension?)` | ❌ | ✅ (GUID name) | Temp file paths (caller creates file) |
| `BasePath` | N/A | N/A | Read the base temp directory path |

### Current Usages of IDirectoryService

#### `CreateSubdirectory(prefix)` - Unique directories with GUID suffix

| Location | Prefix | Purpose |
|----------|--------|---------|
| `Locations.cs:27` | `"dcp"` | DCP session storage (per-run isolation) |
| `AzurePublishingContext.cs:151` | `"azure"` | Azure bicep module generation (parallel test isolation) |
| `BicepProvisioner.cs:140` | `"azure"` | Azure provisioning bicep files (parallel test isolation) |

#### `CreateSubdirectoryPath(name)` - Named directories

| Location | Name | Purpose |
|----------|------|---------|
| `PipelineOutputService.cs:31` | `"pipelines"` | Pipeline output storage (consistent path across app lifetime) |
| `MauiAndroidEnvironmentAnnotation.cs:77` | `"maui/android-env"` | Android environment targets files |
| `MauiiOSEnvironmentAnnotation.cs:77` | `"maui/ios-env"` | iOS environment targets files |

## Legacy APIs Not Yet Migrated

### `Path.GetTempFileName()` - Creates empty temp file

| Location | Purpose | Migration Notes |
|----------|---------|-----------------|
| `AspireStore.cs:46` | Temp file for store downloads | Could use `GetFilePath()` |
| `UserSecretsManagerFactory.cs:184` | Temp file for secrets | Could use `GetFilePath()` |
| `MySqlBuilderExtensions.cs:380` | Temp file for MySQL init script | Could use `GetFilePath()` |

### `Directory.CreateTempSubdirectory()` - Creates temp subdirectory

| Location | Prefix | Purpose | Migration Notes |
|----------|--------|---------|-----------------|
| `AzureProvisioningResource.cs:83` | `"aspire"` | Provisioning temp files | Should use `CreateSubdirectory()` |
| `AzureBicepResource.cs:162` | `"aspire"` | Bicep template generation | Should use `CreateSubdirectory()` |
| `CliDownloader.cs:58` | `"aspire-cli-download"` | CLI download extraction | CLI doesn't have IDirectoryService |
| `UpdateCommand.cs:292` | `"aspire-cli-extract"` | CLI update extraction | CLI doesn't have IDirectoryService |

### `Path.GetTempPath()` - Gets system temp directory

| Location | Purpose | Migration Notes |
|----------|---------|-----------------|
| `InitCommand.cs:265` | CLI init temp project | CLI doesn't have IDirectoryService |
| `TemporaryNuGetConfig.cs:22` | CLI NuGet config | CLI doesn't have IDirectoryService |

## Test Code Usages

Test code extensively uses `Path.GetTempPath()` for test isolation, which is appropriate since tests don't run within an AppHost context.

### Common Test Patterns

```csharp
// CLI tests - create isolated directories for each test
var tempPath = Path.Combine(Path.GetTempPath(), "aspire-cli-tests", Guid.NewGuid().ToString());

// Execution context with SDKs directory
new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-sdks"))
```

## Usage Pattern Analysis

### Pattern 1: Unique per-operation (use `CreateSubdirectory`)

When operations need isolation from each other (especially for parallel execution):

```csharp
// Each call gets a unique directory like "azure-abc123def456..."
var azureTempDir = directoryService.TempDirectory.CreateSubdirectory("azure");
```

**Use cases:**
- Azure bicep generation (tests run in parallel)
- DCP session directories (each run is isolated)
- Any operation where concurrent executions must not interfere

### Pattern 2: Well-known path (use `CreateSubdirectoryPath`)

When multiple callers need to reference the same directory:

```csharp
// Always returns the same "pipelines" directory
var pipelinesDir = directoryService.TempDirectory.CreateSubdirectoryPath("pipelines");
```

**Use cases:**
- Pipeline output (multiple components write to same location)
- MAUI environment files (targets files have unique names within directory)
- Configuration storage

### Pattern 3: Unique temp file (use `GetFilePath`)

When you need a unique file path but will create the file yourself:

```csharp
var tempFile = directoryService.TempDirectory.GetFilePath(".json");
File.WriteAllText(tempFile, content);
```

**Use cases:**
- Downloading files
- Generating config files
- Any single-file temp storage

## Migration Priority

### High Priority (directly affects AppHost temp management)

1. `AzureProvisioningResource.cs:83` - Uses `Directory.CreateTempSubdirectory("aspire")`
2. `AzureBicepResource.cs:162` - Uses `Directory.CreateTempSubdirectory("aspire")`
3. `AspireStore.cs:46` - Uses `Path.GetTempFileName()`

### Medium Priority (internal utilities)

4. `UserSecretsManagerFactory.cs:184` - Uses `Path.GetTempFileName()`
5. `MySqlBuilderExtensions.cs:380` - Uses `Path.GetTempFileName()`

### Low Priority (CLI - separate context)

The CLI (`Aspire.Cli`) operates outside the AppHost context and doesn't have access to `IDirectoryService`. These usages are acceptable:

- `CliDownloader.cs:58`
- `UpdateCommand.cs:292`
- `InitCommand.cs:265`
- `TemporaryNuGetConfig.cs:22`

## Configuration

The `IDirectoryService` temp directory location can be configured:

| Setting | Example | Notes |
|---------|---------|-------|
| Default | `~/.aspire/temp/{apphost}-{sha12}/` | Under user home directory |
| `ASPIRE_TEMP_FOLDER` env var | `/custom/temp` | Environment variable override |
| `Aspire:TempDirectory` config | `~/my-aspire-temp` | Configuration setting (supports `~/`) |

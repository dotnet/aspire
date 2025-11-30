# Temp Folder Inventory

This document catalogs all usages of temporary file and directory APIs in the Aspire codebase to understand usage patterns and guide migration to `IDirectoryService`.

## Summary

| API | Count in `src/` | Count in `tests/` | Migration Status |
|-----|-----------------|-------------------|------------------|
| `Path.GetTempPath()` | 2 | 30+ | CLI only (not migrated) |
| `Path.GetTempFileName()` | 0 | 0 | ✅ Migrated |
| `Directory.CreateTempSubdirectory()` | 3 | 0 | Partial (2 in Azure, 1 in UserSecrets) |
| `IDirectoryService.TempDirectory.CreateTempSubdirectory()` | 10 | 1 | ✅ New API |

## New IDirectoryService API

The `IDirectoryService` provides an abstraction over `Directory.CreateTempSubdirectory()` for testability and consistency.

### API Methods

| Method | Description |
|--------|-------------|
| `CreateTempSubdirectory(prefix?)` | Creates a unique temporary subdirectory using the system temp folder. Wraps `Directory.CreateTempSubdirectory()`. |

### Current Usages of IDirectoryService

| Location | Prefix | Purpose |
|----------|--------|---------|
| `Locations.cs:27` | `"aspire-dcp"` | DCP session storage |
| `ProjectResource.cs:139` | `"aspire-dockerfile"` | Project Dockerfile generation |
| `PipelineOutputService.cs:31` | `"aspire-pipelines"` | Pipeline output storage |
| `ContainerResourceBuilderExtensions.cs:667` | `"aspire-dockerfile-{name}"` | Container Dockerfile generation |
| `DashboardEventHandlers.cs:230,273` | `"aspire-dashboard-config"` | Dashboard runtime config |
| `MauiAndroidEnvironmentAnnotation.cs:77` | `"aspire-maui-android-env"` | Android environment targets |
| `MauiiOSEnvironmentAnnotation.cs:77` | `"aspire-maui-ios-env"` | iOS environment targets |
| `AzurePublishingContext.cs:151` | `"aspire-azure"` | Azure bicep module generation |
| `BicepProvisioner.cs:140` | `"aspire-azure"` | Azure provisioning bicep files |
| `AspireStore.cs:48` | `"aspire-store"` | Store content hashing temp files |
| `MySqlBuilderExtensions.cs:385` | `"aspire-phpmyadmin-config"` | PhpMyAdmin configuration |

## Migrated from Path.GetTempFileName()

These usages were migrated from `Path.GetTempFileName()` to use `IDirectoryService`:

| Location | Old API | New Approach |
|----------|---------|--------------|
| `AspireStore.cs` | `Path.GetTempFileName()` | `CreateTempSubdirectory("aspire-store")` + file |
| `MySqlBuilderExtensions.cs` | `Path.GetTempFileName()` | `CreateTempSubdirectory("aspire-phpmyadmin-config")` + file |

These usages were migrated to use `Directory.CreateTempSubdirectory()` directly (singleton without DI access):

| Location | Old API | New Approach |
|----------|---------|--------------|
| `UserSecretsManagerFactory.cs` | `Path.GetTempFileName()` | `Directory.CreateTempSubdirectory("aspire-secrets")` + file |

## Legacy APIs Not Yet Migrated

### `Directory.CreateTempSubdirectory()` - Creates temp subdirectory

| Location | Prefix | Purpose | Migration Notes |
|----------|--------|---------|-----------------|
| `AzureProvisioningResource.cs:83` | `"aspire"` | Provisioning temp files | Should use `IDirectoryService` |
| `AzureBicepResource.cs:162` | `"aspire"` | Bicep template generation | Should use `IDirectoryService` |
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

## Migration Priority

### High Priority (directly affects AppHost temp management)

1. ✅ `AspireStore.cs:46` - Migrated to `IDirectoryService`
2. ✅ `MySqlBuilderExtensions.cs:380` - Migrated to `IDirectoryService`
3. `AzureProvisioningResource.cs:83` - Uses `Directory.CreateTempSubdirectory("aspire")`
4. `AzureBicepResource.cs:162` - Uses `Directory.CreateTempSubdirectory("aspire")`

### Medium Priority (singleton without DI)

5. ✅ `UserSecretsManagerFactory.cs:184` - Migrated to `Directory.CreateTempSubdirectory`

### Low Priority (CLI - separate context)

The CLI (`Aspire.Cli`) operates outside the AppHost context and doesn't have access to `IDirectoryService`. These usages are acceptable:

- `CliDownloader.cs:58`
- `UpdateCommand.cs:292`
- `InitCommand.cs:265`
- `TemporaryNuGetConfig.cs:22`

## Design Notes

The current implementation is intentionally simple:

- `CreateTempSubdirectory(prefix)` wraps `Directory.CreateTempSubdirectory()` directly
- Uses the system temp folder (no custom temp folder management yet)
- Enables testability through the `IDirectoryService` abstraction
- Establishes consistent prefix naming pattern (`aspire-*`)

Future enhancements could include:
- Custom temp folder location via configuration
- AppHost-specific temp directories for better isolation
- Automatic cleanup of temp directories

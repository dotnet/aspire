# Cache .NET SDK for Arcade Action

A composite GitHub Action for aggressively caching the `.dotnet` directory used by the Aspire repository with Arcade SDK.

## Overview

This action caches the `.dotnet` directory using `actions/cache@v4` and only runs the build script for SDK installation on cache misses. This significantly speeds up CI/CD workflows by avoiding redundant SDK installations. The action uses a minimal empty project file with the `-restore` flag to trigger only SDK installation via Arcade, without building any actual code.

## Why Not Use `actions/setup-dotnet`?

The Aspire repository has complex requirements in `global.json` that `actions/setup-dotnet` doesn't support:

- **Custom SDK paths**: Points to local `.dotnet` directory via the `paths` array
- **Variable substitution**: Uses variables like `$(DotNetRuntimePreviousVersionForTesting)` in runtime versions
- **Architecture-specific runtimes**: Defines separate x64 and arm64 runtime versions
- **Custom error messages**: Provides repository-specific guidance when SDK is missing

Instead, this action uses the repository's Arcade build infrastructure (`eng/build.ps1` or `build.sh`) with a minimal empty project file to trigger SDK installation through the standard Arcade restore mechanism.

## Usage

### Basic Example

```yaml
steps:
  - uses: actions/checkout@v4

  - name: Cache .NET SDK
    uses: ./.github/actions/cache-dotnet-sdk-for-arcade
```

### With Custom Key Prefix

```yaml
steps:
  - uses: actions/checkout@v4

  - name: Cache .NET SDK
    uses: ./.github/actions/cache-dotnet-sdk-for-arcade
    with:
      key-prefix: custom-dotnet-sdk
```

### With Cache Hit Detection

```yaml
steps:
  - uses: actions/checkout@v4

  - name: Cache .NET SDK
    id: cache-sdk
    uses: ./.github/actions/cache-dotnet-sdk-for-arcade

  - name: Check cache status
    run: |
      if [ "${{ steps.cache-sdk.outputs.cache-hit }}" == "true" ]; then
        echo "SDK restored from cache"
      else
        echo "SDK installed from scratch"
      fi
```

## Inputs

| Input | Required | Default | Description |
|-------|----------|---------|-------------|
| `key-prefix` | No | `dotnet-sdk-v1` | Prefix for the cache key. The full key is automatically constructed as `{key-prefix}-{OS}-{arch}-{global.json-hash}` |

## Outputs

| Output | Description |
|--------|-------------|
| `cache-hit` | Boolean indicating if an exact cache match was found for `cache-key` |

## Cache Key Strategy

The action automatically constructs cache keys with the following components:

1. **key-prefix**: User-provided or default `dotnet-sdk-v1` (versioned to allow cache invalidation)
2. **Operating system**: `${{ runner.os }}` - Different OSes need different SDKs
3. **Architecture**: `${{ runner.arch }}` - x64 vs arm64 require different binaries
4. **global.json hash**: `${{ hashFiles('global.json') }}` - Changes when SDK version changes

The full cache key format is: `{key-prefix}-{OS}-{arch}-{hash}`

### Example: Using Custom Key Prefix

```yaml
# For workflow-specific caching
- name: Cache .NET SDK
  uses: ./.github/actions/cache-dotnet-sdk-for-arcade
  with:
    key-prefix: build-workflow-dotnet-sdk-v1

# Results in key like: build-workflow-dotnet-sdk-v1-Linux-X64-a1b2c3d4
```

## How It Works

1. **Cache Lookup**: Attempts to restore `.dotnet` directory from cache using the constructed cache key
2. **Cache Miss**: If no exact match, tries restore keys with prefix matching (OS and architecture)
3. **SDK Installation**: On cache miss:
   - Creates a minimal empty project file in `artifacts/tmp/install-sdks.proj`
   - Runs `eng/build.ps1` (Windows) or `build.sh` (Linux/macOS) with `-restore` flag
   - Arcade's restore mechanism installs required SDKs to `.dotnet` directory
   - Cleans up the temporary project file
4. **Save**: After workflow completion, cache action automatically saves the `.dotnet` directory

## Testing

A test workflow is available at `.github/workflows/test-cache-dotnet-sdk.yml`. Run it manually from the Actions tab to verify the action works correctly.

## Implementation Details

- Uses `actions/cache@0057852bfaa89a56745cba8c7296529d2fc39830` (v4.6.1)
- Conditional SDK installation: `if: steps.cache-dotnet.outputs.cache-hit != 'true'`
- Cross-platform: Uses PowerShell on Windows, Bash on Linux/macOS
- Minimal overhead: Only creates a small empty project file, no actual code compilation
- Automatic cleanup: Removes temporary project file after SDK installation
- Cache key includes OS, architecture, and global.json hash for optimal cache hits
- **Versioned caching**: The default prefix includes `-v1` suffix to allow global cache invalidation by bumping the version number

# Cache .NET SDK Action

A composite GitHub Action for aggressively caching the `.dotnet` directory used by the Aspire repository.

## Overview

This action caches the `.dotnet` directory using `actions/cache@v4` and only runs the restore script on cache misses. This significantly speeds up CI/CD workflows by avoiding redundant SDK installations.

## Why Not Use `actions/setup-dotnet`?

The Aspire repository has complex requirements in `global.json` that `actions/setup-dotnet` doesn't support:

- **Custom SDK paths**: Points to local `.dotnet` directory via the `paths` array
- **Variable substitution**: Uses variables like `$(DotNetRuntimePreviousVersionForTesting)` in runtime versions
- **Architecture-specific runtimes**: Defines separate x64 and arm64 runtime versions
- **Custom error messages**: Provides repository-specific guidance when SDK is missing

Instead, this action uses the repository's `restore.sh` or `restore.cmd` scripts which properly handle these advanced features.

## Usage

### Basic Example

```yaml
steps:
  - uses: actions/checkout@v4

  - name: Cache .NET SDK
    uses: ./.github/actions/cache-dotnet-sdk
    with:
      cache-key: dotnet-sdk-${{ runner.os }}-${{ hashFiles('global.json') }}
      restore-keys: |
        dotnet-sdk-${{ runner.os }}-
```

### With Cache Hit Detection

```yaml
steps:
  - uses: actions/checkout@v4

  - name: Cache .NET SDK
    id: cache-sdk
    uses: ./.github/actions/cache-dotnet-sdk
    with:
      cache-key: dotnet-sdk-${{ runner.os }}-${{ hashFiles('global.json') }}
      restore-keys: |
        dotnet-sdk-${{ runner.os }}-

  - name: Check cache status
    run: |
      if [ "${{ steps.cache-sdk.outputs.cache-hit }}" == "true" ]; then
        echo "SDK restored from cache"
      else
        echo "SDK restored from scratch"
      fi
```

## Inputs

| Input | Required | Default | Description |
|-------|----------|---------|-------------|
| `cache-key` | Yes | - | Primary cache key for the `.dotnet` directory |
| `restore-keys` | No | `''` | Ordered list of prefix-matched keys for restoring stale cache |

## Outputs

| Output | Description |
|--------|-------------|
| `cache-hit` | Boolean indicating if an exact cache match was found for `cache-key` |

## Cache Key Recommendations

For optimal cache performance, include:

1. **Operating system**: `${{ runner.os }}` - Different OSes need different SDKs
2. **global.json hash**: `${{ hashFiles('global.json') }}` - Changes when SDK version changes
3. **Architecture**: For multi-arch builds, include `${{ runner.arch }}`

### Example Cache Keys

```yaml
# Basic (OS + global.json)
cache-key: dotnet-sdk-${{ runner.os }}-${{ hashFiles('global.json') }}

# With architecture
cache-key: dotnet-sdk-${{ runner.os }}-${{ runner.arch }}-${{ hashFiles('global.json') }}

# With workflow name (for workflow-specific caches)
cache-key: dotnet-sdk-${{ github.workflow }}-${{ runner.os }}-${{ hashFiles('global.json') }}
```

## How It Works

1. **Cache Lookup**: Attempts to restore `.dotnet` directory from cache using `cache-key`
2. **Cache Miss**: If no exact match, tries `restore-keys` prefixes for partial matches
3. **Restore**: On cache miss, runs `restore.sh` (Linux/macOS) or `restore.cmd` (Windows)
4. **Save**: After workflow completion, cache action automatically saves the `.dotnet` directory

## Testing

A test workflow is available at `.github/workflows/test-cache-dotnet-sdk.yml`. Run it manually from the Actions tab to verify the action works correctly.

## Implementation Details

- Uses `actions/cache@0057852bfaa89a56745cba8c7296529d2fc39830` (v4)
- Conditional restore step: `if: steps.cache-dotnet.outputs.cache-hit != 'true'`
- Cross-platform: Detects and runs appropriate restore script for the OS
- Error handling: Fails if neither `restore.sh` nor `restore.cmd` is found

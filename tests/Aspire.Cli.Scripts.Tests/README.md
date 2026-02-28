# Aspire CLI Scripts Tests

This test project provides **safe, isolated functional testing** for the CLI acquisition scripts in `eng/scripts/`.

## Target Scripts

- `get-aspire-cli.sh` / `get-aspire-cli.ps1` - Download release/rolling builds
- `get-aspire-cli-pr.sh` / `get-aspire-cli-pr.ps1` - Download PR builds

## Safety Features

This test suite is designed with **zero risk** to user environments:

- ✅ All operations in temporary directories only
- ✅ No modifications to `$HOME`, `%USERPROFILE%`, or user directories
- ✅ No changes to shell profiles (`.bashrc`, `.zshrc`, etc.)
- ✅ Extensive use of `--dry-run` mode
- ✅ Tests can run thousands of times with no side effects

## Test Categories

### Parameter Validation Tests
- Invalid parameter values are rejected
- Help flags work correctly
- Conflicting parameters are detected

### Platform Detection Tests
- OS/architecture detection via environment variables
- RID generation (linux-x64, win-arm64, etc.)
- Platform override parameters

### Dry-run Behavior Tests
- Scripts show intended actions without executing
- Verify URLs, paths, and commands in output
- Check for key steps like downloading and installing

### Integration Tests (On-Demand)
- Real PR builds from GitHub (requires gh CLI and GH_TOKEN)
- End-to-end workflow validation
- **Marked with `[Trait("Category", "integration")]`**
- **Excluded from default test runs**

## Running Tests

```bash
# Build the test project
dotnet build tests/Aspire.Cli.Scripts.Tests/

# Run unit tests only (default - excludes integration tests)
dotnet test tests/Aspire.Cli.Scripts.Tests/ -- \
  --filter-not-trait "Category=integration" \
  --filter-not-trait "quarantined=true" \
  --filter-not-trait "outerloop=true"

# Run integration tests only (on-demand, requires GH_TOKEN)
export GH_TOKEN=<your-github-token>
dotnet test tests/Aspire.Cli.Scripts.Tests/ -- --filter-trait "Category=integration"

# Run specific test class
dotnet test tests/Aspire.Cli.Scripts.Tests/ -- --filter-class "*.ReleaseScriptShellTests"

# Run with verbose output
dotnet test tests/Aspire.Cli.Scripts.Tests/ -v n
```

## Requirements

### Unit Tests (Run by Default)
- **For shell script tests**: bash (automatically skipped on Windows)
- **For PowerShell tests**: pwsh (PowerShell 7+)
- **Mock gh CLI**: Automatically created by tests (no authentication needed)

### Integration Tests (On-Demand Only)
- **gh CLI**: GitHub CLI must be installed and available in PATH
- **GH_TOKEN**: Environment variable with valid GitHub token
- **Note**: These tests are excluded from default runs and must be explicitly enabled

## Architecture

### Core Infrastructure

- **TestEnvironment**: Creates isolated temp directories for each test
- **ScriptToolCommand**: Extends `ToolCommand` to run bash/PowerShell scripts
- **RequiresGHCliAttribute**: Skips tests when gh CLI is not available
- **RealGitHubPRFixture**: Discovers suitable PRs with required artifacts

### Test Organization

Tests are organized by script type and shell:

- `ReleaseScriptShellTests` - bash release script tests (16 tests, bash required)
- `ReleaseScriptPowerShellTests` - PowerShell release script tests (6 tests, pwsh required)
- `PRScriptShellTests` - bash PR parameter tests (13 tests, bash + mock gh)
- `PRScriptPowerShellTests` - PowerShell PR parameter tests (11 tests, pwsh + mock gh)
- `PRScriptIntegrationTests` - Integration tests with real PRs (4 tests, gh + GH_TOKEN, **on-demand only**)

**Total: 50 tests (46 unit tests + 4 integration tests)**

## Safety Verification

After running tests, verify no user environment modifications:

```bash
# This should return 0 (no new aspire directories in home)
find ~ -name "*aspire*" -type d -newer tests/Aspire.Cli.Scripts.Tests/ 2>/dev/null | wc -l
```

## Contributing

When adding new tests:

1. **Always use TestEnvironment** for directory isolation
2. **Prefer --dry-run** mode when possible
3. **Use temp directories** for any actual file operations
4. **Never modify user directories** or shell profiles
5. **Clean up** in test disposal methods

## Design Principles

This test suite follows key lessons learned:

- ✅ Parse JSON in C# (avoid jq quoting issues)
- ✅ Use class-level attributes (no repetition)
- ✅ Extend existing `ToolCommand` class
- ✅ Use existing `TestUtils.FindRepoRoot()`
- ✅ Throw on fixture failures (don't silently skip)
- ✅ Use `forceShowBuildOutput: true` for debugging

**Safety first, functionality second, completeness third.**

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

### Integration Tests
- Real PR builds from GitHub (requires gh CLI)
- End-to-end workflow validation

## Running Tests

```bash
# Build the test project
dotnet build tests/Aspire.Cli.Scripts.Tests/

# Run all tests
dotnet test tests/Aspire.Cli.Scripts.Tests/

# Run specific test class
dotnet test tests/Aspire.Cli.Scripts.Tests/ -- --filter-class "*.ReleaseScriptShellTests"

# Run with verbose output
dotnet test tests/Aspire.Cli.Scripts.Tests/ -v n
```

## Requirements

- **For shell script tests**: bash (automatically skipped on Windows)
- **For PowerShell tests**: pwsh (PowerShell 7+)
- **For PR integration tests**: gh CLI (GitHub CLI) authenticated with GitHub

## Architecture

### Core Infrastructure

- **TestEnvironment**: Creates isolated temp directories for each test
- **ScriptToolCommand**: Extends `ToolCommand` to run bash/PowerShell scripts
- **RequiresGHCliAttribute**: Skips tests when gh CLI is not available
- **RealGitHubPRFixture**: Discovers suitable PRs with required artifacts

### Test Organization

Tests are organized by script type and shell:

- `ReleaseScriptShellTests` - bash release script tests (bash required)
- `ReleaseScriptPowerShellTests` - PowerShell release script tests (pwsh required)
- `PRScriptShellTests` - bash PR parameter tests (bash and gh required)
- `PRScriptPowerShellTests` - PowerShell PR parameter tests (pwsh and gh required)
- `PRScriptIntegrationTests` - Integration tests with real PRs (gh required)

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

# Aspire CLI Scripts Tests

This test project provides functional testing for the CLI acquisition scripts in `eng/scripts/`.

## Scripts Under Test

- `get-aspire-cli.sh` / `get-aspire-cli.ps1` - Download release/rolling builds
- `get-aspire-cli-pr.sh` / `get-aspire-cli-pr.ps1` - Download PR builds

## Safety Design

All tests are designed with **zero risk** to user environments:

- ✅ All operations use isolated temporary directories (`Path.GetTempPath()`)
- ✅ No modifications to user home directories or shell profiles
- ✅ Extensive use of `--dry-run` mode for safe testing
- ✅ Automatic cleanup of temporary directories
- ✅ Repeatable with no side effects

## Test Categories

### Parameter Validation Tests
- Invalid parameter values are rejected
- Help flags work correctly
- Conflicting parameters are detected

### Platform Detection Tests
- Test platform override parameters (--os, --arch)
- Verify correct RID generation in dry-run output

### Installation Path Tests
- Custom path validation (in temp directories only)
- Verify paths appear correctly in dry-run output

### Dry-run Behavior Tests
- Scripts show intended actions without executing
- Verify output contains expected keywords
- Verify URLs and paths in dry-run output

### Integration Tests
- Test with real GitHub PRs (requires `gh` CLI and GH_TOKEN)
- Download artifacts in dry-run mode
- Verify version extraction from packages

## Running Tests

```bash
# Build the test project
dotnet build tests/Aspire.Cli.Scripts.Tests/

# Run all tests (excluding quarantined/outerloop)
dotnet test tests/Aspire.Cli.Scripts.Tests/ -- --filter-not-trait "quarantined=true" --filter-not-trait "outerloop=true"

# Run only shell script tests (Linux/macOS)
dotnet test tests/Aspire.Cli.Scripts.Tests/ -- --filter-class "*ShellTests" --filter-not-trait "quarantined=true" --filter-not-trait "outerloop=true"

# Run only PowerShell script tests
dotnet test tests/Aspire.Cli.Scripts.Tests/ -- --filter-class "*PowerShellTests" --filter-not-trait "quarantined=true" --filter-not-trait "outerloop=true"

# Run integration tests (requires gh CLI and GH_TOKEN)
export GH_TOKEN="your_github_token"
dotnet test tests/Aspire.Cli.Scripts.Tests/ -- --filter-class "*IntegrationTests" --filter-not-trait "quarantined=true" --filter-not-trait "outerloop=true"
```

## Platform-Specific Tests

- **Shell script tests** are skipped on Windows (bash not available)
- **PowerShell tests** require `pwsh` (PowerShell Core) to be installed
- **Integration tests** require `gh` CLI to be installed and authenticated

## Architecture

- **TestEnvironment** - Provides isolated temporary directory management
- **ScriptToolCommand** - Extends ToolCommand for script execution
- **RealGitHubPRFixture** - Finds real PRs for integration testing
- **RequiresGHCliAttribute** - Skips tests when gh CLI is not available

## Safety Verification

After running tests, you can verify no user directories were modified:

```bash
# This should return 0 (no files found)
find ~ -name "*aspire*" -type d -newer tests/Aspire.Cli.Scripts.Tests/ 2>/dev/null | wc -l
```

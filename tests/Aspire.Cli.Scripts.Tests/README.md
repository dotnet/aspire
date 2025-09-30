# CLI Scripts Tests

This project contains functional tests for the Aspire CLI acquisition scripts located in `eng/scripts/`.

## Target Scripts

The tests validate the following scripts:

- `get-aspire-cli.sh` / `get-aspire-cli.ps1` - Release and rolling builds
- `get-aspire-cli-pr.sh` / `get-aspire-cli-pr.ps1` - PR builds

## Safety Guarantees

**These tests are designed with safety as the top priority:**

1. **No User Directory Modifications**: All operations use temporary directories created in system temp locations
2. **Isolated Execution**: Each test gets its own isolated `TestEnvironment` with mock HOME directories
3. **No Shell Profile Changes**: Tests do not modify `.bashrc`, `.zshrc`, or any shell configuration files
4. **Repeatable**: Tests can be run thousands of times with no side effects
5. **No Real Downloads**: Tests primarily use `--dry-run` and `-WhatIf` modes

## Test Structure

```text
tests/Aspire.Cli.Scripts.Tests/
├── Common/
│   ├── TestEnvironment.cs      # Isolated temp directory management
│   └── ScriptExecutor.cs       # Safe script execution with environment isolation
├── ReleaseScriptTests.cs       # Tests for release CLI scripts
└── PrScriptTests.cs           # Tests for PR CLI scripts
```

## Running Tests

```bash
# Build the test project
dotnet build tests/Aspire.Cli.Scripts.Tests/

# Run all tests
dotnet test tests/Aspire.Cli.Scripts.Tests/ -- --filter-not-trait "quarantined=true" --filter-not-trait "outerloop=true"

# Run specific test
dotnet test tests/Aspire.Cli.Scripts.Tests/ -- --filter-method "Shell_HelpFlag_ShowsUsageWithoutError" --filter-not-trait "quarantined=true" --filter-not-trait "outerloop=true"
```

## Test Coverage

### Release Scripts (16 tests)

- Help flag functionality
- Dry-run/WhatIf modes
- Parameter validation (quality, OS, architecture)
- Custom install paths
- Verbose output
- Error handling for invalid inputs

### PR Scripts (12 tests)

- Missing PR number handling
- Dry-run/WhatIf modes
- Custom install paths
- OS/Architecture parameters
- Special flags (--hive-only, --skip-extension)
- GitHub CLI authentication handling

## Notes

- **PR Script Tests**: These tests handle the case where GitHub CLI is not authenticated, which is expected in CI environments
- **PowerShell Tests**: Tests gracefully skip if PowerShell/pwsh is not available
- **Shell Script Tests**: On Windows, tests skip if bash is not available
- **Execution Time**: Test suite typically completes in ~1 minute

## Implementation Details

### TestEnvironment Class

Creates an isolated temporary directory structure:

```text
/tmp/aspire-test-{guid}/
└── home/
    └── .bashrc  # Mock shell config to prevent script errors
```

### ScriptExecutor Class

- Executes scripts with redirected HOME/USERPROFILE environment variables
- Sets CI=true to prevent interactive prompts
- Captures stdout and stderr for validation
- Implements 60-second timeout to prevent hanging
- Properly cleans up processes


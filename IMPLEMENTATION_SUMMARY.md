# Aspire Doctor Command - Implementation Summary

## Overview
This PR adds VS Code extension support for the `aspire doctor` command to diagnose Aspire environment issues and check configuration. The implementation follows the established CLI/extension interaction pattern used in other commands like `aspire config`.

## Changes Made

### 1. CLI Changes (src/Aspire.Cli/Commands/DoctorCommand.cs)
- Added extension mode detection in `ExecuteAsync` method
- When running in extension mode (`ExtensionInteractionService`), automatically uses JSON format
- No changes to existing functionality - both human-readable and JSON outputs work as before
- Extension mode leverages existing JSON serialization and response structures

### 2. VS Code Extension Changes

#### package.json
- Added new command: `aspire-vscode.doctor` with title "Run diagnostics"
- Command is accessible via Command Palette

#### package.nls.json (Localization)
- Added command title: `"command.doctor": "Run diagnostics"`
- Added output channel name: `"aspireDoctorOutputChannel": "Aspire Diagnostics"`
- Added progress title: `"aspireDoctorTitle": "Aspire Doctor"`
- Added summary format: `"doctorSummary": "Aspire Doctor: {0} passed, {1} warnings, {2} failed"`

#### src/loc/strings.ts
- Added TypeScript exports for the new localized strings

#### src/commands/doctor.ts (New File)
- Implements the `doctorCommand` function
- Executes `aspire doctor --format Json` via `spawnCliProcess`
- Parses JSON response with structured types:
  - `DoctorCheckResult`: Individual check result
  - `DoctorCheckSummary`: Aggregate statistics
  - `DoctorCheckResponse`: Complete response structure
- Displays formatted results in Output Channel:
  - Groups checks by category (SDK, Container, Environment)
  - Shows status icons (✓, ⚠, ✗)
  - Includes fix suggestions and documentation links
  - Displays summary with counts
- Shows summary notification:
  - Error notification for failures
  - Warning notification for warnings
  - Information notification for all passed

#### src/extension.ts
- Imported `doctorCommand` from `./commands/doctor`
- Registered command: `vscode.commands.registerCommand('aspire-vscode.doctor', ...)`
- Added to context subscriptions for proper cleanup

## Architecture Pattern

The implementation follows the established pattern:
1. **Extension** invokes CLI command with `--format Json`
2. **CLI** detects extension mode and provides structured JSON output
3. **Extension** parses JSON and displays results in Output Channel
4. **No prompting logic in TypeScript** - all logic is in the CLI

## Testing

### Build Verification
- ✅ CLI builds successfully (`dotnet build src/Aspire.Cli/Aspire.Cli.csproj`)
- ✅ Extension compiles successfully (`npm run compile`)

### CLI Testing
- ✅ Human-readable output works correctly (`aspire doctor`)
- ✅ JSON output works correctly (`aspire doctor --format Json`)
- ✅ Exit codes work correctly (0 for pass, 1 for failures)

### Manual Testing Required
The following require manual verification in VS Code:
- Open Command Palette and run "Aspire: Run diagnostics"
- Verify Output Channel opens with formatted results
- Verify summary notification appears with correct severity
- Test with various environment states (all pass, warnings, failures)

See `TESTING_DOCTOR_COMMAND.md` for detailed manual testing guide.

## Sample Output

### CLI Human-Readable
```
Aspire Environment Check
========================

.NET SDK
  ✓  .NET 10.0.102 installed (x64)
  ⚠  HTTPS development certificate is not trusted
        Run: dotnet dev-certs https --trust
        See: https://aka.ms/aspire-prerequisites#dev-certs

Container Runtime
  ⚠  Docker Engine detected (version 28.0.4)
        Set environment variable: ASPIRE_ENABLE_CONTAINER_TUNNEL=true

Summary: 1 passed, 2 warnings, 0 failed
```

### CLI JSON (used by extension)
```json
{
  "checks": [
    {
      "category": "sdk",
      "name": "dotnet-sdk",
      "status": "pass",
      "message": ".NET 10.0.102 installed (x64)"
    },
    {
      "category": "sdk",
      "name": "dev-certs",
      "status": "warning",
      "message": "HTTPS development certificate is not trusted",
      "fix": "Run: dotnet dev-certs https --trust",
      "link": "https://aka.ms/aspire-prerequisites#dev-certs"
    }
  ],
  "summary": {
    "passed": 1,
    "warnings": 1,
    "failed": 0
  }
}
```

## Benefits

1. **Troubleshooting**: Helps users diagnose Aspire environment issues quickly
2. **Validation**: Verifies prerequisites before starting development
3. **Guidance**: Provides actionable fix suggestions and documentation links
4. **Integration**: Seamlessly integrates with existing VS Code workflow
5. **Consistency**: Uses the same diagnostic logic as the CLI

## Future Enhancements (Optional)

- Add VS Code Problems panel integration for critical issues
- Add quick-fix actions in Output Channel (e.g., clickable "Run command" links)
- Cache results to avoid re-running diagnostics frequently
- Add configuration to auto-run on workspace open

## Related Issue
Closes #14204 (part of the Aspire CLI VS Code extension support)

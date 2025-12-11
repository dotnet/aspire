# CLI Error Handling and User Feedback Improvements

This document describes the improvements made to the Aspire CLI error handling and user feedback based on user study feedback.

## Changes Implemented

### 1. Error Logging Infrastructure

**Feature**: All errors and exceptions are now automatically logged to persistent log files.

**Location**: `~/.aspire/logs/aspire-cli-YYYY-MM-DD.log`

**Implementation**:
- `IErrorLogger` service for centralized error logging
- `ErrorLogger` class that writes detailed error information including:
  - Timestamp (UTC and local)
  - Command context
  - Exception type and message
  - Full stack trace
  - Inner exceptions (if any)
  - Exception data

**Usage**:
```csharp
// Automatically logged by global exception handler in Program.cs
// Can also be used explicitly:
errorLogger.LogError(exception, "aspire run");
errorLogger.LogError("Error message", "Additional details", "aspire command");
```

### 2. Clean Error Display with --verbose Flag

**Feature**: Stack traces are hidden by default, showing user-friendly error messages instead.

**Default Behavior**:
- Shows clean error message
- Indicates that --verbose flag can be used for details
- Shows path to log file where full error is stored
- Provides link to troubleshooting guide

**With --verbose Flag**:
- Shows full exception details including stack traces
- Still logs to file and shows log file path

**Example Output (Default)**:

```text
An unexpected error occurred: The specified project file does not exist.
Use --verbose flag for detailed error information.
Full error details have been logged to: ~/.aspire/logs/aspire-cli-2025-12-10.log
For troubleshooting help, visit: https://aka.ms/aspire-troubleshooting
```

**Example Output (--verbose)**:

```text
System.IO.FileNotFoundException: The specified project file does not exist.
   at Aspire.Cli.Projects.ProjectLocator.FindProjectFile(...)
   at Aspire.Cli.Commands.RunCommand.ExecuteAsync(...)
Full error details have been logged to: ~/.aspire/logs/aspire-cli-2025-12-10.log
For troubleshooting help, visit: https://aka.ms/aspire-troubleshooting
```

### 3. Certificate Trust Dialog Warning

**Feature**: When certificate trust is required, users are warned that OS dialogs may appear in the background.

**Warning Message**:

```text
A certificate trust dialog may appear in the background. Please check your taskbar and bring the dialog to the foreground to approve the certificate trust.
```

**Location**: Shown during `aspire run` when certificates need to be trusted.

### 4. Prerequisite Checking

**Feature**: Comprehensive system prerequisite validation before running commands.

**Checks Include**:
1. **.NET SDK Version**: Validates that .NET 10.0 or later is installed
2. **Container Runtime**: Detects Docker Desktop, Docker Engine, or Podman
3. **WSL Environment**: Detects WSL and provides setup guidance
4. **Docker Engine vs Desktop**: Identifies Docker Engine usage and provides tunnel configuration guidance

**Implementation**:
- `IPrerequisiteChecker` service
- `PrerequisiteChecker` class with methods for each check
- `PrerequisiteCheckResult` containing errors and warnings

**Example Usage**:
```csharp
var result = await prerequisiteChecker.CheckPrerequisitesAsync(cancellationToken);

if (!result.IsValid)
{
    foreach (var error in result.Errors)
    {
        interactionService.DisplayError(error);
    }
}

foreach (var warning in result.Warnings)
{
    interactionService.DisplayMessage("warning", warning);
}
```

### 5. WSL Environment Detection

**Feature**: Automatically detects WSL environment and provides helpful warnings.

**Detection Methods**:
1. Checks `/proc/version` for "microsoft" or "WSL"
2. Checks `WSL_DISTRO_NAME` environment variable

**Warning Example**:

```text
Running in WSL environment. For optimal performance, ensure WSL integration is properly configured with Docker Desktop.
See: https://aka.ms/aspire-setup
```

### 6. Docker Engine vs Desktop Detection

**Feature**: Detects whether Docker Engine (non-Desktop) is being used.

**Detection Method**:
- Runs `docker context ls` to check for desktop context
- If no desktop context found, likely using Docker Engine

**Warning Example**:

```text
Using Docker Engine (not Docker Desktop). You may need to configure the Aspire tunnel for service-to-service communication.
See: https://aka.ms/aspire-docker-engine
```

### 7. Consistent Troubleshooting Links

**Feature**: All error exits now show a link to the troubleshooting guide.

**URL**: `https://aka.ms/aspire-troubleshooting`

**Implementation**:
- Added to `ErrorStrings.resx` as `TroubleshootingGuideUrl`
- Displayed by `ErrorHandlingExtensions` helper methods
- Shown by global exception handler in `Program.cs`

### 8. Terminal Capability Detection

**Feature**: Already implemented through `CliHostEnvironment` and `AnsiConsole`.

**Capabilities**:
- Detects ANSI support
- Detects emoji support
- Falls back to plain rendering when not supported
- Respects `NO_COLOR` environment variable
- Supports `ASPIRE_ANSI_PASS_THRU` for forcing ANSI in redirected output

## New Command-Line Options

### --verbose

**Purpose**: Display detailed error information including stack traces.

**Usage**: `aspire [command] --verbose`

**Example**: 
```bash
aspire run --verbose
aspire new --verbose
```

**Recursive**: This option is available for all commands.

## New Services

### IErrorLogger / ErrorLogger

**Purpose**: Centralized error logging to files.

**Registration**: Singleton in DI container.

**Methods**:
- `string LogError(Exception exception, string? commandContext = null)`
- `string LogError(string message, string? details = null, string? commandContext = null)`
- `DirectoryInfo GetLogsDirectory()`

### IPrerequisiteChecker / PrerequisiteChecker

**Purpose**: System prerequisite validation.

**Registration**: Singleton in DI container.

**Methods**:
- `Task<PrerequisiteCheckResult> CheckPrerequisitesAsync(CancellationToken cancellationToken = default)`
- `Task<(bool IsValid, string? InstalledVersion, string? Message)> CheckDotNetSdkAsync(...)`
- `Task<(bool IsAvailable, string? RuntimeName, string? Message)> CheckContainerRuntimeAsync(...)`
- `(bool IsWSL, string? Warning) CheckWSLEnvironment()`
- `Task<(bool IsDockerEngine, string? Message)> CheckDockerEngineAsync(...)`

## New Resource Strings

### ErrorStrings

- `ErrorLoggedToFile`: "Full error details have been logged to: {0}"
- `TroubleshootingGuideUrl`: "For troubleshooting help, visit: https://aka.ms/aspire-troubleshooting"
- `CertificateTrustDialogWarning`: "A certificate trust dialog may appear in the background..."
- `UnexpectedError`: "An unexpected error occurred: {0}"
- `UseVerboseForDetails`: "Use --verbose flag for detailed error information."

### RunCommandStrings

- `StartupTakingLongerThanExpected`: "Startup is taking longer than expected. Press Ctrl+C to cancel or wait to continue..."
- `StillWaitingForStartup`: "Still waiting for startup to complete..."

## Error Handling Flow

### Global Exception Handler (Program.cs)

1. Command execution wrapped in try-catch
2. On exception:
   - Log full error details to file via `IErrorLogger`
   - Check if `--verbose` flag was provided
   - Display user-friendly error message (or full details if verbose)
   - Show log file path
   - Show troubleshooting guide link
   - Return appropriate exit code

### Command-Level Exception Handling

Individual commands can use `ErrorHandlingExtensions`:

```csharp
try
{
    // Command execution
}
catch (Exception ex)
{
    errorLogger.HandleException(
        interactionService,
        ex,
        commandContext: "aspire run",
        verbose: verboseFlag);
    return ExitCodeConstants.FailedToDoSomething;
}
```

## Testing

### Unit Tests

**Location**: `tests/Aspire.Cli.Tests/Utils/ErrorLoggerTests.cs`

**Tests**:
- `LogError_Exception_CreatesLogFile`: Verifies exception logging creates log file
- `LogError_Message_CreatesLogFile`: Verifies message logging creates log file
- `GetLogsDirectory_ReturnsCorrectPath`: Verifies logs directory path

**Run Tests**:
```bash
dotnet test tests/Aspire.Cli.Tests/Aspire.Cli.Tests.csproj \
  --filter "FullyQualifiedName~ErrorLoggerTests" \
  -- --filter-not-trait "quarantined=true" --filter-not-trait "outerloop=true"
```

## Future Enhancements

### Not Yet Implemented

1. **Long Startup Handling**: 
   - Add timeout monitoring for app-host startup
   - Show periodic "still waiting" messages
   - Strings already added to `RunCommandStrings.resx`

2. **Consistent Error Pattern**: 
   - Apply error logging to all command exception handlers
   - Ensure all commands show troubleshooting links on error

3. **Enhanced Terminal Detection**:
   - Add explicit suggestions for modern terminals when capabilities are limited
   - Provide fallback rendering modes

## Configuration

### Environment Variables

- `ASPIRE_ANSI_PASS_THRU`: Force ANSI colors even when output is redirected
- `ASPIRE_CONSOLE_WIDTH`: Override terminal width detection
- `NO_COLOR`: Disable ANSI colors
- `ASPIRE_NON_INTERACTIVE`: Disable interactive prompts
- `ASPIRE_PLAYGROUND`: Force interactive mode

### Log File Location

Logs are written to: `$HOME/.aspire/logs/aspire-cli-YYYY-MM-DD.log`

One log file is created per day to prevent files from growing too large.

## Migration Guide

### For CLI Users

No migration required. New features are additive:
- Error logging happens automatically
- Use `--verbose` flag for detailed errors when needed
- Check log files in `~/.aspire/logs/` for full error details

### For CLI Contributors

When adding new commands or modifying existing ones:

1. **Exception Handling**: Use `ErrorHandlingExtensions` for consistent error reporting:
   ```csharp
   catch (Exception ex)
   {
       errorLogger.HandleException(interactionService, ex, "command context", verbose);
       return ExitCodeConstants.Appropriate;
   }
   ```

2. **Prerequisite Checks**: Consider adding prerequisite validation:
   ```csharp
   var prereqResult = await prerequisiteChecker.CheckPrerequisitesAsync(cancellationToken);
   if (!prereqResult.IsValid)
   {
       // Handle prerequisite failures
   }
   ```

3. **Resource Strings**: Add new error messages to appropriate `.resx` files and update `.Designer.cs` files.

4. **Testing**: Add tests for error scenarios to ensure proper logging and user feedback.

## References

- Issue: "Onboarding feedback from user studies take 0"
- Troubleshooting Guide: https://aka.ms/aspire-troubleshooting (placeholder)
- Setup Guide: https://aka.ms/aspire-setup (placeholder)
- Docker Engine Configuration: https://aka.ms/aspire-docker-engine (placeholder)

# Aspire CLI Non-Interactive Mode Specification

## Overview

This document specifies the design and implementation plan for making the Aspire CLI's `--non-interactive` mode work holistically across all commands. Currently, the CLI is primarily interactive, and while a `--non-interactive` flag exists, commands do not consistently handle it, leading to failures in automation scenarios (CI/CD pipelines, scripting, agent workflows).

## Background

### Current State

The Aspire CLI currently has partial non-interactive support:

1. **Global `--non-interactive` flag**: Defined in `RootCommand.cs` as a recursive option
2. **`ICliHostEnvironment`**: Tracks `SupportsInteractiveInput` and `SupportsInteractiveOutput` properties
3. **`ConsoleInteractionService`**: Throws `InvalidOperationException` when prompts are called in non-interactive mode
4. **CI Detection**: Automatically detects CI environments and disables interactive features
5. **Environment Variable**: Supports `ASPIRE_NON_INTERACTIVE=true|1` to force non-interactive mode

### Problems with Current Implementation

1. **Commands fail unexpectedly**: Many commands call interactive prompts without first checking if alternatives exist
2. **No CLI argument alternatives**: Interactive prompts lack corresponding command-line options
3. **Error messages are not actionable**: When prompts fail in non-interactive mode, users don't know what CLI arguments to provide instead
4. **Inconsistent behavior**: Some commands work, others fail, with no clear documentation
5. **No defaults in non-interactive mode**: Commands don't provide sensible defaults when no input is available

## Goals

1. **Every command should work in non-interactive mode** with appropriate CLI arguments or sensible defaults
2. **Clear error messages** when required input is missing in non-interactive mode
3. **Consistent patterns** across all commands for handling non-interactive scenarios
4. **Documentation** of required arguments for each command in non-interactive mode
5. **Backwards compatibility** with existing interactive behavior

## Non-Goals

- Removing interactive features from the CLI
- Changing the default interactive behavior
- Adding prompts to commands that currently don't need them

## Design

### Design Principles

1. **CLI arguments first, prompts second**: Every interactive prompt should have a corresponding CLI argument
2. **Fail fast with actionable errors**: In non-interactive mode, fail immediately with clear guidance on required arguments
3. **Sensible defaults**: Where possible, provide defaults that allow commands to succeed without prompts
4. **Consistent error format**: Use a standard format for missing-argument errors in non-interactive mode

### Error Message Format

When a command requires user input that wasn't provided via CLI arguments:

```
Error: The '<argument-name>' argument is required in non-interactive mode.
       Use '--<argument-name> <value>' to specify the value.
       
       Example: aspire <command> --<argument-name> <example-value>
```

For selection prompts where multiple values are possible:

```
Error: The '<argument-name>' argument is required in non-interactive mode.
       Use '--<argument-name> <value>' to specify the value.
       
       Available options:
         - option1: Description of option1
         - option2: Description of option2
         
       Example: aspire <command> --<argument-name> option1
```

### Implementation Pattern

Each command that uses interactive prompts should follow this pattern:

```csharp
protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
{
    // 1. Get value from CLI arguments
    var value = parseResult.GetValue<string?>("--argument-name");
    
    // 2. If value is null and we're interactive, prompt for it
    if (value is null)
    {
        if (_hostEnvironment.SupportsInteractiveInput)
        {
            value = await _prompter.PromptForValueAsync(options, cancellationToken);
        }
        else
        {
            // 3. In non-interactive mode, either use a default or fail with clear error
            if (CanUseDefault)
            {
                value = DefaultValue;
                InteractionService.DisplaySubtleMessage($"Using default value: {value}");
            }
            else
            {
                return DisplayNonInteractiveModeError("argument-name", availableOptions);
            }
        }
    }
    
    // 4. Continue with command execution
    // ...
}
```

## Command-by-Command Analysis

### `aspire new`

**Interactive behaviors:**
- Prompts for project template selection (if not specified as subcommand)
- Prompts for project name (if `--name` not specified)
- Prompts for output path (if `--output` not specified)
- Prompts for template version (if multiple versions available)

**Current CLI arguments:**
- `--name, -n`: Project name
- `--output, -o`: Output directory
- `--version, -v`: Template version
- `--source, -s`: NuGet source
- `--channel`: Package channel

**Required changes:**
1. Add template selection via subcommand (already exists) or `--template` argument
2. Default project name to current directory name if not specified
3. Default output path to current directory if not specified
4. Default to latest stable version if not specified
5. Add clear error messages when template is required but not specified

**Non-interactive behavior:**
```bash
# Works - template specified as subcommand, uses defaults for name/output
aspire new starter

# Works - fully specified
aspire new starter --name MyApp --output ./MyApp

# Fails with clear error - no template specified
aspire new --name MyApp
# Error: A template must be specified in non-interactive mode.
#        Available templates: starter, aspire, aspire-empty, ...
#        Example: aspire new starter --name MyApp
```

### `aspire init`

**Interactive behaviors:**
- Prompts for project selections when adding to AppHost
- Prompts for ServiceDefaults configuration
- Prompts for template version selection

**Current CLI arguments:**
- `--version, -v`: Template version
- `--source, -s`: NuGet source
- `--channel`: Package channel

**Required changes:**
1. Add `--add-projects` argument to specify which projects to add (comma-separated or "all"/"none")
2. Add `--add-service-defaults` argument to specify ServiceDefaults behavior ("all"/"choose"/"none")
3. In non-interactive mode, default to adding no projects (AppHost-only initialization)
4. Default to latest stable template version

**Non-interactive behavior:**
```bash
# Works - initializes AppHost only, no project additions
aspire init

# Works - adds all projects to AppHost
aspire init --add-projects all

# Works - adds specific projects
aspire init --add-projects "Project1,Project2" --add-service-defaults all
```

### `aspire add`

**Interactive behaviors:**
- Prompts for integration package selection
- Prompts for package version selection

**Current CLI arguments:**
- `integration`: Positional argument for integration name
- `--project`: AppHost project file
- `--version, -v`: Package version
- `--source, -s`: NuGet source

**Required changes:**
1. Require `integration` argument in non-interactive mode
2. Default to latest version if `--version` not specified
3. Better error message listing available integrations

**Non-interactive behavior:**
```bash
# Works - integration and version specified
aspire add redis --version 9.0.0

# Works - uses latest version
aspire add postgresql

# Fails with clear error
aspire add
# Error: The 'integration' argument is required in non-interactive mode.
#        Use 'aspire add <integration-name>' to specify the integration.
#        
#        To list available integrations, run: aspire add --list
```

### `aspire run`

**Interactive behaviors:**
- No significant interactive prompts (mostly output-focused)
- Status spinners and progress indicators

**Current CLI arguments:**
- `--project`: AppHost project file
- `--wait-for-debugger`: Wait for debugger attachment

**Required changes:**
1. Already works well in non-interactive mode
2. Status spinners already fall back to simple messages in non-interactive mode

**Non-interactive behavior:**
```bash
# Works
aspire run --project ./MyApp.AppHost/MyApp.AppHost.csproj
```

### `aspire publish`

**Interactive behaviors:**
- Prompts for publisher selection
- Pipeline activity prompts for user input (credentials, confirmations, etc.)

**Current CLI arguments:**
- `--project`: AppHost project file
- `--output-path, -o`: Output path for artifacts
- `--log-level`: Logging verbosity
- `--environment, -e`: Target environment

**Required changes:**
1. Add `--publisher` argument for non-interactive publisher selection
2. Pipeline prompts need special handling (see "Pipeline Commands" section)
3. Add `--yes` or `--assume-yes` flag to auto-confirm prompts
4. Add ability to provide answers to known prompts via arguments or environment variables

**Non-interactive behavior:**
```bash
# Works if no prompts are required
aspire publish --output-path ./artifacts

# For pipeline prompts, use environment variables
export ASPIRE_PUBLISH_AZURE_SUBSCRIPTION=<subscription-id>
export ASPIRE_PUBLISH_RESOURCE_GROUP=<resource-group>
aspire publish
```

### `aspire deploy`

**Interactive behaviors:**
- Same as `aspire publish` (extends `PipelineCommandBase`)
- Pipeline activity prompts

**Required changes:**
- Same as `aspire publish`

### `aspire do`

**Interactive behaviors:**
- Same as `aspire publish` (extends `PipelineCommandBase`)
- Pipeline activity prompts

**Required changes:**
- Same as `aspire publish`

### `aspire doctor`

**Interactive behaviors:**
- Status spinner during checks

**Current CLI arguments:**
- `--json`: Output results as JSON

**Required changes:**
1. Already works well in non-interactive mode
2. `--json` flag provides machine-readable output

### `aspire config`

**Interactive behaviors:**
- Minimal interactive elements

**Required changes:**
1. Already works well in non-interactive mode

### `aspire cache`

**Interactive behaviors:**
- Minimal interactive elements

**Required changes:**
1. Already works well in non-interactive mode

### `aspire update`

**Interactive behaviors:**
- May prompt for confirmation on updates

**Required changes:**
1. Add `--yes` flag to auto-confirm updates
2. Default to showing update info only (no action) in non-interactive mode

### `aspire mcp`

**Interactive behaviors:**
- MCP server is inherently non-interactive (stdio transport)

**Required changes:**
1. Already designed for non-interactive use

### `aspire exec`

**Interactive behaviors:**
- Minimal interactive elements

**Required changes:**
1. Already works in non-interactive mode

## Pipeline Commands Special Handling

The `PipelineCommandBase`-derived commands (`publish`, `deploy`, `do`) have special interactive requirements because they forward prompts from the AppHost pipeline execution.

### Current Behavior

When `SupportsInteractiveInput` is false, the pipeline sets `env[KnownConfigNames.InteractivityEnabled] = "false"` which tells the AppHost to disable interactive prompts.

### Required Changes

1. **AppHost pipeline should have non-interactive defaults**: When interactivity is disabled, the pipeline should use sensible defaults or fail with clear error messages

2. **Environment variable-based answers**: Support providing answers to known prompts via environment variables:
   ```
   ASPIRE_PROMPT_<PROMPT_ID>=<value>
   ```

3. **Answers file**: Support a JSON file with pre-configured answers:
   ```bash
   aspire publish --answers-file ./publish-config.json
   ```
   
   ```json
   {
     "azure-subscription": "subscription-id",
     "resource-group": "my-rg",
     "confirm-deployment": true
   }
   ```

4. **Fail-fast mode**: In non-interactive mode, if a required prompt cannot be answered, fail immediately with clear error listing what's needed

## Implementation Phases

### Phase 1: Foundation (Current PR)

- [x] Document current state and gaps
- [ ] Define standard patterns for non-interactive handling
- [ ] Create helper methods for non-interactive error messages
- [ ] Update `IInteractionService` with non-interactive-aware methods

### Phase 2: Core Commands

- [ ] Update `aspire new` with complete non-interactive support
- [ ] Update `aspire init` with `--add-projects` and `--add-service-defaults`
- [ ] Update `aspire add` with better error messages

### Phase 3: Pipeline Commands

- [ ] Design environment variable-based prompt answering
- [ ] Implement answers file support
- [ ] Update `aspire publish`, `aspire deploy`, `aspire do`

### Phase 4: Documentation and Testing

- [ ] Update CLI help text with non-interactive usage examples
- [ ] Add comprehensive tests for non-interactive scenarios
- [ ] Create documentation for CI/CD usage

## Testing Strategy

### Unit Tests

Each command should have tests that verify:
1. Command works with all required arguments in non-interactive mode
2. Command fails with clear error when required arguments are missing
3. Default values are applied correctly

### Integration Tests

End-to-end tests in CI environment:
1. Run commands with `--non-interactive` flag
2. Verify no hanging prompts
3. Verify exit codes are correct

### Example Test Structure

```csharp
[Fact]
public async Task NewCommand_NonInteractive_FailsWithClearError_WhenTemplateNotSpecified()
{
    // Arrange
    var command = CreateNewCommand(nonInteractive: true);
    
    // Act
    var result = await command.ExecuteAsync(["new", "--name", "MyApp"]);
    
    // Assert
    Assert.Equal(ExitCodeConstants.MissingRequiredArgument, result);
    Assert.Contains("template must be specified in non-interactive mode", _output.ToString());
    Assert.Contains("Available templates:", _output.ToString());
}

[Fact]
public async Task NewCommand_NonInteractive_Succeeds_WithAllRequiredArguments()
{
    // Arrange
    var command = CreateNewCommand(nonInteractive: true);
    
    // Act
    var result = await command.ExecuteAsync(["new", "starter", "--name", "MyApp"]);
    
    // Assert
    Assert.Equal(ExitCodeConstants.Success, result);
}
```

## Migration Guide

### For Users

If you're using the Aspire CLI in scripts or CI/CD:

1. Always pass `--non-interactive` to ensure consistent behavior
2. Provide all required arguments explicitly
3. Check command exit codes for success/failure
4. Use `--json` output format where available for parsing

### For Command Authors

When adding new commands or modifying existing ones:

1. Every interactive prompt must have a CLI argument alternative
2. Use `_hostEnvironment.SupportsInteractiveInput` before prompting
3. Provide sensible defaults where possible
4. Use the standard error message format for missing arguments
5. Add tests for non-interactive scenarios

## Appendix: Command Reference (Non-Interactive Mode)

### Quick Reference Table

| Command | Required Args (Non-Interactive) | Optional Args | Default Behavior |
|---------|--------------------------------|---------------|------------------|
| `aspire new <template>` | Template (as subcommand) | `--name`, `--output`, `--version` | Uses directory name, current dir, latest version |
| `aspire init` | None | `--add-projects`, `--add-service-defaults`, `--version` | Creates AppHost only |
| `aspire add <integration>` | `integration` | `--version`, `--project` | Uses latest version |
| `aspire run` | None | `--project` | Finds AppHost automatically |
| `aspire publish` | None (unless prompts required) | `--output-path`, `--publisher`, `--answers-file` | Fails if prompts needed |
| `aspire deploy` | None (unless prompts required) | `--output-path`, `--answers-file` | Fails if prompts needed |
| `aspire doctor` | None | `--json` | Runs all checks |
| `aspire config` | Subcommand | Various | Depends on subcommand |

## References

- [Aspire CLI README](../../src/Aspire.Cli/README.md)
- [System.CommandLine Documentation](https://docs.microsoft.com/en-us/dotnet/standard/commandline/)
- [CI/CD Best Practices](https://docs.microsoft.com/en-us/azure/devops/pipelines/process/best-practices)

## Changelog

- **2025-06-25**: Initial draft based on codebase investigation

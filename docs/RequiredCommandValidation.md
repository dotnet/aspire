# Required Command Validation for Resources

This document describes how to use the `RequiredCommandAnnotation` feature to declare that resources require specific executables to be available on the local machine before they can start.

## Overview

The `RequiredCommandAnnotation` allows any resource to declare that it requires a specific command/executable to be available on the local machine PATH. The orchestrator will validate these requirements before starting the resource, providing clear error messages if dependencies are missing.

## Basic Usage

### Declaring a Required Command

Use the `WithRequiredCommand` extension method to add a required command annotation to any resource:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Require that 'docker' is available
builder.AddContainer("mycontainer", "myimage")
    .WithRequiredCommand("docker");

// Require that 'node' is available, with a help link
builder.AddExecutable("frontend", "npm", workingDirectory: "./frontend", args: ["run", "dev"])
    .WithRequiredCommand("node", helpLink: "https://nodejs.org/en/download/");

// Require that 'dotnet' is available
builder.AddProject<Projects.MyService>("myservice")
    .WithRequiredCommand("dotnet");
```

### Multiple Required Commands

A resource can declare multiple required commands:

```csharp
builder.AddContainer("buildcontainer", "buildimage")
    .WithRequiredCommand("docker")
    .WithRequiredCommand("git")
    .WithRequiredCommand("make");
```

## Advanced Usage

### Custom Validation

You can provide a custom validation callback that is invoked after the command is resolved to a full path. This allows you to verify additional requirements, such as version checks:

```csharp
builder.AddExecutable("app", "node", workingDirectory: "./app")
    .WithRequiredCommand("node", async (resolvedPath, ct) =>
    {
        // Run 'node --version' to check the version
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = resolvedPath,
            Arguments = "--version",
            RedirectStandardOutput = true,
            UseShellExecute = false
        });
        
        if (process == null)
        {
            return (false, "Failed to execute node");
        }

        await process.WaitForExitAsync(ct);
        var versionOutput = await process.StandardOutput.ReadToEndAsync(ct);
        
        // Parse and check version (simplified example)
        if (!versionOutput.StartsWith("v20") && !versionOutput.StartsWith("v22"))
        {
            return (false, $"Node.js version 20 or 22 required, found: {versionOutput.Trim()}");
        }

        return (true, null);
    },
    helpLink: "https://nodejs.org/en/download/");
```

## How It Works

1. **Annotation Declaration**: Resources are annotated with `RequiredCommandAnnotation` instances using `WithRequiredCommand`.

2. **Lifecycle Hook**: A lifecycle hook (`RequiredCommandValidationLifecycleHook`) subscribes to `BeforeResourceStartedEvent`.

3. **Command Resolution**: When a resource is about to start, the hook:
   - Retrieves all `RequiredCommandAnnotation` instances from the resource
   - For each annotation, resolves the command to a full path using `CommandResolver.ResolveCommand`
   - If a custom validation callback is provided, invokes it with the resolved path

4. **Validation Results**:
   - If a command is found and passes validation (or no validation is provided), the resource starts normally
   - If a command is missing or fails validation, a `DistributedApplicationException` is thrown with a detailed error message
   - If a help link is provided, it's included in the error message

## Command Resolution

Commands are resolved in the following order:

1. **Path with Directory Separator**: If the command contains a directory separator (e.g., `/usr/bin/node` or `./local/bin/app`), it's treated as a path and checked for existence
   
2. **PATH Environment Variable**: The command is searched for in directories listed in the PATH environment variable

3. **PATHEXT on Windows**: On Windows, if the command has no extension, PATHEXT environment variable is used to try different extensions (`.COM`, `.EXE`, `.BAT`, `.CMD`)

## Error Messages

### Missing Command

```
Required command 'my-tool' was not found on PATH or at the specified location.
```

### Missing Command with Help Link

```
Required command 'my-tool' was not found on PATH or at the specified location. For installation instructions, see: https://example.com/install
```

### Failed Validation

```
Command 'node' validation failed: Node.js version 20 or 22 required, found: v18.20.0. For installation instructions, see: https://nodejs.org/en/download/
```

## Comparison with RequiredCommandValidator

Previously, the `RequiredCommandValidator` class in `Aspire.Hosting.DevTunnels` provided similar functionality but was tightly coupled to specific use cases. The new `RequiredCommandAnnotation` approach offers:

- **Generic**: Works with any resource type, not just executables
- **Declarative**: Uses annotations instead of inheritance
- **Flexible**: Supports custom validation callbacks
- **Consistent**: Integrates with the standard resource lifecycle events
- **Reusable**: The `CommandResolver` utility can be used independently

The original `RequiredCommandValidator` continues to work and now uses the same `CommandResolver` utility internally.

## API Reference

### RequiredCommandAnnotation

```csharp
public class RequiredCommandAnnotation(string command) : IResourceAnnotation
{
    public string Command { get; }
    public string? HelpLink { get; init; }
    public Func<string, CancellationToken, Task<(bool IsValid, string? ValidationMessage)>>? ValidationCallback { get; init; }
}
```

### WithRequiredCommand Extension Methods

```csharp
public static IResourceBuilder<T> WithRequiredCommand<T>(
    this IResourceBuilder<T> builder,
    string command,
    string? helpLink = null) where T : IResource

public static IResourceBuilder<T> WithRequiredCommand<T>(
    this IResourceBuilder<T> builder,
    string command,
    Func<string, CancellationToken, Task<(bool IsValid, string? ValidationMessage)>> validationCallback,
    string? helpLink = null) where T : IResource
```

### CommandResolver

```csharp
public static class CommandResolver
{
    public static string? ResolveCommand(string command);
}
```

# Container Stdin Support

## Overview

This document describes the implementation of bidirectional console log streaming support between the Aspire dashboard and DCP (Developer Control Plane). This feature enables writing data to the standard input (stdin) of containers, which is useful for scenarios where applications communicate via stdio pipes.

## Motivation

Some applications are designed to communicate via standard input/output streams. For example:
- Language servers (LSP) that communicate over stdio
- Interactive CLI tools
- Process-to-process communication pipelines
- AI model inference servers that accept prompts via stdin

Previously, Aspire only supported reading container logs (stdout/stderr). This enhancement adds the ability to write to container stdin, enabling full bidirectional communication.

## Architecture

### Components Modified

#### DCP (Developer Control Plane) - Go

1. **Container Spec Extension** (`api/v1/container_types.go`)
   - Added `Stdin bool` field to `ContainerSpec`
   - This field indicates whether the container should be started with stdin support enabled

2. **Container Orchestrator Interface** (`internal/containers/container_orchestrator.go`)
   - Added `WriteContainerStdinOptions` struct for passing stdin write parameters
   - Added `WriteContainerStdin` interface with the write method
   - Extended `ContainerOrchestrator` interface to include stdin writing capability

3. **Docker CLI Orchestrator** (`internal/docker/cli_orchestrator.go`)
   - Modified `applyCreateContainerOptions` to add `-i` flag when `Stdin` is true
   - Implemented `WriteContainerStdin` method using `docker exec -i container /bin/cat`

4. **Podman CLI Orchestrator** (`internal/podman/cli_orchestrator.go`)
   - Implemented `WriteContainerStdin` method using `podman exec -i container sh -c cat`

#### Aspire.Hosting - C#

1. **DCP Model** (`Dcp/Model/Container.cs`)
   - Added `Stdin` property to `ContainerSpec` class to match DCP's Go model

2. **Container Stdin Annotation** (`ApplicationModel/ContainerStdinAnnotation.cs`)
   - New annotation class that marks a container resource as requiring stdin support

3. **Builder Extensions** (`ContainerResourceBuilderExtensions.cs`)
   - Added `WithStdin<T>()` extension method to enable stdin on container resources

4. **DCP Executor** (`Dcp/DcpExecutor.cs`)
   - Modified `PrepareContainers` to check for `ContainerStdinAnnotation` and set `Spec.Stdin = true`

### Data Flow

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│                 │     │                 │     │                 │
│  Aspire App     │────▶│      DCP        │────▶│    Container    │
│  (AppHost)      │     │   (API Server)  │     │   (Docker/      │
│                 │     │                 │     │    Podman)      │
└─────────────────┘     └─────────────────┘     └─────────────────┘
        │                       │                       │
        │  WithStdin()          │  Stdin: true          │  -i flag
        │  annotation           │  in ContainerSpec     │  on create
        │                       │                       │
        ▼                       ▼                       ▼
   Container is            DCP creates             Container runs
   configured for          container with          with stdin
   stdin support           stdin enabled           attached
```

## Usage

### Enabling Stdin on a Container

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add a container with stdin support enabled
var myContainer = builder.AddContainer("my-app", "my-image")
    .WithStdin();  // Enable stdin support

builder.Build().Run();
```

### How It Works

1. When `WithStdin()` is called, a `ContainerStdinAnnotation` is added to the resource
2. During container preparation, `DcpExecutor` checks for this annotation
3. If present, `Spec.Stdin = true` is set on the DCP Container resource
4. DCP creates the container with the `-i` flag (interactive mode)
5. The container's stdin remains open for writing

### Writing to Container Stdin

When stdin support is enabled via `WithStdin()`, a "Send Input" command is automatically added to the resource. This command appears in the Aspire dashboard and allows users to send text input to the container's stdin stream.

The command:
1. Prompts the user for text input via a dialog
2. Sends the input (with a trailing newline) to the container's stdin
3. Only appears when the container is in the "Running" state

The dashboard uses the internal `IResourceConsoleInputService` to send input to the container via DCP's stdin writing capability.

## API Reference

### ContainerStdinAnnotation

```csharp
/// <summary>
/// Annotation that enables stdin support for a container resource.
/// When this annotation is present, the container will be started with
/// stdin attached, allowing data to be written to the container's stdin.
/// </summary>
public sealed class ContainerStdinAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Gets or sets whether stdin support is enabled. Defaults to true.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
```

### WithStdin Extension Method

```csharp
/// <summary>
/// Enables stdin support for a container resource.
/// When stdin is enabled, a "Send Input" command is added to the resource
/// that allows users to send text input to the container's stdin from the dashboard.
/// </summary>
/// <typeparam name="T">The type of container resource.</typeparam>
/// <param name="builder">The resource builder.</param>
/// <param name="enabled">Whether stdin support is enabled. Defaults to true.</param>
/// <returns>The resource builder for chaining.</returns>
public static IResourceBuilder<T> WithStdin<T>(
    this IResourceBuilder<T> builder, 
    bool enabled = true) where T : ContainerResource
```

### DCP WriteContainerStdinOptions (Go)

```go
// WriteContainerStdinOptions defines options for writing to container stdin
type WriteContainerStdinOptions struct {
    // The container (name/id) to write to
    Container string

    // The data to write to stdin
    Data []byte
}
```

## Testing

A playground application `StdioEndToEnd` is provided to demonstrate the stdin functionality. It includes:

1. A container that reads from stdin and echoes to stdout
2. Configuration showing how to enable stdin support

## Future Work

1. **gRPC Streaming**: Implement bidirectional gRPC streaming for real-time stdin/stdout communication
2. **Log Subresource POST Handler**: Add HTTP POST endpoint in DCP for writing to stdin via the logs subresource
3. **Advanced Input Modes**: Support for binary data, file uploads, and multi-line input

## Related Issues

- Bidirectional console log streaming between dashboard and DCP

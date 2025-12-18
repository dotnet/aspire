# Stdio End-to-End Playground

This playground demonstrates the container stdin support feature in Aspire.

## Overview

The `WithStdin()` extension method enables stdin support for containers, allowing them to be started with the `-i` (interactive) flag. This is useful for:

- Interactive shells and CLI tools
- Language servers (LSP) that communicate via stdio
- Process-to-process communication pipelines
- Any application that reads from stdin

## Using the "Send Input" Command

When `WithStdin()` is called on a container resource, a **"Send Input"** command is automatically added to the resource. This command appears in the Aspire dashboard and allows you to send text input to the container's stdin.

### To use the command:

1. Run the playground: `dotnet run`
2. Open the Aspire dashboard (URL shown in console output)
3. Navigate to the **Resources** page
4. Click on a container with stdin enabled (e.g., `stdin-echo`)
5. Click the **"Send Input"** button in the resource commands
6. Enter your text in the dialog and click OK
7. The input will be sent to the container's stdin (with a trailing newline)
8. View the container's console logs to see the output

### Example Workflow

For the `interactive-shell` container:
1. Send "help" to see available commands
2. Send "status" to get container status
3. Send "echo Hello World" to echo text back

## Containers in this Playground

### 1. stdin-echo

A simple container that reads from stdin and echoes each line back with a "Received: " prefix.

```csharp
builder.AddContainer("stdin-echo", "alpine")
    .WithArgs("sh", "-c", "while read line; do echo \"Received: $line\"; done")
    .WithStdin();
```

### 2. stdin-filter

A container that filters stdin input, only outputting lines containing "hello".

```csharp
builder.AddContainer("stdin-filter", "alpine")
    .WithArgs("sh", "-c", "grep 'hello'")
    .WithStdin();
```

### 3. interactive-shell

A more complex container simulating an interactive command-line interface with multiple commands.

```csharp
builder.AddContainer("interactive-shell", "alpine")
    .WithArgs("sh", "-c", "...")  // Shell script with command handling
    .WithStdin();
```

## Running the Playground

```bash
cd playground/StdioEndToEnd/StdioEndToEnd.AppHost
dotnet run
```

## How It Works

1. `WithStdin()` adds a `ContainerStdinAnnotation` to the container resource
2. A "Send Input" command is added that uses `IInteractionService` to prompt for input
3. During container creation, `DcpExecutor` checks for the annotation
4. If present, it sets `Spec.Stdin = true` on the DCP Container resource
5. DCP creates the container with the `-i` flag via Docker/Podman
6. The container's stdin remains open for writing
7. When the "Send Input" command is executed, it sends the text via `IResourceConsoleInputService`

## Architecture

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  Dashboard UI   │────▶│    gRPC API     │────▶│   DcpExecutor   │
│  "Send Input"   │     │ SendConsoleInput│     │ WriteStdinAsync │
│    Command      │     │    Endpoint     │     │                 │
└─────────────────┘     └─────────────────┘     └────────┬────────┘
                                                         │
                                                         ▼
                                                ┌─────────────────┐
                                                │  Kubernetes API │
                                                │ logs subresource│
                                                │  ?source=stdin  │
                                                └────────┬────────┘
                                                         │
                                                         ▼
                                                ┌─────────────────┐
                                                │      DCP        │
                                                │ WriteStdinAsync │
                                                └────────┬────────┘
                                                         │
                                                         ▼
                                                ┌─────────────────┐
                                                │    Container    │
                                                │   (with -i)     │
                                                └─────────────────┘
```

## API Usage

```csharp
// Enable stdin on any container resource
builder.AddContainer("my-container", "my-image")
    .WithStdin();

// Optionally disable (useful for conditional logic)
builder.AddContainer("my-container", "my-image")
    .WithStdin(enabled: false);
```

## Future Enhancements

To enable programmatic stdin access from user code, a public interface could be exposed:

```csharp
// Proposed public API
public interface IResourceInputService
{
    Task SendInputAsync(string resourceName, string input, CancellationToken cancellationToken);
}
```

This would allow playground apps and user code to write to container stdin directly.

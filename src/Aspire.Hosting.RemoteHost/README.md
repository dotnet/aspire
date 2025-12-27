# Aspire.Hosting.RemoteHost

This package provides the remote host server for polyglot .NET Aspire AppHosts. It enables non-.NET languages (like TypeScript) to define and run Aspire applications by communicating with a .NET process via JSON-RPC over Unix domain sockets.

## Overview

The RemoteHost acts as a bridge between polyglot AppHost projects and the .NET Aspire hosting infrastructure. It:

- Accepts JSON-RPC commands over a Unix domain socket
- Executes instructions to create and configure `DistributedApplicationBuilder`
- Invokes methods on Aspire resources and builders
- Supports bidirectional communication for callbacks (e.g., environment configuration lambdas)
- Manages object lifecycle with a registry-based proxy system

## Architecture

```text
┌─────────────────────────────┐
│   TypeScript/JS AppHost    │
│   (or other language)       │
└─────────────┬───────────────┘
              │ JSON-RPC over Unix Socket
              ▼
┌─────────────────────────────┐
│   RemoteHostServer          │
│   ├── JsonRpcServer         │
│   ├── InstructionProcessor  │
│   └── Object Registry       │
└─────────────┬───────────────┘
              │
              ▼
┌─────────────────────────────┐
│   .NET Aspire Hosting       │
│   (DistributedApplication)  │
└─────────────────────────────┘
```

## Components

### RemoteHostServer

The main entry point. Call `RemoteHostServer.RunAsync(args)` to start the server:

```csharp
await Aspire.Hosting.RemoteHost.RemoteHostServer.RunAsync(args);
```

### InstructionProcessor

Handles the execution of instructions sent from the client:

- `CREATE_BUILDER` - Creates a new `DistributedApplicationBuilder`
- `RUN_BUILDER` - Builds and runs the application
- `INVOKE` - Invokes methods on objects (resources, builders, etc.)
- `pragma` - Configuration directives

### JsonRpcServer

Manages client connections and JSON-RPC communication:

- Listens on a Unix domain socket
- Supports multiple concurrent clients
- Provides bidirectional RPC for callbacks

### Object Marshalling

Complex .NET objects are marshalled as proxies with unique IDs. The client can:

- Invoke methods on proxied objects
- Get/set properties
- Access indexers (for lists and dictionaries)
- Receive callbacks for delegate parameters

## Environment Variables

| Variable | Description |
|----------|-------------|
| `REMOTE_APP_HOST_SOCKET_PATH` | Path to the Unix domain socket. Defaults to `{temp}/aspire/remote-app-host.sock` |
| `REMOTE_APP_HOST_PID` | Parent process ID for orphan detection. If set, the server shuts down when the parent exits |

## Usage

This package is typically not used directly. Instead, the Aspire CLI scaffolds a project that references this package and calls the entry point. The generated `Program.cs` is simply:

```csharp
await Aspire.Hosting.RemoteHost.RemoteHostServer.RunAsync(args);
```

## JSON-RPC Methods

| Method | Description |
|--------|-------------|
| `ping` | Health check, returns "pong" |
| `executeInstruction` | Execute a typed instruction (CREATE_BUILDER, INVOKE, etc.) |
| `invokeMethod` | Call a method on a proxied object |
| `getProperty` | Get a property value from a proxied object |
| `setProperty` | Set a property value on a proxied object |
| `getIndexer` | Get a value by index (list) or key (dictionary) |
| `setIndexer` | Set a value by index or key |
| `unregisterObject` | Release a proxied object from the registry |
| `invokeCallback` | (Server→Client) Invoke a callback registered by the client |

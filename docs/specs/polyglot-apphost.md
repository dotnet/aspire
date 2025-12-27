# Polyglot AppHost Support

This document describes how the Aspire CLI supports non-.NET app hosts. Currently, TypeScript is the supported guest language.

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Development Mode](#development-mode)
- [Process Lifecycle](#process-lifecycle)
- [JSON-RPC Protocol](#json-rpc-protocol)
- [Type System and Marshalling](#type-system-and-marshalling)
- [Code Generation](#code-generation)
- [TypeScript Implementation](#typescript-implementation)
- [Publish Mode](#publish-mode)
- [Adding New Guest Languages](#adding-new-guest-languages)

## Overview

The polyglot apphost feature allows developers to write Aspire app hosts in non-.NET languages. The CLI detects the guest language entry point and orchestrates the guest runtime alongside a .NET GenericAppHost process.

**Terminology:**
- **Host**: The .NET GenericAppHost process running Aspire.Hosting
- **Guest**: The non-.NET runtime executing the user's apphost code

**Design Goals:**
1. **Reuse Existing Integrations** - All 100+ Aspire.Hosting.* packages work automatically
2. **Native Language Experience** - Generated SDKs with idiomatic APIs
3. **Consistent CLI Experience** - `aspire run`, `aspire add`, `aspire publish` work identically
4. **Leverage .NET Ecosystem** - Container orchestration, service discovery, telemetry stay in .NET

## Architecture

The CLI scaffolds a temporary .NET GenericAppHost project that references the required Aspire.Hosting packages. Code generation reflects over these assemblies to produce a language-specific SDK. At runtime, GenericAppHost starts a JSON-RPC server over Unix domain sockets, and the guest connects to send instructions. Each instruction (e.g., `AddRedis`, `WithEnvironment`) is executed by the `InstructionProcessor` against the real Aspire.Hosting APIs. Complex objects are registered in an `ObjectRegistry` and returned to the guest as proxies that can invoke methods remotely. An `OrphanDetector` monitors the CLI process and terminates GenericAppHost if the parent dies.

### Key Projects

| Project | Purpose |
|---------|---------|
| `Aspire.Hosting.CodeGeneration` | Reflection-based model building from Aspire.Hosting assemblies |
| `Aspire.Hosting.CodeGeneration.<Language>` | Language-specific SDK generator |
| `Aspire.Hosting.RemoteHost` | JSON-RPC server, instruction processor, object registry |

## Development Mode

Set `ASPIRE_REPO_ROOT` to your local Aspire repository for development:

```bash
export ASPIRE_REPO_ROOT=/path/to/aspire
```

This:
- Skips SDK caching (always regenerates)
- Uses local build artifacts from `artifacts/bin/` instead of NuGet packages

## Process Lifecycle

### Startup Sequence

1. **Detection**: `ProjectLocator` finds the guest entry point (e.g., `apphost.ts`)
2. **GenericAppHost Preparation**:
   - `ProjectModel` scaffolds a .NET project in `$TMPDIR/.aspire/hosts/<hash>/`
   - References `Aspire.AppHost.Sdk` and required hosting packages
   - Builds the project with `dotnet build`
3. **Code Generation**:
   - Loads assemblies from build output using `AssemblyLoaderContext`
   - Builds `ApplicationModel` via reflection on loaded assemblies
   - Generates SDK into language-specific output folder
4. **Host Launch**: GenericAppHost started via `dotnet exec` with:
   - `REMOTE_APP_HOST_SOCKET_PATH` - Unix domain socket path for JSON-RPC
   - `REMOTE_APP_HOST_PID` - CLI process ID for orphan detection
5. **Guest Launch**: Guest runtime started with the entry point
6. **Connection**: Guest connects to host over Unix domain socket

### Shutdown Sequence

Shutdown can be triggered by:

1. **User Interrupt (Ctrl+C)**: CLI receives signal, terminates child processes
2. **CLI Death**: `OrphanDetector` in host monitors parent PID, terminates when parent dies
3. **Connection Loss**: Guest detects disconnection and exits
4. **Startup Failure**: Errors propagate back through JSON-RPC and terminate all processes

---

## JSON-RPC Protocol

Communication between the guest and host uses JSON-RPC 2.0 over Unix domain sockets (or named pipes on Windows).

### Transport Layer

The protocol uses **header-delimited messages** matching the `vscode-jsonrpc` format:

```text
Content-Length: 123\r\n
\r\n
{"jsonrpc":"2.0","id":1,"method":"ping","params":[]}
```

### RPC Methods

| Method | Parameters | Description |
|--------|------------|-------------|
| `ping` | none | Health check, returns "pong" |
| `executeInstruction` | `instructionJson: string` | Execute a typed instruction (see below) |
| `invokeMethod` | `objectId, methodName, args?` | Call method on registered object |
| `getProperty` | `objectId, propertyName` | Get property value |
| `setProperty` | `objectId, propertyName, value` | Set property value |
| `getIndexer` | `objectId, key` | Get indexed value (list or dict) |
| `setIndexer` | `objectId, key, value` | Set indexed value |
| `unregisterObject` | `objectId` | Release object from registry |
| `invokeCallback` | `callbackId, args` | Host → Guest callback invocation |

### Instructions

Instructions are the primary way to interact with the Aspire.Hosting API:

**CREATE_BUILDER** - Create a DistributedApplicationBuilder
```json
{
    "name": "CREATE_BUILDER",
    "builderName": "builder",
    "args": ["--operation", "run"],
    "projectDirectory": "/path/to/project"
}
```

**INVOKE** - Call a method on a registered object
```json
{
    "name": "INVOKE",
    "source": "builder",
    "target": "redis",
    "methodAssembly": "Aspire.Hosting.Redis",
    "methodType": "RedisBuilderExtensions",
    "methodName": "AddRedis",
    "methodArgumentTypes": ["IDistributedApplicationBuilder", "String"],
    "metadataToken": 123456,
    "args": { "name": "cache" }
}
```

**RUN_BUILDER** - Build and run the application
```json
{
    "name": "RUN_BUILDER",
    "builderName": "builder"
}
```

### Callback Mechanism

Callbacks allow the host to invoke guest functions during method execution (e.g., `WithEnvironment` callbacks):

1. Guest registers a callback function with a unique ID (e.g., `callback_1_1234567890`)
2. Guest passes the callback ID as an argument to an instruction
3. Host executes the method, which invokes the callback
4. Host sends `invokeCallback` request to guest with the callback ID and args
5. Guest executes the callback and returns the result

---

## Type System and Marshalling

The polyglot architecture bridges two type systems: the host (.NET) and the guest.

### Design Principles

1. **Primitives pass directly**: Strings, numbers, booleans serialize as JSON primitives
2. **Complex objects become proxies**: Non-primitive types are registered in the host and accessed via JSON-RPC calls
3. **Callbacks are bidirectional**: Guest can register callbacks that the host invokes

### Object Registry

The `ObjectRegistry` in the host maintains a `ConcurrentDictionary<string, object>` mapping unique IDs to live .NET objects. When a complex object needs to be returned to the guest:

1. Object is registered with a unique ID (e.g., `obj_1`, `obj_2`)
2. A marshalled representation is sent: `{ $id, $type, $fullType, $methods, ...properties }`
3. Guest wraps this in a proxy class
4. Subsequent operations use the `$id` to reference the object in the host

### Marshalled Object Format

```json
{
    "$id": "obj_1",
    "$type": "RedisResource",
    "$fullType": "Aspire.Hosting.Redis.RedisResource",
    "$methods": ["WithEnvironment", "WithArgs", "GetEndpoint"],
    "Name": "cache"
}
```

### Type Mappings

#### Guest → Host

| Guest Type | .NET Type | Handling |
|------------|-----------|----------|
| String | `string` | Direct JSON |
| Number | `int`, `long`, `double` | Type coercion |
| Boolean | `bool` | Direct JSON |
| Null | `null` | Direct JSON |
| Object with `$id` | Registry lookup | Proxy reference resolved |
| `{ $referenceExpression, format }` | `ReferenceExpression` | Special handling |
| Arrays | `T[]`, `List<T>` | JSON deserialization |

#### Host → Guest

| .NET Type | Guest Type | Notes |
|-----------|------------|-------|
| Primitives | string/number/boolean | Direct |
| `DateTime`, `Guid` | string | ISO 8601 / string format |
| Enums | string | Enum name |
| Complex objects | Proxy | Marshalled with `$id` |

### ReferenceExpression

`ReferenceExpression` allows building connection strings that reference host objects:

```json
{ "$referenceExpression": true, "format": "redis://{obj_4}" }
```

The format string contains `{$id}` placeholders. The host reconstructs the expression using object registry lookups.

---

## Code Generation

The CLI generates language-specific SDK code that provides type-safe APIs with instance methods for all Aspire integrations.

### Generation Trigger

Code generation runs automatically when:

1. **First Run**: SDK folder doesn't exist
2. **Package Changes**: Hash of package references has changed
3. **After `aspire add`**: When adding new integrations
4. **Development Mode**: When `ASPIRE_REPO_ROOT` is set

### What Gets Generated

For each Aspire integration, the generator creates:

1. **Builder methods** on `DistributedApplicationBuilder`
2. **Resource-specific builder classes** with fluent methods
3. **Proxy wrapper classes** for callback contexts and model types

---

## TypeScript Implementation

This section covers TypeScript-specific details for the polyglot apphost feature.

### Generated File Structure

```text
.modules/
├── .codegen-hash              # SHA256 hash of package references
├── distributed-application.ts # Main SDK with builder classes
├── types.ts                   # Instruction type definitions
└── RemoteAppHostClient.ts     # JSON-RPC client implementation
```

### Base Proxy Classes

**`DotNetProxy`** - Foundation for all remote object access:
```typescript
class DotNetProxy {
    readonly $id: string;
    readonly $type: string;

    async invokeMethod(name: string, args?: Record<string, unknown>): Promise<unknown>;
    async getProperty(name: string): Promise<unknown>;
    async setProperty(name: string, value: unknown): Promise<void>;
    async getIndexer(key: string | number): Promise<unknown>;
    async setIndexer(key: string | number, value: unknown): Promise<void>;
    async dispose(): Promise<void>;
}
```

**`ListProxy<T>`** - For `IList<T>` operations:
```typescript
class ListProxy<T> {
    async add(item: T): Promise<void>;
    async get(index: number): Promise<T>;
    async set(index: number, value: T): Promise<void>;
    async count(): Promise<number>;
    async clear(): Promise<void>;
    async remove(item: T): Promise<boolean>;
    async removeAt(index: number): Promise<void>;
}
```

### Generated Proxy Wrappers

The code generator produces **specially generated proxy wrapper classes** for callback context types. These provide typed access to .NET objects passed into callbacks.

| Generated Proxy | .NET Type | Purpose |
|-----------------|-----------|---------|
| `EnvironmentCallbackContextProxy` | `EnvironmentCallbackContext` | Access `EnvironmentVariables` dictionary |
| `CommandLineArgsCallbackContextProxy` | `CommandLineArgsCallbackContext` | Access `Args` list |
| `ContainerRuntimeArgsCallbackContextProxy` | `ContainerRuntimeArgsCallbackContext` | Access container runtime `Args` list |
| `EndpointReferenceProxy` | `EndpointReference` | Access endpoint metadata |
| `EndpointAnnotationProxy` | `EndpointAnnotation` | Access endpoint configuration |

All generated proxies implement `HasProxy`:
```typescript
interface HasProxy {
    proxy: DotNetProxy;
}
```

Example generated proxy:
```typescript
class EnvironmentCallbackContextProxy {
    private _proxy: DotNetProxy;
    get proxy(): DotNetProxy { return this._proxy; }

    async getEnvironmentVariables(): Promise<DotNetProxy>;
    async getResource(): Promise<DotNetProxy>;
    async getExecutionContext(): Promise<DotNetProxy>;
}
```

### ReferenceExpression Support

The `refExpr` tagged template literal creates reference expressions:

```typescript
const endpoint = await redis.getEndpoint("tcp");
const expr = refExpr`redis://${endpoint}`;
// Serializes as: { $referenceExpression: true, format: "redis://{obj_4}" }
```

### Example Usage

```typescript
// apphost.ts
import { createBuilder, refExpr } from './.modules/distributed-application.js';
import { EnvironmentCallbackContextProxy } from './.modules/distributed-application.js';

async function main() {
    const builder = await createBuilder();

    const redis = await builder.addRedis('cache');

    // Callback receives specially generated proxy wrapper
    await redis.withEnvironmentCallback(async (context: EnvironmentCallbackContextProxy) => {
        const envVars = await context.getEnvironmentVariables();
        await envVars.set("REDIS_CONFIG", "custom-value");
    });

    // ListProxy for args manipulation
    await redis.withArgs2(async (context) => {
        const args = await context.getArgs();
        await args.add("--maxmemory");
        await args.add("256mb");
    });

    const app = builder.build();
    await app.run();
}

main();
```

### Configuration

**.aspire/settings.json**
```json
{
  "appHostPath": "../apphost.ts",
  "packages": {
    "Aspire.Hosting.Redis": "13.1.0"
  }
}
```

**apphost.run.json** - Launch settings for the app host.

---

## Publish Mode

TypeScript apphosts support `aspire publish`, `aspire deploy`, and `aspire do` through the same `PipelineCommandBase` used by .NET apphosts.

### Process Launch and Data Flow

`PipelineCommandBase` delegates to `IAppHostProject.PublishAsync()` which handles both .NET and TypeScript apphosts uniformly.

**TypeScript publish flow:**

1. **CLI starts GenericAppHost** with `ASPIRE_BACKCHANNEL_PATH` environment variable
   - GenericAppHost opens a backchannel server for progress reporting
   - CLI connects to receive pipeline status updates

2. **CLI starts TypeScript process** with publish arguments
   - Arguments like `--operation publish --step deploy` passed via command line
   - `createBuilder()` defaults to `process.argv.slice(2)`, forwarding args to `CREATE_BUILDER`

3. **GenericAppHost receives args via CREATE_BUILDER instruction**
   - Creates `DistributedApplicationBuilder` with the publish arguments
   - Sets `ExecutionContext.IsPublishMode = true`

4. **TypeScript calls `app.run()`**
   - Sends `RUN_BUILDER` instruction to GenericAppHost
   - GenericAppHost executes the publish pipeline
   - Progress reported via backchannel to CLI

5. **Completion and shutdown**
   - GenericAppHost exits when pipeline completes
   - TypeScript detects disconnect via `client.onDisconnect()` and exits
   - CLI reports final status

### Run vs Publish

| Aspect | Run Mode | Publish Mode |
|--------|----------|--------------|
| ExecutionContext | `IsRunMode = true` | `IsPublishMode = true` |
| Lifecycle | Runs until Ctrl+C | Exits on completion |
| Guest exit trigger | SIGINT | Host disconnect |

---

## Adding New Guest Languages

The polyglot architecture supports additional languages. The host-side infrastructure (`Aspire.Hosting.RemoteHost`) is language-agnostic—only the code generator and CLI integration are language-specific.

### Components to Implement

| Component | Location | Purpose |
|-----------|----------|---------|
| Code Generator | `Aspire.Hosting.CodeGeneration.<Language>` | Generate idiomatic SDK from `ApplicationModel` |
| CLI Project Handler | `Aspire.Cli/AppHostRunning/<Language>AppHostProject.cs` | Implement `IAppHostProject` |
| Project Locator | `Aspire.Cli/Projects/ProjectLocator.cs` | Detect entry point file |
| Runtime Client | Embedded or generated | JSON-RPC client with proxy classes |

### Code Generator

Create `Aspire.Hosting.CodeGeneration.<Language>` implementing `ICodeGenerator`:

```csharp
public interface ICodeGenerator
{
    bool NeedsGeneration(string projectDirectory, IReadOnlyDictionary<string, string> packages);
    Task GenerateAsync(string projectDirectory, IReadOnlyDictionary<string, string> packages, CancellationToken cancellationToken);
}
```

Key concerns:
- Map .NET types to language equivalents
- Generate builder classes with instance methods
- Generate proxy wrappers for callback contexts
- Emit JSON-RPC client infrastructure

### CLI Integration

Implement `IAppHostProject`:

```csharp
internal interface IAppHostProject
{
    Task<int> RunAsync(RunContext context, CancellationToken cancellationToken);
    Task<int> PublishAsync(PublishContext context, CancellationToken cancellationToken);
    Task<PackageAddResult> AddPackageAsync(...);
    Task<ProjectValidationResult> ValidateAsync(...);
}
```

Register with a keyed service for the `AppHostType`:

```csharp
services.AddKeyedSingleton<IAppHostProject, PythonAppHostProject>(AppHostType.Python);
```

### Runtime Client Requirements

The guest language needs a JSON-RPC client that:
1. Connects to Unix domain socket (path from `REMOTE_APP_HOST_SOCKET_PATH`)
2. Implements `vscode-jsonrpc` header-delimited message format
3. Handles `invokeCallback` requests from host
4. Wraps marshalled objects (`$id`, `$type`) in proxy classes

### Reusable Infrastructure

These components work unchanged for any guest language:
- `Aspire.Hosting.RemoteHost` - JSON-RPC server, instruction processor, object registry
- `Aspire.Hosting.CodeGeneration` - Reflection-based model building
- GenericAppHost scaffolding and build process
- Backchannel for publish progress reporting

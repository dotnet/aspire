# Polyglot AppHost Support

This document describes how the Aspire CLI supports non-.NET app hosts using the **Aspire Type System (ATS)**.

## Table of Contents

1. [Overview](#overview)
2. [Design Philosophy](#design-philosophy)
3. [Architecture](#architecture)
4. [Aspire Type System (ATS)](#aspire-type-system-ats)
5. [JSON-RPC Protocol](#json-rpc-protocol)
6. [Code Generation](#code-generation)
7. [TypeScript Implementation](#typescript-implementation)
8. [CLI Integration](#cli-integration)
9. [Configuration](#configuration)
10. [Adding New Guest Languages](#adding-new-guest-languages)
11. [Security](#security)

---

## Overview

The polyglot apphost feature allows developers to write Aspire app hosts in non-.NET languages. The **Aspire Type System (ATS)** is the foundation—a portable type system that maps .NET types to a unified representation any language can work with.

Integration authors expose their existing extension methods to ATS by adding `[AspireExport]` attributes. No wrapper code needed.

**Key Concepts:**
- **ATS Type ID** - A portable type identifier derived from assembly and type name (e.g., `Aspire.Hosting.Redis/Aspire.Hosting.ApplicationModel.RedisResource`)
- **Capability** - A named operation (e.g., `Aspire.Hosting.Redis/addRedis`)
- **Handle** - An opaque typed reference to a .NET object
- **DTO** - A serializable data transfer object

---

## Design Philosophy

ATS flattens .NET's polymorphism into a simple, portable model that any language can work with:

| .NET Concept | ATS Approach |
|--------------|--------------|
| Interface inheritance | Expanded to concrete types at scan time |
| Generic constraints | Resolved to concrete types at scan time |
| Method overloading | **Not supported** - method names must be unique within each target type |
| Capability versioning | **Not needed** - NuGet package version handles compatibility |

**The result**: A flat type system where:
- Each concrete type has a complete list of capabilities
- No type hierarchy to reason about
- No generic type parameters
- Guest languages don't need to understand .NET's type system

### Why Flat?

.NET's type system is powerful but complex. Interfaces, generics, and method overloading require sophisticated type checking that many languages don't support natively. By flattening at scan time:

1. **Go, C, and other non-OOP languages** get a natural API without emulating inheritance
2. **TypeScript, Swift** can optionally use the original interface hierarchy
3. **All languages** see the same capabilities - just grouped differently

### Collision Detection

The same method name can appear on different types (e.g., `withEnvironment` on Redis, Container, Project). However, within a single target type, method names must be unique—even across packages.

The scanner detects and reports conflicts:

```text
Error: Method 'withDataVolume' has multiple definitions for target 'Aspire.Hosting.Redis/Aspire.Hosting.ApplicationModel.RedisResource':
  - Aspire.Hosting.Redis/withDataVolume
  - ThirdParty.Integration/withDataVolume

Resolution: Use [AspireExport("uniqueMethodName")] to disambiguate.
```

---

## Architecture

The CLI orchestrates two processes: the **AppHost Server** (.NET) and the **Guest Runtime** (e.g., Node.js). They communicate via JSON-RPC over a Unix domain socket.

```mermaid
flowchart TB
    subgraph CLI["Aspire CLI"]
        direction LR
        subgraph Guest["Guest Runtime (Node.js)"]
            direction TB
            UserCode["User Code<br/>(apphost.ts)"]
            SDK["Generated SDK<br/>(aspire.ts)"]
            ATSClient["ATS Client"]
            UserCode --> SDK --> ATSClient
        end

        subgraph Host["AppHost Server (.NET)"]
            direction TB
            Packages["Aspire.Hosting.*<br/>(Redis, etc)"]
            Dispatcher["CapabilityDispatcher<br/>HandleRegistry"]
            RPCServer["JSON-RPC Server"]
            Packages --> Dispatcher --> RPCServer
        end

        ATSClient <-->|"JSON-RPC<br/>(Unix Socket)"| RPCServer
    end

    CLI -.->|spawns| Guest
    CLI -.->|spawns| Host
```

**Startup Sequence:**

1. CLI scaffolds an AppHost server project in `$TMPDIR/.aspire/hosts/<hash>/`
2. CLI adds required hosting packages (Redis, Postgres, etc.)
3. CLI builds the .NET project
4. Code generation scans assemblies for `[AspireExport]` and generates SDK
5. CLI starts the AppHost server with socket path and auth token
6. CLI starts the Guest runtime
7. Guest connects, authenticates, and invokes capabilities

```mermaid
sequenceDiagram
    participant CLI as Aspire CLI
    participant Host as AppHost Server
    participant Guest as Guest (TypeScript)

    CLI->>Host: Start (socket path, auth token)
    CLI->>Guest: Start (socket path, auth token)

    Guest->>Host: authenticate(token)
    Host-->>Guest: true

    Guest->>Host: invokeCapability("Aspire.Hosting/createBuilder", {})
    Host-->>Guest: { $handle: "Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder:1" }

    Guest->>Host: invokeCapability("Aspire.Hosting.Redis/addRedis", {builder, name})
    Host-->>Guest: { $handle: "Aspire.Hosting.Redis/Aspire.Hosting.ApplicationModel.RedisResource:1" }

    Guest->>Host: invokeCapability("Aspire.Hosting/build", {builder})
    Host-->>Guest: { $handle: "Aspire.Hosting/Aspire.Hosting.DistributedApplication:1" }

    Guest->>Host: invokeCapability("Aspire.Hosting/run", {app})
    Host-->>Guest: Started (orchestration running)
```

---

## Aspire Type System (ATS)

ATS is the central type system that bridges .NET and guest languages. Every type crossing the boundary has an **ATS type ID** that serves as its portable identity.

### Type IDs

Type IDs are portable identifiers for .NET types. They are automatically derived from the assembly name and full type name (including namespace).

**Format:** `{AssemblyName}/{FullTypeName}`

| ATS Type ID | .NET Type |
|-------------|-----------|
| `Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder` | `IDistributedApplicationBuilder` |
| `Aspire.Hosting/Aspire.Hosting.DistributedApplication` | `DistributedApplication` |
| `Aspire.Hosting/Aspire.Hosting.DistributedApplicationExecutionContext` | `DistributedApplicationExecutionContext` |
| `Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResource` | `IResource` |
| `Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEnvironment` | `IResourceWithEnvironment` |
| `Aspire.Hosting/Aspire.Hosting.ApplicationModel.ContainerResource` | `ContainerResource` |
| `Aspire.Hosting/Aspire.Hosting.ApplicationModel.ExecutableResource` | `ExecutableResource` |
| `Aspire.Hosting/Aspire.Hosting.ApplicationModel.EndpointReference` | `EndpointReference` |

Declared with `[AspireExport]`:

```csharp
// On a type you own - type ID is derived automatically
// namespace Aspire.Hosting.ApplicationModel, assembly Aspire.Hosting.Redis
[AspireExport]
public class RedisResource : ContainerResource { }
// Type ID = Aspire.Hosting.Redis/Aspire.Hosting.ApplicationModel.RedisResource

// At assembly level for types you don't own
[assembly: AspireExport(typeof(IDistributedApplicationBuilder))]
// Type ID = Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder
```

### Type Exporting and Polymorphism Flattening

ATS doesn't have a closed set of primitive types. Instead, any .NET type can be exported using `[AspireExport]`, and the scanner automatically expands capabilities based on type relationships.

#### How Exporting Works

When a type is marked with `[AspireExport]`, the scanner:

1. **Registers the type** with its ATS type ID (`{AssemblyName}/{FullTypeName}`)
2. **Scans extension methods** that target the type or its interfaces
3. **Expands interface relationships** so each concrete type gets all applicable capabilities

```csharp
// RedisResource is in Aspire.Hosting.Redis assembly
[AspireExport]
public class RedisResource : ContainerResource, IResourceWithConnectionString { }

// This extension method targets IResourceWithEnvironment
public static class ResourceExtensions
{
    [AspireExport("withEnvironment")]
    public static IResourceBuilder<T> WithEnvironment<T>(this IResourceBuilder<T> builder, ...)
        where T : IResourceWithEnvironment { }
}
```

Because `RedisResource` implements `IResourceWithEnvironment` (via `ContainerResource`), the scanner adds `withEnvironment` to `RedisResource`'s capability list.

#### Flattening in Action

A concrete type gets capabilities from:
- **Direct exports** on the type itself
- **Interface implementations** - capabilities targeting any interface it implements
- **Base class** - capabilities inherited from parent classes

```text
RedisResource capabilities:
├── Aspire.Hosting.Redis/addRedis          (direct - creates Redis)
├── Aspire.Hosting.Redis/withPersistence   (direct - Redis-specific)
├── Aspire.Hosting/withEnvironment         (via IResourceWithEnvironment)
├── Aspire.Hosting/withEndpoint            (via IResourceWithEndpoints)
├── Aspire.Hosting/waitFor                 (via IResourceWithWaitSupport)
└── Aspire.Hosting/getConnectionString     (via IResourceWithConnectionString)
```

Guest languages see a flat list—no need to understand .NET's type hierarchy.

#### Core Types

Some commonly used exported types:

| Type | Purpose |
|------|---------|
| `IDistributedApplicationBuilder` | Entry point for building the app |
| `DistributedApplication` | Built application, ready to run |
| `DistributedApplicationExecutionContext` | Runtime context (run vs publish mode) |
| `EndpointReference` | Reference to a network endpoint |
| `ReferenceExpression` | Dynamic expression with embedded references |

These aren't special—they're just types that happen to be exported. Integration packages export their own types (e.g., `RedisResource`, `PostgresResource`).

### Capabilities

Capabilities are named operations with globally unique IDs. They replace direct method invocation.

**Format:** `{Package}/{MethodName}`

- Package = NuGet package name (derived from assembly)
- MethodName = specified in `[AspireExport]` attribute

| Capability ID | Description |
|---------------|-------------|
| `Aspire.Hosting/createBuilder` | Create a DistributedApplicationBuilder |
| `Aspire.Hosting/addContainer` | Add a container resource |
| `Aspire.Hosting/withEnvironment` | Set an environment variable |
| `Aspire.Hosting/build` | Build the application |
| `Aspire.Hosting/run` | Run the application |
| `Aspire.Hosting.Redis/addRedis` | Add a Redis resource |

Declared on extension methods:

```csharp
[AspireExport("addRedis", Description = "Adds a Redis resource")]
public static IResourceBuilder<RedisResource> AddRedis(
    this IDistributedApplicationBuilder builder,
    [ResourceName] string name,
    int? port = null)
{
    // Existing implementation - unchanged
}
// Scanner computes capability ID: Aspire.Hosting.Redis/addRedis
```

### Handles

Handles are opaque references to .NET objects. They carry an ATS type ID for identification.

**Format:** `{atsTypeId}:{instanceId}` (e.g., `Aspire.Hosting.Redis/Aspire.Hosting.ApplicationModel.RedisResource:42`)

```json
{
    "$handle": "Aspire.Hosting.Redis/Aspire.Hosting.ApplicationModel.RedisResource:42",
    "$type": "Aspire.Hosting.Redis/Aspire.Hosting.ApplicationModel.RedisResource"
}
```

**Flat Model:** ATS uses a flat type model where capabilities are grouped by their first parameter type:
- Methods with `IDistributedApplicationBuilder` as first param → on `DistributedApplicationBuilder`
- Methods with `IResourceBuilder<T>` as first param → on all resource builders
- Methods with `DistributedApplication` as first param → on `DistributedApplication`

Type validation happens at runtime when the CLR invokes the method. Invalid type combinations produce `TYPE_MISMATCH` errors.

### DTOs

Data transfer objects for passing structured data. Must be marked `[AspireDto]`:

```csharp
[AspireDto]
public sealed class ContainerMountOptions
{
    public required string Source { get; init; }
    public required string Target { get; init; }
    public bool IsReadOnly { get; init; }
}
```

**Strict Enforcement:**

| Direction | `[AspireDto]` Type | Non-`[AspireDto]` Type |
|-----------|-------------------|----------------------|
| Input (JSON → .NET) | Deserialized | **Error** |
| Output (.NET → JSON) | Serialized | Marshaled as Handle |

### Callbacks

Guest-provided functions the host can invoke during execution. Callbacks are automatically inferred from delegate parameters:

```csharp
[AspireExport("withEnvironmentCallback")]
public static IResourceBuilder<T> WithEnvironmentCallback<T>(
    this IResourceBuilder<T> resource,
    Func<EnvironmentCallbackContext, Task> callback)  // Delegate = callback
    where T : IResourceWithEnvironment
// Scanner computes: Aspire.Hosting/withEnvironmentCallback
```

Callbacks are passed as string IDs:

```json
{
    "resource": {"$handle": "Aspire.Hosting.Redis/Aspire.Hosting.ApplicationModel.RedisResource:1"},
    "callback": "callback_1_1234567890"
}
```

#### Callback Handle Wrapping (TypeScript)

When the host invokes a callback, handle parameters are automatically converted to typed wrapper classes:

1. Host sends: `{ p0: { $handle: "...", $type: "Aspire.Hosting/Aspire.Hosting.ApplicationModel.EnvironmentCallbackContext" } }`
2. SDK looks up type in **handle wrapper registry**
3. SDK creates wrapper: `new EnvironmentCallbackContext(handle, client)`
4. User callback receives typed instance

Generated code registers factories for each wrapper class at module load:

```typescript
registerHandleWrapper('Aspire.Hosting/Aspire.Hosting.ApplicationModel.EnvironmentCallbackContext',
    (handle, client) => new EnvironmentCallbackContext(handle as EnvironmentCallbackContextHandle, client));
```

This enables callbacks to receive properly typed objects:

```typescript
.withEnvironmentCallback(async (ctx: EnvironmentCallbackContext) => {
    // ctx is a typed wrapper, not a raw Handle
    await ctx.environmentVariables.set("KEY", "value");
})
```

### Context Types

Objects passed to callbacks with auto-exposed properties. The type ID is automatically derived from `{AssemblyName}/{FullTypeName}`:

```csharp
// namespace Aspire.Hosting.ApplicationModel
[AspireExport(ExposeProperties = true)]  // Type ID = Aspire.Hosting/Aspire.Hosting.ApplicationModel.EnvironmentCallbackContext
public class EnvironmentCallbackContext
{
    // Auto-exposed as "Aspire.Hosting/Aspire.Hosting.ApplicationModel.EnvironmentCallbackContext.environmentVariables"
    public Dictionary<string, object> EnvironmentVariables { get; }

    // Auto-exposed as "Aspire.Hosting/Aspire.Hosting.ApplicationModel.EnvironmentCallbackContext.executionContext"
    public DistributedApplicationExecutionContext ExecutionContext { get; }
}
```

### Collection Wrappers (TypeScript)

For mutable .NET collections exposed to TypeScript, the SDK provides wrapper classes with capability-based operations.

#### AspireDict<K, V>

Wrapper for `IDictionary<K, V>`:

```typescript
const envVars = ctx.environmentVariables;  // AspireDict<string, string | ReferenceExpression>
await envVars.set("KEY", "value");
await envVars.get("KEY");
await envVars.containsKey("KEY");
await envVars.remove("KEY");
await envVars.keys();
await envVars.count();
```

**Lazy Handle Resolution:** When a dictionary is accessed via a context property, the wrapper lazily fetches the actual dictionary handle on first operation:

```typescript
// Context has handle to EnvironmentCallbackContext
// First operation calls getter capability to get dictionary handle
await ctx.environmentVariables.set("KEY", "value");
```

#### AspireList<T>

Wrapper for `IList<T>`:

```typescript
await list.add(item);
await list.get(index);
await list.count();
await list.removeAt(index);
await list.toArray();
```

### Reference Expressions

Dynamic values that reference endpoints, parameters, and other providers:

```json
{
    "$expr": {
        "format": "redis://{0}:{1}",
        "valueProviders": [
            { "$handle": "Aspire.Hosting/Aspire.Hosting.ApplicationModel.EndpointReference:1" },
            "6379"
        ]
    }
}
```

---

## JSON-RPC Protocol

Guest and host communicate via JSON-RPC 2.0 over Unix domain sockets (named pipes on Windows).

### Wire Format

Messages use the LSP/vscode-jsonrpc header format:

```text
Content-Length: 123\r\n
\r\n
{"jsonrpc":"2.0","id":1,"method":"ping","params":[]}
```

### Methods

| Method | Direction | Purpose |
|--------|-----------|---------|
| `authenticate` | Guest → Host | Authenticate with secret token |
| `ping` | Guest → Host | Health check |
| `invokeCapability` | Guest → Host | Call a capability |
| `getCapabilities` | Guest → Host | List available capabilities |
| `createCancellationToken` | Guest → Host | Create cancellation token |
| `cancel` | Guest → Host | Cancel operation |
| `invokeCallback` | Host → Guest | Invoke guest callback |

### authenticate

Must be called first (except for `ping`):

```json
// Request
{"jsonrpc":"2.0","id":1,"method":"authenticate","params":["<secret-token>"]}

// Response
{"jsonrpc":"2.0","id":1,"result":true}
```

### invokeCapability

```json
// Request
{"jsonrpc":"2.0","id":2,"method":"invokeCapability","params":[
    "Aspire.Hosting.Redis/addRedis",
    {
        "builder": {"$handle": "Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder:1"},
        "name": "cache",
        "port": 6379
    }
]}

// Response
{"jsonrpc":"2.0","id":2,"result":{
    "$handle": "Aspire.Hosting.Redis/Aspire.Hosting.ApplicationModel.RedisResource:1",
    "$type": "Aspire.Hosting.Redis/Aspire.Hosting.ApplicationModel.RedisResource"
}}
```

### invokeCallback (Host → Guest)

```json
// Request
{"jsonrpc":"2.0","id":100,"method":"invokeCallback","params":[
    "callback_1_1234567890",
    {"context": {"$handle": "Aspire.Hosting/EnvironmentCallbackContext:5"}}
]}

// Response
{"jsonrpc":"2.0","id":100,"result":null}
```

### Error Responses

```json
{
    "jsonrpc": "2.0",
    "id": 1,
    "result": {
        "$error": {
            "code": "CAPABILITY_NOT_FOUND",
            "message": "Unknown capability: Contoso.Aspire/bar",
            "capability": "Contoso.Aspire/bar"
        }
    }
}
```

| Code | Description |
|------|-------------|
| `CAPABILITY_NOT_FOUND` | Unknown capability ID |
| `HANDLE_NOT_FOUND` | Handle doesn't exist |
| `TYPE_MISMATCH` | Handle type incompatible |
| `INVALID_ARGUMENT` | Missing/invalid argument |
| `CALLBACK_ERROR` | Callback invocation failed |
| `INTERNAL_ERROR` | Unexpected error |

### Supported Types

**Primitives:**

| .NET Type | JSON Type | Notes |
|-----------|-----------|-------|
| `string` | string | |
| `char` | string | Single character |
| `bool` | boolean | |
| `int`, `long` | number | |
| `float`, `double`, `decimal` | number | |
| `DateTime` | string | ISO 8601 |
| `DateTimeOffset` | string | ISO 8601 |
| `TimeSpan` | number | **Total milliseconds** |
| `DateOnly` | string | YYYY-MM-DD |
| `TimeOnly` | string | HH:mm:ss |
| `Guid` | string | |
| `Uri` | string | |
| `enum` | string | Enum name |
| `object` | any | Accepts any supported ATS type |

**Complex Types:**

| Type | JSON Shape |
|------|------------|
| Handle | `{ "$handle": "type:id", "$type": "type" }` |
| DTO | Plain object (requires `[AspireDto]`) |
| Array/List | JSON array |
| Dictionary | JSON object |
| Nullable | Value or `null` |
| ReferenceExpression | `{ "$expr": { "format": "...", "valueProviders": [...] } }` |

**TimeSpan Example:**

```csharp
TimeSpan.FromMinutes(5)  →  300000
TimeSpan.FromSeconds(30) →  30000
```

---

## Code Generation

The CLI generates language-specific SDKs from ATS capabilities.

### When It Runs

- First run (no `.modules/` folder)
- Package hash changed (after `aspire add`)
- Development mode (`ASPIRE_REPO_ROOT` set)

### Process

1. Load assemblies from AppHost server build
2. Scan for `[AspireExport]` attributes using `AtsCapabilityScanner`
3. Expand interface targets to concrete implementations
4. Generate language-specific SDK

### Capability Expansion

Capabilities can target either concrete types or interfaces. For example:

```csharp
// Targets concrete type - no expansion needed
[AspireExport("withPersistence")]
public static IResourceBuilder<RedisResource> WithPersistence(
    this IResourceBuilder<RedisResource> builder, ...)
// → Aspire.Hosting.Redis/withPersistence

// Targets interface - expanded to all implementing types
[AspireExport("withEnvironment")]
public static IResourceBuilder<T> WithEnvironment<T>(
    this IResourceBuilder<T> builder, string name, string value)
    where T : IResourceWithEnvironment
// → Aspire.Hosting/withEnvironment (expands to Redis, Container, Project, ...)
```

The scanner performs **2-pass expansion**:

**Pass 1: Collect type hierarchy**
- For each concrete resource type, collect all implemented interfaces
- `RedisResource` → `[IResourceWithEnvironment, IResourceWithEndpoints, ...]`

**Pass 2: Expand capabilities**
- Capabilities targeting interfaces are expanded to concrete types
- `withEnvironment` (targets `IResourceWithEnvironment`) → expands to `[Redis, Container, Project, ...]`

Each capability has:
- `TargetTypeId` - The declared target (e.g., `Aspire.Hosting/IResourceWithEnvironment`)
- `ExpandedTargetTypeIds` - Pre-computed list of concrete types (e.g., `[Aspire.Hosting.Redis/Aspire.Hosting.ApplicationModel.RedisResource, Aspire.Hosting/Aspire.Hosting.ApplicationModel.ContainerResource, ...]`)

**Language generator usage:**
- Languages with inheritance (TypeScript, Swift) can use `TargetTypeId` for interface-based generation
- Languages without inheritance (Go, C) use `ExpandedTargetTypeIds` for flat generation

```mermaid
flowchart LR
    A["Assemblies<br/>(with CLR types)"] --> B["AtsCapabilityScanner"]
    B --> C["List&lt;AtsCapabilityInfo&gt;<br/>(pure ATS, no CLR types)"]
    C --> D["Code Generation<br/>(group by ExpandedTargetTypeIds)"]
    C --> E["Runtime<br/>(index by CapabilityId)"]
```

### Output (TypeScript)

```text
.modules/
├── .codegen-hash     # Hash of package references for cache invalidation
├── aspire.ts         # Generated SDK (builder classes, wrapper registrations)
├── base.ts           # Base classes, ReferenceExpression, AspireDict, AspireList
└── transport.ts      # JSON-RPC client, Handle, MarshalledHandle, callbacks
```

| File | Contents |
|------|----------|
| `transport.ts` | `AspireClient`, `Handle`, `MarshalledHandle`, callback registry, `registerHandleWrapper` |
| `base.ts` | `DistributedApplicationBuilderBase`, `ResourceBuilderBase`, `ReferenceExpression`, `refExpr`, `AspireDict`, `AspireList` |
| `aspire.ts` | Generated builders, wrapper classes, `createBuilder()`, handle wrapper registrations |

### Type Mapping

Capabilities are grouped by their (expanded) target type to generate builder classes:

| ATS Type ID | TypeScript Class | Capabilities |
|-------------|------------------|--------------|
| `Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder` | `DistributedApplicationBuilder` | `addRedis`, `addContainer`, `build` |
| `Aspire.Hosting.Redis/Aspire.Hosting.ApplicationModel.RedisResource` | `RedisBuilder` | `withPersistence`, `withEnvironment`, ... |
| `Aspire.Hosting/Aspire.Hosting.ApplicationModel.ContainerResource` | `ContainerBuilder` | `withBindMount`, `withEnvironment`, ... |
| `Aspire.Hosting/Aspire.Hosting.DistributedApplication` | `DistributedApplication` | `run` |

Primitive type mapping:

| ATS Type | TypeScript |
|----------|------------|
| `string` | `string` |
| `number` | `number` |
| `boolean` | `boolean` |
| `any` | `any` |
| `{Assembly}/{Type}` | `Handle<'{Assembly}/{Type}'>` (typed handle alias) |

---

## TypeScript Implementation

### Generated SDK Usage

```typescript
import { createBuilder, refExpr, EnvironmentCallbackContext } from './.modules/aspire.js';

const builder = await createBuilder();

// Add resources using fluent chaining
const cache = await builder
    .addRedis("cache")
    .withRedisCommander();

// Get endpoint for reference expressions
const endpoint = await cache.getEndpoint("tcp");

// Create dynamic connection string using tagged template literal
const redisUrl = refExpr`redis://${endpoint}`;

// Add container with environment callback
const api = await builder
    .addContainer("api", "mcr.microsoft.com/dotnet/samples:aspnetapp")
    .withEnvironmentCallback(async (ctx: EnvironmentCallbackContext) => {
        // Access execution context to check run/publish mode
        const execContext = await ctx.executionContext.get();
        const isRunMode = await execContext.isRunMode.get();

        // Set environment variables using AspireDict
        await ctx.environmentVariables.set("MY_CONSTANT", "hello from TypeScript");
        await ctx.environmentVariables.set("REDIS_URL", redisUrl);
    })
    .waitFor(cache)        // Accepts wrapper types directly
    .withReference(cache);

// Build and run
await builder.build().run();
```

### Fluent Async Chaining

Methods return `Thenable` wrappers enabling single-await chains:

```typescript
// Single await for entire chain
const cache = await builder
    .addRedis("cache")
    .withRedisCommander()
    .withDataVolume();

// Build and run in one await
await builder.build().run();
```

### Reference Expressions

```typescript
const redis = await builder.addRedis("cache");
const endpoint = await redis.getEndpoint("tcp");

// Tagged template literal
const connectionString = refExpr`redis://${endpoint}`;

await api.withEnvironment("REDIS_URL", connectionString);
```

### Type Hierarchy

TypeScript uses three layers to bridge the wire format and user API:

| Layer | Description | Example |
|-------|-------------|---------|
| `MarshalledHandle` | Wire format (plain JSON) | `{ $handle: "...", $type: "..." }` |
| `Handle<T>` | TypeScript class with type safety | `Handle<'Aspire.Hosting.Redis/Aspire.Hosting.ApplicationModel.RedisResource'>` |
| Wrapper Classes | User-facing API with methods | `RedisBuilder`, `EndpointReference` |

**MarshalledHandle** is what travels over JSON-RPC:

```typescript
interface MarshalledHandle {
    $handle: string;  // "Aspire.Hosting.Redis/Aspire.Hosting.ApplicationModel.RedisResource:42"
    $type: string;    // "Aspire.Hosting.Redis/Aspire.Hosting.ApplicationModel.RedisResource"
}
```

**Handle<T>** wraps marshalled data with type safety and serialization:

```typescript
class Handle<T extends string> {
    get $handle(): string;
    get $type(): T;
    toJSON(): MarshalledHandle;  // For serialization
}
```

**Wrapper Classes** provide the user API and must include `toJSON()` for use in reference expressions:

```typescript
class EndpointReference {
    constructor(handle: Handle, client: AspireClient) { ... }
    toJSON(): MarshalledHandle { return this._handle.toJSON(); }

    // Property accessors
    url = { get: async () => ... };
    host = { get: async () => ... };
}
```

---

## CLI Integration

The CLI uses `IAppHostProject` as the extension point for language support:

```mermaid
classDiagram
    class IAppHostProject {
        <<interface>>
        +LanguageId: string
        +DisplayName: string
        +DetectionPatterns: string[]
        +CanHandle(FileInfo): bool
        +ScaffoldAsync()
        +RunAsync()
        +PublishAsync()
        +AddPackageAsync()
    }

    IAppHostProject <|.. DotNetAppHostProject
    IAppHostProject <|.. TypeScriptAppHostProject
```

| Command | Method | Description |
|---------|--------|-------------|
| `aspire init` | `ScaffoldAsync` | Create apphost in current directory |
| `aspire new` | `ScaffoldAsync` | Create new project with apphost |
| `aspire run` | `RunAsync` | Build and run (development) |
| `aspire publish` | `PublishAsync` | Build and run (publish mode) |
| `aspire add` | `AddPackageAsync` | Add integration package |

---

## Configuration

### .aspire/settings.json

Package references for polyglot app hosts:

```json
{
  "packages": {
    "Aspire.Hosting.Redis": "9.0.0",
    "Aspire.Hosting.PostgreSQL": "9.0.0"
  }
}
```

Updated by `aspire add` command.

### apphost.run.json

Launch settings:

```json
{
  "profiles": {
    "https": {
      "applicationUrl": "https://localhost:17193;http://localhost:15069",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

---

## Adding New Guest Languages

To add a new language:

1. **Implement `IAppHostProject`** in `Aspire.Cli`
2. **Create code generator** in `Aspire.Hosting.CodeGeneration.<Language>`
3. **Implement ATS client** with JSON-RPC support

### Reusable Infrastructure

| Component | Project | Purpose |
|-----------|---------|---------|
| JSON-RPC Server | `Aspire.Hosting.RemoteHost` | Handles all RPC |
| Capability Dispatcher | `Aspire.Hosting.RemoteHost` | Routes to implementations |
| Handle Registry | `Aspire.Hosting.RemoteHost` | Object lifecycle |
| Capability Scanner | `Aspire.Hosting` | Discovers `[AspireExport]` |

### Code Generator Requirements

Implement `ICodeGenerator`:

```csharp
public interface ICodeGenerator
{
    string Language { get; }
    Dictionary<string, string> GenerateDistributedApplication(
        IReadOnlyList<AtsCapabilityInfo> capabilities);
}
```

The generator receives a list of `AtsCapabilityInfo` with:
- `CapabilityId` - Unique ID (e.g., `Aspire.Hosting.Redis/addRedis`)
- `MethodName` - Method name (e.g., `addRedis`)
- `TargetTypeId` - Original declared target (e.g., `Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder`)
- `ExpandedTargetTypeIds` - Concrete types for interface targets
- `ReturnTypeId` - Return type ID
- `Parameters` - List of parameter info
- `Description` - Documentation

**Generation steps:**

1. Group capabilities by `ExpandedTargetTypeIds` (or `TargetTypeId` for inheritance-based languages)
2. For each type, generate a builder class with all its capabilities as methods
3. Handle async/promise patterns appropriate for the language
4. Marshal handles, DTOs, and primitives according to the wire protocol

---

## Security

Both guest and host run locally on the same machine, started by the CLI. This is **not** remote execution.

### Protections

| Protection | Description |
|------------|-------------|
| **Capability allowlist** | Only `[AspireExport]` methods callable |
| **Runtime type validation** | CLR validates types at invocation time |
| **DTO enforcement** | Only `[AspireDto]` types serialized |
| **Socket authentication** | Secret token required |
| **Socket permissions** | Unix socket owner-only (0600) |

### What's NOT Exposed

- Arbitrary .NET reflection
- File system access
- Process spawning
- Network operations
- Any type without explicit `[AspireExport]`

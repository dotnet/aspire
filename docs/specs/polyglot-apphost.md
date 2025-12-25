# Polyglot AppHost Support

This document describes how the Aspire CLI supports non-.NET app hosts (TypeScript, Python) through a polyglot architecture.

## Overview

The polyglot apphost feature allows developers to write Aspire app hosts in languages other than C#. The CLI detects the app host type based on entry point files (`apphost.ts`, `apphost.py`) and orchestrates the appropriate runtime.

## Design Goals

The architecture is designed around these key principles:

1. **Reuse Existing Integrations**: All 100+ existing Aspire.Hosting.* NuGet packages work automatically with TypeScript and Python app hosts. No need to rewrite or port integrations - they're available immediately.

2. **Native Language Experience**: Generated SDKs provide idiomatic APIs with instance methods (e.g., `builder.addRedis("cache")`) rather than function-based approaches.

3. **Consistent CLI Experience**: Commands like `aspire run`, `aspire add`, and `aspire new` work identically regardless of the app host language. Developers don't need to learn different workflows.

4. **Leverage .NET Ecosystem**: The heavy lifting (container orchestration, service discovery, health checks, telemetry) remains in .NET where the mature Aspire.Hosting libraries live. Language runtimes focus on providing idiomatic APIs.

This approach means that when a new Aspire integration is released (e.g., `Aspire.Hosting.Milvus`), it's immediately available to TypeScript and Python developers via `aspire add milvus` - no SDK updates required.

## Architecture

```mermaid
flowchart TB
    subgraph CLI["Aspire CLI"]
        RC[RunCommand]
        AC[AddCommand]
        TSP[TypeScriptAppHostProject]
        PL[ProjectLocator]
        PM[ProjectModel]
        CGS[TypeScriptCodeGeneratorService]
    end

    subgraph CodeGen["Code Generation Library"]
        ACG[Aspire.Hosting.CodeGeneration]
        TSCG[Aspire.Hosting.CodeGeneration.TypeScript]
        ALR[AssemblyLoaderContext]
        AM[ApplicationModel]
    end

    subgraph GenericAppHost["GenericAppHost (.NET)"]
        JRS[JsonRpcServer]
        IP[InstructionProcessor]
        OD[OrphanDetector]
        AH[Aspire.Hosting]
    end

    subgraph LanguageRuntime["Language Runtime"]
        TS[TypeScript AppHost]
        Client[JSON-RPC Client]
    end

    RC --> PL
    AC --> PL
    PL -->|Detects apphost.ts| TSP
    AC -->|Updates settings.json| CGS
    TSP -->|1. Scaffolds & Builds| PM
    PM -->|Creates| GenericAppHost
    TSP -->|2. Runs code generation| CGS
    CGS --> ACG
    ACG --> ALR
    ALR -->|Loads assemblies| AM
    AM --> TSCG
    TSCG -->|Generates .modules/| TS
    TSP -->|3. Starts via dotnet exec| JRS
    TSP -->|4. Starts via npx tsx| TS

    TS --> Client
    Client <-->|Unix Domain Socket| JRS
    JRS --> IP
    IP -->|Executes Instructions| AH

    OD -->|Monitors CLI PID| CLI
    OD -->|Terminates on parent death| GenericAppHost
```

## Process Lifecycle

### Startup Sequence

1. **Detection**: `ProjectLocator` finds `apphost.ts` or `apphost.py` in the working directory
2. **GenericAppHost Preparation**:
   - `ProjectModel` scaffolds a .NET project in `$TMPDIR/.aspire/hosts/<hash>/`
   - References `Aspire.AppHost.Sdk` and required hosting packages
   - Builds the project with `dotnet build`
3. **Code Generation**:
   - Loads assemblies from build output using `AssemblyLoaderContext`
   - Builds `ApplicationModel` via reflection on loaded assemblies
   - Generates TypeScript SDK into `.modules/` folder
4. **GenericAppHost Launch**: Started via `dotnet exec` with:
   - `REMOTE_APP_HOST_SOCKET_PATH` - Unix domain socket path for JSON-RPC
   - `REMOTE_APP_HOST_PID` - CLI process ID for orphan detection
   - Environment variables from `apphost.run.json`
5. **Language Runtime Launch**: Started via `npx tsx` (TypeScript) or `python` (Python)
6. **Connection**: Language runtime connects to GenericAppHost over Unix domain socket

### Shutdown Sequence

Shutdown can be triggered by:

1. **User Interrupt (Ctrl+C)**: CLI receives signal, terminates child processes
2. **CLI Death**: `OrphanDetector` in GenericAppHost monitors parent PID, terminates when parent dies
3. **Connection Loss**: Language runtime detects disconnection and exits
4. **Startup Failure**: Errors (e.g., port conflicts) propagate back through JSON-RPC and terminate all processes

## JSON-RPC Protocol

Communication between the language runtime and GenericAppHost uses JSON-RPC 2.0 over Unix domain sockets.

### Instructions

| Instruction | Description |
|-------------|-------------|
| `CREATE_BUILDER` | Creates a `DistributedApplicationBuilder` |
| `INVOKE` | Invokes a method on a resource or builder |
| `RUN_BUILDER` | Builds and runs the distributed application |

### Example Flow

```mermaid
sequenceDiagram
    participant TS as TypeScript AppHost
    participant RPC as JSON-RPC Server
    participant IP as InstructionProcessor
    participant Aspire as Aspire.Hosting

    TS->>RPC: CREATE_BUILDER {name: "app"}
    RPC->>IP: Execute
    IP->>Aspire: DistributedApplication.CreateBuilder()
    IP-->>RPC: {success: true, builderName: "app"}
    RPC-->>TS: Result

    TS->>RPC: INVOKE {method: "AddRedis", args: ["cache"]}
    RPC->>IP: Execute
    IP->>Aspire: builder.AddRedis("cache")
    IP-->>RPC: {success: true, resourceName: "cache"}
    RPC-->>TS: Result

    TS->>RPC: RUN_BUILDER {builderName: "app"}
    RPC->>IP: Execute
    IP->>Aspire: app.RunAsync()
    IP-->>RPC: {success: true, status: "running"}
    RPC-->>TS: Result
```

## Code Generation

The CLI generates TypeScript SDK code that provides type-safe APIs with instance methods for all Aspire integrations.

### Architecture

```mermaid
flowchart LR
    subgraph Input
        CFG[.aspire/settings.json]
        ASM[Built Assemblies]
    end

    subgraph Library["Aspire.Hosting.CodeGeneration"]
        ALC[AssemblyLoaderContext]
        AM[ApplicationModel]
        IM[IntegrationModel]
        RM[ResourceModel]
    end

    subgraph TSLib["Aspire.Hosting.CodeGeneration.TypeScript"]
        TSG[TypeScriptCodeGenerator]
        EMB[Embedded Resources]
    end

    subgraph Output[".modules/"]
        DA[distributed-application.ts]
        TYP[types.ts]
        RPC[RemoteAppHostClient.ts]
    end

    CFG -->|Package refs| ALC
    ASM -->|PE metadata| ALC
    ALC --> AM
    AM --> IM
    IM --> RM
    AM --> TSG
    EMB --> TSG
    TSG --> DA
    TSG --> TYP
    TSG --> RPC
```

### How It Works

1. **Assembly Loading**: `AssemblyLoaderContext` uses `System.Reflection.Metadata` (PEReader) to load assemblies without executing them - this is AOT-compatible and lightweight.

2. **Model Building**: `ApplicationModel` aggregates `IntegrationModel` instances for each package, extracting:
   - Extension methods on `IDistributedApplicationBuilder`
   - Resource types implementing `IResource`
   - Method signatures and parameter types

3. **Code Generation**: `TypeScriptCodeGenerator` produces:
   - `DistributedApplicationBuilder` class with instance methods
   - Resource-specific builder classes (e.g., `RedisResourceBuilder`)
   - Type definitions for parameters and return types

### Generation Trigger

Code generation runs automatically when:

1. **First Run**: `.modules/` folder doesn't exist
2. **Package Changes**: Hash of package references has changed
3. **After `aspire add`**: When adding new integrations

The CLI computes a SHA256 hash of all package IDs and versions. This hash is stored in `.modules/.codegen-hash` and compared on each run.

### Generated File Structure

```
.modules/
├── .codegen-hash              # SHA256 hash of package references
├── distributed-application.ts # Main SDK with builder classes
├── types.ts                   # Instruction types for JSON-RPC
└── RemoteAppHostClient.ts     # JSON-RPC client implementation
```

### Generated Code Example

For `Aspire.Hosting.Redis`, the generator creates instance methods on `DistributedApplicationBuilder`:

```typescript
// .modules/distributed-application.ts (excerpt)

export class DistributedApplicationBuilder extends DistributedApplicationBuilderBase {
  /**
   * Adds a Redis resource to the application.
   * @param name The name of the resource.
   * @param port The host port for Redis.
   * @returns A RedisResourceBuilder for further configuration.
   */
  async addRedis(name: string, port?: number | null): Promise<RedisResourceBuilder> {
    // ... implementation
  }
}

export class RedisResourceBuilder extends ResourceBuilderBase {
  /**
   * Configures Redis persistence.
   */
  async withPersistence(interval?: number | null, keysChangedThreshold?: number | null): Promise<this> {
    // ... implementation
  }
}
```

This allows TypeScript app hosts to write idiomatic code:

```typescript
// apphost.ts
import { createBuilder } from './.modules/distributed-application.js';

const builder = await createBuilder();
const cache = await builder.addRedis('cache');
await cache.withPersistence();

const app = builder.build();
await app.run();
```

## Adding Integrations

The `aspire add` command works consistently across all app host types:

```bash
# Works the same for .NET, TypeScript, and Python projects
aspire add redis
aspire add Aspire.Hosting.Redis --version 13.1.0
```

### How It Works

```mermaid
flowchart TB
    subgraph AddCommand
        DETECT[Detect AppHost Type]
        SEARCH[Search NuGet Packages]
        SELECT[Select Package/Version]
    end

    subgraph DotNet[".NET Projects"]
        DOTNET_ADD[dotnet add package]
    end

    subgraph Polyglot["TypeScript/Python Projects"]
        UPDATE_CFG[Update .aspire/settings.json]
        REGEN[Regenerate SDK Code]
    end

    DETECT --> SEARCH
    SEARCH --> SELECT
    SELECT -->|.csproj| DOTNET_ADD
    SELECT -->|apphost.ts/py| UPDATE_CFG
    UPDATE_CFG --> REGEN
```

### For .NET Projects
- Uses `dotnet add package` to add the NuGet package reference

### For TypeScript/Python Projects
1. Updates `.aspire/settings.json` with the package reference
2. Regenerates SDK code to include new integration methods
3. On next `aspire run`, the GenericAppHost will restore and include the new package

## Configuration

### .aspire/settings.json

The `.aspire/settings.json` file configures the polyglot app host. The `packages` field uses an object literal format similar to npm's `package.json`:

```json
{
  "appHostPath": "../apphost.ts",
  "packages": {
    "Aspire.Hosting.Redis": "13.1.0",
    "Aspire.Hosting.PostgreSQL": "13.1.0"
  }
}
```

| Field | Description |
|-------|-------------|
| `appHostPath` | Relative path to the app host entry point |
| `packages` | Object mapping package names to versions |

### apphost.run.json

The `apphost.run.json` file configures the app host runtime, using the same format as .NET launch settings:

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "https": {
      "applicationUrl": "https://localhost:17000;http://localhost:15000",
      "environmentVariables": {
        "ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL": "https://localhost:21000",
        "ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL": "https://localhost:22000"
      }
    }
  }
}
```

The CLI reads this file and passes environment variables to the GenericAppHost process.

## Version Handling

The GenericAppHost uses the same Aspire package version as the installed CLI:

- Production versions (e.g., `13.1.0`): Used directly
- Dev versions (e.g., `13.2.0-dev`): Falls back to latest stable (`13.1.0`)
- Override: Set `ASPIRE_POLYGLOT_PACKAGE_VERSION` environment variable

## File Locations

| Path | Description |
|------|-------------|
| `$TMPDIR/.aspire/hosts/<hash>/` | GenericAppHost project directory |
| `$TMPDIR/.aspire/sockets/<hash>.sock` | Unix domain socket for JSON-RPC |
| `.aspire/settings.json` | Project configuration with package references |
| `.modules/` | Generated TypeScript SDK code |
| `apphost.run.json` | Launch settings (in project root) |

The `<hash>` is derived from the SHA256 of the app host directory path, ensuring unique locations per project.

## Orphan Detection

The `OrphanDetector` class monitors the CLI process to prevent orphaned GenericAppHost processes:

```csharp
// GenericAppHost monitors CLI PID
var cliPid = Environment.GetEnvironmentVariable("REMOTE_APP_HOST_PID");
OrphanDetector.MonitorParentProcess(int.Parse(cliPid), () => {
    Environment.Exit(0);
});
```

This ensures cleanup even if the CLI crashes or is killed unexpectedly.

## Error Handling

Errors during startup (e.g., port conflicts, missing dependencies) are propagated through the JSON-RPC connection:

1. GenericAppHost catches the exception in `InstructionProcessor`
2. Error is returned as JSON-RPC error response
3. Language runtime receives error and exits with failure code
4. CLI detects child process exit and terminates

This ensures the entire process tree terminates cleanly on startup failures.

# Aspire.Hosting.RemoteHost

`Aspire.Hosting.RemoteHost` provides the `aspire-server` process used by polyglot Aspire AppHosts.

It exposes Aspire hosting capabilities over JSON-RPC so non-.NET AppHosts (for example TypeScript) can create builders, invoke hosting APIs, receive callback invocations, and use language-specific scaffolding/code-generation services.

## Overview

The current design keeps the JSON-RPC transport and process boundary in **RemoteHost**, while the ATS runtime that executes capabilities lives in **Aspire.Hosting**.

At a high level:

- the **CLI/bundle carries `aspire-server`**
- the **application restores `Aspire.Hosting` and its extension packages**
- `aspire-server` loads those restored assemblies into an integration load context
- RemoteHost crosses that boundary through reflection proxies
- the actual ATS catalog/session runtime runs inside the loaded `Aspire.Hosting` assembly set

## Architecture

```text
┌──────────────────────────────────────────────┐
│ Polyglot AppHost client                      │
│ (TypeScript / Python / Go / Java / Rust)     │
└──────────────────────┬───────────────────────┘
                       │ JSON-RPC
                       ▼
┌──────────────────────────────────────────────┐
│ RemoteHost (aspire-server, CLI-provided)     │
│ Default load context                         │
│                                              │
│  Hosted services                             │
│  - JsonRpcServer                             │
│  - OrphanDetector                            │
│                                              │
│  Singleton services                          │
│  - AssemblyLoader                            │
│  - AtsCatalogProxy                           │
│  - CodeGenerationService                     │
│  - CodeGeneratorResolver                     │
│  - LanguageService                           │
│  - LanguageSupportResolver                   │
│                                              │
│  Scoped per-client services                  │
│  - JsonRpcCallbackInvoker                    │
│  - AtsSessionProxy                           │
│  - RemoteAppHostService                      │
└──────────────────────┬───────────────────────┘
                       │ reflection boundary
                       ▼
┌──────────────────────────────────────────────┐
│ IntegrationLoadContext                       │
│ (app-restored Aspire assemblies)             │
│                                              │
│  - Aspire.Hosting                            │
│  - Aspire.Hosting.* extensions               │
│  - codegen/language implementations          │
│                                              │
│  Hosting-side ATS runtime                    │
│  - AtsCatalog   (singleton scan/catalog)     │
│  - AtsSession   (scoped per-client runtime)  │
└──────────────────────────────────────────────┘
```

### Boundary split

The important split is:

- **RemoteHost owns**
  - JSON-RPC transport
  - callback connection plumbing
  - reflection proxies
  - projected metadata used for RPC/code generation responses
- **Aspire.Hosting owns**
  - ATS scanning/catalog state
  - capability dispatch
  - handle/cancellation/callback runtime
  - session lifetime for capability execution

This keeps the execution runtime aligned with the app-restored `Aspire.Hosting` version instead of duplicating it in `aspire-server`.

## Assembly loading and load contexts

`AssemblyLoader` reads the `AtsAssemblies` configuration and loads those assemblies on demand.

### Configuration

```json
{
  "AtsAssemblies": [
    "Aspire.Hosting",
    "Aspire.Hosting.Redis",
    "Aspire.Hosting.PostgreSQL"
  ]
}
```

### Integration load context

When `ASPIRE_INTEGRATION_LIBS_PATH` is set, RemoteHost creates a collectible `IntegrationLoadContext` and loads the configured assemblies from that directory. This is the normal polyglot flow used by the CLI/bundle.

Some framework/shared assemblies stay shared from the default context, including:

- `System`
- `System.Private.CoreLib`
- `mscorlib`
- `netstandard`
- `StreamJsonRpc`
- selected diagnostics/eventing assemblies

Everything else is loaded from the integration directory when available so the app's restored `Aspire.Hosting` packages are the ones executing capability logic.

## Core components

### `RemoteHostServer`

`RemoteHostServer.RunAsync(args)` starts the host, registers services, and begins serving JSON-RPC requests.

There is no longer an API that accepts assemblies directly. Assembly selection is driven by configuration plus the integration load context.

### `AtsCatalogProxy`

`AtsCatalogProxy` is the singleton reflection boundary over `Aspire.Hosting.Ats.AtsCatalog`.

It:

- loads `Aspire.Hosting` from the configured assembly set
- creates the Hosting-side `AtsCatalog`
- exposes a projected `AtsContext` back to RemoteHost
- creates per-client `AtsSessionProxy` instances

### `AtsSessionProxy`

`AtsSessionProxy` is the scoped reflection boundary over `Aspire.Hosting.Ats.AtsSession`.

It:

- invokes capabilities in the Hosting-side session
- forwards token cancellation
- unwraps reflected exceptions
- translates Hosting-side `CapabilityException` values back into RemoteHost error models

### `RemoteAppHostService`

This is the JSON-RPC-facing service for runtime capability execution.

It is intentionally thin and delegates to:

- `JsonRpcCallbackInvoker` for server-to-client callback calls
- `AtsSessionProxy` for capability execution and cancellation

### `CodeGenerationService` and `LanguageService`

These remain singleton services on the RemoteHost side.

- `CodeGenerationService` uses projected ATS metadata for `getCapabilities` and the isolated Hosting context for `generateCode`
- `LanguageService` provides scaffolding, language detection, and runtime spec queries
- resolver discovery still happens in RemoteHost, but it acts over the assemblies loaded through `AssemblyLoader`

## JSON-RPC surface

### Methods exposed by the server

| Method | Description |
|--------|-------------|
| `ping` | Health check |
| `invokeCapability` | Invoke an ATS capability |
| `cancelToken` | Cancel a previously issued token |
| `getCapabilities` | Return projected ATS capability/type metadata |
| `generateCode` | Generate SDK files for a codegen language |
| `scaffoldAppHost` | Scaffold a new polyglot AppHost |
| `detectAppHostType` | Detect the language/runtime of an AppHost directory |
| `getRuntimeSpec` | Return runtime execution metadata for a language |

### Client callback method expected by the server

| Method | Description |
|--------|-------------|
| `invokeCallback` | Invoked by RemoteHost on the connected client when callback parameters need to be executed |

## Environment variables

| Variable | Description |
|----------|-------------|
| `ASPIRE_INTEGRATION_LIBS_PATH` | Directory containing the restored integration assemblies to load into the integration load context |
| `REMOTE_APP_HOST_SOCKET_PATH` | Transport path used by the RPC server |
| `REMOTE_APP_HOST_PID` | Parent process ID used by orphan detection |

## Security

The transport is local-machine only and relies on OS-level access controls:

- on Unix/macOS, the socket file is created with restrictive permissions
- on Windows, named pipe ACLs restrict access to the current user

## Versioning and compatibility

The most important versioning fact is:

- **the CLI ships `aspire-server` (`Aspire.Hosting.RemoteHost`)**
- **the application restores `Aspire.Hosting` and extension packages**

Because of that, RemoteHost and Hosting do **not** need to be identical binaries, but they do need to be **compatible across the reflection boundary**.

Today that boundary is intentionally narrow but still brittle to breaking shape changes. RemoteHost reflects on internal Hosting-side members such as:

- `Aspire.Hosting.Ats.AtsCatalog.Create(...)`
- `AtsCatalog.GetIsolatedContext()`
- `AtsCatalog.CreateSession(...)`
- `AtsSession.InvokeCapabilityAsync(...)`
- `AtsSession.CancelToken(...)`

Implications:

- **additive changes** on the Hosting side are usually low risk
- **renames, removals, or signature changes** to those reflected members require coordinated updates
- CLI/bundle and restored hosting packages should generally stay on the same channel/build line

This split is still an improvement over the older design because the ATS execution runtime now lives with the app-restored Hosting packages rather than being duplicated inside RemoteHost.

## Typical usage

This package is normally launched by the Aspire CLI rather than referenced directly by user code. The CLI sets up the transport, provides the integration libraries path, and starts `aspire-server` with configuration that lists the assemblies the app wants to expose.

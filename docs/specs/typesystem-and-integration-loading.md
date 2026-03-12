# Aspire.TypeSystem and Integration Assembly Loading

## Overview

This document describes the architecture for how ATS (Aspire Type System) contracts are organized and how integration assemblies are loaded at runtime by the RemoteHost server (`aspire-server`).

## Aspire.TypeSystem Assembly

`Aspire.TypeSystem` is a standalone assembly that contains the ATS scanner, model types, and codegen contracts. It was extracted from `Aspire.Hosting` to establish a clean boundary between the host process and integration assemblies.

### What it contains

- **ATS Scanner** (`AtsCapabilityScanner`) — scans assemblies for `[AspireExport]` attributes and builds capability models
- **ATS Context** (`AtsContext`) — the runtime context holding scanned capabilities, handle types, and DTO types
- **ATS Constants** (`AtsConstants`) — type IDs and capability IDs for the ATS protocol
- **Codegen contracts** (`ICodeGenerator`, `ILanguageSupport`) — interfaces for polyglot code generation
- **Type helpers** (`HostingTypeHelpers`, `AttributeDataReader`) — name-based type matching for cross-assembly discovery
- **Model types** (`AtsCapabilityInfo`, `AtsError`, `RuntimeSpec`, `LanguageModels`)

### Key design decisions

- All types are **public** — no `InternalsVisibleTo` is needed
- The namespace remains `Aspire.Hosting.Ats` to minimize churn across consumers
- `Aspire.Hosting` does **not** reference `Aspire.TypeSystem` — the ATS attributes (`[AspireExport]`, `[AspireDto]`, etc.) remain in `Aspire.Hosting` and are discovered by name-based matching, not by type identity
- `Aspire.TypeSystem` is shared across load contexts — it is loaded in the default context and the `IntegrationLoadContext` defers to the default when resolving it

### Consumers

| Assembly | Relationship |
|----------|-------------|
| `Aspire.Hosting.RemoteHost` | Project reference — uses scanner, context, codegen contracts |
| `Aspire.Hosting.CodeGeneration.*` | Project reference — implements `ICodeGenerator` |
| `Aspire.Cli` | Project reference — uses `RuntimeSpec` |
| `Aspire.Hosting` | **No reference** — attributes are matched by name |

## Integration Assembly Loading

### Architecture

The RemoteHost server (`aspire-server`) runs inside `aspire-managed` and loads integration assemblies (e.g., `Aspire.Hosting`, `Aspire.Hosting.JavaScript`, `Aspire.Hosting.Azure.*`) in an isolated `IntegrationLoadContext`. This isolation prevents dependency conflicts between the host process and the integrations.

```
┌─────────────────────────────────────────────────────────────┐
│ Default Load Context (aspire-managed / aspire-server)       │
│                                                             │
│  aspire-managed.dll                                         │
│  Aspire.Hosting.RemoteHost.dll (aspire-server)              │
│  Aspire.TypeSystem.dll  ◄── shared with IntegrationALC      │
│  StreamJsonRpc.dll                                          │
│  Microsoft.Extensions.Hosting.dll (from bundle)             │
│  System.Diagnostics.DiagnosticSource.dll (from runtime)     │
│  ...                                                        │
├─────────────────────────────────────────────────────────────┤
│ IntegrationLoadContext ("Aspire.Integrations")              │
│                                                             │
│  Aspire.Hosting.dll                                         │
│  Aspire.Hosting.JavaScript.dll                              │
│  Aspire.Hosting.Azure.AppContainers.dll                     │
│  Aspire.Hosting.CodeGeneration.TypeScript.dll               │
│  Google.Protobuf.dll, KubernetesClient.dll, ...             │
│                                                             │
│  Framework assemblies (System.*, Microsoft.Extensions.*)    │
│  ──► deferred to default context via version unification    │
└─────────────────────────────────────────────────────────────┘
```

### IntegrationLoadContext

The `IntegrationLoadContext` is a custom `AssemblyLoadContext` that:

1. **Probes directories** for integration assemblies — the `ASPIRE_INTEGRATION_LIBS_PATH` (NuGet restore output) and `AppContext.BaseDirectory`
2. **Shares `Aspire.TypeSystem`** — always defers to the default context so `ICodeGenerator`, `ILanguageSupport`, and `AtsContext` have the same type identity across the boundary
3. **Performs version unification** — before loading an assembly from the probe directory, checks if the default context already provides it at a higher or equal version. If so, defers to the default. This prevents loading old NuGet package versions of framework assemblies (e.g., `System.Diagnostics.DiagnosticSource` 6.0) when the runtime provides a newer version (e.g., 9.0).

### Version Unification

The NuGet restore for integration packages may include transitive dependencies on framework-provided assemblies at older versions. For example, a transitive dependency chain might pull in `System.Diagnostics.DiagnosticSource` 6.0, even though the runtime provides 9.0.

Without version unification, loading the old 6.0 version causes `MissingMethodException` because `Aspire.Hosting` was compiled against the newer runtime APIs.

The `IntegrationLoadContext` handles this by attempting to load the assembly from the default context first:

```csharp
protected override Assembly? Load(AssemblyName assemblyName)
{
    // Find in probe directories
    var probedPath = FindInProbeDirectories(assemblyName);
    if (probedPath is null) return null;

    // Version unification: if the default context has this assembly
    // at a higher or equal version, defer to it
    if (TryGetDefaultContextVersion(assemblyName, out var defaultVersion))
    {
        var probedVersion = AssemblyName.GetAssemblyName(probedPath).Version;
        if (defaultVersion >= probedVersion)
            return null; // default context wins
    }

    return LoadFromAssemblyPath(probedPath);
}
```

This works in all deployment models (framework-dependent, self-contained, single-file) because it checks the default context at runtime rather than scanning framework directories on disk.

### Cross-ALC Communication

All communication between the default context (RemoteHost) and the integration ALC is through:

- **Reflection** — `MethodInfo.Invoke()` for capability dispatch; works across ALCs
- **Opaque handles** — objects created in the integration ALC are stored as `object` in the `HandleRegistry` and passed back by handle ID
- **JSON marshalling** — `AtsMarshaller` converts between JSON and .NET types using the target type's metadata
- **Shared `Aspire.TypeSystem` types** — `ICodeGenerator` and `ILanguageSupport` have the same type identity because `Aspire.TypeSystem` is shared, so `IsAssignableFrom` works for contract discovery

### Aspire.Managed Simplification

`Aspire.Managed` (the `aspire-managed` executable) directly references `Aspire.Hosting.RemoteHost` as a normal project reference. The previous architecture used:

- An embedded RemoteHost payload with resource assemblies
- A generated `ServerSharedAssemblyManifest` from an inline MSBuild task
- A `RemoteHostLoadContext` for loading the embedded payload

All of this has been removed. `Program.cs` directly calls `RemoteHostServer.RunAsync(args)`.

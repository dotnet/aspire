# Native integration providers

These nonshipping examples demonstrate **integration implementation** in C# and
TypeScript against the native server. Each external-API integration retains its
own exported `AddExternalApi`, `WithApiKey`, and resource identity API. Its
implementation uses generated native composition methods, not managed
`Aspire.Hosting`, capability-ID strings, invitations, or a test worker.

`AddExternalApi` declares an external HTTP(S) service; it does not provision that
service or perform an HTTP health check. `WithApiKey` stores a symbolic secret
reference without reading the credential in either integration language.

## Source layout

- `CSharp/ExternalApi`: ordinary resource class and `[AspireExport]` methods.
- `CSharp/Contracts`: offline provider dispatcher, metadata, and consumer
  projection generation. Generated files remain under `artifacts/obj`.
- `CSharp/Provider`: framework provider entry point.
- `CSharp/AppHost`: typed consumer with explicit framework provider activation.
- `TypeScript/external-api.mts`: `AspireExport`/`defineIntegration` implementation.
- `TypeScript/provider.mts`: framework provider entry point and offline metadata
  emission.
- `TypeScript/apphost.mts`: consumer of the offline-generated provider client.

## Validation

From the repository root:

```bash
./restore.sh
dotnet build playground/NativeIntegrations/CSharp/Provider/NativeIntegrations.Provider.csproj
dotnet build playground/NativeIntegrations/CSharp/AppHost/NativeIntegrations.AppHost.Sample.csproj
dotnet test --project tests/Aspire.Hosting.Native.Core.Tests/Aspire.Hosting.Native.Core.Tests.csproj \
  --no-launch-profile -- --filter-class '*.NativeProviderClientTests' \
  --filter-not-trait 'quarantined=true' --filter-not-trait 'outerloop=true'
```

Use the local `.dotnet` SDK/runtime as described in the repository instructions.
`eng/scripts/test-native-hosting.sh` additionally publishes the actual AOT server,
builds these providers and AppHosts, emits TS metadata, generates the TS consumer
projection, and executes both genuine AppHosts against authenticated native
sockets. That full lane requires Docker, DCP, and Node.js 24 or later.

For a separately running native server, the C# sample accepts a loopback endpoint
and the built provider executable path:

```bash
export ASPIRE_NATIVE_RPC_TOKEN='<native server authentication token>'
dotnet run --project playground/NativeIntegrations/CSharp/AppHost/NativeIntegrations.AppHost.Sample.csproj \
  -- 127.0.0.1:43210 "$PWD/artifacts/bin/NativeIntegrations.Provider/Debug/net11.0/NativeIntegrations.Provider"
```

Registration is explicit and one-use. The shared framework activates a selected
provider, discovers its offline metadata, and routes calls through bidirectional
RPC. Native core handles remain delegated to their original connection; a
provider-owned resource must declare an authorized native lifetime. Disconnects
fail pending calls without retries and do not retire the application's model.
Portable provider metadata is available through `getIntegrationCapabilities`;
the static `getCapabilities` contract remains separate from integration APIs.

## Deliberate boundaries

This phase supports required primitive arguments/results, typed handles, and
projected callbacks with nonvoid results. DTO/union/optional provider shapes and
void callback writeback are explicitly rejected.
The native core adds no Redis-specific APIs; this does not prove existing
integration compatibility.

Generic declarative workload composition now resides on the shared native
surface: `AddContainer`/`AddExecutable`, `WithEnvironment`, `WithArgument`,
`WithEndpoint`, `WithReference`, entrypoints, and retained named volumes.
Ordinary AppHosts declare the graph, then `StartApplication` activates native
workloads. `WaitApplicationReady` observes native dependency and protocol-health
ordering; no resource-owner invitations are needed. The native static contract
has 79 exports. Both generated languages project these same contracts.

## Real Redis integration

`CSharp/Redis/RedisBuilderExtensions.cs` and
`TypeScript/Redis/redis-authoring.mts` implement run-only Redis using ordinary
generic builder composition: `AddContainer`, environment/argument/endpoint
configuration, symbolic connection expressions, and executable health checks.
Inert typed resource data is separate from the provider's runtime callback
object. This is integration-author code, not generated RPC calls with new names.

Ordinary C# composition is synchronous:

```csharp
var builder = DistributedApplication.CreateBuilder(args);
var cache = builder.AddRedis("cache").WithDataVolume();
builder.AddExecutable("consumer", executablePath, workingDirectory)
    .WithReference(cache)
    .WaitFor(cache);
await builder.Build().RunAsync();
```

The equivalent TypeScript integration contributes a fluent builder method:

```typescript
const builder = await createBuilder();
const cache = await builder.addRedis('cache').withDataVolume();
await builder.addExecutable('consumer', executablePath, workingDirectory)
    .withReference(cache)
    .waitFor(cache);
await builder.build().run();
```

Composition and `Build` only record/seal a typed transport plan; they never
connect, block on RPC, resolve secrets, or create a language-local runtime graph.
Async execution materializes declarations and configuration through the
generated ATS projections into a staged native revision. The shared framework
owns authenticated transport, selected provider activation, workspace commits,
readiness, and retirement. Provider-side generated adapters materialize the
integration's same generic builder implementation under delegated native
authority; portable lifecycle callbacks remain out-of-process.

`WithReference` is consumer-side environment injection; `WaitFor` is a separate
explicit dependency operation. Native value references already contribute their
own readiness dependencies, so these are distinct authoring concepts, not a
claim that connection injection has no native scheduling dependency.

`AddRedis` defaults to a cryptographically generated **native-owned** secret
parameter, never an empty password. An explicit `AddParameter` can supply a
credential, including a changed value in a staged revision. Credentials are not
returned to the integration author. Redis receives its password through quoted
environment expansion, and an integration-owned protocol executable performs
real `AUTH` and `PING` before dependents can start. No raw socket readiness
substitute is used.

The integration enables AOF with `appendfsync always`; data volumes mount at
`/data` and survive application retirement. Ordinary AppHosts use a generated
credential and a real protocol consumer. Separate
`CSharp/RedisValidation` and `TypeScript/Redis/redis-validation.mts` scenarios use
the shared application `UpdateAsync`/`update` API to rotate an explicit password,
replace the container and affected consumer, and read retained AOF data. The
native workspace—not the recorder—owns revision identity and retention decisions.
Validation removes only its uniquely named volume after retirement.

```bash
dotnet build playground/NativeIntegrations/CSharp/RedisProvider/NativeIntegrations.Redis.Provider.csproj
dotnet build playground/NativeIntegrations/CSharp/RedisAppHost/NativeIntegrations.Redis.AppHost.Sample.csproj
dotnet build playground/NativeIntegrations/CSharp/RedisValidation/NativeIntegrations.Redis.Validation.csproj
```

For the C# ordinary sample, framework configuration can select a separately
running server/provider and finite consumer without adding transport code:

```bash
export ASPIRE_NATIVE_RPC_TOKEN='<native server authentication token>'
dotnet run --project playground/NativeIntegrations/CSharp/RedisAppHost/NativeIntegrations.Redis.AppHost.Sample.csproj -- \
  --native-endpoint=127.0.0.1:43210 \
  --native-provider=example.csharp.redis="$PWD/artifacts/bin/NativeIntegrations.Redis.Provider/Debug/net11.0/NativeIntegrations.Redis.Provider" \
  --consumer-path="$PWD/artifacts/bin/NativeIntegrations.Redis.Provider/Debug/net11.0/NativeIntegrations.Redis.Provider" \
  --native-exit-on-completion=consumer
```

Provider selection is framework configuration, not application registration.
Without `--native-exit-on-completion`, `RunAsync`/`run` waits for shutdown and
retires before closing its provider. `--native-volume-prefix` namespaces the
default `<prefix>-cache-data` retained volume. C# framework time/log services can
be injected with `NativeBuilderOptions`. TypeScript provider configuration uses
`--native-provider=example.ts.redis=<node>|<provider.mjs>`; quote the whole flag
so `|` is not interpreted by the shell.

The dedicated native harness also generates the TS Redis provider/consumer,
executes both AppHosts against the actual AOT server and DCP, and validates
authenticated readiness, persistence, revisions, provider retirement callbacks,
and workload cleanup.

`WithLifecycleExecutable` declares provider-owned one-shot `initialize`,
`health`, `ready`, and `retire` commands with server-resolved symbolic inputs.
Health commands may retry within the startup deadline; non-health side effects
are not retried. Retirement executes before workload deletion, retains failed
cleanup obligations, and never sends resolved inputs or command output back to
the integration authoring process.

The Redis resource also exports integration-owned ATS lifecycle handlers.
C# uses `[NativeResourceLifecycle("ready")]` alongside `[AspireExport]`; TS uses
the same portable `lifecyclePhase` capability metadata. Returning a resource
automatically binds its validated handlers to its original native declaration.
Ordinary AppHosts do not construct bindings, scope handles, or callback requests.
Handlers receive a generated `ResourceLifecycleScope` and cancellation
(`CancellationToken` in C#, an `AbortSignal` in TS), not resolved secrets.
Initialization, allocation, readiness, and retirement are native-ordered,
bounded, and never replayed. The retirement handler requests actual
authenticated Redis `SAVE` using native-captured input slots before the
container is deleted. Validation checks its resulting `dump.rdb`.

Scope authority permits resource logs and bounded one-shot native executable
commands only; there is no credential/reference read API or general declaration
authority. Public handles are revoked before retirement; the cleanup scope
expires after the phase and drains its native executions. Provider-local
objects are released deterministically after generation replacement, resource
removal, aborted authoring, or disconnect. Retained physical workloads adopt
the next authoring object's handlers without restarting or replaying
initialization. C# `DisposeAsync` and TS `dispose` release local state once.

The 79-export builder examples also passed against the independent Rust runtime.
Both ordinary C#/TS AppHosts exercised native-generated default credentials,
authenticated readiness, references, retirement SAVE and owned-volume cleanup.
Both separate validators exercised rejected revisions, password rotation and
retained data. Rust source is not included in this change. In-process .NET tests
are not Rust execution evidence.

Ongoing health monitoring, container-network references,
TLS, Redis modules/admin companions, provider-catalog discovery, dynamic provider
addition, and publish/deploy remain outside this
bounded run-only sample. Unsupported container-context endpoint references fail
explicitly instead of using host loopback. No existing Redis parity is claimed.
`--publish=true` fails explicitly; no TLS authoring operation is exposed.
Default generated credentials are new native declarations per materialized
revision; use an explicit parameter when controlling retention/rotation.
Backing-declaration retirement immediately revokes native provider handles.
Prestart allocated-endpoint barriers, external endpoint publication/revocation,
provider notification/login selection, and remote-object retention policies
needed by other integration archetypes are not covered by this Redis example.

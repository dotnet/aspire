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
has 78 exports. Both generated languages project these same contracts.

## Real Redis integration

`CSharp/Redis` and `TypeScript/Redis/redis.mts` implement run-only Redis APIs on
those shared declarations, retaining their own `AddRedis`, `WithDataVolume`, and
`WithReference` APIs. Passwords are required symbolic parameters; integration
authoring code never reads them. Redis receives its password through quoted
environment expansion, and an integration-owned protocol executable performs
real `AUTH` and `PING` before dependents can start. No raw socket readiness
substitute is used.

The integration enables AOF with `appendfsync always`; data volumes mount at
`/data` and survive application retirement. The ordinary C#/TS Redis AppHosts
launch a real protocol consumer, write/read data, commit a password-rotation
revision that replaces the Redis container and its affected consumer, then read
the persisted value again. Native reference resolution supplies the new
endpoint and credential. The validation scenario removes its own uniquely named
volume after both revisions.

```bash
dotnet build playground/NativeIntegrations/CSharp/RedisProvider/NativeIntegrations.Redis.Provider.csproj
dotnet build playground/NativeIntegrations/CSharp/RedisAppHost/NativeIntegrations.Redis.AppHost.Sample.csproj
```

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

The same C# and TS Redis integrations and AppHosts also pass against the local
independent Rust runtime, including authenticated retirement callbacks, snapshot
files and owned-volume cleanup. Rust source is not included in this repository
change. In-process .NET lifecycle tests are not Rust execution evidence.

Ongoing health monitoring, container-network references,
TLS, Redis modules/admin companions, and publish/deploy remain outside this
bounded run-only sample. Unsupported container-context endpoint references fail
explicitly instead of using host loopback. No existing Redis parity is claimed.
Backing-declaration retirement immediately revokes native provider handles.
Prestart allocated-endpoint barriers, external endpoint publication/revocation,
provider notification/login selection, and remote-object retention policies
needed by other integration archetypes are not covered by this Redis example.

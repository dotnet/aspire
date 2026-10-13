# Aspire App Model v2: Concepts, Design, and Authoring Guidance

> **Status:** AppHost Server v2 implementation behind the default-off project feature `features.appHostServerV2`; not a replacement for managed Hosting.
> **Audience:** Hosting integration authors and contributors exploring a language-neutral, NativeAOT-friendly application model.
> **Baseline:** [Aspire Resource Model](appmodel.md).
> **Related:** [Polyglot integrations](polyglot-integrations.md#single-runtime-representation).

## Overview

App Model v2 preserves Aspire's familiar authoring model: declare resources,
configure them with fluent APIs, connect them through references, and let the
runtime coordinate execution. The difference is where the model lives and how
integrations participate.

The application model is owned by a small AppHost Server v2 core. Integrations use
language-neutral contracts through the Aspire Type System (ATS), rather than
depending on live CLR objects inside the AppHost server. C# and TypeScript
projections provide ordinary typed APIs over the same model.

The guiding principles are:

- **Inert composition:** Declaring a resource does not start it or contact a service.
- **Authoritative model:** Identity, configuration, relationships and execution belong to the server.
- **Portable contracts:** Integration behavior must not depend on the core being implemented in C#.
- **Explicit ownership:** Resources, providers and operations have defined scopes and lifetimes.
- **Small core:** Service-specific implementation and dependencies stay in integrations.

## Quick Start

The current Redis examples use familiar fluent APIs:

```csharp
var builder = DistributedApplication.CreateBuilder(args);
var cache = builder.AddRedis("cache").WithDataVolume();

builder.AddExecutable("consumer", executablePath, workingDirectory)
    .WithReference(cache)
    .WaitFor(cache);

await builder.Build().RunAsync();
```

The equivalent TypeScript authoring shape is:

```typescript
const builder = await createBuilder();
const cache = await builder.addRedis('cache').withDataVolume();

await builder.addExecutable('consumer', executablePath, workingDirectory)
    .withReference(cache)
    .waitFor(cache);

await builder.build().run();
```

`WithReference` supplies connection information to the consumer. `WaitFor`
expresses an explicit readiness dependency. These are different operations,
although resolving a value can itself require another resource to be ready.

The [native integration examples](../../playground/NativeIntegrations/README.md)
contain the current C# and TypeScript implementations. Other examples in this
guide describe design requirements, not additional supported APIs.

## Architecture

There are three main participants:

| Participant | Responsibility |
| --- | --- |
| AppHost | Compose the graph, select integrations and request execution |
| Native hosting core | Own declarations, relationships, values, scheduling and workload lifetime |
| Integration host | Implement integration APIs and service-specific runtime behavior |

ATS describes the contracts between these participants. Generated projections
carry typed values, resource references and callbacks across the boundary.
Callers use resource APIs, not protocol messages or transport-specific details.

An integration host can use service clients, local services and background
controllers. Those dependencies do not become dependencies of the native core.
The execution backend starts generic containers and executables; the core does
not need a Redis implementation to run a Redis resource.

### Code Generation and JSON Boundaries

The TypeScript SDK uses the existing ATS TypeScript generator, not a separate
SDK-generation system. `Aspire.Hosting.Native.CodeGeneration` is a nonshipping
build tool: the server's MSBuild target scans ATS declarations offline, emits
static C# dispatch and embeds its contract and core SDK. Roslyn and reflection
stay in that build tool, not in the execution server.

For provider-backed product AppHosts, the CLI invokes
`aspire-managed apphost-codegen-v2` during preparation. This combines the bundled
core contract and provider metadata with the same ATS TypeScript generator.
The execution server exposes these prepared SDK files through its ordinary
code-generation RPC.

The core model has no JSON dependency. At the server boundary, `JsonElement`
reads and validates immutable wire data, including rejecting duplicate keys.
`JsonObject`/`JsonArray` are used where replies or delegated handle arguments
must be constructed or rewritten. These representations serve different
purposes; mutable JSON does not become authoritative model state.

`SymbolicValue` is the core's immutable representation of a reference
expression. It retains endpoint and parameter dependencies, secret
classification and generation ownership until the runtime can resolve a value.
The familiar authoring `ReferenceExpression` is a client facade over this
server-owned expression, not a second independently authoritative model.

### Core Baseline Versus In-Process Conveniences

The existing hosting model mixes portable semantics with useful .NET
implementation techniques. V2 preserves the semantics without requiring those
techniques at the server boundary.

| Existing pattern | V2 direction |
| --- | --- |
| Resource classes and `IResourceBuilder<T>` | Typed facades over server-owned resource identities |
| Standard resource interfaces | Language-neutral capability contracts |
| Arbitrary annotation objects | Structured extension data with declared semantics |
| Captured value-provider delegates | Structured expressions and explicit provider contracts |
| Application event subscriptions | Resource-scoped lifecycle registrations |
| In-process custom resource loops | Integration-owned controllers |
| Manifest writer callbacks | Typed model views and structured publishing contributions |

A projection can still use extension methods, classes or interfaces locally.
They are authoring conveniences, not the authoritative representation of the
graph.

## Resource Basics

A resource is a named entity in the application graph. Its declaration describes
configuration, capabilities, outputs and relationships. Runtime state is kept
separately from that declaration.

Resource identity is more than its display name. A replacement resource with
the same name must not inherit the previous resource's execution authority or
pending operations.

Resources are inert during composition. Constructors and `AddX` methods must
not start workloads, allocate endpoints, resolve credentials or provision
external services.

### Resource Type and Execution Kind

An integration's resource type describes what the resource represents. Its
execution kind describes how it operates.

| Resource | Execution kind |
| --- | --- |
| Redis or PostgreSQL server | Container |
| Application or tool | Executable |
| PostgreSQL database | Logical child with initialization behavior |
| Dev Tunnel | Integration-owned controller |
| Parameter or connection value | Model data without independent execution |

A Redis resource can build on generic container capabilities while adding
Redis-specific configuration and connection properties. It should not require
a Redis-specific type inside the core.

General integration-defined resource schemas remain part of the design
direction. A caller-provided type name alone must never grant additional
capabilities.

## Standard Capabilities and Polymorphism

Capabilities describe what a resource supports independently of its concrete
integration type.

| Capability | Purpose |
| --- | --- |
| Environment | Configure literal or deferred environment values |
| Arguments | Configure executable or container arguments |
| Endpoints | Declare named ports and reference their allocated values |
| Connection properties | Expose connection information for consumers |
| Readiness dependencies | Coordinate startup with other resources |
| Lifecycle ownership | Tie a resource's execution to an owner |
| Observations | Read state, health, endpoints and configuration status |

Generic tooling should work against these capabilities rather than enumerate
every integration's resource type. A publisher should be able to inspect a
container or connection expression without loading the integration's CLR class.

Generated clients provide compile-time guidance. The server independently
checks applicability and authority; possessing a resource reference does not
automatically permit changing its runtime state.

## Add and With Authoring Patterns

### Adding Resources

`AddX` creates a resource, establishes sensible defaults and contributes the
configuration or behavior needed by that integration.

For Redis, this includes a container image, a TCP endpoint, a symbolic password,
connection expressions and authenticated readiness checking. The integration
owns those Redis-specific choices; the core owns the resulting graph and
generic workload execution.

An `AddX` method should return an appropriately typed resource builder. The
resource remains configurable until composition is sealed.

### Configuring Resources

`WithX` modifies a declaration without changing its identity:

```csharp
var cache = builder.AddRedis("cache").WithDataVolume();
var consumer = builder.AddExecutable("consumer", executablePath, workingDirectory)
    .WithReference(cache)
    .WaitFor(cache);
```

Configuration methods should distinguish settings, value references,
relationships and runtime behavior. A method that creates a child resource
should make that distinction clear.

### Integration Implementation

An integration has two kinds of work:

| Kind | Examples |
| --- | --- |
| Composition | Declare a workload, add endpoints, configure arguments, expose connection properties |
| Runtime behavior | Initialize a database, perform a health probe, monitor a tunnel, handle a command |

Simple integrations can be composed entirely from generic workload primitives.
Others register behavior implemented in their integration host. Exported
methods and their generated projections form the shared API; consumers do not
need to know how providers are connected.

## Annotations and Extension Data

Annotations remain an important extensibility concept, but arbitrary live
objects cannot be the portable representation.

The intended contract is structured, typed data with explicit semantics:

- Singleton settings replace a previous contribution.
- Repeatable contributions preserve their identity and ordering.
- Relationship and expression fields retain their references.
- Runtime behavior is registered explicitly, not serialized as a delegate.

For example, Redis persistence settings could be represented as typed
integration metadata. Another integration or publisher could inspect that data
without loading a Redis implementation.

Portable annotation schemas are not implemented as a general native API today.
Integration-local objects are allowed, but are not shared model annotations.

## Values and References

A value can be a literal, parameter, endpoint property or expression composed
from other values.

Connection information should remain structured rather than be resolved into
a string during composition:

```text
Redis connection
  = scheme
  + encoded password parameter
  + endpoint host
  + endpoint port
```

This preserves references and sensitivity until the value is needed. Runtime
resolution supplies the values appropriate to execution; a future publisher
must translate the same expression for its deployment target.

Sensitivity propagates through expressions. Formatting a secret into a URI
does not make the result safe to include in model inspection or logs.

### Endpoints

An endpoint declaration describes its name, protocol, scheme and requested
ports. An allocation supplies the actual runtime address.

References target the declaration, not a cached local port. Replacement or
restart can change the allocation without changing the authored expression.
A workload can expose multiple independently named endpoints.

Resolution must ultimately account for the consumer's network context. A
host-facing address is not automatically the correct address for another
container. General consumer-network resolution remains an open requirement.

## Relationships and Resource Hierarchy

Different relationships express different obligations:

| Relationship | Meaning |
| --- | --- |
| Visual grouping | Nest resources in presentation without controlling execution |
| Lifecycle ownership | Tie a child's lifetime to its owner |
| Readiness dependency | Wait for another resource to satisfy a condition |
| Value reference | Consume a parameter, endpoint or output |

These relationships are explicit, not inferred from names. A visual parent is
not automatically a lifetime owner, and sharing a connection is not equivalent
to requesting a readiness wait.

Validation must distinguish allocation dependencies from execution
dependencies. Mutually referenced public URLs, for example, should not
necessarily become a startup cycle.

## Lifecycle and Health

The core coordinates execution. Integrations supply service-specific behavior
through scoped registrations.

| Behavior | Example |
| --- | --- |
| Preparation | Produce files or configuration required for execution |
| Initialization | Create a PostgreSQL database after the server is ready |
| Pre-start work | Configure a tunnel using allocated endpoint information |
| Health checking | Authenticate to Redis and issue a protocol probe |
| Controller | Monitor an external service and report observations |
| Disposal | Release integration-owned state and await background work |

Initialization is bounded work that completes. A controller has an ongoing
lifetime that must be explicitly owned and stopped. Neither belongs in a
resource constructor.

Running and ready are separate concepts. A container process can be running
while its service is unavailable. Dependents must observe the declared
readiness condition, not merely an open port.

Provider failures and disconnects must be visible. The core must not retain
healthy state indefinitely after the integration responsible for it disappears.

## State, Logs, Commands and Interactions

Declarations describe desired configuration. Observations describe what is
currently happening: state, health, endpoints and applied configuration.
Consumers receive snapshots and updates without being able to mutate another
owner's resource.

Resource logs are human-readable diagnostics, not a replacement for structured
state. Generic workload output and integration-provided messages belong to the
same resource-facing experience.

Commands expose resource-scoped operations such as restart or integration
maintenance. Their execution needs explicit ownership, cancellation and
concurrency rules; disabling a Dashboard button is not sufficient protection.

Interactions let an integration request user input or confirmation without
depending on a particular UI. An unavailable or dismissed interaction must not
be treated as successful approval.

The Dashboard and CLI consume these shared contracts. They adapt the model for
presentation rather than become alternate owners of its state.

## Custom Resources: Talking Clock

The [original guide's Talking Clock](appmodel.md#example-custom-resource---talking-clock)
illustrates a resource with no container or executable of its own. Its v2
equivalent would:

1. Declare the clock and its child resources.
2. Register an integration-owned controller.
3. Publish tick/tock observations and resource logs while active.
4. Stop and await the controller when its resource is retired.

The core knows the resource's identity, relationships and lifetime, not how a
clock ticks. A Dev Tunnel controller belongs to the same category, while
additionally reporting external endpoints.

This is a design example, not an implemented native Talking Clock integration.

## Model Revisions and Guest Reload

The guest AppHost process, application model and integration host have separate
lifetimes. Restarting authoring code does not have to restart every integration
host or workload.

A workspace accepts a staged revision. The server validates it before making
it authoritative, retains compatible resources, and retires resources that
were removed or must be replaced. Integration owners apply configuration and
participate in any necessary restart.

Retirement revokes old authority and stops owned work. Late callbacks or
observations cannot update a replacement resource, even if its name is
unchanged.

Resource-preserving revisions exist in the current exploration. Automatic CLI
file watching and complete reconciliation of every value and integration are
separate requirements, not consequences of keeping a server process alive.

## Publishing and Deployment

Publishing should consume the same structured model without first running
local workloads or resolving local secrets.

A publisher works with typed model views, capabilities, expressions and
relationships. Integrations can contribute target-specific metadata or
artifacts outside the core. Run-only resources must be explicitly excluded.

The goal is to preserve the original model's run/publish distinction without
remoting a manifest writer operation by operation.

Native publishing and deployment are not implemented. The model must still
retain the information needed for them rather than optimize solely for local
execution.

## Current Scope

The exploration includes a BCL-only kernel, NativeAOT server, generated C# and
TypeScript clients, generic container/executable execution, provider lifetimes,
resource-preserving revisions, and CLI/Dashboard integration.

Current Redis integrations demonstrate typed fluent authoring, symbolic
credentials, authenticated readiness, references and retained data. This does
not establish full parity with existing hosting integrations.

The native executor consumes generic workload plans, not the managed resource
graph. Watched reconciliation, replicas, shared container networks and
prelaunch endpoint allocation remain gaps. Integration-specific preparation
belongs in providers rather than service-specific branches in the executor.

The remaining design work includes general resource schemas, portable
annotations, supported integration packaging, consumer-network resolution,
automatic guest reload and publishing. These should extend the shared model,
not introduce service-specific behavior into the core.

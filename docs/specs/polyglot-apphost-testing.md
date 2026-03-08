# Polyglot AppHost Testing

> **Status:** In Progress — TypeScript SDK spike implemented in `src/aspire-sdk-js/`

This document describes how to write integration tests for Aspire applications using the Aspire CLI as the orchestration layer. This approach is language-agnostic and works with any test framework.

## Table of Contents

1. [Overview](#overview)
2. [Motivation](#motivation)
3. [Architecture](#architecture)
4. [CLI Primitives](#cli-primitives)
5. [Resource Snapshot Schema](#resource-snapshot-schema)
6. [TypeScript SDK (`@aspire/sdk`)](#typescript-sdk-aspiresdk)
7. [Usage Examples](#usage-examples)
8. [Comparison with Aspire.Hosting.Testing](#comparison-with-aspirehostingtesting)
9. [Future Work](#future-work)

---

## Overview

The polyglot testing approach provides a way to write integration tests for Aspire applications without depending on .NET-specific testing infrastructure. Instead of using `Aspire.Hosting.Testing` (which requires invoking the AppHost entry point via reflection and hooking into DiagnosticListener events), tests use the Aspire CLI to:

1. Start the AppHost in detached mode
2. Query resource snapshots to get endpoints, connection strings, and health status
3. Wait for resources to reach desired states
4. Stop the AppHost when done

Each language (TypeScript, Python, Go, etc.) provides a thin wrapper library that spawns CLI commands and parses the structured JSON output.

---

## Motivation

### The Problem

`Aspire.Hosting.Testing` is deeply tied to .NET:

- Requires the AppHost to be a .NET assembly with an entry point
- Uses `DiagnosticListener` to intercept builder lifecycle events
- Invokes the entry point via reflection on a background thread
- Only works from .NET test projects

For polyglot AppHosts (TypeScript, Python, etc.) or when testing from non-.NET test frameworks, this approach doesn't work.

### The Solution

Use the Aspire CLI as a language-agnostic orchestration layer:

```text
┌─────────────────┐     spawn      ┌─────────────┐     runs     ┌─────────────┐
│   Test Runner   │ ────────────▶  │  Aspire CLI │ ──────────▶  │   AppHost   │
│  (Jest, pytest, │                │             │              │ (.NET, TS,  │
│   xUnit, etc.)  │ ◀────────────  │             │ ◀──────────  │  Python)    │
└─────────────────┘   JSON output  └─────────────┘  backchannel └─────────────┘
```

**Benefits:**
- Works with any test framework in any language
- Works with any AppHost (polyglot or .NET)
- CLI handles all the complexity (DCP, containers, service discovery)
- Structured JSON output is easy to parse
- Same primitives can be used for scripting, CI/CD, etc.

---

## Architecture

### Existing Infrastructure

The Aspire CLI already has the building blocks we need:

1. **`aspire run --detach`** - Starts the AppHost in the background and returns immediately
2. **Auxiliary Backchannel** - JSON-RPC connection between CLI and running AppHost
3. **`WatchResourceSnapshotsAsync`** - Streams resource state changes via the backchannel
4. **`aspire stop`** - Gracefully stops a running AppHost
5. **`aspire ps --format json`** - Lists running AppHosts with their PIDs and Dashboard URLs

### New CLI Commands

The following commands are available for programmatic automation:

```bash
# Start an AppHost in the background (returns JSON with PIDs and dashboard URL)
aspire start --format json [--apphost <path>]

# Stop a running AppHost
aspire stop [--apphost <path>]

# Describe resources (snapshot or streaming)
aspire describe [<resource>] --format json [--apphost <path>]
aspire describe --follow --format json [--apphost <path>]

# Wait for a resource to reach a status
aspire wait <resource> [--status healthy|up|down] [--timeout <seconds>]

# Console logs (snapshot or streaming)
aspire logs [<resource>] --format json [--apphost <path>]
aspire logs [<resource>] --follow --format json [--apphost <path>]

# OpenTelemetry data
aspire otel traces [<resource>] --format json [--apphost <path>]
aspire otel spans [<resource>] --format json [--follow] [--apphost <path>]
aspire otel logs [<resource>] --format json [--follow] [--apphost <path>]

# Resource commands
aspire resource <resource> <command>   # e.g. restart, stop, start

# Export telemetry and resource data
aspire export [<resource>] [-o <path>] [--apphost <path>]

# List running AppHosts
aspire ps --format json [--resources]
```

---

## CLI Primitives

### `aspire start`

Starts the AppHost in the background.

```bash
aspire start --format json --apphost ./MyApp.AppHost/MyApp.AppHost.csproj
```

**Output (JSON):**
```json
{
  "appHostPath": "/path/to/MyApp.AppHost/MyApp.AppHost.csproj",
  "appHostPid": 12345,
  "cliPid": 12340,
  "dashboardUrl": "http://localhost:15000/login?t=abc123",
  "logFile": "/path/to/logs/apphost.log"
}
```

The command:
1. Spawns the AppHost as a background process
2. Waits for the backchannel connection to be established
3. Returns connection info as JSON and exits

### `aspire stop`

Gracefully stops a running AppHost.

```bash
aspire stop [--project <path>]
```

If `--project` is not specified, stops the AppHost in the current directory (or prompts if multiple are found).

### `aspire describe`

Returns a snapshot of all resources.

```bash
aspire describe [--project <path>]
```

**Output (JSON):**
```json
{
  "resources": [
    {
      "name": "redis",
      "type": "Container",
      "state": "Running",
      "healthStatus": "Healthy",
      "endpoints": [
        {
          "name": "tcp",
          "url": "tcp://localhost:6379",
          "isInternal": false
        }
      ],
      "connectionString": "localhost:6379",
      "properties": {
        "container.image": "redis:7.4",
        "container.id": "abc123def456"
      }
    },
    {
      "name": "api",
      "type": "Project",
      "state": "Running",
      "healthStatus": "Healthy",
      "endpoints": [
        {
          "name": "http",
          "url": "http://localhost:5001",
          "isInternal": false
        },
        {
          "name": "https",
          "url": "https://localhost:5002",
          "isInternal": false
        }
      ],
      "connectionString": null,
      "properties": {
        "project.path": "/path/to/Api/Api.csproj"
      }
    }
  ]
}
```

### `aspire describe --follow`

Streams resource snapshots as NDJSON (newline-delimited JSON).

```bash
aspire describe --follow [--project <path>]
```

**Output (NDJSON):**
```json
{"name":"redis","type":"Container","state":"Starting","healthStatus":null,"endpoints":[],"connectionString":null}
{"name":"redis","type":"Container","state":"Running","healthStatus":"Healthy","endpoints":[{"name":"tcp","url":"tcp://localhost:6379"}],"connectionString":"localhost:6379"}
{"name":"api","type":"Project","state":"Starting","healthStatus":null,"endpoints":[]}
{"name":"api","type":"Project","state":"Running","healthStatus":"Healthy","endpoints":[{"name":"http","url":"http://localhost:5001"}]}
```

Each line is a complete resource snapshot. The stream continues until:
- The AppHost stops
- The command is interrupted (Ctrl+C)
- The `--timeout` is reached (if specified)

### `aspire logs`

Retrieves resource console logs from the `ResourceLoggerService`. These are the same logs displayed in the Aspire Dashboard's console logs view - stdout/stderr output captured from containers, projects, and executables.

> **Note:** These are console logs (stdout/stderr), not OpenTelemetry structured logs. For OTel logs, traces, and metrics, use the Dashboard's telemetry views or configure an external collector.

See [GitHub Issue #8069](https://github.com/dotnet/aspire/issues/8069) for the original feature request.

```bash
aspire logs [resource] [--project <path>] [--follow] [--format json]
```

**Arguments:**
- `resource` - Optional resource name. Required unless using `--follow`.

**Options:**
- `--follow`, `-f` - Stream logs in real-time (like `docker logs -f`)
- `--format json` - Output logs as JSON/NDJSON for programmatic consumption
- `--project <path>` - Path to the AppHost project

**Tip:** Pipe output to files if needed: `aspire logs redis --follow > redis.log`

#### JSON Output

Use `--format json` for structured log output that's easy to parse programmatically.

**Single snapshot (`aspire logs <resource> --format json`):**

Outputs NDJSON (one JSON object per line):
```json
{"resourceName":"redis","content":"1:C 15 Jan 2024 10:30:14.123 * oO0OoO0OoO0Oo Redis is starting","isError":false}
{"resourceName":"redis","content":"1:M 15 Jan 2024 10:30:14.456 * Ready to accept connections","isError":false}
```

**Streaming (`aspire logs --format json --follow`):**

Outputs NDJSON continuously as logs are written:
```json
{"resourceName":"redis","content":"Ready to accept connections","isError":false}
{"resourceName":"api","content":"info: Application started","isError":false}
{"resourceName":"api","content":"warn: Cache miss","isError":false}
```

**Log Format Fields:**
- `resourceName` - The name of the resource that produced the log
- `content` - The log line text (may include embedded timestamps from the source)
- `isError` - Whether this came from stderr

#### Get All Logs

Retrieve logs from all resources at once. Useful for collecting diagnostic information after a test run.

```bash
# Get logs from a specific resource
aspire logs redis

# Stream all logs to a file
aspire logs --follow > all-logs.txt
```

**Output (interleaved with resource prefix):**

```text
[redis] 1:C 15 Jan 2024 10:30:14.123 * oO0OoO0OoO0Oo Redis is starting
[redis] 1:M 15 Jan 2024 10:30:14.456 * Ready to accept connections
[api] info: Application started
[api] info: Listening on http://localhost:5001
[postgres] LOG: database system is ready to accept connections
```

#### Tail/Follow Logs

Stream logs in real-time, similar to `docker logs -f` or `kubectl logs -f`.

```bash
# Follow all logs
aspire logs --follow

# Follow logs for a specific resource
aspire logs api --follow
```

**Termination criteria:**
The stream continues until:
- The AppHost stops (the CLI will exit when the connection is lost)
- The command is interrupted (Ctrl+C)
- The resource enters a terminal state (Exited, Finished, FailedToStart)

> **Note:** Logs from the shutdown process may not be captured if the AppHost stops before the log stream flushes. For complete diagnostic logs in failure scenarios, consider using the AppHost's log file (shown in `aspire run --detach` output).

#### Logs for a Specific Resource

Get logs for a single resource by name.

```bash
# Get all logs for a resource
aspire logs api

# Get last 100 lines
aspire logs postgres --tail 100

# Stream logs for a resource
aspire logs redis --follow
```

**Output (single resource - no prefix needed):**

```text
[2024-01-15T10:30:15.123Z] info: Application started
[2024-01-15T10:30:15.456Z] info: Listening on http://localhost:5001
[2024-01-15T10:30:16.789Z] warn: Cache miss for key 'products'
```

#### CI/CD Integration

The logs command is designed to work well in CI/CD pipelines:

```bash
#!/bin/bash
set -e

# Start the AppHost
aspire run --detach --format json > apphost.json

# Run tests
npm test || TEST_FAILED=1

# Always collect logs (even if tests fail) - pipe to files
aspire logs redis --follow > ./test-artifacts/logs/redis.log 2>&1 &
aspire logs api --follow > ./test-artifacts/logs/api.log 2>&1 &
sleep 2  # Give logs time to collect
kill %1 %2 2>/dev/null || true

# Stop the AppHost
aspire stop

# Exit with test result
exit ${TEST_FAILED:-0}
```

---

## Resource Snapshot Schema

The resource snapshot schema is designed to provide all information needed for testing:

```typescript
interface ResourceSnapshot {
  /** Unique name of the resource */
  name: string;
  
  /** Resource type: "Project", "Container", "Executable", etc. */
  type: string;
  
  /** Current state: "Starting", "Running", "Stopping", "Exited", "FailedToStart", etc. */
  state: string | null;
  
  /** Health status: "Healthy", "Unhealthy", "Degraded", or null if not running/no health checks */
  healthStatus: "Healthy" | "Unhealthy" | "Degraded" | null;
  
  /** Endpoints exposed by the resource */
  endpoints: Endpoint[];
  
  /** Connection string if the resource exposes one */
  connectionString: string | null;
  
  /** Additional properties (container image, project path, etc.) */
  properties: Record<string, string>;
  
  /** Relationships to other resources */
  relationships: Relationship[];
}

interface Endpoint {
  /** Endpoint name (e.g., "http", "https", "tcp") */
  name: string;
  
  /** Full URL including scheme, host, and port */
  url: string;
  
  /** Whether this is an internal endpoint (not exposed externally) */
  isInternal: boolean;
}

interface Relationship {
  /** Name of the related resource */
  resourceName: string;
  
  /** Relationship type: "Parent", "Reference", etc. */
  type: string;
}
```

### State Values

The `state` field uses values from `KnownResourceStates`:

| State | Description |
|-------|-------------|
| `Starting` | Resource is starting up |
| `Running` | Resource is running and ready |
| `Stopping` | Resource is shutting down |
| `Exited` | Executable/project has stopped (check `exitCode` property) |
| `Finished` | Container has stopped (check `exitCode` property) |
| `FailedToStart` | Resource failed to start |
| `Waiting` | Resource is waiting for a dependency |
| `NotStarted` | Resource was not started (e.g., `.WithExplicitStart()`) |

> **Note:** Both `Exited` and `Finished` indicate the resource has stopped. `Exited` is used for executables/projects, while `Finished` is used for containers. Check the `exitCode` property to determine if it was a successful completion (0) or a failure (non-zero).

### Health Status Values

| Status | Description |
|--------|-------------|
| `Healthy` | All health checks passing |
| `Unhealthy` | One or more health checks failing |
| `Degraded` | Health checks indicate degraded state |
| `null` | Resource not running or no health checks configured |

---

## TypeScript SDK (`@aspire/sdk`)

The TypeScript SDK lives in `src/aspire-sdk-js/` and provides a programmatic API for automating the Aspire CLI. It wraps CLI commands with typed interfaces, async generators for streaming, and `AbortSignal` for cancellation.

**Package:** `@aspire/sdk` · **Zero runtime dependencies** · ESM only

### Installation

```bash
npm install @aspire/sdk
```

### API Overview

| Method | CLI Command | Description |
|--------|-------------|-------------|
| `AspireHost.start(opts)` | `aspire start --format json` | Start AppHost, return handle |
| `host.stop()` | `aspire stop` | Stop the AppHost |
| `host.getResources()` | `aspire describe --format json` | Get all resource snapshots |
| `host.getResource(name)` | `aspire describe <name> --format json` | Get a single resource |
| `host.getEndpoint(resource, name?)` | ↳ extracts from resource | Get endpoint URL |
| `host.waitForResource(name, opts)` | `aspire wait` | Wait for health/state |
| `host.watchResources({ signal })` | `aspire describe --follow` | Stream resource changes |
| `host.getLogs(resource?, opts)` | `aspire logs --format json` | Get log snapshot |
| `host.streamLogs(resource?, { signal })` | `aspire logs --follow` | Stream logs |
| `host.getTraces(opts)` | `aspire otel traces --format json` | Get OTEL traces |
| `host.getSpans(opts)` | `aspire otel spans --format json` | Get OTEL spans |
| `host.streamSpans({ signal })` | `aspire otel spans --follow` | Stream OTEL spans |
| `host.getStructuredLogs(opts)` | `aspire otel logs --format json` | Get OTEL structured logs |
| `host.streamStructuredLogs({ signal })` | `aspire otel logs --follow` | Stream OTEL logs |
| `host.executeCommand(resource, cmd)` | `aspire resource <name> <cmd>` | Restart/stop/start resource |
| `host.export(opts)` | `aspire export` | Export telemetry to zip |
| `AspireHost.list(opts)` | `aspire ps --format json` | List running AppHosts |

### Quick Start

```typescript
import { AspireHost } from '@aspire/sdk';

const host = await AspireHost.start({ appHost: './apphost.ts' });

try {
  // Wait for resources to be healthy
  await host.waitForResource('api', { status: 'healthy' });
  await host.waitForResource('redis', { status: 'healthy' });

  // Get endpoints
  const endpoint = await host.getEndpoint('api', 'http');
  console.log(endpoint.url); // "http://localhost:5001"

  // Make requests
  const response = await fetch(`${endpoint.url}/api/products`);
  console.log(response.status);

  // Query OTEL traces
  const traces = await host.getTraces({ resource: 'api' });
  console.log(`${traces.returnedCount} traces`);

} finally {
  await host.stop();
}
```

### Streaming with AbortSignal

All streaming methods use standard `AbortSignal` for cancellation:

```typescript
// Watch resources until one fails
const ac = new AbortController();
for await (const snapshot of host.watchResources({ signal: ac.signal })) {
  console.log(`${snapshot.displayName}: ${snapshot.state}`);
  if (snapshot.state === 'FailedToStart') ac.abort();
}

// Stream logs with a timeout
for await (const entry of host.streamLogs('api', { signal: AbortSignal.timeout(30_000) })) {
  console.log(`[${entry.resourceName}] ${entry.content}`);
}

// Stream OTEL spans in real-time
for await (const data of host.streamSpans({ signal: ac.signal })) {
  for (const rs of data.data.resourceSpans ?? []) {
    for (const ss of rs.scopeSpans ?? []) {
      for (const span of ss.spans ?? []) {
        console.log(`${span.name} (${span.spanId})`);
      }
    }
  }
}
```

### Log Collection

```typescript
// Get all logs as structured data
const logs = await host.getLogs();

// Get logs for a specific resource
const apiLogs = await host.getLogs('api');

// Get last N lines
const recentLogs = await host.getLogs('api', { tail: 100 });

// Stream logs in real-time
for await (const entry of host.streamLogs('api', { signal })) {
  console.log(`[${entry.resourceName}] ${entry.content}`);
}
```

### Implementation

The SDK is structured as three modules:

- **`cli.ts`** — Low-level helpers (`aspireExec`, `aspireJson`) that shell out to the `aspire` CLI with `--non-interactive --nologo --format Json` flags
- **`types.ts`** — TypeScript interfaces matching the CLI's JSON output schemas (resource snapshots, OTLP types, start output, etc.)
- **`aspire-host.ts`** — The `AspireHost` class with all public methods. Streaming uses a private `streamCommand<T>()` async generator that spawns NDJSON-producing CLI commands and yields parsed objects with `AbortSignal` cleanup.

### Python

```python
from aspire.testing import AspireHost

async with AspireHost.start(app_host='./MyApp.AppHost') as host:
    await host.wait_for_resource('redis', status='healthy')
    await host.wait_for_resource('api', status='healthy')

    api = await host.get_resource('api')
    endpoint = api.urls[0]

    async with httpx.AsyncClient() as client:
        response = await client.get(f'{endpoint.url}/api/products')
        assert response.status_code == 200
```

### .NET

For .NET tests that want CLI-based testing (instead of `Aspire.Hosting.Testing`):

```csharp
await using var host = await AspireHost.StartAsync(new StartOptions
{
    AppHost = "./MyApp.AppHost/MyApp.AppHost.csproj"
});

await host.WaitForResourceAsync("redis", status: "healthy");
await host.WaitForResourceAsync("api", status: "healthy");

var api = await host.GetResourceAsync("api");
var httpEndpoint = api.Urls.First(e => e.Name == "http");

using var httpClient = new HttpClient { BaseAddress = new Uri(httpEndpoint.Url) };
var response = await httpClient.GetAsync("/api/products");
response.EnsureSuccessStatusCode();
```

---

## Usage Examples

### Jest Test

```typescript
import { AspireHost } from '@aspire/sdk';
import { describe, it, expect, beforeAll, afterAll } from '@jest/globals';

describe('Product API', () => {
  let host: AspireHost;
  let apiUrl: string;

  beforeAll(async () => {
    host = await AspireHost.start({ appHost: './MyApp.AppHost' });
    await host.waitForResource('api', { status: 'healthy' });
    const endpoint = await host.getEndpoint('api', 'http');
    apiUrl = endpoint.url;
  }, 120000);

  afterAll(async () => {
    await host?.stop();
  });

  it('should list products', async () => {
    const response = await fetch(`${apiUrl}/api/products`);
    expect(response.ok).toBe(true);
    
    const products = await response.json();
    expect(products).toBeInstanceOf(Array);
  });

  it('should create a product', async () => {
    const response = await fetch(`${apiUrl}/api/products`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name: 'Test Product', price: 9.99 })
    });
    expect(response.status).toBe(201);
  });

  it('should have OTEL traces', async () => {
    const traces = await host.getTraces({ resource: 'api' });
    expect(traces.returnedCount).toBeGreaterThan(0);
  });
});
```

### pytest Test

```python
import pytest
from aspire.sdk import AspireHost

@pytest.fixture(scope='module')
async def host():
    host = await AspireHost.start(app_host='./MyApp.AppHost')
    await host.wait_for_resource('api', status='healthy')
    yield host
    await host.stop()

@pytest.fixture
async def api_url(host):
    resource = await host.get_resource('api')
    return resource.urls[0].url

@pytest.mark.asyncio
async def test_list_products(api_url):
    async with httpx.AsyncClient() as client:
        response = await client.get(f'{api_url}/api/products')
        assert response.status_code == 200
        assert isinstance(response.json(), list)
```

---

## Comparison with Aspire.Hosting.Testing

| Feature | Aspire.Hosting.Testing | CLI-Based Testing |
|---------|------------------------|-------------------|
| **Language Support** | .NET only | Any language |
| **AppHost Types** | .NET only | .NET, TypeScript, Python, etc. |
| **Builder Access** | Full access to `IDistributedApplicationBuilder` | No builder access |
| **Resource Mutation** | Can modify resources before run | Cannot modify resources |
| **Test Framework** | xUnit, NUnit, MSTest | Any (Jest, pytest, etc.) |
| **Startup Time** | Fast (in-process) | Slower (process spawn) |
| **Debugging** | Easy (same process) | Requires attach to child process |
| **Isolation** | Shares process with test | Full process isolation |

### When to Use Which

**Use `Aspire.Hosting.Testing` when:**
- Testing .NET AppHosts from .NET tests
- Need to modify the AppHost configuration in tests
- Want faster test execution
- Need to access internal services for mocking or override DI
- Testing hosting integrations themselves

**Use CLI-based testing when:**
- Testing polyglot AppHosts (TypeScript, Python, etc.)
- Using non-.NET test frameworks
- Want full process isolation
- Testing the AppHost as a "black box"
- Writing cross-platform CI/CD scripts

---

## Future Work

### Phase 1: Core CLI ✅
- [x] `aspire start --format json` — start AppHost in background
- [x] `aspire stop` — stop running AppHost
- [x] `aspire describe [--follow] --format json` — resource snapshots and streaming
- [x] `aspire wait` — wait for resource health/state
- [x] `aspire logs [--follow] --format json` — console log access
- [x] `aspire resource <name> <cmd>` — resource commands
- [x] `aspire otel traces/spans/logs --format json` — OTEL telemetry queries
- [x] `aspire export` — export telemetry to zip
- [x] `aspire ps --format json` — list running AppHosts

### Phase 2: TypeScript SDK ✅ (Spike)
- [x] Create `@aspire/sdk` package (`src/aspire-sdk-js/`)
- [x] `AspireHost.start()`, `stop()`, `getResources()`, `getResource()`
- [x] `waitForResource()` via `aspire wait`
- [x] `watchResources()`, `streamLogs()` with `AbortSignal` cancellation
- [x] OTEL APIs: `getTraces()`, `getSpans()`, `getStructuredLogs()`, streaming
- [x] `export()`, `AspireHost.list()`
- [x] Integration tests (11 passing)

### Phase 3: Polish & Publish
- [ ] Error handling and typed error classes
- [ ] Connection string access on resource snapshots
- [ ] Publish to npm as `@aspire/sdk`
- [ ] README and API documentation

### Phase 4: Additional Languages
- [ ] Python SDK (`aspire-sdk` pip package)
- [ ] Go SDK
- [ ] .NET SDK (for non-`TEntryPoint` scenarios)

### Phase 5: Enhanced Features
- [ ] `Disposable` / `AsyncDisposable` support for automatic cleanup
- [ ] Test framework integrations (Jest fixtures, pytest fixtures, xUnit fixtures)
- [ ] CI/CD helpers (log collection on failure, artifact export)

---

## References

- [`src/aspire-sdk-js/`](../../../src/aspire-sdk-js/) - TypeScript SDK implementation
- [Aspire.Hosting.Testing](../../../src/Aspire.Hosting.Testing/) - Existing .NET testing infrastructure
- [Polyglot AppHost](./polyglot-apphost.md) - Polyglot AppHost architecture
- [CLI Backchannel](../../../src/Aspire.Cli/Backchannel/) - CLI-to-AppHost communication

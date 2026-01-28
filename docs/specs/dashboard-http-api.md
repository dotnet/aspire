# Dashboard Telemetry HTTP API

This document describes the HTTP API for exposing telemetry data (traces and structured logs) from the Aspire Dashboard, enabling CLI commands and other programmatic access.

## Overview

The Dashboard already exposes telemetry data via MCP (Model Context Protocol) for AI assistants. This spec defines a simpler HTTP API that provides the same data in pure JSON format, suitable for CLI tools and automation.

**Related:**

- Issue: <https://github.com/dotnet/aspire/issues/14138>
- MCP implementation: `src/Aspire.Dashboard/Mcp/AspireTelemetryMcpTools.cs`

## Design Philosophy

1. **Formal DTO Classes**: The HTTP API uses explicit DTO classes (not anonymous objects) for clear contracts and versioning.
2. **RESTful Design**: Standard HTTP verbs and resource-oriented URLs.
3. **Same Auth as MCP**: Uses `McpApiKeyAuthenticationHandler` with `X-API-Key` header.
4. **Pure JSON Responses**: Clean JSON responses with `camelCase` naming, no markdown wrappers.
5. **Dashboard Owns the Contract**: DTOs defined in `src/Aspire.Dashboard/Model/Api/`, source-linked to CLI.

### Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Response format | Formal DTO classes | Explicit contract, easier to version, better for documentation |
| JSON naming | `camelCase` | .NET convention, matches other Aspire APIs |
| DTO location | `src/Aspire.Dashboard/Model/Api/` | Dashboard owns the HTTP API contract |
| CLI consumption | Source-link DTOs from Dashboard | Single source of truth, no duplication |
| MCP alignment | Separate for now, align later | Less risk, cleaner separation initially |

---

## Part 1: HTTP API

### Endpoints

#### Traces

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/telemetry/traces` | List distributed traces |
| GET | `/api/telemetry/traces/{traceId}` | Get a specific trace with all spans |
| GET | `/api/telemetry/traces/{traceId}/logs` | Get structured logs for a trace |

#### Structured Logs

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/telemetry/logs` | List structured logs |
| GET | `/api/telemetry/logs/{logId}` | Get a specific log entry |

### Authentication

All endpoints require the `X-API-Key` header:

```http
GET /api/telemetry/traces HTTP/1.1
Host: localhost:18888
X-API-Key: <mcp-api-token>
```

### Error Responses

All error responses use [RFC 7807 Problem Details](https://tools.ietf.org/html/rfc7807) format with `application/problem+json` content type.

#### 401 Unauthorized

Missing or invalid API key:

```json
{
  "type": "https://tools.ietf.org/html/rfc7807#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Missing or invalid X-API-Key header"
}
```

#### 404 Not Found

Resource not found:

```json
{
  "type": "https://tools.ietf.org/html/rfc7807#section-3.1",
  "title": "Not Found",
  "status": 404,
  "detail": "Trace with ID '4bf92f3577b34da6a3ce929d0e0e4736' was not found",
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

```json
{
  "type": "https://tools.ietf.org/html/rfc7807#section-3.1",
  "title": "Not Found",
  "status": 404,
  "detail": "Log entry with ID '12345' was not found",
  "logId": 12345
}
```

#### 400 Bad Request

Invalid query parameters:

```json
{
  "type": "https://tools.ietf.org/html/rfc7807#section-3.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Invalid severity value 'Foo'. Valid values: Trace, Debug, Information, Warning, Error, Critical"
}
```

### `GET /api/telemetry/traces`

List distributed traces with optional filtering.

**Query Parameters:**

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `resource` | string | No | - | Filter to traces involving this resource |
| `hasError` | bool | No | - | Filter to traces with errors |
| `limit` | int | No | 200 | Maximum traces to return |

**Response:** `200 OK`

```json
{
  "traces": [ <TraceDto>, ... ],
  "totalCount": 1500,
  "returnedCount": 200
}
```

### `GET /api/telemetry/traces/{traceId}`

Get a single trace by ID.

**Response:** `200 OK` — Single `TraceDto` object

**Response:** `404 Not Found`

```json
{
  "error": "Trace not found",
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

### `GET /api/telemetry/traces/{traceId}/logs`

Get structured logs associated with a specific trace.

**Response:** `200 OK`

```json
{
  "logs": [ <LogEntryDto>, ... ],
  "totalCount": 50,
  "returnedCount": 50
}
```

### `GET /api/telemetry/logs`

List structured logs with optional filtering.

**Query Parameters:**

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `resource` | string | No | - | Filter to logs from this resource |
| `traceId` | string | No | - | Filter to logs from this trace |
| `severity` | string | No | - | Filter by severity level |
| `limit` | int | No | 200 | Maximum logs to return |

**Severity Values:** `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`

**Response:** `200 OK`

```json
{
  "logs": [ <LogEntryDto>, ... ],
  "totalCount": 5000,
  "returnedCount": 200
}
```

### `GET /api/telemetry/logs/{logId}`

Get a single structured log by ID.

**Response:** `200 OK` — Single `LogEntryDto` object

**Response:** `404 Not Found`

```json
{
  "error": "Log entry not found",
  "logId": 12345
}
```

---

## Part 2: Models

The HTTP API uses formal DTO classes defined in `src/Aspire.Dashboard/Model/Api/`. These are source-linked to the CLI for convenience.

**JSON naming convention**: `camelCase` (matching .NET conventions)

### TraceDto

Represents a distributed trace with all its spans.

```json
{
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "durationMs": 142,
  "title": "GET /api/products",
  "hasError": false,
  "timestamp": "2026-01-28T06:30:00.000Z",
  "spans": [ <SpanDto>, ... ],
  "dashboardLink": {
    "url": "https://localhost:18888/traces/4bf92f3577b34da6a3ce929d0e0e4736",
    "text": "4bf92f..."
  }
}
```

| Field | Type | Description |
|-------|------|-------------|
| `traceId` | string | The trace ID (hex string) |
| `durationMs` | number | Total trace duration in milliseconds |
| `title` | string | Human-readable title (usually root span name) |
| `hasError` | bool | Whether any span in the trace has an error |
| `timestamp` | string | ISO 8601 timestamp of trace start |
| `spans` | SpanDto[] | All spans in the trace |
| `dashboardLink` | LinkDto | Link to view in Dashboard UI |

### SpanDto

Represents a single span within a trace.

```json
{
  "spanId": "00f067aa0ba902b7",
  "parentSpanId": null,
  "kind": "Server",
  "name": "GET /api/products",
  "status": "Ok",
  "source": "apiservice",
  "destination": null,
  "durationMs": 142,
  "attributes": {
    "http.method": "GET",
    "http.status_code": "200"
  }
}
```

| Field | Type | Description |
|-------|------|-------------|
| `spanId` | string | The span ID (hex string) |
| `parentSpanId` | string? | Parent span ID, null for root span |
| `kind` | string | Span kind: Server, Client, Producer, Consumer, Internal |
| `name` | string | Span name/operation |
| `status` | string | Span status: Ok, Error, Unset |
| `source` | string | Resource name that created this span |
| `destination` | string? | Target resource (for client spans) |
| `durationMs` | number | Span duration in milliseconds |
| `attributes` | object | Key-value attributes on the span |

### LogEntryDto

Represents a structured log entry.

```json
{
  "logId": 12345,
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "spanId": "00f067aa0ba902b7",
  "message": "Processing request for user 42",
  "severity": "Information",
  "resourceName": "apiservice",
  "timestamp": "2026-01-28T06:30:00.123Z",
  "attributes": {
    "userId": "42"
  },
  "exception": null,
  "source": "MyApp.Controllers.ProductController",
  "dashboardLink": {
    "url": "https://localhost:18888/structuredlogs?logEntryId=12345",
    "text": "logId: 12345"
  }
}
```

| Field | Type | Description |
|-------|------|-------------|
| `logId` | number | Unique log entry ID |
| `traceId` | string? | Associated trace ID |
| `spanId` | string? | Associated span ID |
| `message` | string | Log message (template rendered) |
| `severity` | string | Log level |
| `resourceName` | string | Resource that emitted this log |
| `timestamp` | string | ISO 8601 timestamp |
| `attributes` | object | Structured log properties |
| `exception` | string? | Exception details if present |
| `source` | string | Logger category/source |
| `dashboardLink` | LinkDto | Link to view in Dashboard UI |

### LinkDto

A link to the Dashboard UI.

```json
{
  "url": "https://localhost:18888/traces/abc123",
  "text": "abc123..."
}
```

### ListResponse Wrapper

All list endpoints return a wrapper with counts:

```json
{
  "traces": [ ... ],       // or "logs": [ ... ]
  "totalCount": 1500,      // total matching items in repository
  "returnedCount": 200     // items returned (limited)
}
```

---

## Part 3: CLI Commands

### `aspire telemetry`

New subcommand group for telemetry operations.

```text
Description:
  View and query telemetry data from a running Aspire application.

Usage:
  aspire telemetry [command] [options]

Commands:
  traces  List and view distributed traces.
  logs    List and view structured logs.
```

### `aspire telemetry traces`

```text
Description:
  List and view distributed traces.

Usage:
  aspire telemetry traces [<resource>] [options]

Arguments:
  <resource>  The name of the resource to filter traces. If not specified,
              traces from all resources are shown.

Options:
  --id <traceId>         Get a specific trace by ID.
  --has-error            Show only traces with errors.
  --format <Json|Table>  Output format (default: Table).
  --limit <n>            Max traces to return (default: 200).
  --project <project>    The path to the Aspire AppHost project file.
```

**Examples:**

```bash
# List all recent traces
aspire telemetry traces

# Filter to specific resource
aspire telemetry traces apiservice

# Get specific trace by ID
aspire telemetry traces --id 4bf92f3577b34da6a3ce929d0e0e4736

# Show only errors, JSON output
aspire telemetry traces --has-error --format json
```

### `aspire telemetry logs`

```text
Description:
  List and view structured logs.

Usage:
  aspire telemetry logs [<resource>] [options]

Arguments:
  <resource>  The name of the resource to filter logs. If not specified,
              logs from all resources are shown.

Options:
  --trace-id <traceId>   Filter logs to a specific trace.
  --severity <level>     Filter by severity (Error, Warning, Information,
                         Debug, Trace).
  --format <Json|Table>  Output format (default: Table).
  --limit <n>            Max logs to return (default: 200).
  --project <project>    The path to the Aspire AppHost project file.
```

**Examples:**

```bash
# List all recent structured logs
aspire telemetry logs

# Filter to specific resource
aspire telemetry logs apiservice

# Filter by severity
aspire telemetry logs --severity Error

# Logs for a specific trace
aspire telemetry logs --trace-id 4bf92f3577b34da6a3ce929d0e0e4736

# Combine filters
aspire telemetry logs apiservice --severity Warning --limit 100
```

### CLI → Dashboard Communication

```text
┌─────────┐                      ┌─────────┐                    ┌───────────┐
│   CLI   │─── Backchannel ─────▶│ AppHost │                    │ Dashboard │
│         │  GetDashboardInfo()  │         │                    │           │
│         │◀─── URL + Token ─────│         │                    │           │
│         │                      └─────────┘                    │           │
│         │                                                     │           │
│         │─────────────── HTTP GET /api/telemetry/traces ─────▶│           │
│         │                      X-API-Key: <token>             │           │
│         │◀──────────────────── JSON Response ─────────────────│           │
└─────────┘                                                     └───────────┘
```

**Flow:**

1. CLI connects to AppHost via backchannel
2. CLI calls `GetDashboardInfoV2Async()` to get Dashboard URL and API token
3. CLI makes HTTP requests directly to Dashboard with `X-API-Key` header
4. Dashboard returns JSON responses
5. CLI formats and displays (table or JSON based on `--format`)

---

## Part 4: Implementation

### Dashboard Implementation

Add endpoints in `DashboardEndpointsBuilder.cs`:

```csharp
app.MapGet("/api/telemetry/traces", async (
    string? resource,
    bool? hasError,
    int? limit,
    TelemetryRepository telemetryRepository,
    IEnumerable<IOutgoingPeerResolver> outgoingPeerResolvers,
    IOptionsMonitor<DashboardOptions> dashboardOptions) =>
{
    // Reuse AIHelpers from MCP
    var traces = telemetryRepository.GetTraces(...);
    var traceDtos = traces.Select(t => AIHelpers.GetTraceDto(...));
    return Results.Ok(new { traces = traceDtos, totalCount = ..., returnedCount = ... });
})
.RequireAuthorization(McpApiKeyAuthenticationHandler.PolicyName);
```

### CLI Implementation

#### Reusing Existing Infrastructure

The CLI already has shared infrastructure for connecting to AppHosts:

1. **`AppHostConnectionResolver`** (`src/Aspire.Cli/Backchannel/AppHostConnectionResolver.cs`)
   - Finds running AppHosts via socket scanning
   - Handles `--project` option for fast path
   - Prompts user when multiple AppHosts exist
   - Returns `AppHostAuxiliaryBackchannel` connection

2. **`AppHostAuxiliaryBackchannel.McpInfo`** property
   - `EndpointUrl` — Dashboard base URL for HTTP calls
   - `ApiToken` — API key for X-API-Key header

The telemetry commands will follow the same pattern as `LogsCommand` and `ResourcesCommand`.

#### DashboardHttpClient Service

Create `src/Aspire.Cli/Dashboard/DashboardHttpClient.cs`:

```csharp
internal interface IDashboardHttpClient
{
    // Traces
    Task<TracesResponse> GetTracesAsync(
        string? resource = null,
        bool? hasError = null,
        int? limit = null,
        CancellationToken cancellationToken = default);
    
    Task<TraceDto?> GetTraceByIdAsync(
        string traceId,
        CancellationToken cancellationToken = default);
    
    Task<LogsResponse> GetTraceLogsAsync(
        string traceId,
        CancellationToken cancellationToken = default);
    
    // Logs
    Task<LogsResponse> GetLogsAsync(
        string? resource = null,
        string? traceId = null,
        string? severity = null,
        int? limit = null,
        CancellationToken cancellationToken = default);
    
    Task<LogEntryDto?> GetLogByIdAsync(
        long logId,
        CancellationToken cancellationToken = default);
}

internal sealed class DashboardHttpClient(
    HttpClient httpClient,
    ILogger<DashboardHttpClient> logger) : IDashboardHttpClient
{
    // Connection is passed per-call from command (via AppHostConnectionResolver)
    public async Task<TracesResponse> GetTracesAsync(
        AppHostAuxiliaryBackchannel connection,
        string? resource = null,
        bool? hasError = null,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        if (connection.McpInfo is null)
        {
            throw new InvalidOperationException("Dashboard not available for this AppHost");
        }
        
        var queryParams = new List<string>();
        if (resource is not null) queryParams.Add($"resource={Uri.EscapeDataString(resource)}");
        if (hasError is not null) queryParams.Add($"hasError={hasError.Value.ToString().ToLowerInvariant()}");
        if (limit is not null) queryParams.Add($"limit={limit.Value}");
        
        var url = $"{connection.McpInfo.EndpointUrl}/api/telemetry/traces";
        if (queryParams.Count > 0) url += "?" + string.Join("&", queryParams);
        
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-API-Key", connection.McpInfo.ApiToken);
        
        var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<TracesResponse>(
            TelemetryJsonContext.Default.TracesResponse, 
            cancellationToken) ?? throw new InvalidOperationException("Invalid response");
    }
    
    // ... other methods follow same pattern
}
```

#### Command Structure

| Command | HTTP Call | Display |
|---------|-----------|---------|
| `aspire telemetry traces` | `GET /api/telemetry/traces` | Table or JSON |
| `aspire telemetry traces --id X` | `GET /api/telemetry/traces/{X}` | Single trace detail |
| `aspire telemetry logs` | `GET /api/telemetry/logs` | Table or JSON |
| `aspire telemetry logs --trace-id X` | `GET /api/telemetry/logs?traceId=X` | Filtered logs |

#### TelemetryTracesCommand

```csharp
internal sealed class TelemetryTracesCommand : BaseCommand
{
    private static readonly Argument<string?> s_resourceArgument = new("resource")
    {
        Description = "Filter traces to this resource",
        Arity = ArgumentArity.ZeroOrOne
    };
    
    private static readonly Option<string?> s_idOption = new("--id")
    {
        Description = "Get a specific trace by ID"
    };
    
    private static readonly Option<bool> s_hasErrorOption = new("--has-error")
    {
        Description = "Show only traces with errors"
    };
    
    private static readonly Option<OutputFormat> s_formatOption = new("--format")
    {
        Description = "Output format (Table or Json)"
    };
    
    private static readonly Option<int?> s_limitOption = new("--limit")
    {
        Description = "Maximum traces to return (default: 200)"
    };
    
    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var resource = parseResult.GetValue(s_resourceArgument);
        var traceId = parseResult.GetValue(s_idOption);
        var hasError = parseResult.GetValue(s_hasErrorOption);
        var format = parseResult.GetValue(s_formatOption);
        var limit = parseResult.GetValue(s_limitOption);
        
        // If --id specified, get single trace
        if (traceId is not null)
        {
            var trace = await _dashboardClient.GetTraceByIdAsync(traceId, cancellationToken);
            if (trace is null)
            {
                _interactionService.DisplayError($"Trace '{traceId}' not found");
                return ExitCodeConstants.NotFound;
            }
            OutputTrace(trace, format);
            return ExitCodeConstants.Success;
        }
        
        // Otherwise list traces
        var response = await _dashboardClient.GetTracesAsync(
            resource, 
            hasError ? true : null, 
            limit, 
            cancellationToken);
        
        OutputTraces(response, format);
        return ExitCodeConstants.Success;
    }
    
    private void OutputTraces(TracesResponse response, OutputFormat format)
    {
        if (format == OutputFormat.Json)
        {
            var json = JsonSerializer.Serialize(response, TelemetryJsonContext.Default.TracesResponse);
            _interactionService.DisplayRawText(json);
            return;
        }
        
        // Table output
        var table = new Table();
        table.AddColumn("Trace ID");
        table.AddColumn("Duration");
        table.AddColumn("Title");
        table.AddColumn("Error");
        table.AddColumn("Timestamp");
        
        foreach (var trace in response.Traces)
        {
            table.AddRow(
                trace.TraceId[..12] + "...",
                $"{trace.DurationMs}ms",
                trace.Title.EscapeMarkup(),
                trace.HasError ? "[red]✗[/]" : "[green]✓[/]",
                trace.Timestamp.ToString("HH:mm:ss"));
        }
        
        AnsiConsole.Write(table);
        
        if (response.TotalCount > response.ReturnedCount)
        {
            _interactionService.DisplayMessage("ℹ️", 
                $"Showing {response.ReturnedCount} of {response.TotalCount} traces. Use --limit to see more.");
        }
    }
}
```

Create commands in `src/Aspire.Cli/Commands/`:

- `TelemetryCommand.cs` — parent command group
- `TelemetryTracesCommand.cs` — traces subcommand  
- `TelemetryLogsCommand.cs` — logs subcommand (similar pattern)

---

## Part 5: Testing

### Unit Tests

Location: `tests/Aspire.Dashboard.Tests/Api/TelemetryApiTests.cs`

Test endpoint logic with mocked `TelemetryRepository`:

- `GetTraces_NoFilters_ReturnsAllTraces`
- `GetTraces_FilterByResource_ReturnsFilteredTraces`
- `GetTraces_FilterByError_ReturnsOnlyErrorTraces`
- `GetTraceById_Exists_ReturnsTrace`
- `GetTraceById_NotFound_ReturnsNull`
- `GetLogs_FilterBySeverity_ReturnsFilteredLogs`
- `GetLogs_FilterByTraceId_ReturnsLogsForTrace`
- `GetLogById_Exists_ReturnsLog`
- `GetLogById_NotFound_ReturnsNull`

### Integration Tests

Location: `tests/Aspire.Dashboard.Tests/Integration/TelemetryApiIntegrationTests.cs`

Spin up `DashboardWebApplication` and make HTTP requests:

- `GetTraces_WithApiKey_Returns200`
- `GetTraces_WithoutApiKey_Returns401`
- `GetTraces_WithInvalidApiKey_Returns401`
- `GetTraceById_Exists_ReturnsTrace`
- `GetTraceById_NotExists_Returns404`
- `GetLogs_WithFilters_ReturnsFilteredLogs`
- `GetLogById_NotExists_Returns404`

---

## Comparison with MCP

| Aspect | MCP | HTTP API |
|--------|-----|----------|
| Protocol | JSON-RPC over SSE | REST over HTTP |
| Auth | X-API-Key header | Same |
| Response format | Text with embedded JSON | Pure JSON |
| Streaming | Yes (SSE) | No (snapshot only) |
| Client | MCP SDK required | Any HTTP client |
| Use case | AI assistants | CLI, automation, scripts |

---

## Future Considerations

### Streaming

For real-time log tailing:

```text
GET /api/telemetry/logs/stream
Accept: text/event-stream
```

### Metrics

When metrics support is added:

```text
GET /api/telemetry/metrics
GET /api/telemetry/metrics/{metricName}
```

### Pagination

For large datasets, add cursor-based pagination:

```text
GET /api/telemetry/logs?limit=100&after=12345
```

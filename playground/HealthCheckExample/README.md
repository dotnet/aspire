# Health Check Example

This example demonstrates surfacing multiple health checks through a single endpoint in Aspire.

## The Problem

Previously, to show each health check in the Dashboard:
1. Create separate `/health/*` endpoints for each health check
2. Add multiple `.WithHttpHealthCheck()` calls in the AppHost

This created maintenance overhead and tight coupling.

## The Solution

Now you can:
1. **Service**: Register all health checks + map ONE `/health` endpoint with `AspireHealthCheckResponseWriter`
2. **AppHost**: Use ONE `.WithHttpHealthCheck("/health")` call
3. **Result**: All health checks displayed individually in the Dashboard

The key: **ASP.NET Core already aggregates health checks**. We just format the response as JSON and Aspire auto-detects it.

## How It Works

### Service (Backend/Program.cs)

```csharp
builder.Services.AddHealthChecks()
    .AddCheck("database", () => HealthCheckResult.Healthy("Database is healthy"))
    .AddCheck("blob_storage", () => HealthCheckResult.Healthy("Storage is accessible"))
    .AddCheck("cache", () => HealthCheckResult.Degraded("Cache is slow"));

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = AspireHealthCheckResponseWriter.WriteResponse  // Formats as JSON
});
```

### AppHost (AppHost/Program.cs)

```csharp
var backend = builder.AddProject<Projects.Backend>("backend")
    .WithHttpHealthCheck("/health");  // Auto-detects JSON format
```

### Dashboard

Shows three separate health checks:
- ✅ database - "Database is healthy"
- ✅ blob_storage - "Storage is accessible"
- ⚠️ cache - "Cache is slow"

## Running

```bash
cd playground/HealthCheckExample/HealthCheckExample.AppHost
dotnet run
```

Open the Dashboard and check the "backend" resource's health checks!

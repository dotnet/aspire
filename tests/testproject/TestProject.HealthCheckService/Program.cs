// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Register multiple health checks with different statuses
builder.Services.AddHealthChecks()
    .AddCheck("database", () => HealthCheckResult.Healthy("Database is responsive"))
    .AddCheck("cache", () => HealthCheckResult.Healthy("Cache is connected"))
    .AddCheck("storage", () => HealthCheckResult.Degraded("Storage is slow", data: new Dictionary<string, object>
    {
        ["latency"] = "250ms",
        ["threshold"] = "100ms"
    }))
    .AddCheck("external_api", () => HealthCheckResult.Unhealthy("External API is unavailable", new TimeoutException("Connection timeout")));

var app = builder.Build();

app.MapGet("/", () => "Health Check Service");

// Map health check endpoint with Aspire response writer
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = AspireHealthCheckResponseWriter.WriteResponse
});

// Map a simple health endpoint for comparison
app.MapHealthChecks("/health-simple");

app.Run();

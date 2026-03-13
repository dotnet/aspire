using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Aspire.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Register multiple individual health checks
builder.Services.AddHealthChecks()
    .AddCheck("database", () =>
    {
        // Simulate a database health check
        return HealthCheckResult.Healthy("Database connection is healthy");
    })
    .AddCheck("blob_storage", () =>
    {
        // Simulate a blob storage health check
        return HealthCheckResult.Healthy("Blob storage is accessible");
    })
    .AddCheck("cache", () =>
    {
        // Simulate a cache health check
        return HealthCheckResult.Degraded("Cache is slow but functional");
    })
    .AddCheck("message_queue", () =>
    {
        // Simulate a message queue health check
        return HealthCheckResult.Unhealthy("Message queue is unavailable");
    });

var app = builder.Build();

// Map a single health check endpoint that returns all health checks in the Aspire JSON format
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = AspireHealthCheckResponseWriter.WriteResponse
});

app.MapGet("/", () => "Backend service with multiple health checks!");

app.Run();

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

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
        return HealthCheckResult.Healthy("Message queue is connected");
    });

var app = builder.Build();

// Map a single health check endpoint that returns all health checks in the Aspire JSON format
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = AspireHealthCheckResponseWriter.WriteResponse
});

app.MapGet("/", () => "Backend service with multiple health checks!");

app.Run();

// Standalone AspireHealthCheckResponseWriter
// (Copied from Aspire.Hosting.Health.AspireHealthCheckResponseWriter - you can also reference Aspire.Hosting directly)
static class AspireHealthCheckResponseWriter
{
    public static Task WriteResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var result = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.ToString(),
            entries = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    status = entry.Value.Status.ToString(),
                    duration = entry.Value.Duration.ToString(),
                    description = entry.Value.Description,
                    exception = entry.Value.Exception?.Message,
                    data = entry.Value.Data.Count > 0 ? entry.Value.Data : null
                })
        };

        return context.Response.WriteAsJsonAsync(result, options);
    }
}

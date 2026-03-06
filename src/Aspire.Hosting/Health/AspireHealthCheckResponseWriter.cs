// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting.Health;

/// <summary>
/// A health check response writer that formats health check results in a way that Aspire can parse and display individual checks.
/// </summary>
/// <remarks>
/// <para>
/// This response writer is designed to be used with <c>MapHealthChecks</c> in your service to expose all registered
/// health checks through a single endpoint. The Aspire Dashboard will parse this response and display each health check
/// as a separate entry.
/// </para>
/// <para>
/// <strong>Note for service projects:</strong> Since services typically don't reference the <c>Aspire.Hosting</c> package,
/// you can either:
/// <list type="bullet">
/// <item><description>Copy this class into your service project</description></item>
/// <item><description>Add a reference to <c>Aspire.Hosting</c> in your service project (less common)</description></item>
/// </list>
/// </para>
/// <example>
/// In your service's Program.cs:
/// <code lang="C#">
/// var builder = WebApplication.CreateBuilder(args);
///
/// // Register all your health checks
/// builder.Services.AddHealthChecks()
///     .AddCheck("database", () => HealthCheckResult.Healthy("Database is responsive"))
///     .AddCheck("blob_storage", () => HealthCheckResult.Healthy("Blob storage is accessible"))
///     .AddCheck("cache", () => HealthCheckResult.Healthy("Cache is connected"));
///
/// var app = builder.Build();
///
/// // Map a single endpoint with the Aspire response writer
/// app.MapHealthChecks("/health", new HealthCheckOptions
/// {
///     ResponseWriter = AspireHealthCheckResponseWriter.WriteResponse
/// });
/// </code>
///
/// Then in your AppHost's Program.cs:
/// <code lang="C#">
/// var backend = builder.AddProject&lt;Projects.Backend&gt;("backend")
///                      .WithHttpHealthCheck("/health", useAggregate: true);
/// </code>
/// </example>
/// </remarks>
public static class AspireHealthCheckResponseWriter
{
    /// <summary>
    /// Writes the health check response in a format that Aspire can parse to display individual health checks.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="report">The health report containing all health check results.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
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

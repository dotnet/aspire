// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.HealthChecks;

/// <summary>
/// A health check response writer that formats health check results in a way that Aspire can parse and display individual checks.
/// </summary>
/// <remarks>
/// This response writer formats the health check results as a JSON object with the following structure:
/// <list type="table">
/// <item>
/// <term>status</term>
/// <description>The overall <see cref="HealthStatus"/> of the health report, serialized as a string.</description>
/// </item>
/// <item>
/// <term>totalDuration</term>
/// <description>The total duration of all health checks, serialized as a string.</description>
/// </item>
/// <item>
/// <term>entries</term>
/// <description>
/// An object whose properties are the individual health check names. Each property value is an object with:
/// <list type="table">
/// <item>
/// <term>status</term>
/// <description>The status of the individual check, serialized as a string.</description>
/// </item>
/// <item>
/// <term>duration</term>
/// <description>The duration of the individual check, serialized as a string.</description>
/// </item>
/// <item>
/// <term>description</term>
/// <description>The optional description associated with the check result.</description>
/// </item>
/// <item>
/// <term>exception</term>
/// <description>The message from any exception thrown during the check, or <c>null</c> if none occurred.</description>
/// </item>
/// <item>
/// <term>data</term>
/// <description>The optional data dictionary attached to the check result, or <c>null</c> if no data is present.</description>
/// </item>
/// </list>
/// </description>
/// </item>
/// </list>
///
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
///                      .WithHttpHealthCheck("/health");
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting.Health;

/// <summary>
/// A health check that polls an HTTP endpoint and parses the response.
/// Supports the Aspire health check JSON format which can contain multiple individual health check results.
/// </summary>
/// <param name="uriProvider">A function that provides the URI to check.</param>
/// <param name="httpClientFactory">A function that creates an HTTP client.</param>
/// <param name="resourceName">The name of the resource being checked (used for error messages).</param>
/// <param name="expectedStatusCode">The expected HTTP status code for a successful response.</param>
internal sealed class AspireHttpHealthCheck(
    Func<Uri> uriProvider,
    Func<HttpClient> httpClientFactory,
    string resourceName,
    int expectedStatusCode = 200) : IHealthCheck
{
    /// <summary>
    /// Checks the health of the endpoint. If the response is in Aspire JSON format with multiple entries,
    /// they are parsed and included in the result for expansion by the dashboard.
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var uri = uriProvider();
            var httpClient = httpClientFactory();
            var response = await httpClient.GetAsync(uri, cancellationToken).ConfigureAwait(false);
            if ((int)response.StatusCode != expectedStatusCode)
            {
                return HealthCheckResult.Unhealthy(
                    $"HTTP request to {uri} returned status code {response.StatusCode} (expected {expectedStatusCode})");
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            // Try to parse as Aspire JSON format with multiple entries
            if (TryParseHealthCheckResponse(content, out var entries, out var overallStatus, out _))
            {
                // Contains multiple health check entries - mark for expansion
                var data = new Dictionary<string, object>
                {
                    ["__AspireMultipleHealthChecks"] = true,
                    ["SubEntries"] = entries
                };

                return new HealthCheckResult(
                    overallStatus,
                    description: $"Health check for {resourceName}",
                    data: data);
            }

            // Response is not in Aspire format - treat as single health check based on status code
            return HealthCheckResult.Healthy($"HTTP request to {uri} returned expected status code {expectedStatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Exception during health check for {resourceName}", ex);
        }
    }

    private static bool TryParseHealthCheckResponse(
        string json,
        [NotNullWhen(true)] out Dictionary<string, HealthReportEntry>? entries,
        out HealthStatus overallStatus,
        [NotNullWhen(false)] out string? errorMessage)
    {
        entries = null;
        overallStatus = HealthStatus.Unhealthy;
        errorMessage = null;

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            // Parse overall status
            if (root.TryGetProperty("status", out var statusElement))
            {
                overallStatus = Enum.Parse<HealthStatus>(statusElement.GetString()!, ignoreCase: true);
            }

            // Parse entries
            if (!root.TryGetProperty("entries", out var entriesElement))
            {
                errorMessage = "Response does not contain 'entries' property";
                return false;
            }

            entries = [];
            foreach (var entryProperty in entriesElement.EnumerateObject())
            {
                var entryName = entryProperty.Name;
                var entryValue = entryProperty.Value;
                var status = HealthStatus.Unhealthy;
                if (entryValue.TryGetProperty("status", out var entryStatusElement))
                {
                    status = Enum.Parse<HealthStatus>(entryStatusElement.GetString()!, ignoreCase: true);
                }

                var description = entryValue.TryGetProperty("description", out var descElement)
                    ? descElement.GetString()
                    : null;

                var duration = TimeSpan.Zero;
                if (entryValue.TryGetProperty("duration", out var durationElement) &&
                    TimeSpan.TryParse(durationElement.GetString(), out var parsedDuration))
                {
                    duration = parsedDuration;
                }

                Exception? exception = null;
                if (entryValue.TryGetProperty("exception", out var exceptionElement))
                {
                    var exceptionText = exceptionElement.GetString();
                    if (!string.IsNullOrEmpty(exceptionText))
                    {
                        exception = new InvalidOperationException(exceptionText);
                    }
                }

                var data = new Dictionary<string, object>();
                if (entryValue.TryGetProperty("data", out var dataElement))
                {
                    foreach (var dataProp in dataElement.EnumerateObject())
                    {
                        data[dataProp.Name] = dataProp.Value.ToString()!;
                    }
                }

                entries[entryName] = new HealthReportEntry(
                    status,
                    description,
                    duration,
                    exception,
                    data);
            }

            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }
}

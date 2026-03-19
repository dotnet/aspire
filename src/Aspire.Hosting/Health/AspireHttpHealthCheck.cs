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
            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            // Try to parse as Aspire JSON format with multiple entries
            // We parse the body REGARDLESS of status code to allow services to use 503 for unhealthy dependencies
            // while still surfacing detailed health check information to the dashboard
            if (TryParseHealthCheckResponse(content, out var entries, out var parsedStatus, out var parseError))
            {
                // Contains multiple health check entries - mark for expansion
                var data = new Dictionary<string, object>
                {
                    [HealthCheckConstants.DataKeys.MultipleHealthChecks] = true,
                    [HealthCheckConstants.DataKeys.SubEntries] = entries
                };

                var overallStatus = parsedStatus ?? HealthStatus.Unhealthy;
                return new HealthCheckResult(overallStatus, description: $"Health check for {resourceName}", data: data);
            }

            // Response is not in Aspire format - fall back to status code check
            if ((int)response.StatusCode != expectedStatusCode)
            {
                var description = $"HTTP request to {uri} returned status code {response.StatusCode} (expected {expectedStatusCode}). Parse error: {parseError?.Message}";
                return HealthCheckResult.Unhealthy(description, parseError);
            }

            return HealthCheckResult.Healthy($"HTTP request to {uri} returned expected status code {expectedStatusCode}. Content length: {content.Length}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Exception during health check for {resourceName}", ex);
        }
    }

    private static bool TryParseHealthCheckResponse(
        string json,
        [NotNullWhen(true)] out Dictionary<string, HealthReportEntry>? entries,
        out HealthStatus? parsedStatus,
        [NotNullWhen(false)] out Exception? exception)
    {
        entries = null;
        parsedStatus = null;
        exception = null;

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.TryGetProperty(HealthCheckConstants.JsonProperties.Status, out var statusElement))
            {
                parsedStatus = Enum.Parse<HealthStatus>(statusElement.GetString()!, ignoreCase: true);
            }

            if (!root.TryGetProperty(HealthCheckConstants.JsonProperties.Entries, out var entriesElement))
            {
                exception = new InvalidOperationException("Response does not contain 'entries' property");
                return false;
            }

            entries = [];
            foreach (var entryProperty in entriesElement.EnumerateObject())
            {
                var entryName = entryProperty.Name;
                var entryValue = entryProperty.Value;
                var status = HealthStatus.Unhealthy;
                if (entryValue.TryGetProperty(HealthCheckConstants.JsonProperties.Status, out var entryStatusElement))
                {
                    status = Enum.Parse<HealthStatus>(entryStatusElement.GetString()!, ignoreCase: true);
                }

                var description = entryValue.TryGetProperty(HealthCheckConstants.JsonProperties.Description, out var descElement)
                    ? descElement.GetString()
                    : null;

                var duration = TimeSpan.Zero;
                if (entryValue.TryGetProperty(HealthCheckConstants.JsonProperties.Duration, out var durationElement) &&
                    TimeSpan.TryParse(durationElement.GetString(), out var parsedDuration))
                {
                    duration = parsedDuration;
                }

                Exception? entryException = null;
                if (entryValue.TryGetProperty(HealthCheckConstants.JsonProperties.Exception, out var exceptionElement))
                {
                    var exceptionText = exceptionElement.GetString();
                    if (!string.IsNullOrEmpty(exceptionText))
                    {
                        entryException = new InvalidOperationException(exceptionText);
                    }
                }

                var data = new Dictionary<string, object>();
                if (entryValue.TryGetProperty(HealthCheckConstants.JsonProperties.Data, out var dataElement))
                {
                    foreach (var dataProp in dataElement.EnumerateObject())
                    {
                        data[dataProp.Name] = dataProp.Value.ToString()!;
                    }
                }

                entries[entryName] = new HealthReportEntry(status, description, duration, entryException, data);
            }

            return true;
        }
        catch (Exception ex)
        {
            exception = ex;
            return false;
        }
    }
}

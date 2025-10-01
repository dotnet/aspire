// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting.OpenAI;

/// <summary>
/// An adaptive health check for OpenAI resources that changes behavior based on configuration.
/// </summary>
/// <param name="httpClientFactory">The HTTP client factory.</param>
/// <param name="resource">The OpenAI resource.</param>
internal sealed class OpenAIHealthCheck(IHttpClientFactory httpClientFactory, OpenAIResource resource) : IHealthCheck
{
    private const string DefaultEndpoint = "https://api.openai.com/v1";
    private HealthCheckResult? _result;

    /// <summary>
    /// Checks the health of the OpenAI resource.
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (_result is not null)
        {
            return _result.Value;
        }

        try
        {
            // Case 1: Default endpoint - use StatusPageHealthCheck
            if (resource.Endpoint == DefaultEndpoint)
            {
                return await CheckStatusPageAsync(cancellationToken).ConfigureAwait(false);
            }

            // Case 2: Custom endpoint without model health check - return healthy
            // We can't check the endpoint without a model, so we just return healthy
            // The model-level health check will do the actual verification if WithHealthCheck is called
            _result = HealthCheckResult.Healthy("Custom OpenAI endpoint configured");
            return _result.Value;
        }
        catch (Exception ex)
        {
            _result = HealthCheckResult.Unhealthy($"Failed to check OpenAI resource: {ex.Message}", ex);
            return _result.Value;
        }
    }

    private async Task<HealthCheckResult> CheckStatusPageAsync(CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("OpenAIHealthCheck");
        var statusEndpoint = new Uri("https://status.openai.com/api/v2/status.json");
        var timeout = TimeSpan.FromSeconds(5);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        using var req = new HttpRequestMessage(HttpMethod.Get, statusEndpoint);
        req.Headers.Accept.ParseAdd("application/json");

        HttpResponseMessage resp;
        try
        {
            resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException oce) when (!cancellationToken.IsCancellationRequested)
        {
            _result = HealthCheckResult.Unhealthy($"StatusPage request timed out after {timeout.TotalSeconds:0.#}s.", oce);
            return _result.Value;
        }
        catch (Exception ex)
        {
            _result = HealthCheckResult.Unhealthy("StatusPage request failed.", ex);
            return _result.Value;
        }

        if (!resp.IsSuccessStatusCode)
        {
            _result = HealthCheckResult.Unhealthy($"StatusPage returned {(int)resp.StatusCode} {resp.ReasonPhrase}.");
            return _result.Value;
        }

        try
        {
            using var stream = await resp.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cts.Token).ConfigureAwait(false);

            if (!doc.RootElement.TryGetProperty("status", out var statusEl))
            {
                _result = HealthCheckResult.Unhealthy("Missing 'status' object in StatusPage response.");
                return _result.Value;
            }

            var indicator = statusEl.TryGetProperty("indicator", out var indEl) && indEl.ValueKind == JsonValueKind.String
                ? indEl.GetString() ?? string.Empty
                : string.Empty;

            var description = statusEl.TryGetProperty("description", out var descEl) && descEl.ValueKind == JsonValueKind.String
                ? descEl.GetString() ?? string.Empty
                : string.Empty;

            var data = new Dictionary<string, object>
            {
                ["indicator"] = indicator,
                ["description"] = description,
                ["endpoint"] = statusEndpoint.ToString()
            };

            _result = indicator switch
            {
                "none" => HealthCheckResult.Healthy(description.Length > 0 ? description : "All systems operational."),
                "minor" => HealthCheckResult.Degraded(description.Length > 0 ? description : "Minor service issues."),
                "major" => HealthCheckResult.Unhealthy(description.Length > 0 ? description : "Major service outage."),
                "critical" => HealthCheckResult.Unhealthy(description.Length > 0 ? description : "Critical service outage."),
                _ => HealthCheckResult.Unhealthy($"Unknown indicator '{indicator}'", data: data)
            };

            return _result.Value;
        }
        catch (JsonException jex)
        {
            _result = HealthCheckResult.Unhealthy("Failed to parse StatusPage JSON.", jex);
            return _result.Value;
        }
    }
}

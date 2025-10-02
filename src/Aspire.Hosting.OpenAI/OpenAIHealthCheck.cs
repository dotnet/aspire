// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting.OpenAI;

/// <summary>
/// Health check for OpenAI resources that adapts based on endpoint configuration.
/// </summary>
internal sealed class OpenAIHealthCheck : IHealthCheck
{
    private static readonly Uri s_defaultEndpointUri = new("https://api.openai.com/v1");
    private static readonly Uri s_statusPageUri = new("https://status.openai.com/api/v2/status.json");
    
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenAIResource _resource;
    private readonly string? _httpClientName;
    private readonly TimeSpan _timeout;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAIHealthCheck"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The factory to create HTTP clients.</param>
    /// <param name="resource">The OpenAI resource.</param>
    /// <param name="httpClientName">The optional name of the HTTP client to use.</param>
    /// <param name="timeout">The optional timeout for the HTTP request.</param>
    public OpenAIHealthCheck(
        IHttpClientFactory httpClientFactory,
        OpenAIResource resource,
        string? httpClientName = null,
        TimeSpan? timeout = null)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _resource = resource ?? throw new ArgumentNullException(nameof(resource));
        _httpClientName = httpClientName;
        _timeout = timeout ?? TimeSpan.FromSeconds(5);
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // Case 1: Default endpoint - check StatusPage
        if (Uri.TryCreate(_resource.Endpoint, UriKind.Absolute, out var endpointUri) &&
            Uri.Compare(endpointUri, s_defaultEndpointUri, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0)
        {
            return await CheckStatusPageAsync(cancellationToken).ConfigureAwait(false);
        }

        // Case 2: Custom endpoint - return healthy
        return HealthCheckResult.Healthy("Custom OpenAI endpoint configured");
    }

    /// <summary>
    /// Checks the StatusPage endpoint for the default OpenAI service.
    /// </summary>
    private async Task<HealthCheckResult> CheckStatusPageAsync(CancellationToken cancellationToken)
    {
        var client = string.IsNullOrWhiteSpace(_httpClientName)
            ? _httpClientFactory.CreateClient()
            : _httpClientFactory.CreateClient(_httpClientName);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_timeout);

        using var req = new HttpRequestMessage(HttpMethod.Get, s_statusPageUri);
        req.Headers.Accept.ParseAdd("application/json");

        HttpResponseMessage resp;
        try
        {
            resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException oce) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy($"StatusPage request timed out after {_timeout.TotalSeconds:0.#}s.", oce);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("StatusPage request failed.", ex);
        }

        if (!resp.IsSuccessStatusCode)
        {
            return HealthCheckResult.Unhealthy($"StatusPage returned {(int)resp.StatusCode} {resp.ReasonPhrase}.");
        }

        try
        {
            using var stream = await resp.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cts.Token).ConfigureAwait(false);

            // Expected shape:
            // {
            //   "page": { ... },
            //   "status": { "indicator": "none|minor|major|critical", "description": "..." }
            // }
            if (!doc.RootElement.TryGetProperty("status", out var statusEl))
            {
                return HealthCheckResult.Unhealthy("Missing 'status' object in StatusPage response.");
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
                ["endpoint"] = s_statusPageUri.ToString()
            };

            // Map indicator -> HealthStatus
            return indicator switch
            {
                "none" => HealthCheckResult.Healthy(description.Length > 0 ? description : "All systems operational."),
                "minor" => HealthCheckResult.Degraded(description.Length > 0 ? description : "Minor service issues."),
                "major" => HealthCheckResult.Unhealthy(description.Length > 0 ? description : "Major service outage."),
                "critical" => HealthCheckResult.Unhealthy(description.Length > 0 ? description : "Critical service outage."),
                _ => HealthCheckResult.Unhealthy($"Unknown indicator '{indicator}'", data: data)
            };
        }
        catch (JsonException jex)
        {
            return HealthCheckResult.Unhealthy("Failed to parse StatusPage JSON.", jex);
        }
    }
}

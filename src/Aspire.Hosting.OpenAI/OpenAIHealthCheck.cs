// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
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
            // Case 1: Default endpoint - use StatusPage check
            if (resource.Endpoint == DefaultEndpoint)
            {
                return await CheckStatusPageAsync(cancellationToken).ConfigureAwait(false);
            }

            // Case 2: Custom endpoint with model health check - use model health check
            if (resource.UseModelHealthCheck && resource.ModelConnectionString is not null)
            {
                return await CheckModelHealthAsync(cancellationToken).ConfigureAwait(false);
            }

            // Case 3: Custom endpoint without model health check - return healthy
            return await CheckEndpointHealthAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _result = HealthCheckResult.Unhealthy($"Failed to check OpenAI resource: {ex.Message}", ex);
            return _result.Value;
        }
    }

    /// <summary>
    /// Checks the StatusPage endpoint for the default OpenAI service.
    /// </summary>
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

            // Expected shape:
            // {
            //   "page": { ... },
            //   "status": { "indicator": "none|minor|major|critical", "description": "..." }
            // }
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

            // Map indicator -> HealthStatus
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

    /// <summary>
    /// Returns healthy for custom endpoints when no model health check is configured.
    /// </summary>
    private Task<HealthCheckResult> CheckEndpointHealthAsync()
    {
        _result = HealthCheckResult.Healthy("Custom OpenAI endpoint configured");
        return Task.FromResult(_result.Value);
    }

    /// <summary>
    /// Checks the health of the OpenAI endpoint by sending a test request to the model endpoint.
    /// </summary>
    private async Task<HealthCheckResult> CheckModelHealthAsync(CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient("OpenAIHealthCheck");
        var connectionString = resource.ModelConnectionString;

        if (connectionString is null)
        {
            _result = HealthCheckResult.Unhealthy("Model connection string not available");
            return _result.Value;
        }

        try
        {
            var builder = new DbConnectionStringBuilder() { ConnectionString = await connectionString().ConfigureAwait(false) };
            var endpoint = builder["Endpoint"];
            var model = builder["Model"];

            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"{endpoint}/models/{model}"));

            // Add required headers
            request.Headers.Add("Authorization", $"Bearer {builder["Key"]}");

            using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            _result = response.StatusCode switch
            {
                HttpStatusCode.OK => HealthCheckResult.Healthy(),
                HttpStatusCode.Unauthorized => HealthCheckResult.Unhealthy("OpenAI API key is invalid"),
                HttpStatusCode.NotFound => await HandleNotFound(response, cancellationToken).ConfigureAwait(false),
                HttpStatusCode.TooManyRequests => HealthCheckResult.Unhealthy("OpenAI API rate limit exceeded"),
                _ => HealthCheckResult.Unhealthy($"OpenAI endpoint returned unexpected status code: {response.StatusCode}")
            };
        }
        catch (Exception ex)
        {
            _result = HealthCheckResult.Unhealthy($"Failed to check OpenAI endpoint: {ex.Message}", ex);
        }

        return _result.Value;
    }

    private static async Task<HealthCheckResult> HandleNotFound(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        OpenAIErrorResponse? errorResponse = null;

        try
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            errorResponse = JsonSerializer.Deserialize<OpenAIErrorResponse>(content);

            if (errorResponse?.Error?.Code == "model_not_found")
            {
                var message = !string.IsNullOrEmpty(errorResponse.Error.Message)
                    ? errorResponse.Error.Message
                    : "Model not found";
                return HealthCheckResult.Unhealthy($"OpenAI: {message}");
            }
        }
        catch
        {
        }

        return HealthCheckResult.Unhealthy($"OpenAI returned an unsupported response: ({response.StatusCode}) {errorResponse?.Error?.Message}");
    }

    /// <summary>
    /// Represents the error response from OpenAI API.
    /// </summary>
    private sealed class OpenAIErrorResponse
    {
        [JsonPropertyName("error")]
        public OpenAIError? Error { get; set; }
    }

    /// <summary>
    /// Represents an error from OpenAI API.
    /// </summary>
    private sealed class OpenAIError
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }
}

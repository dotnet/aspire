// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.OpenAI;

/// <summary>
/// Checks a StatusPage "status.json" endpoint and maps indicator to ASP.NET Core health status.
/// </summary>
internal sealed class StatusPageHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Uri _statusEndpoint;
    private readonly string? _httpClientName;
    private readonly TimeSpan _timeout;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatusPageHealthCheck"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The factory to create HTTP clients.</param>
    /// <param name="statusEndpoint">The URI of the status.json endpoint.</param>
    /// <param name="httpClientName">The optional name of the HTTP client to use.</param>
    /// <param name="timeout">The optional timeout for the HTTP request.</param>
    public StatusPageHealthCheck(
        IHttpClientFactory httpClientFactory,
        Uri statusEndpoint,
        string? httpClientName = null,
        TimeSpan? timeout = null)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _statusEndpoint = statusEndpoint ?? throw new ArgumentNullException(nameof(statusEndpoint));
        _httpClientName = httpClientName;
        _timeout = timeout ?? TimeSpan.FromSeconds(5);
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var client = string.IsNullOrWhiteSpace(_httpClientName)
            ? _httpClientFactory.CreateClient()
            : _httpClientFactory.CreateClient(_httpClientName);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_timeout);

        using var req = new HttpRequestMessage(HttpMethod.Get, _statusEndpoint);
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
                ["endpoint"] = _statusEndpoint.ToString()
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

internal static class StatuspageHealthCheckExtensions
{
    /// <summary>
    /// Registers a StatusPage health check for a given status.json URL.
    /// </summary>
    public static IDistributedApplicationBuilder AddStatusPageCheck(
        this IDistributedApplicationBuilder builder,
        string name,
        string statusJsonUrl,
        string? httpClientName = null,
        TimeSpan? timeout = null,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(statusJsonUrl);

        // Ensure IHttpClientFactory is available by registering HTTP client services
        builder.Services.AddHttpClient();

        builder.Services.AddHealthChecks().Add(new HealthCheckRegistration(
            name: name,
            factory: sp =>
            {
                var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
                return new StatusPageHealthCheck(httpFactory, new Uri(statusJsonUrl), httpClientName, timeout);
            },
            failureStatus: failureStatus,
            tags: tags));

        return builder;
    }
}

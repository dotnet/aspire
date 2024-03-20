// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Seq;

/// <summary>
/// A diagnostic health check implementation for Seq servers.
/// </summary>
/// <param name="seqUri">The URI of the Seq server to check.</param>
internal sealed class SeqHealthCheck(string seqUri) : IHealthCheck
{
    readonly HttpClient _client = new(new SocketsHttpHandler { ActivityHeadersPropagator = null }) { BaseAddress = new Uri(seqUri) };

    /// <summary>
    /// Checks the health of a Seq server by calling its <a href="https://docs.datalust.co/docs/using-the-http-api#checking-health">health</a> endpoint.
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext _, CancellationToken cancellationToken = new CancellationToken())
    {
        using var response = await _client.GetAsync("/health", cancellationToken)
            .ConfigureAwait(false);

        return response.IsSuccessStatusCode
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy();
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting.Azure.WebPubSub;

internal sealed class WebPubSubEmulatorHealthCheck(AzureWebPubSubResource resource, Func<HttpClient> httpClientFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var endpoint = resource.EmulatorEndpoint;
        if (!endpoint.IsAllocated)
        {
            return HealthCheckResult.Unhealthy("The emulator endpoint is not allocated.");
        }

        // The Web PubSub service health API only supports HEAD requests.
        using var client = httpClientFactory();
        using var request = new HttpRequestMessage(HttpMethod.Head, new Uri(new Uri(endpoint.Url), "/api/health"));
        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

        return response.StatusCode == HttpStatusCode.OK
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy($"The emulator returned HTTP {(int)response.StatusCode}.");
    }
}

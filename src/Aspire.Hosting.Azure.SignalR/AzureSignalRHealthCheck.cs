// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting.Azure.SignalR;
internal sealed class AzureSignalRHealthCheck : IHealthCheck
{
    private readonly Uri _hostName;
    private readonly IHttpClientFactory _clientFactory;

    public AzureSignalRHealthCheck(Uri hostName, IHttpClientFactory clientFactory)
    {
        ArgumentNullException.ThrowIfNull(hostName, nameof(hostName));
        _hostName = hostName;
        _clientFactory = clientFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using HttpClient client = _clientFactory.CreateClient();
        try
        {
            Console.WriteLine($"Checking health of {_hostName.AbsoluteUri}");
            var response = await client.GetAsync(_hostName.AbsoluteUri + "api/health", cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return new HealthCheckResult(context.Registration.FailureStatus, description: $"The health check endpoint returned status code {response.StatusCode}.");
            }
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}

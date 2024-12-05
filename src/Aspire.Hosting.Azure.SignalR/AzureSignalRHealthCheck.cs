// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting.Azure.SignalR;
internal sealed class AzureSignalRHealthCheck : IHealthCheck
{
    private readonly HttpClient _client;
    private readonly String _hostName;

    public AzureSignalRHealthCheck(string hostName)
    {
        ArgumentNullException.ThrowIfNull(hostName, nameof(hostName));
        _client = new HttpClient();
        _hostName = hostName;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.GetAsync(_hostName + "/api/health", cancellationToken).ConfigureAwait(false);
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

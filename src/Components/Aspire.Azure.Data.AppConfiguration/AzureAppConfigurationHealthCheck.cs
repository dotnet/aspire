// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Data.AppConfiguration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Azure.Data.AppConfiguration;

internal sealed class AzureAppConfigurationHealthCheck : IHealthCheck
{
    private readonly ConfigurationClient _client;

    public AzureAppConfigurationHealthCheck(ConfigurationClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await _client.GetConfigurationSettingAsync("*", null, cancellationToken).ConfigureAwait(false);

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}

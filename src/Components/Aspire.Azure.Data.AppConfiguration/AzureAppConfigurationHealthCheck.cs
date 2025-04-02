// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure;
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
            var selector = new SettingSelector
            {
                KeyFilter = "__UNEXISTED_KEY__",
                LabelFilter = null
            };
            AsyncPageable<ConfigurationSetting> pageableSettings = _client.GetConfigurationSettingsAsync(selector, cancellationToken);
            await foreach (var page in pageableSettings.AsPages().ConfigureAwait(false))
            {
                _ = page.GetRawResponse(); // If healthy, the response should be 200 and with empty content
            }

            return HealthCheckResult.Healthy();
        }
        catch (RequestFailedException ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Azure.Core;
using Azure.Core.Extensions;
using Azure.Data.AppConfiguration;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Azure.Data.AppConfiguration;
internal sealed class AppConfigurationComponent : AzureComponent<AzureDataAppConfigurationSettings, ConfigurationClient, ConfigurationClientOptions>
{
    protected override IAzureClientBuilder<ConfigurationClient, ConfigurationClientOptions> AddClient(
            AzureClientFactoryBuilder azureFactoryBuilder, AzureDataAppConfigurationSettings settings,
            string connectionName, string configurationSectionName)
    {
        return azureFactoryBuilder.AddClient<ConfigurationClient, ConfigurationClientOptions>((options, cred, _) =>
        {
            if (settings.Endpoint is null)
            {
                throw new InvalidOperationException($"Endpoint is missing. It should be provided in 'ConnectionStrings:{connectionName}' or under the 'Endpoint' key in the '{configurationSectionName}' configuration section.");
            }

            return new ConfigurationClient(settings.Endpoint, cred, options);
        });
    }

    protected override IHealthCheck CreateHealthCheck(ConfigurationClient client, AzureDataAppConfigurationSettings settings)
    {
        return new AzureAppConfigurationHealthCheck(client);
    }

    protected override void BindClientOptionsToConfiguration(IAzureClientBuilder<ConfigurationClient, ConfigurationClientOptions> clientBuilder, IConfiguration configuration)
    {
#pragma warning disable IDE0200 // Remove unnecessary lambda expression - needed so the ConfigBinder Source Generator works
        clientBuilder.ConfigureOptions(options => configuration.Bind(options));
#pragma warning restore IDE0200
    }

    protected override void BindSettingsToConfiguration(AzureDataAppConfigurationSettings settings, IConfiguration configuration)
    {
        configuration.Bind(settings);
    }

    protected override bool GetHealthCheckEnabled(AzureDataAppConfigurationSettings settings)
        => !settings.DisableHealthChecks;

    protected override TokenCredential? GetTokenCredential(AzureDataAppConfigurationSettings settings)
        => settings.Credential;

    protected override bool GetMetricsEnabled(AzureDataAppConfigurationSettings settings)
        => false;

    protected override bool GetTracingEnabled(AzureDataAppConfigurationSettings settings)
        => !settings.DisableTracing;
}

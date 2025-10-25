// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Aspire.Azure.Data.AppConfiguration;
using Azure.Core;
using Azure.Core.Extensions;
using Azure.Data.AppConfiguration;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering <see cref="ConfigurationClient"/> as a singleton in the services provided by the <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static class AspireAppConfigurationExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Azure:Data:AppConfiguration";

    /// <summary>
    /// Registers <see cref="ConfigurationClient"/> as a singleton in the services provided by the <paramref name="builder"/>.
    /// Enables retries, corresponding health check, logging and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureDataAppConfigurationSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Data:AppConfiguration" section.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureDataAppConfigurationSettings.ConnectionString"/> nor <see cref="AzureDataAppConfigurationSettings.Endpoint"/> is provided.</exception>
    public static void AddAzureAppConfigurationClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureDataAppConfigurationSettings>? configureSettings = null,
        Action<IAzureClientBuilder<ConfigurationClient, ConfigurationClientOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        new AppConfigurationComponent().AddClient(builder, DefaultConfigSectionName, configureSettings, configureClientBuilder, connectionName, serviceKey: null);
    }

    /// <summary>
    /// Registers <see cref="ConfigurationClient"/> as a singleton for given <paramref name="name"/> in the services provided by the <paramref name="builder"/>.
    /// Enables retries, corresponding health check, logging and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureDataAppConfigurationSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Data:AppConfiguration:{name}" section.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureDataAppConfigurationSettings.ConnectionString"/> nor <see cref="AzureDataAppConfigurationSettings.Endpoint"/> is provided.</exception>
    public static void AddKeyedAzureAppConfigurationClient(
        this IHostApplicationBuilder builder,
        string name,
        Action<AzureDataAppConfigurationSettings>? configureSettings = null,
        Action<IAzureClientBuilder<ConfigurationClient, ConfigurationClientOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        new AppConfigurationComponent().AddClient(builder, DefaultConfigSectionName, configureSettings, configureClientBuilder, connectionName: name, serviceKey: name);
    }

    private sealed class AppConfigurationComponent : AzureComponent<AzureDataAppConfigurationSettings, ConfigurationClient, ConfigurationClientOptions>
    {
        protected override IAzureClientBuilder<ConfigurationClient, ConfigurationClientOptions> AddClient(
            AzureClientFactoryBuilder azureFactoryBuilder, AzureDataAppConfigurationSettings settings, string connectionName,
            string configurationSectionName)
        {
            return ((IAzureClientFactoryBuilderWithCredential)azureFactoryBuilder).RegisterClientFactory<ConfigurationClient, ConfigurationClientOptions>((options, cred) =>
            {
                var connectionString = settings.ConnectionString;
                if (string.IsNullOrEmpty(connectionString) && settings.Endpoint is null)
                {
                    throw new InvalidOperationException($"A ConfigurationClient could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}' or specify a 'ConnectionString' or 'Endpoint' in the '{configurationSectionName}' configuration section.");
                }

                if (!string.IsNullOrEmpty(connectionString))
                {
                    return new ConfigurationClient(connectionString, options);
                }
                else if (cred is not null)
                {
                    return new ConfigurationClient(settings.Endpoint!, cred, options);
                }
                else
                {
                    throw new InvalidOperationException($"A ConfigurationClient could not be configured. When using an endpoint, a credential must be provided.");
                }
            }, requiresCredential: false);
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

        protected override IHealthCheck CreateHealthCheck(ConfigurationClient client, AzureDataAppConfigurationSettings settings)
            => new AppConfigurationHealthCheck(client);

        protected override bool GetHealthCheckEnabled(AzureDataAppConfigurationSettings settings)
            => !settings.DisableHealthChecks;

        protected override TokenCredential? GetTokenCredential(AzureDataAppConfigurationSettings settings)
            => settings.Credential;

        protected override bool GetMetricsEnabled(AzureDataAppConfigurationSettings settings)
            => false;

        protected override bool GetTracingEnabled(AzureDataAppConfigurationSettings settings)
            => !settings.DisableTracing;
    }

    private sealed class AppConfigurationHealthCheck : IHealthCheck
    {
        private readonly ConfigurationClient _client;

        public AppConfigurationHealthCheck(ConfigurationClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Try to read configuration settings to verify connectivity
                // We'll attempt to get a single configuration setting with a minimal selector
                var selector = new SettingSelector();
                await foreach (var page in _client.GetConfigurationSettingsAsync(selector, cancellationToken).AsPages().ConfigureAwait(false))
                {
                    // Just getting the first page is sufficient to verify connectivity
                    break;
                }

                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
            }
        }
    }
}
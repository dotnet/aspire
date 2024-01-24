// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Azure;
using Azure.Core;
using Azure.Core.Extensions;
using Azure.Identity;
using Azure.Search.Documents;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Aspire.Azure.AI.Search;

/// <summary>
/// Provides extension methods for registering <see cref="SearchClient"/> as a singleton in the services provided by the <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static class AspireAzureAISearchExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Azure:AI:Search";
    /// <summary>
    /// Registers <see cref="SearchClient"/> as a singleton in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureAISearchSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{SearchClient, SearchClientOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire.Azure.AI.Search" section.</remarks>
    public static void AddAzureAISearch(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureAISearchSettings>? configureSettings = null,
        Action<IAzureClientBuilder<SearchClient, SearchClientOptions>>? configureClientBuilder = null)
    {
        new AISearchComponent().AddClient(builder, DefaultConfigSectionName, configureSettings, configureClientBuilder, connectionName, serviceKey: null);
    }

    /// <summary>
    /// Registers <see cref="SearchClient"/> as a singleton for given <paramref name="name"/> in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureAISearchSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{SearchClient, SearchClientOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire.Azure.AI.Search:{name}" section.</remarks>
    public static void AddKeyedAzureAISearch(
        this IHostApplicationBuilder builder,
        string name,
        Action<AzureAISearchSettings>? configureSettings = null,
        Action<IAzureClientBuilder<SearchClient, SearchClientOptions>>? configureClientBuilder = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var configurationSectionName = AISearchComponent.GetKeyedConfigurationSectionName(name, DefaultConfigSectionName);

        new AISearchComponent().AddClient(builder, configurationSectionName, configureSettings, configureClientBuilder, connectionName: name, serviceKey: name);
    }

    private sealed class AISearchComponent : AzureComponent<AzureAISearchSettings, SearchClient, SearchClientOptions>
    {
        protected override IAzureClientBuilder<SearchClient, SearchClientOptions> AddClient<TBuilder>(TBuilder azureFactoryBuilder, AzureAISearchSettings settings, string connectionName, string configurationSectionName)
        {
            return azureFactoryBuilder.RegisterClientFactory<SearchClient, SearchClientOptions>((options, cred) =>
            {
                if (settings.Endpoint is null || string.IsNullOrWhiteSpace(settings.IndexName))
                {
                    throw new InvalidOperationException($"A SearchClient could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}' or specify an '{nameof(AzureAISearchSettings.Endpoint)}' and '{nameof(AzureAISearchSettings.IndexName)}' in the '{configurationSectionName}' configuration section.");
                }

                if (!string.IsNullOrWhiteSpace(settings.Key))
                {
                    var credential = new AzureKeyCredential(settings.Key);
                    return new SearchClient(settings.Endpoint, settings.IndexName, credential, options);
                }
                else
                {
                    return new SearchClient(settings.Endpoint, settings.IndexName, settings.Credential ?? new DefaultAzureCredential(), options);
                }
            });
        }

        protected override void BindClientOptionsToConfiguration(IAzureClientBuilder<SearchClient, SearchClientOptions> clientBuilder, IConfiguration configuration)
        {
#pragma warning disable IDE0200 // Remove unnecessary lambda expression - needed so the ConfigBinder Source Generator works
            clientBuilder.ConfigureOptions(options => configuration.Bind(options));
#pragma warning restore IDE0200
        }

        protected override void BindSettingsToConfiguration(AzureAISearchSettings settings, IConfiguration config)
        {
            config.Bind(settings);
        }

        protected override IHealthCheck CreateHealthCheck(SearchClient client, AzureAISearchSettings settings)
        {
            throw new NotImplementedException();
        }

        protected override bool GetHealthCheckEnabled(AzureAISearchSettings settings)
        {
            return false;
        }

        protected override TokenCredential? GetTokenCredential(AzureAISearchSettings settings)
            => settings.Credential;

        protected override bool GetTracingEnabled(AzureAISearchSettings settings)
            => settings.Tracing;
    }
}

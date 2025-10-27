// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Aspire.Azure.Search;
using Aspire.Azure.Search.Documents;
using Azure;
using Azure.Core;
using Azure.Core.Extensions;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering <see cref="SearchIndexClient"/> as a singleton in the services provided by the <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static class AspireAzureSearchIndexerExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Azure:Search:Documents";

    /// <summary>
    /// Registers <see cref="SearchIndexClient"/> as a singleton in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">
    /// An optional method that can be used for customizing the <see cref="AzureSearchIndexerSettings"/>.
    /// It's invoked after the settings are read from the configuration.
    /// </param>
    /// <param name="configureClientBuilder">
    /// An optional method that can be used for customizing the <see cref="IAzureClientBuilder{SearchIndexerClient, SearchIndexerClientOptions}"/>.</param>
    /// <remarks>
    /// Reads the configuration from "Aspire:Azure:Search:Documents" section.
    /// </remarks>
    public static void AddAzureSearchClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureSearchIndexerSettings>? configureSettings = null,
        Action<IAzureClientBuilder<SearchIndexerClient, SearchClientOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        new AzureSearchIndexerComponent()
            .AddClient(
                builder,
                DefaultConfigSectionName,
                configureSettings,
                configureClientBuilder,
                connectionName,
                serviceKey: null
            );
    }

    /// <summary>
    /// Registers <see cref="SearchIndexClient"/> as a singleton for given <paramref name="name"/> in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureSearchSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{SearchIndexClient, SearchClientOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Search:Documents:{name}" section.</remarks>
    public static void AddKeyedAzureSearchClient(
        this IHostApplicationBuilder builder,
        string name,
        Action<AzureSearchIndexerSettings>? configureSettings = null,
        Action<IAzureClientBuilder<SearchIndexerClient, SearchClientOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        new AzureSearchIndexerComponent()
            .AddClient(
                builder,
                DefaultConfigSectionName,
                configureSettings,
                configureClientBuilder,
                connectionName: name,
                serviceKey: name
            );
    }

    /// <summary>
    /// Provides an Azure component for configuring and managing a SearchIndexerClient used to interact with Azure
    /// Cognitive Search indexers.
    /// </summary>
    /// <remarks>This class integrates Azure Cognitive Search indexer client registration, configuration
    /// binding, and health check setup within an application using dependency injection. It supports both API key and
    /// token-based authentication, and automatically binds settings and client options from configuration sources.
    /// Tracing is enabled by default unless explicitly disabled in the settings. This component is intended for
    /// internal use within the Azure component infrastructure and is not intended to be instantiated directly by
    /// application code.</remarks>
    private sealed class AzureSearchIndexerComponent :
        AzureComponent<AzureSearchIndexerSettings, SearchIndexerClient, SearchClientOptions>
    {
        // `SearchIndexerClient` is in the Azure.Search.Documents.Indexes namespace
        // but uses `SearchClientOptions` which is in the Azure.Search.Documents namespace
        // https://github.com/Azure/azure-sdk-for-net/blob/bed506dee05319ff2de27ca98500daa10573fe7d/sdk/search/Azure.Search.Documents/src/Indexes/SearchIndexClient.cs#L92
        protected override string[] ActivitySourceNames => ["Azure.Search.Documents.*"];
        protected override IAzureClientBuilder<SearchIndexerClient, SearchClientOptions> AddClient(
            AzureClientFactoryBuilder azureFactoryBuilder,
            AzureSearchIndexerSettings settings,
            string connectionName,
            string configurationSectionName)
        {
            return azureFactoryBuilder.AddClient<SearchIndexerClient, SearchClientOptions>((options, _, _) =>
            {
                if (settings.Endpoint is null)
                {
                    throw new InvalidOperationException(
                        $"A SearchIndexerClient could not be configured. " +
                        $"Ensure valid connection information was provided in " +
                        $"'ConnectionStrings:{connectionName}' or specify an " +
                        $"'{nameof(AzureSearchIndexerSettings.Endpoint)}' in the " +
                        $"'{configurationSectionName}' configuration section.");
                }

                if (!string.IsNullOrWhiteSpace(settings.Key))
                {
                    return new SearchIndexerClient(
                        endpoint:settings.Endpoint,
                        credential: new AzureKeyCredential(settings.Key),
                        options);
                }
                else
                {
                    return new SearchIndexerClient(
                        endpoint: settings.Endpoint,
                        tokenCredential: settings.Credential ?? new DefaultAzureCredential(),
                        options);
                }
            });
        }

        protected override void BindClientOptionsToConfiguration(
                IAzureClientBuilder<SearchIndexerClient,SearchClientOptions> clientBuilder,
                IConfiguration configuration)
        {
#pragma warning disable IDE0200 // Remove unnecessary lambda expression - needed so the ConfigBinder Source Generator works
            clientBuilder.ConfigureOptions(options => configuration.Bind(options));
#pragma warning restore IDE0200
        }

        protected override void BindSettingsToConfiguration(
            AzureSearchIndexerSettings settings,
            IConfiguration configuration)
        {
            configuration.Bind(settings);
        }
        protected override IHealthCheck CreateHealthCheck(
            SearchIndexerClient client,
            AzureSearchIndexerSettings settings) => new AzureSearchIndexerHealthCheck(client);
        protected override bool GetHealthCheckEnabled(
            AzureSearchIndexerSettings settings) => !settings.DisableHealthChecks;
        protected override TokenCredential? GetTokenCredential(AzureSearchIndexerSettings settings)
            => settings.Credential;
        protected override bool GetMetricsEnabled(AzureSearchIndexerSettings settings)
            => false;
        protected override bool GetTracingEnabled(AzureSearchIndexerSettings settings)
            => !settings.DisableTracing;
    }
}

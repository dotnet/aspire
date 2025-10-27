// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
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
    /// Adds and configures an Azure Cognitive Search indexer client for dependency injection using the specified
    /// connection name.
    /// </summary>
    /// <remarks>This method registers a SearchIndexerClient with the application's dependency injection
    /// container, allowing it to be injected into services and components. Use the optional configuration delegates to
    /// customize client behavior or settings as needed.</remarks>
    /// <param name="builder">The application builder used to register services and configure the host.</param>
    /// <param name="connectionName">The name of the connection string or configuration section used to connect to the Azure Cognitive Search
    /// service. Cannot be null or empty.</param>
    /// <param name="configureSettings">An optional delegate to configure additional Azure Search settings before the client is created.</param>
    /// <param name="configureClientBuilder">An optional delegate to further configure the Azure client builder for the SearchIndexerClient.</param>
    public static void AddAzureSearchIndexerClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureSearchSettings>? configureSettings = null,
        Action<IAzureClientBuilder<SearchIndexerClient, SearchClientOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        new AzureSearchIndexerComponent()
            .AddClient(
                builder,
                DefaultConfigSectionName, configureSettings, configureClientBuilder, connectionName, serviceKey: null);
    }

    /// <summary>
    /// Adds a keyed Azure Search Indexer client to the application's dependency injection container using the specified
    /// name and optional configuration actions.
    /// </summary>
    /// <remarks>Use this method to register multiple Azure Search Indexer clients with different
    /// configurations by specifying unique names. The registered client can be resolved by name from the dependency
    /// injection container.</remarks>
    /// <param name="builder">The application builder to which the Azure Search Indexer client will be added. Cannot be null.</param>
    /// <param name="name">The unique name used to register and identify the Azure Search Indexer client instance. Cannot be null or empty.</param>
    /// <param name="configureSettings">An optional action to configure the Azure Search client settings before the client is created.</param>
    /// <param name="configureClientBuilder">An optional action to further configure the Azure client builder for the Search Indexer client.</param>
    public static void AddKeyedAzureSearchIndexerClient(
        this IHostApplicationBuilder builder,
        string name,
        Action<AzureSearchSettings>? configureSettings = null,
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

    private sealed class AzureSearchIndexerComponent : AzureComponent<
        AzureSearchSettings,
        SearchIndexerClient,
        SearchClientOptions>
    {
        // `SearchIndexClient` is in the Azure.Search.Documents.Indexes namespace
        // but uses `SearchClientOptions` which is in the Azure.Search.Documents namespace
        // https://github.com/Azure/azure-sdk-for-net/blob/bed506dee05319ff2de27ca98500daa10573fe7d/sdk/search/Azure.Search.Documents/src/Indexes/SearchIndexClient.cs#L92
        protected override string[] ActivitySourceNames => ["Azure.Search.Documents.*"];

        protected override IAzureClientBuilder<SearchIndexerClient, SearchClientOptions> AddClient(
            AzureClientFactoryBuilder azureFactoryBuilder,
            AzureSearchSettings settings,
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
                        $"'{nameof(AzureSearchSettings.Endpoint)}' in the " +
                        $"'{configurationSectionName}' configuration section.");
                }

                if (!string.IsNullOrWhiteSpace(settings.Key))
                {
                    return new SearchIndexerClient(
                        settings.Endpoint,
                        new AzureKeyCredential(settings.Key),
                        options);
                }
                else
                {
                    return new SearchIndexerClient(
                        settings.Endpoint,
                        settings.Credential ?? new DefaultAzureCredential(),
                        options);
                }
            });
        }

        protected override void BindClientOptionsToConfiguration(IAzureClientBuilder<
            SearchIndexerClient, SearchClientOptions> clientBuilder,
            IConfiguration configuration)
        {
#pragma warning disable IDE0200 // Remove unnecessary lambda expression - needed so the ConfigBinder Source Generator works
            clientBuilder.ConfigureOptions(options => configuration.Bind(options));
#pragma warning restore IDE0200
        }

        protected override void BindSettingsToConfiguration(
            AzureSearchSettings settings,
            IConfiguration configuration)
        {
            configuration.Bind(settings);
        }

        protected override IHealthCheck CreateHealthCheck(
            SearchIndexerClient client,
            AzureSearchSettings settings) => new AzureSearchIndexerHealthCheck(client);
        protected override bool GetHealthCheckEnabled(AzureSearchSettings settings)
            => !settings.DisableHealthChecks;
        protected override TokenCredential? GetTokenCredential(AzureSearchSettings settings)
            => settings.Credential;
        protected override bool GetMetricsEnabled(AzureSearchSettings settings)
            => false;
        protected override bool GetTracingEnabled(AzureSearchSettings settings)
            => !settings.DisableTracing;
    }
}

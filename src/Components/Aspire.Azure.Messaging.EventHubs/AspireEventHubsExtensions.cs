// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Aspire.Azure.Messaging.EventHubs;
using Azure.Core;
using Azure.Core.Extensions;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Primitives;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering Event Hubs clients in the services provided by the <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static class AspireEventHubsExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Azure:Messaging:EventHubs:";

    /// <summary>
    /// Registers <see cref="EventProcessorClient"/> as a singleton in the services provided by the<paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureMessagingEventHubsSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Messaging:EventHubs:{TClient}" section, where {TClient} is the type of Event Hubs client being configured, i.e. EventProcessorClient.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureMessagingEventHubsSettings.ConnectionString"/> nor <see cref="AzureMessagingEventHubsSettings.Namespace"/> is provided.</exception>
    public static void AddAzureEventProcessorClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureMessagingEventHubsSettings>? configureSettings = null,
        Action<IAzureClientBuilder<EventProcessorClient, EventProcessorClientOptions>>? configureClientBuilder = null)
    {
        new EventProcessorClientComponent(builder.Configuration)
            .AddClient(builder, DefaultConfigSectionName + nameof(EventProcessorClient),
                configureSettings, configureClientBuilder, connectionName, serviceKey: null);
    }

    /// <summary>
    /// Registers <see cref="EventProcessorClient"/> as a singleton for given <paramref name="name"/> in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureMessagingEventHubsSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Messaging:EventHubs:{TClient}" section, where {TClient} is the type of Event Hubs client being configured, i.e. EventProcessorClient.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureMessagingEventHubsSettings.ConnectionString"/> nor <see cref="AzureMessagingEventHubsSettings.Namespace"/> is provided.</exception>
    public static void AddKeyedAzureEventHubProcessorClient(
        this IHostApplicationBuilder builder,
        string name,
        Action<AzureMessagingEventHubsSettings>? configureSettings = null,
        Action<IAzureClientBuilder<EventProcessorClient, EventProcessorClientOptions>>? configureClientBuilder = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        string configurationSectionName = EventProcessorClientComponent
            .GetKeyedConfigurationSectionName(name, DefaultConfigSectionName +
                                                    nameof(EventProcessorClient));

        new EventProcessorClientComponent(builder.Configuration)
            .AddClient(builder, configurationSectionName, configureSettings,
                configureClientBuilder, connectionName: name, serviceKey: name);
    }

    /// <summary>
    /// Registers <see cref="PartitionReceiver"/> as a singleton in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureMessagingEventHubsSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Messaging:EventHubs:{TClient}" section, where {TClient} is the type of Event Hubs client being configured, i.e. EventProcessorClient.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureMessagingEventHubsSettings.ConnectionString"/> nor <see cref="AzureMessagingEventHubsSettings.Namespace"/> is provided.</exception>
    public static void AddAzurePartitionReceiverClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureMessagingEventHubsSettings>? configureSettings = null,
        Action<IAzureClientBuilder<PartitionReceiver, PartitionReceiverOptions>>? configureClientBuilder = null)
    {
        new PartitionReceiverClientComponent()
            .AddClient(builder, DefaultConfigSectionName + nameof(PartitionReceiver),
                configureSettings, configureClientBuilder, connectionName, serviceKey: null);
    }

    /// <summary>
    /// Registers <see cref="PartitionReceiver"/> as a singleton for given <paramref name="name"/> in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureMessagingEventHubsSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Messaging:EventHubs:{TClient}" section, where {TClient} is the type of Event Hubs client being configured, i.e. EventProcessorClient.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureMessagingEventHubsSettings.ConnectionString"/> nor <see cref="AzureMessagingEventHubsSettings.Namespace"/> is provided.</exception>
    public static void AddKeyedAzurePartitionReceiverClient(
        this IHostApplicationBuilder builder,
        string name,
        Action<AzureMessagingEventHubsSettings>? configureSettings = null,
        Action<IAzureClientBuilder<PartitionReceiver, PartitionReceiverOptions>>? configureClientBuilder = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        string configurationSectionName = PartitionReceiverClientComponent
            .GetKeyedConfigurationSectionName(name, DefaultConfigSectionName +
                                                    nameof(PartitionReceiver));

        new PartitionReceiverClientComponent()
            .AddClient(builder, configurationSectionName, configureSettings,
                configureClientBuilder, connectionName: name, serviceKey: name);
    }

    /// <summary>
    /// Registers <see cref="EventHubProducerClient"/> as a singleton in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureMessagingEventHubsSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Messaging:EventHubs:{TClient}" section, where {TClient} is the type of Event Hubs client being configured, i.e. EventProcessorClient.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureMessagingEventHubsSettings.ConnectionString"/> nor <see cref="AzureMessagingEventHubsSettings.Namespace"/> is provided.</exception>
    public static void AddAzureEventHubProducerClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureMessagingEventHubsSettings>? configureSettings = null,
        Action<IAzureClientBuilder<EventHubProducerClient, EventHubProducerClientOptions>>? configureClientBuilder = null)
    {
        new EventHubProducerClientComponent()
            .AddClient(builder, DefaultConfigSectionName + nameof(EventHubProducerClient),
                configureSettings, configureClientBuilder, connectionName, serviceKey: null);
    }

    /// <summary>
    /// Registers <see cref="EventHubProducerClient"/> as a singleton for given <paramref name="name"/> in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureMessagingEventHubsSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Messaging:EventHubs:{TClient}" section, where {TClient} is the type of Event Hubs client being configured, i.e. EventProcessorClient.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureMessagingEventHubsSettings.ConnectionString"/> nor <see cref="AzureMessagingEventHubsSettings.Namespace"/> is provided.</exception>
    public static void AddKeyedAzureEventHubProducerClient(
        this IHostApplicationBuilder builder,
        string name,
        Action<AzureMessagingEventHubsSettings>? configureSettings = null,
        Action<IAzureClientBuilder<EventHubProducerClient, EventHubProducerClientOptions>>? configureClientBuilder = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        string configurationSectionName = EventHubProducerClientComponent
            .GetKeyedConfigurationSectionName(name, DefaultConfigSectionName +
                                                    nameof(EventHubProducerClient));

        new EventHubProducerClientComponent()
            .AddClient(builder, configurationSectionName, configureSettings,
                configureClientBuilder, connectionName: name, serviceKey: name);
    }

    /// <summary>
    /// Registers <see cref="EventHubConsumerClient"/> as a singleton in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureMessagingEventHubsSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Messaging:EventHubs:{TClient}" section, where {TClient} is the type of Event Hubs client being configured, i.e. EventProcessorClient.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureMessagingEventHubsSettings.ConnectionString"/> nor <see cref="AzureMessagingEventHubsSettings.Namespace"/> is provided.</exception>
    public static void AddAzureEventHubConsumerClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureMessagingEventHubsSettings>? configureSettings = null,
        Action<IAzureClientBuilder<EventHubConsumerClient, EventHubConsumerClientOptions>>? configureClientBuilder = null)
    {
        new EventHubConsumerClientComponent()
            .AddClient(builder, DefaultConfigSectionName + nameof(EventHubConsumerClient),
                configureSettings, configureClientBuilder, connectionName, serviceKey: null);
    }

    /// <summary>
    /// Registers <see cref="EventHubConsumerClient"/> as a singleton for given <paramref name="name"/> in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureMessagingEventHubsSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Messaging:EventHubs:{TClient}" section, where {TClient} is the type of Event Hubs client being configured, i.e. EventProcessorClient.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureMessagingEventHubsSettings.ConnectionString"/> nor <see cref="AzureMessagingEventHubsSettings.Namespace"/> is provided.</exception>
    public static void AddKeyedAzureEventHubConsumerClient(
        this IHostApplicationBuilder builder,
        string name,
        Action<AzureMessagingEventHubsSettings>? configureSettings = null,
        Action<IAzureClientBuilder<EventHubConsumerClient, EventHubConsumerClientOptions>>? configureClientBuilder = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        string configurationSectionName = EventHubConsumerClientComponent
            .GetKeyedConfigurationSectionName(name, DefaultConfigSectionName +
                                                    nameof(EventHubConsumerClient));

        new EventHubConsumerClientComponent()
            .AddClient(builder, configurationSectionName, configureSettings,
                configureClientBuilder, connectionName: name, serviceKey: name);
    }
}

internal abstract class EventHubsComponent<TClient, TClientOptions> :
    AzureComponent<AzureMessagingEventHubsSettings, TClient, TClientOptions>
    where TClientOptions: class
    where TClient : class
{
    protected override IHealthCheck CreateHealthCheck(TClient client, AzureMessagingEventHubsSettings settings)
        => throw new NotImplementedException();

    protected override void BindSettingsToConfiguration(AzureMessagingEventHubsSettings settings, IConfiguration config)
    {
        config.Bind(settings);
    }

    protected override bool GetHealthCheckEnabled(AzureMessagingEventHubsSettings settings)
        => false;

    protected override TokenCredential? GetTokenCredential(AzureMessagingEventHubsSettings settings)
        => settings.Credential;

    protected override bool GetTracingEnabled(AzureMessagingEventHubsSettings settings)
        => settings.Tracing;

    protected static string GenerateClientIdentifier(AzureMessagingEventHubsSettings settings)
    {
        // configure processor identifier
        var slug = Guid.NewGuid().ToString().Substring(24);
        var identifier = $"{Environment.MachineName}-{settings.EventHubName}-" +
                         $"{settings.ConsumerGroup ?? "default"}-{slug}";

        return identifier;
    }

    protected static string GetNamespaceFromSettings(AzureMessagingEventHubsSettings settings)
    {
        string ns;

        try
        {
            // extract the namespace from the connection string or qualified namespace
            var fullyQualifiedNs = string.IsNullOrWhiteSpace(settings.Namespace) ?
                EventHubsConnectionStringProperties.Parse(settings.ConnectionString).FullyQualifiedNamespace :
                new Uri(settings.Namespace).Host;

            ns = fullyQualifiedNs[..fullyQualifiedNs.IndexOf('.', StringComparison.Ordinal)];
        }
        catch (Exception ex) when (ex is FormatException or IndexOutOfRangeException)
        {
            throw new InvalidOperationException(
                $"A {nameof(TClient)} could not be configured. Please ensure that the ConnectionString or Namespace is well-formed.");
        }

        return ns;
    }
}

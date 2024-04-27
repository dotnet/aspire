// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Messaging.EventHubs;
using Azure.Core.Extensions;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Primitives;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.DependencyInjection;

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
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureMessagingEventHubsProcessorSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Messaging:EventHubs:{TClient}" section, where {TClient} is the type of Event Hubs client being configured, i.e. EventProcessorClient.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureMessagingEventHubsSettings.ConnectionString"/> nor <see cref="AzureMessagingEventHubsSettings.FullyQualifiedNamespace"/> is provided.</exception>
    public static void AddAzureEventProcessorClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureMessagingEventHubsProcessorSettings>? configureSettings = null,
        Action<IAzureClientBuilder<EventProcessorClient, EventProcessorClientOptions>>? configureClientBuilder = null)
    {
        new EventProcessorClientComponent()
            .AddClient(builder, DefaultConfigSectionName + nameof(EventProcessorClient),
                configureSettings, configureClientBuilder, connectionName, serviceKey: null);
    }

    /// <summary>
    /// Registers <see cref="EventProcessorClient"/> as a singleton for given <paramref name="name"/> in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureMessagingEventHubsProcessorSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Messaging:EventHubs:{TClient}" section, where {TClient} is the type of Event Hubs client being configured, i.e. EventProcessorClient.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureMessagingEventHubsSettings.ConnectionString"/> nor <see cref="AzureMessagingEventHubsSettings.FullyQualifiedNamespace"/> is provided.</exception>
    public static void AddKeyedAzureEventProcessorClient(
        this IHostApplicationBuilder builder,
        string name,
        Action<AzureMessagingEventHubsProcessorSettings>? configureSettings = null,
        Action<IAzureClientBuilder<EventProcessorClient, EventProcessorClientOptions>>? configureClientBuilder = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        string configurationSectionName = EventProcessorClientComponent
            .GetKeyedConfigurationSectionName(name, DefaultConfigSectionName +
                                                    nameof(EventProcessorClient));

        new EventProcessorClientComponent()
            .AddClient(builder, configurationSectionName, configureSettings,
                configureClientBuilder, connectionName: name, serviceKey: name);
    }

    /// <summary>
    /// Registers <see cref="PartitionReceiver"/> as a singleton in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureMessagingEventHubsPartitionReceiverSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Messaging:EventHubs:{TClient}" section, where {TClient} is the type of Event Hubs client being configured, i.e. EventProcessorClient.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureMessagingEventHubsSettings.ConnectionString"/> nor <see cref="AzureMessagingEventHubsSettings.FullyQualifiedNamespace"/> is provided.</exception>
    public static void AddAzurePartitionReceiverClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureMessagingEventHubsPartitionReceiverSettings>? configureSettings = null,
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
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureMessagingEventHubsPartitionReceiverSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Messaging:EventHubs:{TClient}" section, where {TClient} is the type of Event Hubs client being configured, i.e. EventProcessorClient.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureMessagingEventHubsSettings.ConnectionString"/> nor <see cref="AzureMessagingEventHubsSettings.FullyQualifiedNamespace"/> is provided.</exception>
    public static void AddKeyedAzurePartitionReceiverClient(
        this IHostApplicationBuilder builder,
        string name,
        Action<AzureMessagingEventHubsPartitionReceiverSettings>? configureSettings = null,
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
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureMessagingEventHubsProducerSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Messaging:EventHubs:{TClient}" section, where {TClient} is the type of Event Hubs client being configured, i.e. EventProcessorClient.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureMessagingEventHubsSettings.ConnectionString"/> nor <see cref="AzureMessagingEventHubsSettings.FullyQualifiedNamespace"/> is provided.</exception>
    public static void AddAzureEventHubProducerClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureMessagingEventHubsProducerSettings>? configureSettings = null,
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
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureMessagingEventHubsProducerSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Messaging:EventHubs:{TClient}" section, where {TClient} is the type of Event Hubs client being configured, i.e. EventProcessorClient.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureMessagingEventHubsSettings.ConnectionString"/> nor <see cref="AzureMessagingEventHubsSettings.FullyQualifiedNamespace"/> is provided.</exception>
    public static void AddKeyedAzureEventHubProducerClient(
        this IHostApplicationBuilder builder,
        string name,
        Action<AzureMessagingEventHubsProducerSettings>? configureSettings = null,
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
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureMessagingEventHubsConsumerSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Messaging:EventHubs:{TClient}" section, where {TClient} is the type of Event Hubs client being configured, i.e. EventProcessorClient.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureMessagingEventHubsSettings.ConnectionString"/> nor <see cref="AzureMessagingEventHubsSettings.FullyQualifiedNamespace"/> is provided.</exception>
    public static void AddAzureEventHubConsumerClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureMessagingEventHubsConsumerSettings>? configureSettings = null,
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
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureMessagingEventHubsConsumerSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Messaging:EventHubs:{TClient}" section, where {TClient} is the type of Event Hubs client being configured, i.e. EventProcessorClient.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureMessagingEventHubsSettings.ConnectionString"/> nor <see cref="AzureMessagingEventHubsSettings.FullyQualifiedNamespace"/> is provided.</exception>
    public static void AddKeyedAzureEventHubConsumerClient(
        this IHostApplicationBuilder builder,
        string name,
        Action<AzureMessagingEventHubsConsumerSettings>? configureSettings = null,
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

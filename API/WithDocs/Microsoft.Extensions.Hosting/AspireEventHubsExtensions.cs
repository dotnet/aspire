// Assembly 'Aspire.Azure.Messaging.EventHubs'

using System;
using Aspire.Azure.Messaging.EventHubs;
using Azure.Core.Extensions;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Primitives;
using Azure.Messaging.EventHubs.Producer;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering Event Hubs clients in the services provided by the <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" />.
/// </summary>
public static class AspireEventHubsExtensions
{
    /// <summary>
    /// Registers <see cref="T:Azure.Messaging.EventHubs.EventProcessorClient" /> as a singleton in the services provided by the<paramref name="builder" />.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="T:Aspire.Azure.Messaging.EventHubs.AzureMessagingEventHubsProcessorSettings" />. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="T:Azure.Core.Extensions.IAzureClientBuilder`2" />.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Messaging:EventHubs:{TClient}" section, where {TClient} is the type of Event Hubs client being configured, i.e. EventProcessorClient.</remarks>
    /// <exception cref="T:System.InvalidOperationException">Thrown when neither <see cref="P:Aspire.Azure.Messaging.EventHubs.AzureMessagingEventHubsBaseSettings.ConnectionString" /> nor <see cref="P:Aspire.Azure.Messaging.EventHubs.AzureMessagingEventHubsBaseSettings.Namespace" /> is provided.</exception>
    public static void AddAzureEventProcessorClient(this IHostApplicationBuilder builder, string connectionName, Action<AzureMessagingEventHubsProcessorSettings>? configureSettings = null, Action<IAzureClientBuilder<EventProcessorClient, EventProcessorClientOptions>>? configureClientBuilder = null);

    /// <summary>
    /// Registers <see cref="T:Azure.Messaging.EventHubs.EventProcessorClient" /> as a singleton for given <paramref name="name" /> in the services provided by the <paramref name="builder" />.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="P:Microsoft.Extensions.DependencyInjection.ServiceDescriptor.ServiceKey" /> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="T:Aspire.Azure.Messaging.EventHubs.AzureMessagingEventHubsProcessorSettings" />. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="T:Azure.Core.Extensions.IAzureClientBuilder`2" />.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Messaging:EventHubs:{TClient}" section, where {TClient} is the type of Event Hubs client being configured, i.e. EventProcessorClient.</remarks>
    /// <exception cref="T:System.InvalidOperationException">Thrown when neither <see cref="P:Aspire.Azure.Messaging.EventHubs.AzureMessagingEventHubsBaseSettings.ConnectionString" /> nor <see cref="P:Aspire.Azure.Messaging.EventHubs.AzureMessagingEventHubsBaseSettings.Namespace" /> is provided.</exception>
    public static void AddKeyedAzureEventProcessorClient(this IHostApplicationBuilder builder, string name, Action<AzureMessagingEventHubsProcessorSettings>? configureSettings = null, Action<IAzureClientBuilder<EventProcessorClient, EventProcessorClientOptions>>? configureClientBuilder = null);

    /// <summary>
    /// Registers <see cref="T:Azure.Messaging.EventHubs.Primitives.PartitionReceiver" /> as a singleton in the services provided by the <paramref name="builder" />.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="T:Aspire.Azure.Messaging.EventHubs.AzureMessagingEventHubsPartitionReceiverSettings" />. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="T:Azure.Core.Extensions.IAzureClientBuilder`2" />.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Messaging:EventHubs:{TClient}" section, where {TClient} is the type of Event Hubs client being configured, i.e. EventProcessorClient.</remarks>
    /// <exception cref="T:System.InvalidOperationException">Thrown when neither <see cref="P:Aspire.Azure.Messaging.EventHubs.AzureMessagingEventHubsBaseSettings.ConnectionString" /> nor <see cref="P:Aspire.Azure.Messaging.EventHubs.AzureMessagingEventHubsBaseSettings.Namespace" /> is provided.</exception>
    public static void AddAzurePartitionReceiverClient(this IHostApplicationBuilder builder, string connectionName, Action<AzureMessagingEventHubsPartitionReceiverSettings>? configureSettings = null, Action<IAzureClientBuilder<PartitionReceiver, PartitionReceiverOptions>>? configureClientBuilder = null);

    /// <summary>
    /// Registers <see cref="T:Azure.Messaging.EventHubs.Primitives.PartitionReceiver" /> as a singleton for given <paramref name="name" /> in the services provided by the <paramref name="builder" />.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="P:Microsoft.Extensions.DependencyInjection.ServiceDescriptor.ServiceKey" /> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="T:Aspire.Azure.Messaging.EventHubs.AzureMessagingEventHubsPartitionReceiverSettings" />. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="T:Azure.Core.Extensions.IAzureClientBuilder`2" />.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Messaging:EventHubs:{TClient}" section, where {TClient} is the type of Event Hubs client being configured, i.e. EventProcessorClient.</remarks>
    /// <exception cref="T:System.InvalidOperationException">Thrown when neither <see cref="P:Aspire.Azure.Messaging.EventHubs.AzureMessagingEventHubsBaseSettings.ConnectionString" /> nor <see cref="P:Aspire.Azure.Messaging.EventHubs.AzureMessagingEventHubsBaseSettings.Namespace" /> is provided.</exception>
    public static void AddKeyedAzurePartitionReceiverClient(this IHostApplicationBuilder builder, string name, Action<AzureMessagingEventHubsPartitionReceiverSettings>? configureSettings = null, Action<IAzureClientBuilder<PartitionReceiver, PartitionReceiverOptions>>? configureClientBuilder = null);

    /// <summary>
    /// Registers <see cref="T:Azure.Messaging.EventHubs.Producer.EventHubProducerClient" /> as a singleton in the services provided by the <paramref name="builder" />.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="T:Aspire.Azure.Messaging.EventHubs.AzureMessagingEventHubsProducerSettings" />. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="T:Azure.Core.Extensions.IAzureClientBuilder`2" />.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Messaging:EventHubs:{TClient}" section, where {TClient} is the type of Event Hubs client being configured, i.e. EventProcessorClient.</remarks>
    /// <exception cref="T:System.InvalidOperationException">Thrown when neither <see cref="P:Aspire.Azure.Messaging.EventHubs.AzureMessagingEventHubsBaseSettings.ConnectionString" /> nor <see cref="P:Aspire.Azure.Messaging.EventHubs.AzureMessagingEventHubsBaseSettings.Namespace" /> is provided.</exception>
    public static void AddAzureEventHubProducerClient(this IHostApplicationBuilder builder, string connectionName, Action<AzureMessagingEventHubsProducerSettings>? configureSettings = null, Action<IAzureClientBuilder<EventHubProducerClient, EventHubProducerClientOptions>>? configureClientBuilder = null);

    /// <summary>
    /// Registers <see cref="T:Azure.Messaging.EventHubs.Producer.EventHubProducerClient" /> as a singleton for given <paramref name="name" /> in the services provided by the <paramref name="builder" />.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="P:Microsoft.Extensions.DependencyInjection.ServiceDescriptor.ServiceKey" /> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="T:Aspire.Azure.Messaging.EventHubs.AzureMessagingEventHubsProducerSettings" />. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="T:Azure.Core.Extensions.IAzureClientBuilder`2" />.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Messaging:EventHubs:{TClient}" section, where {TClient} is the type of Event Hubs client being configured, i.e. EventProcessorClient.</remarks>
    /// <exception cref="T:System.InvalidOperationException">Thrown when neither <see cref="P:Aspire.Azure.Messaging.EventHubs.AzureMessagingEventHubsBaseSettings.ConnectionString" /> nor <see cref="P:Aspire.Azure.Messaging.EventHubs.AzureMessagingEventHubsBaseSettings.Namespace" /> is provided.</exception>
    public static void AddKeyedAzureEventHubProducerClient(this IHostApplicationBuilder builder, string name, Action<AzureMessagingEventHubsProducerSettings>? configureSettings = null, Action<IAzureClientBuilder<EventHubProducerClient, EventHubProducerClientOptions>>? configureClientBuilder = null);

    /// <summary>
    /// Registers <see cref="T:Azure.Messaging.EventHubs.Consumer.EventHubConsumerClient" /> as a singleton in the services provided by the <paramref name="builder" />.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="T:Aspire.Azure.Messaging.EventHubs.AzureMessagingEventHubsConsumerSettings" />. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="T:Azure.Core.Extensions.IAzureClientBuilder`2" />.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Messaging:EventHubs:{TClient}" section, where {TClient} is the type of Event Hubs client being configured, i.e. EventProcessorClient.</remarks>
    /// <exception cref="T:System.InvalidOperationException">Thrown when neither <see cref="P:Aspire.Azure.Messaging.EventHubs.AzureMessagingEventHubsBaseSettings.ConnectionString" /> nor <see cref="P:Aspire.Azure.Messaging.EventHubs.AzureMessagingEventHubsBaseSettings.Namespace" /> is provided.</exception>
    public static void AddAzureEventHubConsumerClient(this IHostApplicationBuilder builder, string connectionName, Action<AzureMessagingEventHubsConsumerSettings>? configureSettings = null, Action<IAzureClientBuilder<EventHubConsumerClient, EventHubConsumerClientOptions>>? configureClientBuilder = null);

    /// <summary>
    /// Registers <see cref="T:Azure.Messaging.EventHubs.Consumer.EventHubConsumerClient" /> as a singleton for given <paramref name="name" /> in the services provided by the <paramref name="builder" />.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="P:Microsoft.Extensions.DependencyInjection.ServiceDescriptor.ServiceKey" /> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="T:Aspire.Azure.Messaging.EventHubs.AzureMessagingEventHubsConsumerSettings" />. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="T:Azure.Core.Extensions.IAzureClientBuilder`2" />.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Messaging:EventHubs:{TClient}" section, where {TClient} is the type of Event Hubs client being configured, i.e. EventProcessorClient.</remarks>
    /// <exception cref="T:System.InvalidOperationException">Thrown when neither <see cref="P:Aspire.Azure.Messaging.EventHubs.AzureMessagingEventHubsBaseSettings.ConnectionString" /> nor <see cref="P:Aspire.Azure.Messaging.EventHubs.AzureMessagingEventHubsBaseSettings.Namespace" /> is provided.</exception>
    public static void AddKeyedAzureEventHubConsumerClient(this IHostApplicationBuilder builder, string name, Action<AzureMessagingEventHubsConsumerSettings>? configureSettings = null, Action<IAzureClientBuilder<EventHubConsumerClient, EventHubConsumerClientOptions>>? configureClientBuilder = null);
}

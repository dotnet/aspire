// Assembly 'Aspire.Azure.Messaging.ServiceBus'

using System;
using Aspire.Azure.Common;
using Aspire.Azure.Messaging.ServiceBus;
using Azure.Core;
using Azure.Core.Extensions;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering <see cref="T:Azure.Messaging.ServiceBus.ServiceBusClient" /> as a singleton in the services provided by the <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" />.
/// </summary>
public static class AspireServiceBusExtensions
{
    /// <summary>
    /// Registers <see cref="T:Azure.Messaging.ServiceBus.ServiceBusClient" /> as a singleton in the services provided by the <paramref name="builder" />.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="T:Aspire.Azure.Messaging.ServiceBus.AzureMessagingServiceBusSettings" />. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="T:Azure.Core.Extensions.IAzureClientBuilder`2" />.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Messaging:ServiceBus" section.</remarks>
    /// <exception cref="T:System.InvalidOperationException">Thrown when neither <see cref="P:Aspire.Azure.Messaging.ServiceBus.AzureMessagingServiceBusSettings.ConnectionString" /> nor <see cref="P:Aspire.Azure.Messaging.ServiceBus.AzureMessagingServiceBusSettings.Namespace" /> is provided.</exception>
    public static void AddAzureServiceBusClient(this IHostApplicationBuilder builder, string connectionName, Action<AzureMessagingServiceBusSettings>? configureSettings = null, Action<IAzureClientBuilder<ServiceBusClient, ServiceBusClientOptions>>? configureClientBuilder = null);

    /// <summary>
    /// Registers <see cref="T:Azure.Messaging.ServiceBus.ServiceBusClient" /> as a singleton for given <paramref name="name" /> in the services provided by the <paramref name="builder" />.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="P:Microsoft.Extensions.DependencyInjection.ServiceDescriptor.ServiceKey" /> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="T:Aspire.Azure.Messaging.ServiceBus.AzureMessagingServiceBusSettings" />. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="T:Azure.Core.Extensions.IAzureClientBuilder`2" />.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Messaging:ServiceBus:{name}" section.</remarks>
    /// <exception cref="T:System.InvalidOperationException">Thrown when neither <see cref="P:Aspire.Azure.Messaging.ServiceBus.AzureMessagingServiceBusSettings.ConnectionString" /> nor <see cref="P:Aspire.Azure.Messaging.ServiceBus.AzureMessagingServiceBusSettings.Namespace" /> is provided.</exception>
    public static void AddKeyedAzureServiceBusClient(this IHostApplicationBuilder builder, string name, Action<AzureMessagingServiceBusSettings>? configureSettings = null, Action<IAzureClientBuilder<ServiceBusClient, ServiceBusClientOptions>>? configureClientBuilder = null);
}

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

public static class AspireServiceBusExtensions
{
    public static void AddAzureServiceBusClient(this IHostApplicationBuilder builder, string connectionName, Action<AzureMessagingServiceBusSettings>? configureSettings = null, Action<IAzureClientBuilder<ServiceBusClient, ServiceBusClientOptions>>? configureClientBuilder = null);
    public static void AddKeyedAzureServiceBusClient(this IHostApplicationBuilder builder, string name, Action<AzureMessagingServiceBusSettings>? configureSettings = null, Action<IAzureClientBuilder<ServiceBusClient, ServiceBusClientOptions>>? configureClientBuilder = null);
}

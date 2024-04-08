// Assembly 'Aspire.Azure.Storage.Queues'

using System;
using Aspire.Azure.Common;
using Aspire.Azure.Storage.Queues;
using Azure.Core;
using Azure.Core.Extensions;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

public static class AspireQueueStorageExtensions
{
    public static void AddAzureQueueClient(this IHostApplicationBuilder builder, string connectionName, Action<AzureStorageQueuesSettings>? configureSettings = null, Action<IAzureClientBuilder<QueueServiceClient, QueueClientOptions>>? configureClientBuilder = null);
    public static void AddKeyedAzureQueueClient(this IHostApplicationBuilder builder, string name, Action<AzureStorageQueuesSettings>? configureSettings = null, Action<IAzureClientBuilder<QueueServiceClient, QueueClientOptions>>? configureClientBuilder = null);
}

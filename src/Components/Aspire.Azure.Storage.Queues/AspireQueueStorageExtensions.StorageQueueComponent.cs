// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Aspire.Azure.Storage.Queues;
using Azure.Core;
using Azure.Core.Extensions;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Specialized;
using HealthChecks.Azure.Storage.Queues;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

public static partial class AspireQueueStorageExtensions
{
    private sealed partial class StorageQueueComponent : AzureComponent<AzureStorageQueueSettings, QueueClient, QueueClientOptions>
    {
        protected override IAzureClientBuilder<QueueClient, QueueClientOptions> AddClient(
            AzureClientFactoryBuilder azureFactoryBuilder, AzureStorageQueueSettings settings, string connectionName, string configurationSectionName)
        {
            return ((IAzureClientFactoryBuilderWithCredential)azureFactoryBuilder).RegisterClientFactory<QueueClient, QueueClientOptions>((options, cred) =>
            {
                if (string.IsNullOrEmpty(settings.QueueName))
                {
                    throw new InvalidOperationException($"The connection string '{connectionName}' does not exist or is missing the queue name.");
                }

                var connectionString = settings.ConnectionString;
                if (string.IsNullOrEmpty(connectionString) && settings.ServiceUri is null)
                {
                    throw new InvalidOperationException($"A QueueServiceClient could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}' or specify a 'ConnectionString' or 'ServiceUri' in the '{configurationSectionName}' configuration section.");
                }

                var queueServiceClient = !string.IsNullOrEmpty(connectionString) ? new QueueServiceClient(connectionString, options) :
                    cred is not null ? new QueueServiceClient(settings.ServiceUri, cred, options) :
                    new QueueServiceClient(settings.ServiceUri, options);

                var client = queueServiceClient.GetQueueClient(settings.QueueName);
                return client;
            }, requiresCredential: false);
        }

        protected override void BindClientOptionsToConfiguration(IAzureClientBuilder<QueueClient, QueueClientOptions> clientBuilder, IConfiguration configuration)
        {
#pragma warning disable IDE0200 // Remove unnecessary lambda expression - needed so the ConfigBinder Source Generator works
            clientBuilder.ConfigureOptions(options => configuration.Bind(options));
#pragma warning restore IDE0200
        }

        protected override void BindSettingsToConfiguration(AzureStorageQueueSettings settings, IConfiguration configuration)
        {
            configuration.Bind(settings);
        }

        protected override IHealthCheck CreateHealthCheck(QueueClient client, AzureStorageQueueSettings settings)
            => new AzureQueueStorageHealthCheck(client.GetParentQueueServiceClient(), new AzureQueueStorageHealthCheckOptions { QueueName = client.Name });

        protected override bool GetHealthCheckEnabled(AzureStorageQueueSettings settings)
            => !settings.DisableHealthChecks;

        protected override TokenCredential? GetTokenCredential(AzureStorageQueueSettings settings)
            => settings.Credential;

        protected override bool GetMetricsEnabled(AzureStorageQueueSettings settings)
            => false;

        protected override bool GetTracingEnabled(AzureStorageQueueSettings settings)
            => !settings.DisableTracing;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Aspire.Azure.Storage.Queues;
using Azure.Core;
using Azure.Core.Extensions;
using Azure.Storage.Queues;
using HealthChecks.Azure.Storage.Queues;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

public static partial class AspireQueueStorageExtensions
{
    private sealed class StorageQueueComponent : AzureComponent<AzureStorageQueuesSettings, QueueServiceClient, QueueClientOptions>
    {
        protected override IAzureClientBuilder<QueueServiceClient, QueueClientOptions> AddClient(
            AzureClientFactoryBuilder azureFactoryBuilder, AzureStorageQueuesSettings settings, string connectionName,
            string configurationSectionName)
        {
            return ((IAzureClientFactoryBuilderWithCredential)azureFactoryBuilder).RegisterClientFactory<QueueServiceClient, QueueClientOptions>((options, cred) =>
            {
                var connectionString = settings.ConnectionString;
                if (string.IsNullOrEmpty(connectionString) && settings.ServiceUri is null)
                {
                    throw new InvalidOperationException($"A QueueServiceClient could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}' or specify a 'ConnectionString' or 'ServiceUri' in the '{configurationSectionName}' configuration section.");
                }

                return !string.IsNullOrEmpty(connectionString)
                    ? new QueueServiceClient(connectionString, options)
                    : cred is not null
                        ? new QueueServiceClient(settings.ServiceUri, cred, options)
                        : new QueueServiceClient(settings.ServiceUri, options);
            }, requiresCredential: false);
        }

        protected override void BindClientOptionsToConfiguration(IAzureClientBuilder<QueueServiceClient, QueueClientOptions> clientBuilder, IConfiguration configuration)
        {
#pragma warning disable IDE0200 // Remove unnecessary lambda expression - needed so the ConfigBinder Source Generator works
            clientBuilder.ConfigureOptions(options => configuration.Bind(options));
#pragma warning restore IDE0200
        }

        protected override void BindSettingsToConfiguration(AzureStorageQueuesSettings settings, IConfiguration configuration)
        {
            configuration.Bind(settings);
        }

        protected override IHealthCheck CreateHealthCheck(QueueServiceClient client, AzureStorageQueuesSettings settings)
            => new AzureQueueStorageHealthCheck(client, new AzureQueueStorageHealthCheckOptions());

        protected override bool GetHealthCheckEnabled(AzureStorageQueuesSettings settings)
            => !settings.DisableHealthChecks;

        protected override TokenCredential? GetTokenCredential(AzureStorageQueuesSettings settings)
            => settings.Credential;

        protected override bool GetMetricsEnabled(AzureStorageQueuesSettings settings)
            => false;

        protected override bool GetTracingEnabled(AzureStorageQueuesSettings settings)
            => !settings.DisableTracing;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Aspire.Azure.Storage.Queues;
using Azure.Core;
using Azure.Core.Extensions;
using Azure.Storage.Queues;
using HealthChecks.Azure.Storage.Queues;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

public static class AspireQueueStorageExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Azure:Storage:Queues";

    /// <summary>
    /// Registers <see cref="QueueServiceClient"/> as a singleton in the services provided by the <paramref name="builder"/>.
    /// Enables retries, corresponding health check, logging and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureStorageQueuesSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{QueueServiceClient, QueueClientOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire.Azure.Storage.Queues" section.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="AzureStorageQueuesSettings.ServiceUri"/> is not provided.</exception>
    public static void AddAzureQueueService(
        this IHostApplicationBuilder builder,
        Action<AzureStorageQueuesSettings>? configureSettings = null,
        Action<IAzureClientBuilder<QueueServiceClient, QueueClientOptions>>? configureClientBuilder = null)
    {
        new StorageQueueComponent().AddClient(builder, DefaultConfigSectionName, configureSettings, configureClientBuilder, name: null);
    }

    /// <summary>
    /// Registers <see cref="QueueServiceClient"/> as a singleton for given <paramref name="name"/> in the services provided by the <paramref name="builder"/>.
    /// Enables retries, corresponding health check, logging and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureStorageQueuesSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{QueueServiceClient, QueueClientOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire.Azure.Storage.Queues:{name}" section.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="AzureStorageQueuesSettings.ServiceUri"/> is not provided.</exception>
    public static void AddAzureQueueService(
        this IHostApplicationBuilder builder,
        string name,
        Action<AzureStorageQueuesSettings>? configureSettings = null,
        Action<IAzureClientBuilder<QueueServiceClient, QueueClientOptions>>? configureClientBuilder = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        string configurationSectionName = StorageQueueComponent.GetKeyedConfigurationSectionName(name, DefaultConfigSectionName);

        new StorageQueueComponent().AddClient(builder, configurationSectionName, configureSettings, configureClientBuilder, name);
    }

    private sealed class StorageQueueComponent : AzureComponent<AzureStorageQueuesSettings, QueueServiceClient, QueueClientOptions>
    {
        protected override string[] ActivitySourceNames => new[] { "Azure.Storage.Queues.QueueClient" };

        protected override IAzureClientBuilder<QueueServiceClient, QueueClientOptions> AddClient<TBuilder>(TBuilder azureFactoryBuilder, AzureStorageQueuesSettings settings)
            => azureFactoryBuilder.AddQueueServiceClient(settings.ServiceUri);

        protected override IHealthCheck CreateHealthCheck(QueueServiceClient client, AzureStorageQueuesSettings settings)
            => new AzureQueueStorageHealthCheck(client, new AzureQueueStorageHealthCheckOptions());

        protected override bool GetHealthCheckEnabled(AzureStorageQueuesSettings settings)
            => settings.HealthChecks;

        protected override TokenCredential? GetTokenCredential(AzureStorageQueuesSettings settings)
            => settings.Credential;

        protected override bool GetTracingEnabled(AzureStorageQueuesSettings settings)
            => settings.Tracing;

        protected override void Validate(AzureStorageQueuesSettings settings, string configurationSectionName)
        {
            if (settings.ServiceUri is null)
            {
                throw new InvalidOperationException($"ServiceUri is missing. It should be provided under 'ServiceUri' key in '{configurationSectionName}' configuration section.");
            }
        }
    }
}

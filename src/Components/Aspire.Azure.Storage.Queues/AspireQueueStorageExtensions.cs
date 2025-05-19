// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Storage.Queues;
using Azure.Core.Extensions;
using Azure.Storage.Queues;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering <see cref="QueueServiceClient"/> as a singleton in the services provided by the <see cref="IHostApplicationBuilder"/>.
/// Enables retries, corresponding health check, logging and telemetry.
/// </summary>
public static partial class AspireQueueStorageExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Azure:Storage:Queues";

    /// <summary>
    ///  Registers <see cref="QueueServiceClient"/> as a singleton in the services provided by the <paramref name="builder"/>.
    ///  Enables retries, corresponding health check, logging and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">
    ///  An optional method that can be used for customizing the <see cref="AzureStorageQueuesSettings"/>. It's invoked after
    ///  the settings are read from the configuration.
    /// </param>
    /// <param name="configureClientBuilder">
    ///  An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.
    /// </param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Storage:Queues" section.</remarks>
    /// <exception cref="InvalidOperationException">
    ///  Neither <see cref="AzureStorageQueuesSettings.ConnectionString"/> nor <see cref="AzureStorageQueuesSettings.ServiceUri"/> is provided.
    /// </exception>
    public static void AddAzureQueueClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureStorageQueuesSettings>? configureSettings = null,
        Action<IAzureClientBuilder<QueueServiceClient, QueueClientOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        new StorageQueueComponent().AddClient(builder, DefaultConfigSectionName, configureSettings, configureClientBuilder, connectionName, serviceKey: null);
    }

    /// <summary>
    ///  Registers <see cref="QueueServiceClient"/> as a singleton for given <paramref name="name"/> in the services provided
    ///  by the <paramref name="builder"/>.
    ///  Enables retries, corresponding health check, logging and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">
    ///  The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service
    ///  and also to retrieve the connection string from the ConnectionStrings configuration section.
    /// </param>
    /// <param name="configureSettings">
    ///  An optional method that can be used for customizing the <see cref="AzureStorageQueuesSettings"/>.
    ///  It's invoked after the settings are read from the configuration.
    /// </param>
    /// <param name="configureClientBuilder">
    ///  An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.
    /// </param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Storage:Queues:{name}" section.</remarks>
    /// <exception cref="InvalidOperationException">
    ///  Neither <see cref="AzureStorageQueuesSettings.ConnectionString"/> nor <see cref="AzureStorageQueuesSettings.ServiceUri"/> is provided.
    /// </exception>
    public static void AddKeyedAzureQueueClient(
        this IHostApplicationBuilder builder,
        string name,
        Action<AzureStorageQueuesSettings>? configureSettings = null,
        Action<IAzureClientBuilder<QueueServiceClient, QueueClientOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        new StorageQueueComponent().AddClient(builder, DefaultConfigSectionName, configureSettings, configureClientBuilder, connectionName: name, serviceKey: name);
    }
}

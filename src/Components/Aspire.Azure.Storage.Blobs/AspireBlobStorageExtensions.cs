// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Storage.Blobs;
using Azure.Core.Extensions;
using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering <see cref="BlobServiceClient"/> as a singleton in the services provided by the <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static partial class AspireBlobStorageExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Azure:Storage:Blobs";

    /// <summary>
    /// Registers <see cref="BlobServiceClient"/> as a singleton in the services provided by the <paramref name="builder"/>.
    /// Enables retries, corresponding health check, logging and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureStorageBlobsSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Storage:Blobs" section.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureStorageBlobsSettings.ConnectionString"/> nor <see cref="AzureStorageBlobsSettings.ServiceUri"/> is provided.</exception>
    public static void AddAzureBlobClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureStorageBlobsSettings>? configureSettings = null,
        Action<IAzureClientBuilder<BlobServiceClient, BlobClientOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        new BlobStorageComponent().AddClient(builder, DefaultConfigSectionName, configureSettings, configureClientBuilder, connectionName, serviceKey: null);
    }

    /// <summary>
    /// Registers <see cref="BlobServiceClient"/> as a singleton for given <paramref name="name"/> in the services provided by the <paramref name="builder"/>.
    /// Enables retries, corresponding health check, logging and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureStorageBlobsSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Storage:Blobs:{name}" section.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureStorageBlobsSettings.ConnectionString"/> nor <see cref="AzureStorageBlobsSettings.ServiceUri"/> is provided.</exception>
    public static void AddKeyedAzureBlobClient(
        this IHostApplicationBuilder builder,
        string name,
        Action<AzureStorageBlobsSettings>? configureSettings = null,
        Action<IAzureClientBuilder<BlobServiceClient, BlobClientOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        new BlobStorageComponent().AddClient(builder, DefaultConfigSectionName, configureSettings, configureClientBuilder, connectionName: name, serviceKey: name);
    }

    /// <summary>
    /// Registers <see cref="BlobContainerClient"/> as a singleton in the services provided by the <paramref name="builder"/>.
    /// Enables retries, corresponding health check, logging and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureStorageBlobsSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Storage:Blobs" section.</remarks>
    /// <exception cref="InvalidOperationException">
    ///  Neither <see cref="AzureStorageBlobsSettings.ConnectionString"/> nor <see cref="AzureStorageBlobsSettings.ServiceUri"/> is provided.
    ///  - or -
    ///  <see cref="AzureBlobStorageContainerSettings.BlobContainerName"/> is not provided in the configuration section.
    /// </exception>
    public static void AddAzureBlobContainerClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureBlobStorageContainerSettings>? configureSettings = null,
        Action<IAzureClientBuilder<BlobContainerClient, BlobClientOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        new BlobStorageContainerComponent().AddClient(builder, DefaultConfigSectionName, configureSettings, configureClientBuilder, connectionName, serviceKey: null);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Aspire.Azure.Storage.Blobs;
using Azure.Core;
using Azure.Core.Extensions;
using Azure.Storage.Blobs;
using HealthChecks.Azure.Storage.Blobs;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

public static class AspireBlobStorageExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Azure:Storage:Blobs";

    /// <summary>
    /// Registers <see cref="BlobServiceClient"/> as a singleton in the services provided by the <paramref name="builder"/>.
    /// Enables retries, corresponding health check, logging and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureStorageBlobsSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{BlobServiceClient, BlobClientOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire.Azure.Storage.Blobs" section.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureStorageBlobsSettings.ConnectionString"/> nor <see cref="AzureStorageBlobsSettings.ServiceUri"/> is provided.</exception>
    public static void AddAzureBlobService(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureStorageBlobsSettings>? configureSettings = null,
        Action<IAzureClientBuilder<BlobServiceClient, BlobClientOptions>>? configureClientBuilder = null)
    {
        new BlobStorageComponent().AddClient(builder, DefaultConfigSectionName, configureSettings, configureClientBuilder, connectionName, serviceKey: null);
    }

    /// <summary>
    /// Registers <see cref="BlobServiceClient"/> as a singleton for given <paramref name="name"/> in the services provided by the <paramref name="builder"/>.
    /// Enables retries, corresponding health check, logging and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureStorageBlobsSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{BlobServiceClient, BlobClientOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire.Azure.Storage.Blobs:{name}" section.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureStorageBlobsSettings.ConnectionString"/> nor <see cref="AzureStorageBlobsSettings.ServiceUri"/> is provided.</exception>
    public static void AddKeyedAzureBlobService(
        this IHostApplicationBuilder builder,
        string name,
        Action<AzureStorageBlobsSettings>? configureSettings = null,
        Action<IAzureClientBuilder<BlobServiceClient, BlobClientOptions>>? configureClientBuilder = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        string configurationSectionName = BlobStorageComponent.GetKeyedConfigurationSectionName(name, DefaultConfigSectionName);

        new BlobStorageComponent().AddClient(builder, configurationSectionName, configureSettings, configureClientBuilder, connectionName: name, serviceKey: name);
    }

    private sealed class BlobStorageComponent : AzureComponent<AzureStorageBlobsSettings, BlobServiceClient, BlobClientOptions>
    {
        protected override IAzureClientBuilder<BlobServiceClient, BlobClientOptions> AddClient<TBuilder>(TBuilder azureFactoryBuilder, AzureStorageBlobsSettings settings)
        {
            var connectionString = settings.ConnectionString;

            return !string.IsNullOrEmpty(connectionString) ?
                azureFactoryBuilder.AddBlobServiceClient(connectionString) :
                azureFactoryBuilder.AddBlobServiceClient(settings.ServiceUri);
        }

        protected override IHealthCheck CreateHealthCheck(BlobServiceClient client, AzureStorageBlobsSettings settings)
            => new AzureBlobStorageHealthCheck(client);

        protected override bool GetHealthCheckEnabled(AzureStorageBlobsSettings settings)
            => settings.HealthChecks;

        protected override TokenCredential? GetTokenCredential(AzureStorageBlobsSettings settings)
            => settings.Credential;

        protected override bool GetTracingEnabled(AzureStorageBlobsSettings settings)
            => settings.Tracing;

        protected override void Validate(AzureStorageBlobsSettings settings, string connectionName, string configurationSectionName)
        {
            if (string.IsNullOrEmpty(settings.ConnectionString) && settings.ServiceUri is null)
            {
                throw new InvalidOperationException($"A BlobServiceClient could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}' or specify a 'ConnectionString' or 'ServiceUri' in the '{configurationSectionName}' configuration section.");
            }
        }
    }
}

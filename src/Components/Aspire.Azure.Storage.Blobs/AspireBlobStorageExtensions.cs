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
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureStorageBlobsSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{BlobServiceClient, BlobClientOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire.Azure.Storage.Blobs" section.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="AzureStorageBlobsSettings.ServiceUri"/> is not provided.</exception>
    public static void AddAzureBlobService(
        this IHostApplicationBuilder builder,
        Action<AzureStorageBlobsSettings>? configureSettings = null,
        Action<IAzureClientBuilder<BlobServiceClient, BlobClientOptions>>? configureClientBuilder = null)
    {
        new BlobStorageComponent().AddClient(builder, DefaultConfigSectionName, configureSettings, configureClientBuilder, name: null);
    }

    /// <summary>
    /// Registers <see cref="BlobServiceClient"/> as a singleton for given <paramref name="name"/> in the services provided by the <paramref name="builder"/>.
    /// Enables retries, corresponding health check, logging and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureStorageBlobsSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{BlobServiceClient, BlobClientOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire.Azure.Storage.Blobs:{name}" section.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="AzureStorageBlobsSettings.ServiceUri"/> is not provided.</exception>
    public static void AddAzureBlobService(
        this IHostApplicationBuilder builder,
        string name,
        Action<AzureStorageBlobsSettings>? configureSettings = null,
        Action<IAzureClientBuilder<BlobServiceClient, BlobClientOptions>>? configureClientBuilder = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        string configurationSectionName = BlobStorageComponent.GetKeyedConfigurationSectionName(name, DefaultConfigSectionName);

        new BlobStorageComponent().AddClient(builder, configurationSectionName, configureSettings, configureClientBuilder, name);
    }

    private sealed class BlobStorageComponent : AzureComponent<AzureStorageBlobsSettings, BlobServiceClient, BlobClientOptions>
    {
        protected override string[] ActivitySourceNames => new[] { "Azure.Storage.Blobs.BlobContainerClient" };

        protected override IAzureClientBuilder<BlobServiceClient, BlobClientOptions> AddClient<TBuilder>(TBuilder azureFactoryBuilder, AzureStorageBlobsSettings settings)
            => azureFactoryBuilder.AddBlobServiceClient(settings.ServiceUri);

        protected override IHealthCheck CreateHealthCheck(BlobServiceClient client, AzureStorageBlobsSettings settings)
            => new AzureBlobStorageHealthCheck(client);

        protected override bool GetHealthCheckEnabled(AzureStorageBlobsSettings settings)
            => settings.HealthChecks;

        protected override TokenCredential? GetTokenCredential(AzureStorageBlobsSettings settings)
            => settings.Credential;

        protected override bool GetTracingEnabled(AzureStorageBlobsSettings settings)
            => settings.Tracing;

        protected override void Validate(AzureStorageBlobsSettings settings, string configurationSectionName)
        {
            if (settings.ServiceUri is null)
            {
                throw new InvalidOperationException($"ServiceUri is missing. It should be provided under 'ServiceUri' key in '{configurationSectionName}' configuration section.");
            }
        }
    }
}

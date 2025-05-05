// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Aspire.Azure.Storage.Blobs;
using Azure.Core;
using Azure.Core.Extensions;
using Azure.Storage.Blobs;
using HealthChecks.Azure.Storage.Blobs;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering <see cref="BlobServiceClient"/> as a singleton in the services provided by the <see cref="IHostApplicationBuilder"/>.
/// </summary>
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

    private sealed class BlobStorageComponent : AzureComponent<AzureStorageBlobsSettings, BlobServiceClient, BlobClientOptions>
    {
        protected override IAzureClientBuilder<BlobServiceClient, BlobClientOptions> AddClient(
            AzureClientFactoryBuilder azureFactoryBuilder, AzureStorageBlobsSettings settings, string connectionName,
            string configurationSectionName)
        {
            return ((IAzureClientFactoryBuilderWithCredential)azureFactoryBuilder).RegisterClientFactory<BlobServiceClient, BlobClientOptions>((options, cred) =>
            {
                var connectionString = settings.ConnectionString;
                if (string.IsNullOrEmpty(connectionString) && settings.ServiceUri is null)
                {
                    throw new InvalidOperationException($"A BlobServiceClient could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}' or specify a 'ConnectionString' or 'ServiceUri' in the '{configurationSectionName}' configuration section.");
                }

                return !string.IsNullOrEmpty(connectionString) ? new BlobServiceClient(connectionString, options) :
                    cred is not null ? new BlobServiceClient(settings.ServiceUri, cred, options) :
                    new BlobServiceClient(settings.ServiceUri, options);
            }, requiresCredential: false);
        }

        protected override void BindClientOptionsToConfiguration(IAzureClientBuilder<BlobServiceClient, BlobClientOptions> clientBuilder, IConfiguration configuration)
        {
#pragma warning disable IDE0200 // Remove unnecessary lambda expression - needed so the ConfigBinder Source Generator works
            clientBuilder.ConfigureOptions(options => configuration.Bind(options));
#pragma warning restore IDE0200
        }

        protected override void BindSettingsToConfiguration(AzureStorageBlobsSettings settings, IConfiguration configuration)
        {
            configuration.Bind(settings);
        }

        protected override IHealthCheck CreateHealthCheck(BlobServiceClient client, AzureStorageBlobsSettings settings)
            => new AzureBlobStorageHealthCheck(client);

        protected override bool GetHealthCheckEnabled(AzureStorageBlobsSettings settings)
            => !settings.DisableHealthChecks;

        protected override TokenCredential? GetTokenCredential(AzureStorageBlobsSettings settings)
            => settings.Credential;

        protected override bool GetMetricsEnabled(AzureStorageBlobsSettings settings)
            => false;

        protected override bool GetTracingEnabled(AzureStorageBlobsSettings settings)
            => !settings.DisableTracing;
    }
}

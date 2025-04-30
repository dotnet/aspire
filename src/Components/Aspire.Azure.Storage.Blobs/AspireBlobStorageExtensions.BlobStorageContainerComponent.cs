// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Aspire.Azure.Storage.Blobs;
using Azure.Core;
using Azure.Core.Extensions;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

partial class AspireBlobStorageExtensions
{
    private sealed partial class BlobStorageContainerComponent : AzureComponent<AzureBlobStorageContainerSettings, BlobContainerClient, BlobClientOptions>
    {
        protected override IAzureClientBuilder<BlobContainerClient, BlobClientOptions> AddClient(
            AzureClientFactoryBuilder azureFactoryBuilder, AzureBlobStorageContainerSettings settings, string connectionName, string configurationSectionName)
        {
            return ((IAzureClientFactoryBuilderWithCredential)azureFactoryBuilder).RegisterClientFactory<BlobContainerClient, BlobClientOptions>((options, cred) =>
            {
                if (string.IsNullOrEmpty(settings.BlobContainerName))
                {
                    throw new InvalidOperationException($"The connection string '{connectionName}' does not exist or is missing the container name.");
                }

                var connectionString = settings.ConnectionString;
                if (string.IsNullOrEmpty(connectionString) && settings.ServiceUri is null)
                {
                    throw new InvalidOperationException($"A BlobServiceClient could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}' or specify a 'ConnectionString' or 'ServiceUri' in the '{configurationSectionName}' configuration section.");
                }

                var blobServiceClient = !string.IsNullOrEmpty(connectionString) ? new BlobServiceClient(connectionString, options) :
                    cred is not null ? new BlobServiceClient(settings.ServiceUri, cred, options) :
                    new BlobServiceClient(settings.ServiceUri, options);

                var containerClient = blobServiceClient.GetBlobContainerClient(settings.BlobContainerName);
                return containerClient;

            }, requiresCredential: false);
        }

        protected override void BindClientOptionsToConfiguration(IAzureClientBuilder<BlobContainerClient, BlobClientOptions> clientBuilder, IConfiguration configuration)
        {
#pragma warning disable IDE0200 // Remove unnecessary lambda expression - needed so the ConfigBinder Source Generator works
            clientBuilder.ConfigureOptions(options => configuration.Bind(options));
#pragma warning restore IDE0200
        }

        protected override void BindSettingsToConfiguration(AzureBlobStorageContainerSettings settings, IConfiguration configuration)
        {
            configuration.Bind(settings);
        }

        protected override IHealthCheck CreateHealthCheck(BlobContainerClient client, AzureBlobStorageContainerSettings settings)
            => new AzureBlobStorageContainerHealthCheck(client);

        protected override bool GetHealthCheckEnabled(AzureBlobStorageContainerSettings settings)
            => !settings.DisableHealthChecks;

        protected override TokenCredential? GetTokenCredential(AzureBlobStorageContainerSettings settings)
            => settings.Credential;

        protected override bool GetMetricsEnabled(AzureBlobStorageContainerSettings settings)
            => false;

        protected override bool GetTracingEnabled(AzureBlobStorageContainerSettings settings)
            => !settings.DisableTracing;
    }
}

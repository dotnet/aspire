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

partial class AspireBlobStorageExtensions
{
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

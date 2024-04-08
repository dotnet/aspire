// Assembly 'Aspire.Azure.Storage.Blobs'

using System;
using Aspire.Azure.Common;
using Aspire.Azure.Storage.Blobs;
using Azure.Core;
using Azure.Core.Extensions;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

public static class AspireBlobStorageExtensions
{
    public static void AddAzureBlobClient(this IHostApplicationBuilder builder, string connectionName, Action<AzureStorageBlobsSettings>? configureSettings = null, Action<IAzureClientBuilder<BlobServiceClient, BlobClientOptions>>? configureClientBuilder = null);
    public static void AddKeyedAzureBlobClient(this IHostApplicationBuilder builder, string name, Action<AzureStorageBlobsSettings>? configureSettings = null, Action<IAzureClientBuilder<BlobServiceClient, BlobClientOptions>>? configureClientBuilder = null);
}

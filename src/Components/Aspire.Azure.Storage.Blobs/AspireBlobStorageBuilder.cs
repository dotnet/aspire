// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Aspire.Azure.Storage.Blobs;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

/// <summary>
///  Represents a builder that can be used to register multiple blob container
///  instances against the same Azure Blob Storage connection.
/// </summary>
public sealed class AspireBlobStorageBuilder(
    IHostApplicationBuilder builder,
    string connectionName,
    string? serviceKey,
    AzureStorageBlobsSettings settings)
{
    /// <summary>
    ///  Registers <see cref="BlobContainerClient"/> as a singleton in the services provided by the parent Azure Blob Storage client.
    ///  Enables retries, corresponding health check, logging and telemetry.
    /// </summary>
    /// <param name="blobContainerName">
    ///  The name of the blob container, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve
    ///  the connection string from the ConnectionStrings configuration section.
    /// </param>
    /// <exception cref="InvalidOperationException">
    ///  Registered parent <see cref="BlobServiceClient"/> has different connection string.
    /// </exception>
    public AspireBlobStorageBuilder AddKeyedAzureBlobContainerClient(string blobContainerName)
    {
        ArgumentException.ThrowIfNullOrEmpty(blobContainerName);

        var rawConnectionString = builder.Configuration.GetConnectionString(connectionName);
        ArgumentException.ThrowIfNullOrEmpty(rawConnectionString);
        ((IConnectionStringSettings)settings).ParseConnectionString(rawConnectionString);

        // Note: the connection string validation is already performed when this builder was constructed.
        string connectionString = settings.ConnectionString!;

        builder.Services.AddKeyedSingleton(blobContainerName, (sp, _) =>
        {
            var blobServiceClient = string.IsNullOrWhiteSpace(serviceKey)
                ? sp.GetRequiredService<BlobServiceClient>()
                : sp.GetRequiredKeyedService<BlobServiceClient>(serviceKey);

            if ((!string.IsNullOrEmpty(connectionString) &&
                 !connectionString.Contains(blobServiceClient.Uri.OriginalString, StringComparison.InvariantCultureIgnoreCase))
                ||
                (settings.ServiceUri is not null &&
                !settings.ServiceUri.AbsolutePath.Equals(blobServiceClient.Uri.AbsolutePath, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new InvalidOperationException($"BlobServiceClient incorrectly registered.");
            }

            var containerClient = blobServiceClient.GetBlobContainerClient(blobContainerName);
            return containerClient;
        });

        return this;
    }
}

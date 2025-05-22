// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using Aspire.Azure.Common;

namespace Aspire.Azure.Storage.Blobs;

/// <summary>
/// Provides the client configuration settings for connecting to Azure Blob Storage container.
/// </summary>
public sealed partial class AzureBlobStorageContainerSettings : AzureStorageBlobsSettings, IConnectionStringSettings
{
    /// <summary>
    ///  Gets or sets the name of the blob container.
    /// </summary>
    public string? BlobContainerName { get; set; }

    void IConnectionStringSettings.ParseConnectionString(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return;
        }

        // NOTE: if ever these contants are changed, the AzureBlobStorageResource in Aspire.Hosting.Azure.Storage class should be updated as well.
        const string Endpoint = nameof(Endpoint);
        const string ContainerName = nameof(ContainerName);

        DbConnectionStringBuilder builder = new() { ConnectionString = connectionString };
        if (builder.TryGetValue(Endpoint, out var endpoint) && endpoint is string endpointValue && builder.TryGetValue(ContainerName, out var containerName))
        {
            // endpoint can be a URI (azure deployment) or key/value pairs (emulator)
            base.ParseConnectionStringInternal(endpointValue);
            BlobContainerName = containerName.ToString();
        }
    }
}

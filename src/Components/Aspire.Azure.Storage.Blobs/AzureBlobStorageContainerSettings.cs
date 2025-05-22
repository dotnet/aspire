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

        // NOTE: if ever these constants are changed, the AzureBlobStorageResource in Aspire.Hosting.Azure.Storage class should be updated as well.
        const string Endpoint = nameof(Endpoint);
        const string ContainerName = nameof(ContainerName);

        try
        {
            DbConnectionStringBuilder builder = new() { ConnectionString = connectionString };
            if (builder.TryGetValue(Endpoint, out var endpoint) && builder.TryGetValue(ContainerName, out var containerName))
            {
                // Remove any quotes around the endpoint value
                string endpointStr = endpoint?.ToString() ?? string.Empty;
                if (endpointStr.StartsWith("\"") && endpointStr.EndsWith("\"") && endpointStr.Length >= 2)
                {
                    endpointStr = endpointStr.Substring(1, endpointStr.Length - 2);
                }

                ConnectionString = endpointStr;
                BlobContainerName = containerName?.ToString();
            }
        }
        catch (ArgumentException ex)
        {
            // Rethrow with more context for easier troubleshooting
            throw new ArgumentException($"Invalid connection string format. Ensure it has the format 'Endpoint=value;ContainerName=value'. See inner exception for details.", ex);
        }
    }
}

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

        // First try to parse as a connection string in the format Endpoint=value;ContainerName=value
        try
        {
            DbConnectionStringBuilder builder = new() { ConnectionString = connectionString };
            if (builder.TryGetValue(Endpoint, out var endpoint) && builder.TryGetValue(ContainerName, out var containerName))
            {
                ConnectionString = endpoint?.ToString();
                BlobContainerName = containerName?.ToString();
                return; // Successfully parsed
            }
        }
        catch (ArgumentException)
        {
            // If this is not a valid connection string, it might be a direct URL endpoint
            // from a deployed environment - we'll handle it in the next step
        }

        // Handle the case where connectionString is a direct URL endpoint (for deployed environments)
        // In this case, we use it as the ConnectionString directly and expect the BlobContainerName
        // to be set separately through configuration
        if (Uri.TryCreate(connectionString, UriKind.Absolute, out _))
        {
            ConnectionString = connectionString;
            // BlobContainerName should be set through configuration
        }
        else
        {
            // If we get here, the string is neither a valid connection string nor a valid URI
            throw new ArgumentException($"Invalid connection string format. Expected either a connection string with format 'Endpoint=value;ContainerName=value' or a valid URI.");
        }
    }
}

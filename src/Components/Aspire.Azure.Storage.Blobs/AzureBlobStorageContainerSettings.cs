// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Text.RegularExpressions;
using Aspire.Azure.Common;

namespace Aspire.Azure.Storage.Blobs;

/// <summary>
/// Provides the client configuration settings for connecting to Azure Blob Storage container.
/// </summary>
public sealed partial class AzureBlobStorageContainerSettings : AzureStorageBlobsSettings, IConnectionStringSettings
{
    [GeneratedRegex(@"(?i)ContainerName\s*=\s*([^;]+);?", RegexOptions.IgnoreCase)]
    private static partial Regex ContainerNameRegex();

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

        DbConnectionStringBuilder builder = new() { ConnectionString = connectionString };

        if (builder.TryGetValue("ContainerName", out var containerName))
        {
            BlobContainerName = containerName?.ToString();

            // Remove the ContainerName property from the connection string as BlobServiceClient would fail to parse it.
            connectionString = ContainerNameRegex().Replace(connectionString, "");

            // NB: we can't remove ContainerName by using the DbConnectionStringBuilder as it would escape the AccountKey value
            // when the connection string is built and BlobServiceClient doesn't support escape sequences. 
        }

        // Connection string built from a URI? e.g., Endpoint=https://{account_name}.blob.core.windows.net;ContainerName=...;
        if (builder.TryGetValue("Endpoint", out var endpoint) && endpoint is string)
        {
            if (Uri.TryCreate(endpoint.ToString(), UriKind.Absolute, out var uri))
            {
                ServiceUri = uri;
            }
        }
        else
        {
            // Otherwise preserve the existing connection string
            ConnectionString = connectionString;
        }
    }
}

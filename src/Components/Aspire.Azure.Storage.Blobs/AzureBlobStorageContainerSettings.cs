// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Aspire.Azure.Common;

namespace Aspire.Azure.Storage.Blobs;

/// <summary>
/// Provides the client configuration settings for connecting to Azure Blob Storage container.
/// </summary>
public sealed partial class AzureBlobStorageContainerSettings : AzureStorageBlobsSettings, IConnectionStringSettings
{
    [GeneratedRegex(@"ContainerName=([^;]*);")]
    private static partial Regex ExtractContainerName();

    public string? BlobContainerName { get; set; }

    void IConnectionStringSettings.ParseConnectionString(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return;
        }

        // In the emulator mode, the connection string may look like:
        //
        //     DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=...;BlobEndpoint=http://127.0.0.1:5555/devstoreaccount1;ContainerName=<container_name>;
        //
        // When run against the real Azure resources, the connection string will look similar to:
        //
        //      https://<storage_name>.blob.core.windows.net/ContainerName=<container_name>;
        //
        // Retrieve the container name from the connection string, if it is present; and then
        // remove it as it will upset BlobServiceClient.

        if (ExtractContainerName().Match(connectionString) is var match)
        {
            BlobContainerName = match.Groups[1].Value;
            connectionString = connectionString.Replace(match.Value, string.Empty);
        }

        if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
        {
            ServiceUri = uri;
        }
        else
        {
            ConnectionString = connectionString;
        }
    }
}

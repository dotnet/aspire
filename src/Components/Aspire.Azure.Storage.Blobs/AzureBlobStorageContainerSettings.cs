// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Globalization;
using System.Text;
using Aspire.Azure.Common;

namespace Aspire.Azure.Storage.Blobs;

/// <summary>
/// Provides the client configuration settings for connecting to Azure Blob Storage container.
/// </summary>
public sealed class AzureBlobStorageContainerSettings : AzureStorageBlobsSettings, IConnectionStringSettings
{
    public string? BlobContainerName { get; set; }

    void IConnectionStringSettings.ParseConnectionString(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return;
        }

        if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
        {
            ServiceUri = uri;

            // TODO: how do we get the container name from the URI?
        }
        else
        {
            var connectionBuilder = new DbConnectionStringBuilder()
            {
                ConnectionString = connectionString
            };

            if (connectionBuilder.TryGetValue("ContainerName", out var containerValue))
            {
                BlobContainerName = (string)containerValue;

                // Remove it from the connection string, it is our custom property.
                connectionBuilder["ContainerName"] = null;
            }

            // We can't use connectionBuilder.ConnectionString here, because connectionBuilder escapes values
            // adding quotes and other characters, which upset the Azure SDK.
            // So, we have rebuilt the connection string manually.

            StringBuilder builder = new();
            foreach (string keyword in connectionBuilder.Keys)
            {
                builder.Append(CultureInfo.InvariantCulture, $"{keyword}={connectionBuilder[keyword]};");
            }

            ConnectionString = builder.ToString();
        }
    }
}

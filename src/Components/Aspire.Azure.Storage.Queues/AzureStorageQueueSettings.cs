// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;

namespace Aspire.Azure.Storage.Queues;

/// <summary>
/// Provides the client configuration settings for connecting to an Azure Storage queue.
/// </summary>
public sealed partial class AzureStorageQueueSettings : AzureStorageQueuesSettings, IConnectionStringSettings
{
    /// <summary>
    ///  Gets or sets the name of the blob container.
    /// </summary>
    public string? QueueName { get; set; }

    void IConnectionStringSettings.ParseConnectionString(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return;
        }

        var connectionStringBuilder = new StableConnectionStringBuilder(connectionString);

        if (connectionStringBuilder.TryGetValue("QueueName", out var containerName))
        {
            QueueName = containerName?.ToString();

            // Remove the QueueName property from the connection string as QueueServiceClient would fail to parse it.
            connectionStringBuilder.Remove("QueueName");

            connectionString = connectionStringBuilder.ConnectionString;
        }

        // Connection string built from a URI? E.g., Endpoint=https://{account_name}.queue.core.windows.net;QueueName=...;
        if (connectionStringBuilder.TryGetValue("Endpoint", out var endpoint) && endpoint is string)
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

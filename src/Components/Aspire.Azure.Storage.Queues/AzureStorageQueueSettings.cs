// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Text.RegularExpressions;
using Aspire.Azure.Common;

namespace Aspire.Azure.Storage.Queues;

/// <summary>
/// Provides the client configuration settings for connecting to Azure Storage queue.
/// </summary>
public sealed partial class AzureStorageQueueSettings : AzureStorageQueuesSettings, IConnectionStringSettings
{
    [GeneratedRegex(@"(?i)QueueName\s*=\s*([^;]+);?", RegexOptions.IgnoreCase)]
    private static partial Regex QueueNameRegex();

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

        DbConnectionStringBuilder builder = new() { ConnectionString = connectionString };

        if (builder.TryGetValue("QueueName", out var containerName))
        {
            QueueName = containerName?.ToString();

            // Remove the QueueName property from the connection string as QueueServiceClient would fail to parse it.
            connectionString = QueueNameRegex().Replace(connectionString, "");

            // NB: we can't remove QueueName by using the DbConnectionStringBuilder as it would escape the AccountKey value
            // when the connection string is built and QueueServiceClient doesn't support escape sequences. 
        }

        // Connection string built from a URI? E.g., Endpoint=https://{account_name}.queue.core.windows.net;QueueName=...;
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

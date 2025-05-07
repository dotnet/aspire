// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using Aspire.Azure.Common;

namespace Aspire.Azure.Storage.Queues;

/// <summary>
/// Provides the client configuration settings for connecting to Azure Storage queue.
/// </summary>
public sealed class AzureStorageQueueSettings : AzureStorageQueuesSettings, IConnectionStringSettings
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

        DbConnectionStringBuilder builder = new() { ConnectionString = connectionString };

        // NOTE: if ever these contants are changed, the AzureQueueStorageResource in Aspire.Hosting.Azure.Storage class should be updated as well.
        if (builder.TryGetValue("Endpoint", out var endpoint) && builder.TryGetValue("QueueName", out var queueName))
        {
            ConnectionString = endpoint.ToString();
            QueueName = queueName.ToString();
        }
    }
}

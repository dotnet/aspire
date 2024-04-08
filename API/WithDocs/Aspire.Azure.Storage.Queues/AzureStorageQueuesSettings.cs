// Assembly 'Aspire.Azure.Storage.Queues'

using System;
using System.Runtime.CompilerServices;
using Aspire.Azure.Common;
using Azure.Core;

namespace Aspire.Azure.Storage.Queues;

/// <summary>
/// Provides the client configuration settings for connecting to Azure Storage Queues.
/// </summary>
public sealed class AzureStorageQueuesSettings : IConnectionStringSettings
{
    /// <summary>
    /// Gets or sets the connection string used to connect to the blob service. 
    /// </summary>
    /// <remarks>
    /// If <see cref="P:Aspire.Azure.Storage.Queues.AzureStorageQueuesSettings.ConnectionString" /> is set, it overrides <see cref="P:Aspire.Azure.Storage.Queues.AzureStorageQueuesSettings.ServiceUri" /> and <see cref="P:Aspire.Azure.Storage.Queues.AzureStorageQueuesSettings.Credential" />.
    /// </remarks>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// A <see cref="T:System.Uri" /> referencing the queue service.
    /// This is likely to be similar to "https://{account_name}.queue.core.windows.net".
    /// </summary>
    /// <remarks>
    /// Must not contain shared access signature.
    /// Used along with <see cref="P:Aspire.Azure.Storage.Queues.AzureStorageQueuesSettings.Credential" /> to establish the connection.
    /// </remarks>
    public Uri? ServiceUri { get; set; }

    /// <summary>
    /// Gets or sets the credential used to authenticate to the Queues Storage.
    /// </summary>
    public TokenCredential? Credential { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the Queues Storage health check is enabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true" />.
    /// </value>
    public bool HealthChecks { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is enabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true" />.
    /// </value>
    public bool Tracing { get; set; }

    public AzureStorageQueuesSettings();
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Azure.Core;

namespace Aspire.Azure.Storage.Blobs;

/// <summary>
/// Provides the client configuration settings for connecting to Azure Blob Storage.
/// </summary>
public sealed class AzureStorageBlobsSettings : IConnectionStringSettings
{
    /// <summary>
    /// Gets or sets the connection string used to connect to the blob service. 
    /// </summary>
    /// <remarks>
    /// If <see cref="ConnectionString"/> is set, it overrides <see cref="ServiceUri"/> and <see cref="Credential"/>.
    /// </remarks>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// A <see cref="Uri"/> referencing the blob service.
    /// This is likely to be similar to "https://{account_name}.blob.core.windows.net".
    /// </summary>
    /// <remarks>
    /// Must not contain shared access signature.
    /// Used along with <see cref="Credential"/> to establish the connection.
    /// </remarks>
    public Uri? ServiceUri { get; set; }

    /// <summary>
    /// Gets or sets the credential used to authenticate to the Blob Storage.
    /// </summary>
    public TokenCredential? Credential { get; set; }

    /// <summary>
    /// <para>Gets or sets a boolean value that indicates whether the Blob Storage health check is enabled or not.</para>
    /// <value>
    /// The default value is <see langword="true"/>.
    /// </value>
    /// </summary>
    public bool HealthChecks { get; set; } = true;

    /// <summary>
    /// <para>Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is enabled or not.</para>
    /// <value>
    /// The default value is <see langword="true"/>.
    /// </value>
    /// </summary>
    public bool Tracing { get; set; } = true;

    void IConnectionStringSettings.ParseConnectionString(string? connectionString)
    {
        if (!string.IsNullOrEmpty(connectionString))
        {
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
}

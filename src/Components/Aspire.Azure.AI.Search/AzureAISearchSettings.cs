// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using Aspire.Azure.Common;
using Azure;
using Azure.Core;

namespace Aspire.Azure.AI.Search;

/// <summary>
/// Provides the client configuration settings for connecting to Azure AI Search.
/// </summary>
public sealed class AzureAISearchSettings : IConnectionStringSettings
{
    private const string ConnectionStringEndpoint = "Endpoint";
    private const string ConnectionStringIndexName = "IndexName";
    private const string ConnectionStringKey = "Key";

    /// <summary>
    /// Gets or sets a <see cref="Uri"/> referencing the AI Search endpoint.
    /// This is likely to be similar to "https://{search_service}.search.windows.net".
    /// </summary>
    /// <remarks>
    /// Must not contain shared access signature.
    /// Used along with <see cref="IndexName"/> and <see cref="Credential"/> or <see cref="Key"/> to establish the connection.
    /// </remarks>
    public Uri? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the name of the AI Search Index.
    /// </summary>
    public string? IndexName { get; set; }

    /// <summary>
    /// Gets or sets the credential used to authenticate to the Azure AI Search resource.
    /// </summary>
    public TokenCredential? Credential { get; set; }

    /// <summary>
    /// Gets or sets the key to use to authenticate to the Azure AI Search endpoint.
    /// </summary>
    /// <remarks>
    /// When defined it will use an <see cref="AzureKeyCredential"/> instance instead of <see cref="Credential"/>.
    /// </remarks>
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is enabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true"/>.
    /// </value>
    public bool Tracing { get; set; } = true;

    void IConnectionStringSettings.ParseConnectionString(string? connectionString)
    {
        var connectionBuilder = new DbConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        if (connectionBuilder.ContainsKey(ConnectionStringEndpoint) && Uri.TryCreate(connectionBuilder[ConnectionStringEndpoint].ToString(), UriKind.Absolute, out var serviceUri))
        {
            Endpoint = serviceUri;
        }

        if (connectionBuilder.ContainsKey(ConnectionStringIndexName))
        {
            IndexName = connectionBuilder[ConnectionStringIndexName].ToString();
        }

        if (connectionBuilder.ContainsKey(ConnectionStringKey))
        {
            Key = connectionBuilder[ConnectionStringKey].ToString();
        }
    }
}

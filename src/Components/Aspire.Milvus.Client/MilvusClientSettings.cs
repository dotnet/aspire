// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;

namespace Aspire.Milvus.Client;

/// <summary>
/// Provides the client configuration settings for connecting to a Milvus server using MilvusClient.
/// </summary>
public sealed class MilvusClientSettings
{
    private const string ConnectionStringEndpoint = "Endpoint";
    private const string ConnectionStringApiKey = "Key";
    private const string ConnectionStringDatabase = "Database";

    /// <summary>
    /// The endpoint URI string of the Milvus server to connect to.
    /// </summary>
    public Uri? Endpoint { get; set; }

    /// <summary>
    /// The auth Key of the Milvus server to connect to.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// The database name of the Milvus server to connect to.
    /// </summary>
    public string? Database { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the Milvus client health check is disabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false"/>.
    /// </value>
    public bool DisableHealthChecks { get; set; }

    internal void ParseConnectionString(string? connectionString)
    {
        if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
        {
            Endpoint = uri;
        }
        else
        {
            var connectionBuilder = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };

            if (connectionBuilder.ContainsKey(ConnectionStringEndpoint) && Uri.TryCreate(connectionBuilder[ConnectionStringEndpoint].ToString(), UriKind.Absolute, out var serviceUri))
            {
                Endpoint = serviceUri;
            }

            if (connectionBuilder.ContainsKey(ConnectionStringApiKey))
            {
                Key = connectionBuilder[ConnectionStringApiKey].ToString();
            }

            if (connectionBuilder.ContainsKey(ConnectionStringDatabase))
            {
                Database = connectionBuilder[ConnectionStringDatabase].ToString();
            }
        }
    }
}

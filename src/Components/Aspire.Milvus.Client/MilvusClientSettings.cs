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

    /// <summary>
    /// The endpoint URI string of the Milvus server to connect to.
    /// </summary>
    public Uri? Endpoint { get; set; }

    /// <summary>
    /// The auth Key of the Milvus server to connect to.
    /// </summary>
    public string? Key { get; set; }

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
        }
    }
}

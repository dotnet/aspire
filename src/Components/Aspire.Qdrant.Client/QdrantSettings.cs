// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;

namespace Aspire.Qdrant.Client;

public sealed class QdrantSettings
{
    private const string ConnectionStringEndpoint = "Endpoint";
    private const string ConnectionStringKey = "Key";

    /// <summary>
    /// The endpoint URI string of the Qdrant server to connect to.
    /// </summary>
    public Uri? Endpoint { get; set; }

    /// <summary>
    /// The API Key of the Qdrant server to connect to.
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

            if (connectionBuilder.ContainsKey(ConnectionStringKey))
            {
                Key = connectionBuilder[ConnectionStringKey].ToString();
            }
        }
    }
}

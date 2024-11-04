// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;

namespace Aspire.Qdrant.Client;

/// <summary>
/// Provides the client configuration settings for connecting to a Qdrant server using QdrantClient.
/// </summary>
public sealed class QdrantClientSettings
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

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the Qdrant client health check is disabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false"/>.
    /// </value>
    public bool DisableHealthChecks { get; set; }

    /// <summary>
    /// Gets or sets the timeout duration for the health check.
    /// </summary>
    /// <value>
    /// The default value is <see langword="null"/>.
    /// </value>
    public TimeSpan? HealthCheckTimeout { get; set; }

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

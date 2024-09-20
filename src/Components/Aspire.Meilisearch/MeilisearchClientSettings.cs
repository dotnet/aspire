// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;

namespace Aspire.Meilisearch;

/// <summary>
/// Provides the client configuration settings for connecting to a Meilisearch server using MeilisearchClient.
/// </summary>
public sealed class MeilisearchClientSettings
{
    private const string ConnectionStringEndpoint = "Endpoint";
    private const string ConnectionStringKey = "MasterKey";

    /// <summary>
    /// The endpoint URI string of the Meilisearch server to connect to.
    /// </summary>
    public Uri? Endpoint { get; set; }

    /// <summary>
    /// The Master Key of the Meilisearch server to connect to.
    /// </summary>
    public string? MasterKey { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the Meilisearch health check is disabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false"/>.
    /// </value>
    public bool DisableHealthChecks { get; set; }

    /// <summary>
    /// Gets or sets a integer value that indicates the Meilisearch health check timeout in milliseconds.
    /// </summary>
    public int? HealthCheckTimeout { get; set; }

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

            if (connectionBuilder.TryGetValue(ConnectionStringEndpoint, out var endpoint) && Uri.TryCreate(endpoint.ToString(), UriKind.Absolute, out var serviceUri))
            {
                Endpoint = serviceUri;
            }

            if (connectionBuilder.TryGetValue(ConnectionStringKey, out var masterKey))
            {
                MasterKey = masterKey.ToString();
            }
        }
    }
}

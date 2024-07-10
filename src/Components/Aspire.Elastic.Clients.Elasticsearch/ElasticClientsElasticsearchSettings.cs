// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;

namespace Aspire.Elastic.Clients.Elasticsearch;

/// <summary>
/// Provides the client configuration settings for connecting to a Elasticsearch using Elastic.Clients.Elasticsearch.
/// </summary>
public sealed class ElasticClientsElasticsearchSettings
{

    private const string ConnectionStringEndpoint = "Endpoint";
    private const string ConnectionStringApiKey = "ApiKey";
    private const string ConnectionStringCloudId = "CloudId";

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the Elasticsearch health check is disabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false"/>.
    /// </value>
    public bool DisableHealthChecks { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is disabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false"/>.
    /// </value>
    public bool DisableTracing { get; set; }

    /// <summary>
    /// Gets or sets a integer value that indicates the Elasticsearch health check timeout in milliseconds.
    /// </summary>
    public int? HealthCheckTimeout { get; set; }

    /// <summary>
    /// The endpoint URI string of the Elasticsearch to connect to.
    /// </summary>
    public Uri? Endpoint { get; set; }

    /// <summary>
    /// The API Key of the Elastic Cloud to connect to.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// The CloudId of the Elastic Cloud to connect to.
    /// </summary>
    public string? CloudId { get; set; }

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
                ApiKey = connectionBuilder[ConnectionStringApiKey].ToString();
            }

            if (connectionBuilder.ContainsKey(ConnectionStringCloudId))
            {
                CloudId = connectionBuilder[ConnectionStringCloudId].ToString();
            }
        }
    }
}

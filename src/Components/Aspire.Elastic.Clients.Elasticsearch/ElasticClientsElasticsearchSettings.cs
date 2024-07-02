// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//using Aspire;
namespace Aspire.Elastic.Clients.Elasticsearch;

public sealed class ElasticClientsElasticsearchSettings
{
    /// <summary>
    /// Gets or sets the comma-delimited configuration string used to connect to the Elasticsearch.
    /// </summary>
    public string? ConnectionString { get; set; }

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
    /// Gets or sets a boolean value that indicates whether the connection to elastic cloud or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// Set value <see langword="true"/> when you want use elastic cloud and provide api key and elasticId in  <see cref="ElasticClientsElasticsearchSettings.Cloud"/>
    /// </remarks>
    public bool UseCloud { get; set; }

    /// <summary>
    /// Gets or sets cloud connection setting that indicates the connection to elastic cloud.
    /// </summary>
    public ElasticClientsElasticsearchCloudSettings? Cloud { get; set; }
}

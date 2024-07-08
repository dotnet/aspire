// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Elastic.Clients.Elasticsearch;

/// <summary>
/// Provides the client configuration settings for connecting to a Elastic Cloud using Elastic.Clients.Elasticsearch.
/// </summary>
public sealed class ElasticClientsElasticsearchCloudSettings
{
    /// <summary>
    /// Gets or sets a string value that indicates the CloudId to use when connecting to elastic cloud.
    /// </summary>
    public string? CloudId { get; set; }

    /// <summary>
    /// Gets or sets a string value that indicates the ApiKey to use when connecting to elastic cloud.
    /// </summary>
    public string? ApiKey { get; set; } 
}

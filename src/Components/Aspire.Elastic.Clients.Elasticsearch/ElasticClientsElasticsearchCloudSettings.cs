// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//using Aspire;
namespace Aspire.Elastic.Clients.Elasticsearch;

public sealed class ElasticClientsElasticsearchCloudSettings
{
    /// <summary>
    /// Gets or sets a string value that indicates the CloudId to connecting elastic cloud.
    /// </summary>
    public string? CloudId { get; set; }

    /// <summary>
    /// Gets or sets a string value that indicates the ApiKey to connecting elastic cloud.
    /// </summary>
    public string? ApiKey { get; set; } 
}

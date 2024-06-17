// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Elastic.Clients.Elasticsearch.Tests;

public class ConfigurationTests
{
    [Fact]
    public void ConnectionStringIsNullByDefault() =>
        Assert.Null(new ElasticClientsElasticsearchSettings().ConnectionString);

    [Fact]
    public void HealthChecksEnabledByDefault() =>
        Assert.False(new ElasticClientsElasticsearchSettings().DisableHealthChecks);

    [Fact]
    public void TracingEnabledByDefault() =>
      Assert.False(new ElasticClientsElasticsearchSettings().DisableTracing);
}

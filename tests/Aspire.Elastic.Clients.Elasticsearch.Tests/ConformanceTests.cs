// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Components.ConformanceTests;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Elastic.Clients.Elasticsearch.Tests;

public class ConformanceTests : ConformanceTests<ElasticsearchClient, ElasticClientsElasticsearchSettings>, IClassFixture<ElasticsearchContainerFixture>
{
    private readonly ElasticsearchContainerFixture _containerFixture;

    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    protected override string ActivitySourceName => "";

    protected override string[] RequiredLogCategories => Array.Empty<string>();

    protected override bool CanConnectToServer => RequiresDockerAttribute.IsSupported;

    protected override bool SupportsKeyedRegistrations => true;

    public ConformanceTests(ElasticsearchContainerFixture containerFixture)
    {
        _containerFixture = containerFixture;
    }

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
    {
        var connectionString = RequiresDockerAttribute.IsSupported ?
          $"{_containerFixture.GetConnectionString()}" :
          "http://elastic:password@localhost:27017";

        configuration.AddInMemoryCollection(
            [
            new KeyValuePair<string, string?>(CreateConfigKey("Aspire:Elastic:Clients:Elasticsearch", key, "ConnectionString"),connectionString),
            new KeyValuePair<string, string?>($"ConnectionStrings:{key}",connectionString)
            ]);
    }

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<ElasticClientsElasticsearchSettings>? configure = null, string? key = null)
    {
        if (key is null)
        {
            builder.AddElasticClientsElasticsearch("elasticsearch", configure);
        }
        else
        {
            builder.AddKeyedElasticClientsElasticsearch(key, configure);
        }
    }

    protected override void SetHealthCheck(ElasticClientsElasticsearchSettings options, bool enabled)
    {
        options.DisableHealthChecks = !enabled;
        options.HealthCheckTimeout = 100;
    }

    protected override void SetMetrics(ElasticClientsElasticsearchSettings options, bool enabled)
    {
        throw new NotImplementedException();
    }

    protected override void SetTracing(ElasticClientsElasticsearchSettings options, bool enabled)
    {
        options.DisableTracing = !enabled;
    }

    protected override void TriggerActivity(ElasticsearchClient service)
    {
        using var source = new CancellationTokenSource(100);

        service.InfoAsync(source.Token).Wait();
    }

    [Fact]
    public void CanAddMultipleKeyedServices()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:elasticsearch1", "http://elastic:password@localhost1:19530"),
            new KeyValuePair<string, string?>("ConnectionStrings:elasticsearch2", "http://elastic:password@localhost1:19531"),
            new KeyValuePair<string, string?>("ConnectionStrings:elasticsearch3", "http://elastic:password@localhost1:19532"),
        ]);

        builder.AddElasticClientsElasticsearch("elasticsearch1");
        builder.AddKeyedElasticClientsElasticsearch("elasticsearch2");
        builder.AddKeyedElasticClientsElasticsearch("elasticsearch3");

        using var host = builder.Build();

        var client1 = host.Services.GetRequiredService<ElasticsearchClient>();
        var client2 = host.Services.GetRequiredKeyedService<ElasticsearchClient>("milvus2");
        var client3 = host.Services.GetRequiredKeyedService<ElasticsearchClient>("milvus3");

        Assert.NotSame(client1, client2);
        Assert.NotSame(client1, client3);
        Assert.NotSame(client2, client3);
    }
}

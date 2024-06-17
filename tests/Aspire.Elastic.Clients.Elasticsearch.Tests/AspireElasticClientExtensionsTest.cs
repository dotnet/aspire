// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Elastic.Clients.Elasticsearch.Tests;

public class AspireElasticClientExtensionsTest : IClassFixture<ElasticsearchContainerFixture>
{

    private const string DefaultConnectionName = "elasticsearch";

    private readonly ElasticsearchContainerFixture _containerFixture;

    public AspireElasticClientExtensionsTest(ElasticsearchContainerFixture containerFixture)
    {
        _containerFixture = containerFixture;
    }
    private string DefaultConnectionString => _containerFixture.GetConnectionString();

    [Fact]
    [RequiresDocker]
    public void AddKeyedElasticsearchClient_HealthCheckShouldNotBeRegisteredWhenDisabled()
    {
        var builder = CreateBuilder(DefaultConnectionString);

        builder.AddKeyedElasticClientsElasticsearch(DefaultConnectionName, settings =>
        {
            settings.DisableHealthChecks = true;
        });

        using var host = builder.Build();

        var healthCheckService = host.Services.GetService<HealthCheckService>();

        Assert.Null(healthCheckService);

    }

    [Fact]
    [RequiresDocker]
    public async Task AddElasticsearchClient_HealthCheckShouldBeRegisteredWhenEnabled()
    {
        var builder = CreateBuilder(DefaultConnectionString);

        builder.AddElasticClientsElasticsearch(DefaultConnectionName, settings =>
        {
            settings.DisableHealthChecks = false;
            settings.HealthCheckTimeout = 1;
        });

        using var host = builder.Build();

        var healthCheckService = host.Services.GetRequiredService<HealthCheckService>();

        var healthCheckReport = await healthCheckService.CheckHealthAsync();

        var healthCheckName = "Elastic.Clients.Elasticsearch";

        Assert.Contains(healthCheckReport.Entries, x => x.Key == healthCheckName);
    }

    [Fact]
    [RequiresDocker]
    public async Task AddKeyedElasticsearchClient_HealthCheckShouldBeRegisteredWhenEnabled()
    {
        var key = DefaultConnectionName;

        var builder = CreateBuilder(DefaultConnectionString);

        builder.AddKeyedElasticClientsElasticsearch(key, settings =>
        {
            settings.DisableHealthChecks = false;
            settings.HealthCheckTimeout = 1;
        });

        using var host = builder.Build();

        var healthCheckService = host.Services.GetRequiredService<HealthCheckService>();

        var healthCheckReport = await healthCheckService.CheckHealthAsync();

        var healthCheckName = $"Elastic.Clients.Elasticsearch_{key}";

        Assert.Contains(healthCheckReport.Entries, x => x.Key == healthCheckName);
    }

    [Fact]
    [RequiresDocker]
    public void AddElasticsearchClient_HealthCheckShouldNotBeRegisteredWhenDisabled()
    {
        var builder = CreateBuilder(DefaultConnectionString);

        builder.AddElasticClientsElasticsearch(DefaultConnectionName, settings =>
        {
            settings.DisableHealthChecks = true;
        });

        using var host = builder.Build();

        var healthCheckService = host.Services.GetService<HealthCheckService>();

        Assert.Null(healthCheckService);
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

    private static HostApplicationBuilder CreateBuilder(string connectionString)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>($"ConnectionStrings:{DefaultConnectionName}", connectionString)
        ]);
        return builder;
    }
}

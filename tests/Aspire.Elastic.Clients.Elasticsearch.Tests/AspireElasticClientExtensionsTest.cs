// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.TestUtilities;
using Elastic.Clients.Elasticsearch;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;
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

    private string DefaultConnectionString =>
            RequiresDockerAttribute.IsSupported ? _containerFixture.GetConnectionString() : "http://elastic:password@localhost:27011";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequiresDocker]
    public async Task AddElasticsearchClient_HealthCheckShouldBeRegisteredWhenEnabled(bool useKeyed)
    {
        var key = DefaultConnectionName;

        var builder = CreateBuilder(DefaultConnectionString);

        if (useKeyed)
        {
            builder.AddKeyedElasticsearchClient(key, settings =>
            {
                settings.DisableHealthChecks = false;
            });
        }
        else
        {
            builder.AddElasticsearchClient(DefaultConnectionName, settings =>
            {
                settings.DisableHealthChecks = false;
            });
        }

        using var host = builder.Build();

        var healthCheckService = host.Services.GetRequiredService<HealthCheckService>();

        var healthCheckReport = await healthCheckService.CheckHealthAsync(TestContext.Current.CancellationToken);

        var healthCheckName = useKeyed ? $"Elastic.Clients.Elasticsearch_{key}" : "Elastic.Clients.Elasticsearch";

        Assert.Contains(healthCheckReport.Entries, x => x.Key == healthCheckName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddElasticsearchClient_HealthCheckShouldNotBeRegisteredWhenDisabled(bool useKeyed)
    {
        var builder = CreateBuilder(DefaultConnectionString);

        if (useKeyed)
        {
            builder.AddKeyedElasticsearchClient(DefaultConnectionName, settings =>
            {
                settings.DisableHealthChecks = true;
            });
        }
        else
        {
            builder.AddElasticsearchClient(DefaultConnectionName, settings =>
            {
                settings.DisableHealthChecks = true;
            });
        }

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

        builder.AddElasticsearchClient("elasticsearch1");
        builder.AddKeyedElasticsearchClient("elasticsearch2");
        builder.AddKeyedElasticsearchClient("elasticsearch3");

        using var host = builder.Build();

        var client1 = host.Services.GetRequiredService<ElasticsearchClient>();
        var client2 = host.Services.GetRequiredKeyedService<ElasticsearchClient>("elasticsearch2");
        var client3 = host.Services.GetRequiredKeyedService<ElasticsearchClient>("elasticsearch3");

        Assert.NotSame(client1, client2);
        Assert.NotSame(client1, client3);
        Assert.NotSame(client2, client3);
    }

    [Fact]
    public void CanAddClientFromEncodedConnectionString()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:elasticsearch1", "Endpoint=http://elastic:password@localhost1:19530"),
            new KeyValuePair<string, string?>("ConnectionStrings:elasticsearch2", "Endpoint=http://localhost1:19531"),
        ]);

        builder.AddElasticsearchClient("elasticsearch1");
        builder.AddKeyedElasticsearchClient("elasticsearch2");

        using var host = builder.Build();

        var client1 = host.Services.GetRequiredService<ElasticsearchClient>();
        var client2 = host.Services.GetRequiredKeyedService<ElasticsearchClient>("elasticsearch2");

        Assert.NotSame(client1, client2);
    }

    [Fact]
    [RequiresDocker]
    public void ElasticsearchInstrumentationEndToEnd()
    {
        // RemoteExecutor is used because OTEL uses a static instance to capture activities

        RemoteExecutor.Invoke(async (connectionString) =>
        {
            var builder = CreateBuilder(connectionString);

            builder.AddElasticsearchClient(DefaultConnectionName);

            using var notifier = new ActivityNotifier();
            builder.Services.AddOpenTelemetry().WithTracing(builder => builder.AddProcessor(notifier));

            using var host = builder.Build();
            host.Start();

            var elasticsearchClient = host.Services.GetRequiredService<ElasticsearchClient>();
            await elasticsearchClient.PingAsync();

            var activityList = await notifier.TakeAsync(1, TimeSpan.FromSeconds(10));
            Assert.Single(activityList);

            var activity = activityList[0];
            Assert.Equal("ping", activity.DisplayName);
            Assert.Equal("HEAD", activity.OperationName);
            Assert.Contains(activity.Tags, kvp => kvp.Key == "db.system" && kvp.Value == "elasticsearch");
        }, DefaultConnectionString, new RemoteInvokeOptions { TimeOut = 120_000 }).Dispose();
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

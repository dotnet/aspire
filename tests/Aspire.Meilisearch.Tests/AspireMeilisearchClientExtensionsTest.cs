// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Meilisearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Meilisearch.Tests;

public class AspireMeilisearchClientExtensionsTest : IClassFixture<MeilisearchContainerFixture>
{
    private const string DefaultConnectionName = "meilisearch";

    private readonly MeilisearchContainerFixture _containerFixture;

    public AspireMeilisearchClientExtensionsTest(MeilisearchContainerFixture containerFixture)
    {
        _containerFixture = containerFixture;
    }

    private string DefaultConnectionString =>
            RequiresDockerAttribute.IsSupported ? _containerFixture.GetConnectionString() : "http://localhost:27011";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequiresDocker]
    public async Task AddMeilisearchClient_HealthCheckShouldBeRegisteredWhenEnabled(bool useKeyed)
    {
        var key = DefaultConnectionName;

        var builder = CreateBuilder(DefaultConnectionString);

        if (useKeyed)
        {
            builder.AddKeyedMeilisearchClient(key, settings =>
            {
                settings.DisableHealthChecks = false;
            });
        }
        else
        {
            builder.AddMeilisearchClient(DefaultConnectionName, settings =>
            {
                settings.DisableHealthChecks = false;
            });
        }

        using var host = builder.Build();

        var healthCheckService = host.Services.GetRequiredService<HealthCheckService>();

        var healthCheckReport = await healthCheckService.CheckHealthAsync();

        var healthCheckName = useKeyed ? $"Meilisearch_{key}" : "Meilisearch";

        Assert.Contains(healthCheckReport.Entries, x => x.Key == healthCheckName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddMeilisearchClient_HealthCheckShouldNotBeRegisteredWhenDisabled(bool useKeyed)
    {
        var builder = CreateBuilder(DefaultConnectionString);

        if (useKeyed)
        {
            builder.AddKeyedMeilisearchClient(DefaultConnectionName, settings =>
            {
                settings.DisableHealthChecks = true;
            });
        }
        else
        {
            builder.AddMeilisearchClient(DefaultConnectionName, settings =>
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
            new KeyValuePair<string, string?>("ConnectionStrings:meilisearch1", "http://localhost:19530"),
            new KeyValuePair<string, string?>("ConnectionStrings:meilisearch2", "http://localhost:19531"),
            new KeyValuePair<string, string?>("ConnectionStrings:meilisearch3", "http://localhost:19532"),
        ]);

        builder.AddMeilisearchClient("meilisearch1");
        builder.AddKeyedMeilisearchClient("meilisearch2");
        builder.AddKeyedMeilisearchClient("meilisearch3");

        using var host = builder.Build();

        var client1 = host.Services.GetRequiredService<MeilisearchClient>();
        var client2 = host.Services.GetRequiredKeyedService<MeilisearchClient>("meilisearch2");
        var client3 = host.Services.GetRequiredKeyedService<MeilisearchClient>("meilisearch3");

        Assert.NotSame(client1, client2);
        Assert.NotSame(client1, client3);
        Assert.NotSame(client2, client3);
    }

    [Fact]
    public void CanAddClientFromEncodedConnectionString()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:meilisearch1", "Endpoint=http://localhost:19530;MasterKey=p@ssw0rd1"),
            new KeyValuePair<string, string?>("ConnectionStrings:meilisearch2", "Endpoint=http://localhost:19531;MasterKey=p@ssw0rd1"),
        ]);

        builder.AddMeilisearchClient("meilisearch1");
        builder.AddKeyedMeilisearchClient("meilisearch2");

        using var host = builder.Build();

        var client1 = host.Services.GetRequiredService<MeilisearchClient>();
        var client2 = host.Services.GetRequiredKeyedService<MeilisearchClient>("meilisearch2");

        Assert.NotSame(client1, client2);
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using Xunit;

namespace Aspire.Hosting.Garnet.Tests;

public class GarnetFunctionalTests
{
    [Fact]
    [RequiresDocker]
    public async Task VerifyGarnetResource()
    {
        var builder = CreateDistributedApplicationBuilder();

        var garnet = builder.AddGarnet("garnet");

        using var app = builder.Build();

        await app.StartAsync();

        var hb = Host.CreateApplicationBuilder();

        hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{garnet.Resource.Name}"] = await garnet.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None)
        });

        hb.AddRedisClient(garnet.Resource.Name);

        using var host = hb.Build();

        await host.StartAsync();

        var redisClient = host.Services.GetRequiredService<IConnectionMultiplexer>();

        var db = redisClient.GetDatabase();

        await db.StringSetAsync("key", "value");

        var value = await db.StringGetAsync("key");

        Assert.Equal("value", value);
    }

    private static TestDistributedApplicationBuilder CreateDistributedApplicationBuilder() =>
        TestDistributedApplicationBuilder.CreateWithTestContainerRegistry();
}

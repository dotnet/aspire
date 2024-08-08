// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using StackExchange.Redis;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Garnet.Tests;

public class GarnetFunctionalTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    [RequiresDocker]
    public async Task VerifyGarnetResource()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var pipeline = new ResiliencePipelineBuilder()
           .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(3) })
           .Build();

        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(testOutputHelper);

        var garnet = builder.AddGarnet("garnet");

        using var app = builder.Build();

        await app.StartAsync();

        var hb = Host.CreateApplicationBuilder();

        hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{garnet.Resource.Name}"] = await garnet.Resource.ConnectionStringExpression.GetValueAsync(default)
        });

        hb.AddRedisClient(garnet.Resource.Name);

        using var host = hb.Build();

        await host.StartAsync();

        var redisClient = host.Services.GetRequiredService<IConnectionMultiplexer>();

        await pipeline.ExecuteAsync(async token =>
         {
             var db = redisClient.GetDatabase();

             await db.StringSetAsync("key", "value");

             var value = await db.StringGetAsync("key");

             Assert.Equal("value", value);

         }, cts.Token);
    }
}

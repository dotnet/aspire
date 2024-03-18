// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.StackExchange.Redis.Tests;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Instrumentation.StackExchangeRedis;
using OpenTelemetry.Trace;
using Xunit;

namespace Aspire.StackExchange.Redis.OutputCaching.Tests;

public class OutputCacheConformanceTests : ConformanceTests
{
    // Schema only references Aspire.StackExchange.Redis' schema so nothing
    // specific to check here
    protected override (string json, string error)[] InvalidJsonToErrorMessage => Array.Empty<(string json, string error)>();

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<StackExchangeRedisSettings>? configure = null, string? key = null)
    {
        if (key is null)
        {
            builder.AddRedisOutputCache("redis", configure);
        }
        else
        {
            builder.AddKeyedRedisOutputCache(key, configure);
        }
    }

    public OutputCacheConformanceTests(RedisContainerFixture containerFixture) : base(containerFixture)
    {
    }

    [ConditionalFact]
    public async Task WorksWithOpenTelemetryTracing()
    {
        SkipIfCanNotConnectToServer();

        var builder = CreateHostBuilder();

        builder.AddRedisOutputCache("redis");

        var notifier = new ActivityNotifier();
        builder.Services.AddOpenTelemetry().WithTracing(builder => builder.AddProcessor(notifier));
        // set the FlushInterval to to zero so the Activity gets created immediately
        builder.Services.Configure<StackExchangeRedisInstrumentationOptions>(options => options.FlushInterval = TimeSpan.Zero);

        using var host = builder.Build();
        // We start the host to make it build TracerProvider.
        // If we don't, nothing gets reported!
        host.Start();

        var cacheStore = host.Services.GetRequiredService<IOutputCacheStore>();
        await cacheStore.GetAsync("myFakeKey", CancellationToken.None);

        // wait for the Activity to be processed
        await notifier.ActivityReceived.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Single(notifier.ExportedActivities);

        var activity = notifier.ExportedActivities[0];
        Assert.Equal("GET", activity.OperationName);
        Assert.Contains(activity.Tags, kvp => kvp.Key == "db.system" && kvp.Value == "redis");
    }
}

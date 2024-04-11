// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Aspire.Components.Common.Tests;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Instrumentation.StackExchangeRedis;
using OpenTelemetry.Trace;
using StackExchange.Redis;
using StackExchange.Redis.Profiling;
using Xunit;

namespace Aspire.StackExchange.Redis.Tests;

public class AspireRedisExtensionsTests : IClassFixture<RedisContainerFixture>
{
    private const string TestingEndpoint = "localhost";
    private readonly RedisContainerFixture _containerFixture;
    private string ConnectionString => _containerFixture.GetConnectionString();

    public AspireRedisExtensionsTests(RedisContainerFixture containerFixture)
    {
        _containerFixture = containerFixture;
    }

    [RequiresDockerFact]
    public void AllowsConfigureConfigurationOptions()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        PopulateConfiguration(builder.Configuration);

        builder.AddRedisClient("redis");

        builder.Services.Configure<ConfigurationOptions>(options =>
        {
            options.User = "aspire-test-user";
        });

        using var host = builder.Build();
        var connection = host.Services.GetRequiredService<IConnectionMultiplexer>();

        Assert.Contains("aspire-test-user", connection.Configuration);
    }

    [RequiresDockerTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadsFromConnectionStringsCorrectly(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:myredis", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedRedisClient("myredis");
        }
        else
        {
            builder.AddRedisClient("myredis");
        }

        using var host = builder.Build();
        var connection = useKeyed ?
            host.Services.GetRequiredKeyedService<IConnectionMultiplexer>("myredis") :
            host.Services.GetRequiredService<IConnectionMultiplexer>();

        Assert.Contains(ConnectionString, connection.Configuration);
    }

    [RequiresDockerTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionStringCanBeSetInCode(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:redis", "unused")
        ]);

        void SetConnectionString(StackExchangeRedisSettings settings) => settings.ConnectionString = ConnectionString;
        if (useKeyed)
        {
            builder.AddKeyedRedisClient("redis", SetConnectionString);
        }
        else
        {
            builder.AddRedisClient("redis", SetConnectionString);
        }

        using var host = builder.Build();
        var connection = useKeyed ?
            host.Services.GetRequiredKeyedService<IConnectionMultiplexer>("redis") :
            host.Services.GetRequiredService<IConnectionMultiplexer>();

        Assert.Contains(ConnectionString, connection.Configuration);
        // the connection string from config should not be used since code set it explicitly
        Assert.DoesNotContain("unused", connection.Configuration);
    }

    [RequiresDockerTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionNameWinsOverConfigSection(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var key = useKeyed ? "redis" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(ConformanceTests.CreateConfigKey("Aspire:StackExchange:Redis", key, "ConnectionString"), "unused"),
            new KeyValuePair<string, string?>("ConnectionStrings:redis", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedRedisClient("redis");
        }
        else
        {
            builder.AddRedisClient("redis");
        }

        using var host = builder.Build();
        var connection = useKeyed ?
            host.Services.GetRequiredKeyedService<IConnectionMultiplexer>("redis") :
            host.Services.GetRequiredService<IConnectionMultiplexer>();

        Assert.Contains(ConnectionString, connection.Configuration);
        // the connection string from config should not be used since it was found in ConnectionStrings
        Assert.DoesNotContain("unused", connection.Configuration);
    }

    public static IEnumerable<object[]> AbortOnConnectFailData =>
    [
        [true, GetDefaultConfiguration(), false],
        [false, GetDefaultConfiguration(), false],

        [true, GetSetsTrueConfig(true), true],
        [false, GetSetsTrueConfig(false), true],

        [true, GetConnectionString(abortConnect: true), true],
        [false, GetConnectionString(abortConnect: true), true],
        [true, GetConnectionString(abortConnect: false), false],
        [false, GetConnectionString(abortConnect: false), false],
    ];

    private static IEnumerable<KeyValuePair<string, string?>> GetDefaultConfiguration() =>
    [
        new KeyValuePair<string, string?>("ConnectionStrings:redis", TestingEndpoint)
    ];

    private static IEnumerable<KeyValuePair<string, string?>> GetSetsTrueConfig(bool useKeyed) =>
    [
        new KeyValuePair<string, string?>("ConnectionStrings:redis", TestingEndpoint),
        new KeyValuePair<string, string?>(ConformanceTests.CreateConfigKey("Aspire:StackExchange:Redis", useKeyed ? "redis" : null, "ConfigurationOptions:AbortOnConnectFail"), "true")
    ];

    private static IEnumerable<KeyValuePair<string, string?>> GetConnectionString(bool abortConnect) =>
    [
        new KeyValuePair<string, string?>("ConnectionStrings:redis", $"{TestingEndpoint},abortConnect={(abortConnect ? "true" : "false")}")
    ];

    [Theory]
    [MemberData(nameof(AbortOnConnectFailData))]
    public void AbortOnConnectFailDefaults(bool useKeyed, IEnumerable<KeyValuePair<string, string?>> configValues, bool expectedAbortOnConnect)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection(configValues);

        if (useKeyed)
        {
            builder.AddKeyedRedisClient("redis");
        }
        else
        {
            builder.AddRedisClient("redis");
        }

        using var host = builder.Build();
        var options = useKeyed ?
            host.Services.GetRequiredService<IOptionsMonitor<ConfigurationOptions>>().Get("redis") :
            host.Services.GetRequiredService<IOptions<ConfigurationOptions>>().Value;

        Assert.Equal(expectedAbortOnConnect, options.AbortOnConnectFail);
    }

    /// <summary>
    /// Verifies that both distributed and output caching components can be added to the same builder and their HealthChecks don't conflict.
    /// See https://github.com/dotnet/aspire/issues/705
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MultipleRedisComponentsCanBeAdded(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        if (useKeyed)
        {
            builder.AddKeyedRedisDistributedCache("redis");
            builder.AddKeyedRedisOutputCache("redis");
        }
        else
        {
            builder.AddRedisDistributedCache("redis");
            builder.AddRedisOutputCache("redis");
        }

        using var host = builder.Build();

        // Note that IDistributedCache and OutputCacheStore don't support keyed services - so only the Redis ConnectionMultiplexer is keyed.

        var distributedCache = host.Services.GetRequiredService<IDistributedCache>();
        Assert.IsAssignableFrom<RedisCache>(distributedCache);

        var cacheStore = host.Services.GetRequiredService<IOutputCacheStore>();
        Assert.StartsWith("Redis", cacheStore.GetType().Name);

        // Explicitly ensure the HealthCheckService can be retrieved. It validates the registrations in its constructor.
        // See https://github.com/dotnet/aspnetcore/blob/94ad7031db6744409de24f75777a59620cb94d9a/src/HealthChecks/HealthChecks/src/DefaultHealthCheckService.cs#L33-L36
        var healthCheckService = host.Services.GetRequiredService<HealthCheckService>();
        Assert.NotNull(healthCheckService);
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_profilingSessionProvider")]
    static extern ref Func<ProfilingSession>? GetProfiler(ConnectionMultiplexer? @this);

    [Fact]
    public void KeyedServiceRedisInstrumentation()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.AddKeyedRedisClient("redis", settings =>
        {
            settings.ConnectionString = "localhost";
            settings.Tracing = true;
        });
        using var host = builder.Build();

        //This will add the instrumentations.
        var tracerProvider = host.Services.GetRequiredService<TracerProvider>();

        var connectionMultiplexer = host.Services.GetRequiredKeyedService<IConnectionMultiplexer>("redis");
        var profiler = GetProfiler(connectionMultiplexer as ConnectionMultiplexer);

        Assert.NotNull(profiler);
    }

    [RequiresDockerFact]
    public void KeyedServiceRedisInstrumentationEndToEnd()
    {
        RemoteExecutor.Invoke(async (connectionString) =>
        {
            var builder = Host.CreateEmptyApplicationBuilder(null);
            builder.Configuration.AddInMemoryCollection([
                new KeyValuePair<string, string?>("ConnectionStrings:redis", connectionString)
            ]);

            var notifier = new ActivityNotifier();
            builder.Services.AddOpenTelemetry().WithTracing(builder => builder.AddProcessor(notifier));
            // set the FlushInterval to to zero so the Activity gets created immediately
            builder.Services.Configure<StackExchangeRedisInstrumentationOptions>(options => options.FlushInterval = TimeSpan.Zero);

            builder.AddKeyedRedisClient("redis");
            using var host = builder.Build();

            // We start the host to make it build TracerProvider.
            // If we don't, nothing gets reported!
            host.Start();

            var connectionMultiplexer = host.Services.GetRequiredKeyedService<IConnectionMultiplexer>("redis");
            var database = connectionMultiplexer.GetDatabase();
            database.StringGet("key");

            await notifier.ActivityReceived.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.Single(notifier.ExportedActivities);

            var activity = notifier.ExportedActivities[0];
            Assert.Equal("GET", activity.OperationName);
            Assert.Contains(activity.Tags, kvp => kvp.Key == "db.system" && kvp.Value == "redis");
        }, ConnectionString).Dispose();
    }

    private void PopulateConfiguration(ConfigurationManager configuration, string? key = null) =>
        configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(ConformanceTests.CreateConfigKey("Aspire:StackExchange:Redis", key, "ConnectionString"), ConnectionString)
        ]);
}

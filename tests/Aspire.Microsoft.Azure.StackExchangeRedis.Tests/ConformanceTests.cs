// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.ConformanceTests;
using Aspire.TestUtilities;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using Xunit;

namespace Aspire.Microsoft.Azure.StackExchangeRedis.Tests;

public class ConformanceTests : ConformanceTests<IConnectionMultiplexer, AzureStackExchangeRedisSettings>, IClassFixture<RedisContainerFixture>
{
    private readonly RedisContainerFixture? _containerFixture;
    protected string ConnectionString { get; private set; }
    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    // https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.StackExchangeRedis/StackExchangeRedisConnectionInstrumentation.cs#L46
    protected override string ActivitySourceName => "OpenTelemetry.Instrumentation.StackExchangeRedis";

    protected override string[] RequiredLogCategories => new string[]
    {
        "StackExchange.Redis"
    };

    protected override bool SupportsKeyedRegistrations => true;

    protected override bool CanConnectToServer => RequiresDockerAttribute.IsSupported;

    protected override string? ConfigurationSectionName => "Aspire:StackExchange:Redis";

    protected override string ValidJsonConfig => """
        {
          "Aspire": {
            "StackExchange": {
              "Redis": {
                "ConnectionString": "localhost:6379",
                "DisableHealthChecks": true,
                "DisableTracing": false
              }
            }
          }
        }
        """;

    protected override (string json, string error)[] InvalidJsonToErrorMessage => [];

    public ConformanceTests(RedisContainerFixture? containerFixture)
    {
        _containerFixture = containerFixture;
        ConnectionString = (_containerFixture is not null && RequiresDockerAttribute.IsSupported)
                                        ? _containerFixture.GetConnectionString()
                                        : "localhost:6379";
    }

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
        => configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[1]
        {
            new KeyValuePair<string, string?>(CreateConfigKey("Aspire:StackExchange:Redis", key, "ConnectionString"), ConnectionString)
        });

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<AzureStackExchangeRedisSettings>? configure = null, string? key = null)
    {
        void Configure(AzureStackExchangeRedisSettings settings)
        {
            configure?.Invoke(settings);
            settings.Credential = new FakeTokenCredential();
        };

        if (key is null)
        {
            builder.AddAzureRedisClient("redis", Configure);
        }
        else
        {
            builder.AddKeyedAzureRedisClient(key, Configure);
        }
    }

    protected override void SetHealthCheck(AzureStackExchangeRedisSettings options, bool enabled)
        => options.DisableHealthChecks = !enabled;

    protected override void SetTracing(AzureStackExchangeRedisSettings options, bool enabled)
        => options.DisableTracing = !enabled;

    protected override void SetMetrics(AzureStackExchangeRedisSettings options, bool enabled)
    {
        // Redis component doesn't have DisableMetrics, so this is a no-op
        // StackExchange.Redis itself provides metrics through the library
    }

    protected override void TriggerActivity(IConnectionMultiplexer service)
    {
        if (CanConnectToServer)
        {
            var database = service.GetDatabase();
            database.StringSet("key", "value");
            database.StringGet("key");
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("key")]
    public void ConnectionMultiplexerCanBeResolved(string? key)
    {
        using IHost host = CreateHostWithComponent(key: key);

        IConnectionMultiplexer? connectionMultiplexer = Resolve<IConnectionMultiplexer>();

        Assert.NotNull(connectionMultiplexer);

        T? Resolve<T>() => key is null ? host.Services.GetService<T>() : host.Services.GetKeyedService<T>(key);
    }

    [Fact]
    [RequiresDocker]
    public void TracingEnablesTheRightActivitySource()
        => RemoteExecutor.Invoke(static connectionStringToUse => RunWithConnectionString(connectionStringToUse, obj => obj.ActivitySourceTest(key: null)),
                                 ConnectionString).Dispose();

    [Fact]
    [RequiresDocker]
    public void TracingEnablesTheRightActivitySource_Keyed()
        => RemoteExecutor.Invoke(static connectionStringToUse => RunWithConnectionString(connectionStringToUse, obj => obj.ActivitySourceTest(key: "key")),
                                 ConnectionString).Dispose();

    private static void RunWithConnectionString(string connectionString, Action<ConformanceTests> test)
        => test(new ConformanceTests(null) { ConnectionString = connectionString });
}
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using Xunit;

namespace Aspire.Microsoft.Azure.StackExchangeRedis.Tests;

public class ConfigurationTests
{
    [Fact]
    public void ConnectionStringCanBeSetFromConnectionStrings()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:redis", "localhost:6379")
        ]);

        builder.AddAzureRedisClient("redis");

        using var host = builder.Build();
        var connectionMultiplexer = host.Services.GetRequiredService<IConnectionMultiplexer>();
        
        Assert.NotNull(connectionMultiplexer);
    }

    [Fact]
    public void ConnectionStringCanBeSetFromAspireConfig()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("Aspire:StackExchange:Redis:ConnectionString", "localhost:6379")
        ]);

        builder.AddAzureRedisClient("redis");

        using var host = builder.Build();
        var connectionMultiplexer = host.Services.GetRequiredService<IConnectionMultiplexer>();
        
        Assert.NotNull(connectionMultiplexer);
    }

    [Fact]
    public void HealthChecksCanBeDisabledFromConfig()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:redis", "localhost:6379"),
            new KeyValuePair<string, string?>("Aspire:StackExchange:Redis:DisableHealthChecks", "true")
        ]);

        builder.AddAzureRedisClient("redis");

        using var host = builder.Build();
        var connectionMultiplexer = host.Services.GetRequiredService<IConnectionMultiplexer>();
        
        Assert.NotNull(connectionMultiplexer);
    }

    [Fact]
    public void TracingCanBeDisabledFromConfig()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:redis", "localhost:6379"),
            new KeyValuePair<string, string?>("Aspire:StackExchange:Redis:DisableTracing", "true")
        ]);

        builder.AddAzureRedisClient("redis");

        using var host = builder.Build();
        var connectionMultiplexer = host.Services.GetRequiredService<IConnectionMultiplexer>();
        
        Assert.NotNull(connectionMultiplexer);
    }

    [Fact]
    public void ConfigurationOptionsCanBeConfigured()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:redis", "localhost:6379"),
            new KeyValuePair<string, string?>("Aspire:StackExchange:Redis:ConfigurationOptions:ConnectTimeout", "5000"),
            new KeyValuePair<string, string?>("Aspire:StackExchange:Redis:ConfigurationOptions:ConnectRetry", "3")
        ]);

        builder.AddAzureRedisClient("redis");

        using var host = builder.Build();
        var connectionMultiplexer = host.Services.GetRequiredService<IConnectionMultiplexer>();
        
        Assert.NotNull(connectionMultiplexer);
    }

    [Fact]
    public void NamedConfigurationCanBeUsed()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:cache1", "localhost:6379"),
            new KeyValuePair<string, string?>("Aspire:StackExchange:Redis:cache1:DisableHealthChecks", "true")
        ]);

        builder.AddKeyedAzureRedisClient("cache1");

        using var host = builder.Build();
        var connectionMultiplexer = host.Services.GetRequiredKeyedService<IConnectionMultiplexer>("cache1");
        
        Assert.NotNull(connectionMultiplexer);
    }
}
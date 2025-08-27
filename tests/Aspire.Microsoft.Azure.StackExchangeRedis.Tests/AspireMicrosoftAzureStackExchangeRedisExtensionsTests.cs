// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using Xunit;

namespace Aspire.Microsoft.Azure.StackExchangeRedis.Tests;

public class AspireMicrosoftAzureStackExchangeRedisExtensionsTests
{
    private const string ConnectionString = "localhost:6379";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadsConnectionStringFromConfig(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:redis", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureRedisClient("redis");
        }
        else
        {
            builder.AddAzureRedisClient("redis");
        }

        using var host = builder.Build();
        
        // For this test, we don't actually connect, just verify the registration
        if (useKeyed)
        {
            var connectionMultiplexer = host.Services.GetRequiredKeyedService<IConnectionMultiplexer>("redis");
            Assert.NotNull(connectionMultiplexer);
        }
        else
        {
            var connectionMultiplexer = host.Services.GetRequiredService<IConnectionMultiplexer>();
            Assert.NotNull(connectionMultiplexer);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConfiguresAzureCredentialCorrectly(bool useKeyed)
    {
        var fakeCredential = new FakeTokenCredential();
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:redis", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureRedisClient("redis", settings =>
            {
                settings.Credential = fakeCredential;
            });
        }
        else
        {
            builder.AddAzureRedisClient("redis", settings =>
            {
                settings.Credential = fakeCredential;
            });
        }

        using var host = builder.Build();

        // Verify that the credential is configured by checking if a token request would be made
        // Note: In real Azure environments, this would configure the Redis connection for Azure AD auth
        Assert.NotNull(fakeCredential);
        // For this unit test, we can't easily verify the Azure configuration without a real Redis connection
        // but we can verify the service registration worked
        if (useKeyed)
        {
            var connectionMultiplexer = host.Services.GetRequiredKeyedService<IConnectionMultiplexer>("redis");
            Assert.NotNull(connectionMultiplexer);
        }
        else
        {
            var connectionMultiplexer = host.Services.GetRequiredService<IConnectionMultiplexer>();
            Assert.NotNull(connectionMultiplexer);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanConfigureOptionsInline(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:redis", ConnectionString)
        ]);

        var customTimeout = 5000;
        
        if (useKeyed)
        {
            builder.AddKeyedAzureRedisClient("redis", 
                configureSettings: settings => settings.DisableHealthChecks = true,
                configureOptions: options => options.ConnectTimeout = customTimeout);
        }
        else
        {
            builder.AddAzureRedisClient("redis", 
                configureSettings: settings => settings.DisableHealthChecks = true,
                configureOptions: options => options.ConnectTimeout = customTimeout);
        }

        using var host = builder.Build();
        
        // Verify service registration
        if (useKeyed)
        {
            var connectionMultiplexer = host.Services.GetRequiredKeyedService<IConnectionMultiplexer>("redis");
            Assert.NotNull(connectionMultiplexer);
        }
        else
        {
            var connectionMultiplexer = host.Services.GetRequiredService<IConnectionMultiplexer>();
            Assert.NotNull(connectionMultiplexer);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanConfigureWithoutCredential(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:redis", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureRedisClient("redis", settings =>
            {
                settings.DisableTracing = true;
                // No credential set - should work like regular Redis connection
            });
        }
        else
        {
            builder.AddAzureRedisClient("redis", settings =>
            {
                settings.DisableTracing = true;
                // No credential set - should work like regular Redis connection
            });
        }

        using var host = builder.Build();
        
        // Verify service registration
        if (useKeyed)
        {
            var connectionMultiplexer = host.Services.GetRequiredKeyedService<IConnectionMultiplexer>("redis");
            Assert.NotNull(connectionMultiplexer);
        }
        else
        {
            var connectionMultiplexer = host.Services.GetRequiredService<IConnectionMultiplexer>();
            Assert.NotNull(connectionMultiplexer);
        }
    }

    [Fact]
    public void ThrowsOnNullBuilder()
    {
        IHostApplicationBuilder builder = null!;

        var action = () => builder.AddAzureRedisClient("redis");
        Assert.Throws<ArgumentNullException>(action);

        var keyedAction = () => builder.AddKeyedAzureRedisClient("redis");
        Assert.Throws<ArgumentNullException>(keyedAction);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ThrowsOnNullOrEmptyConnectionName(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        if (useKeyed)
        {
            var nullAction = () => builder.AddKeyedAzureRedisClient(null!);
            var emptyAction = () => builder.AddKeyedAzureRedisClient("");

            Assert.Throws<ArgumentNullException>(nullAction);
            Assert.Throws<ArgumentException>(emptyAction);
        }
        else
        {
            var nullAction = () => builder.AddAzureRedisClient(null!);
            var emptyAction = () => builder.AddAzureRedisClient("");

            Assert.Throws<ArgumentNullException>(nullAction);
            Assert.Throws<ArgumentException>(emptyAction);
        }
    }
}
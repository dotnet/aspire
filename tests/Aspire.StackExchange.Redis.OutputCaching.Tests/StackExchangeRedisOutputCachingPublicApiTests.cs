// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using Xunit;

namespace Aspire.StackExchange.Redis.OutputCaching.Tests;

public class StackExchangeRedisOutputCachingPublicApiTests
{
    [Fact]
    public void AddRedisOutputCacheShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string connectionName = "redis";

        var action = () => builder.AddRedisOutputCache(
            connectionName,
            default(Action<StackExchangeRedisSettings>?),
            default(Action<ConfigurationOptions>?));

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddRedisOutputCacheShouldThrowWhenConnectionNameIsNullOrEmpty(bool isNull)
    {
        IHostApplicationBuilder builder = new HostApplicationBuilder();
        var connectionName = isNull ? null! : string.Empty;

        var action = () => builder.AddRedisOutputCache(
            connectionName,
            default(Action<StackExchangeRedisSettings>?),
            default(Action<ConfigurationOptions>?));

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(connectionName), exception.ParamName);
    }

    [Fact]
    public void AddKeyedRedisOutputCacheShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string name = "redis";

        var action = () => builder.AddKeyedRedisOutputCache(
            name,
            default(Action<StackExchangeRedisSettings>?),
            default(Action<ConfigurationOptions>?));

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddKeyedRedisOutputCacheShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        IHostApplicationBuilder builder = new HostApplicationBuilder();
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddKeyedRedisOutputCache(
            name,
            default(Action<StackExchangeRedisSettings>?),
            default(Action<ConfigurationOptions>?));

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }
}

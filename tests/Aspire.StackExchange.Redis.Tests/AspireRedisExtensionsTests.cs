// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using Xunit;

namespace Aspire.StackExchange.Redis.Tests;

public class AspireRedisExtensionsTests
{
    [ConditionalFact]
    public void AllowsConfigureConfigurationOptions()
    {
        AspireRedisHelpers.SkipIfCanNotConnectToServer();

        var builder = Host.CreateEmptyApplicationBuilder(null);
        AspireRedisHelpers.PopulateConfiguration(builder.Configuration);

        builder.AddRedis();

        builder.Services.Configure<ConfigurationOptions>(options =>
        {
            options.User = "aspire-test-user";
        });

        var host = builder.Build();
        var connection = host.Services.GetRequiredService<IConnectionMultiplexer>();

        Assert.Contains("aspire-test-user", connection.Configuration);
    }
}

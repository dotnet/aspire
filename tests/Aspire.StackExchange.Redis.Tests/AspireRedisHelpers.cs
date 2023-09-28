// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.XUnitExtensions;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace Aspire.StackExchange.Redis.Tests;

public static class AspireRedisHelpers
{
    public const string TestingEndpoint = "localhost";

    private static readonly Lazy<bool> s_canConnectToServer = new(GetCanConnect);
    public static bool CanConnectToServer => s_canConnectToServer.Value;

    public static void SkipIfCanNotConnectToServer()
    {
        if (!CanConnectToServer)
        {
            throw new SkipTestException("Unable to connect to the server.");
        }
    }

    public static void PopulateConfiguration(ConfigurationManager configuration, string? key = null) =>
        configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(ConformanceTests.CreateConfigKey("Aspire:StackExchange:Redis", key, "ConnectionString"), TestingEndpoint)
        ]);

    private static bool GetCanConnect()
    {
        try
        {
            ConfigurationOptions options = new()
            {
                AbortOnConnectFail = true,
                ConnectRetry = 0
            };
            options.EndPoints.Add(TestingEndpoint);

            ConnectionMultiplexer.Connect(options).Dispose();

            return true;
        }
        catch (RedisConnectionException)
        {
            return false;
        }
    }

}

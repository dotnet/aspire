// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.XUnitExtensions;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Aspire.RabbitMQ.Client.Tests;

public static class AspireRabbitMQHelpers
{
    public const string TestingEndpoint = "amqp://localhost:5672";

    private static readonly Lazy<bool> s_canConnectToServer = new(GetCanConnect);
    public static bool CanConnectToServer => s_canConnectToServer.Value;

    public static void SkipIfCannotConnectToServer()
    {
        if (!CanConnectToServer)
        {
            throw new SkipTestException("Unable to connect to the server.");
        }
    }

    public static void PopulateConfiguration(ConfigurationManager configuration, string? key = null) =>
        configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(ConformanceTests.CreateConfigKey("Aspire:RabbitMQ:Client", key, "ConnectionString"), TestingEndpoint)
        ]);

    private static bool GetCanConnect()
    {
        try
        {
            var factory = new ConnectionFactory()
            {
                Uri = new Uri(TestingEndpoint)
            };
            factory.CreateConnection();

            return true;
        }
        catch (BrokerUnreachableException)
        {
            return false;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using Xunit;

namespace Aspire.RabbitMQ.Client.Tests;

public class AspireRabbitMQExtensionsTests
{
    [ConditionalTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadsFromConnectionStringsCorrectly(bool useKeyed)
    {
        AspireRabbitMQHelpers.SkipIfCannotConnectToServer();

        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:messaging", AspireRabbitMQHelpers.TestingEndpoint)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedRabbitMQ("messaging");
        }
        else
        {
            builder.AddRabbitMQ("messaging");
        }

        var host = builder.Build();
        var connection = useKeyed ?
            host.Services.GetRequiredKeyedService<IConnection>("messaging") :
            host.Services.GetRequiredService<IConnection>();

        Assert.Equal("localhost", connection.Endpoint.HostName);
        Assert.Equal(5672, connection.Endpoint.Port);
    }

    [ConditionalTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionStringCanBeSetInCode(bool useKeyed)
    {
        AspireRabbitMQHelpers.SkipIfCannotConnectToServer();

        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:messaging", "unused")
        ]);

        static void SetConnectionString(RabbitMQClientSettings settings) => settings.ConnectionString = AspireRabbitMQHelpers.TestingEndpoint;
        if (useKeyed)
        {
            builder.AddKeyedRabbitMQ("messaging", SetConnectionString);
        }
        else
        {
            builder.AddRabbitMQ("messaging", SetConnectionString);
        }

        var host = builder.Build();
        var connection = useKeyed ?
            host.Services.GetRequiredKeyedService<IConnection>("messaging") :
            host.Services.GetRequiredService<IConnection>();

        Assert.Equal("localhost", connection.Endpoint.HostName);
        Assert.Equal(5672, connection.Endpoint.Port);
    }

    [ConditionalTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionNameWinsOverConfigSection(bool useKeyed)
    {
        AspireRabbitMQHelpers.SkipIfCannotConnectToServer();

        var builder = Host.CreateEmptyApplicationBuilder(null);

        var key = useKeyed ? "redis" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(ConformanceTests.CreateConfigKey("Aspire:RabbitMQ:Client", key, "ConnectionString"), "unused"),
            new KeyValuePair<string, string?>("ConnectionStrings:messaging", AspireRabbitMQHelpers.TestingEndpoint)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedRabbitMQ("messaging");
        }
        else
        {
            builder.AddRabbitMQ("messaging");
        }

        var host = builder.Build();
        var connection = useKeyed ?
            host.Services.GetRequiredKeyedService<IConnection>("messaging") :
            host.Services.GetRequiredService<IConnection>();

        Assert.Equal("localhost", connection.Endpoint.HostName);
        Assert.Equal(5672, connection.Endpoint.Port);
    }
}

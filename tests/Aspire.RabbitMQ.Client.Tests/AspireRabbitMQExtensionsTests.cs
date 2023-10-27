// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;
using System.Security.Authentication;
using System.Text;
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

    [Fact]
    public void ConnectionFactoryOptionsFromConfig()
    {
        static Stream CreateStreamFromString(string data) => new MemoryStream(Encoding.UTF8.GetBytes(data));

        using var jsonStream = CreateStreamFromString("""
        {
          "Aspire": {
            "RabbitMQ": {
              "Client": {
                "ConnectionFactory": {
                  "AmqpUriSslProtocols": "Tls12",
                  "AutomaticRecoveryEnabled": false,
                  "ConsumerDispatchConcurrency": 2,
                  "SocketReadTimeout": "00:00:03",
                  "Ssl": {
                    "AcceptablePolicyErrors": "RemoteCertificateNameMismatch",
                    "Enabled": true,
                    "Version": "Tls13"
                  },
                  "MaxMessageSize": 304,
                  "ClientProvidedName": "aspire-app"
                }
              }
            }
          }
        }
        """);

        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Configuration.AddJsonStream(jsonStream);

        builder.AddRabbitMQ("messaging");

        var host = builder.Build();
        var connectionFactory = (ConnectionFactory)host.Services.GetRequiredService<IConnectionFactory>();

        Assert.Equal(SslProtocols.Tls12, connectionFactory.AmqpUriSslProtocols);
        Assert.False(connectionFactory.AutomaticRecoveryEnabled);
        Assert.Equal(2, connectionFactory.ConsumerDispatchConcurrency);
        Assert.Equal(SslPolicyErrors.RemoteCertificateNameMismatch, connectionFactory.Ssl.AcceptablePolicyErrors);
        Assert.True(connectionFactory.Ssl.Enabled);
        Assert.Equal(SslProtocols.Tls13, connectionFactory.Ssl.Version);
        Assert.Equal(TimeSpan.FromSeconds(3), connectionFactory.SocketReadTimeout);
        Assert.Equal((uint)304, connectionFactory.MaxMessageSize);
        Assert.Equal("aspire-app", connectionFactory.ClientProvidedName);
    }
}

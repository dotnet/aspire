// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Confluent.Kafka.Tests;

public class ProducerConfigurationTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadsFromConnectionStringsCorrectly(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:messaging", CommonHelpers.TestingEndpoint)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedKafkaProducer<string, string>("messaging");
        }
        else
        {
            builder.AddKafkaProducer<string, string>("messaging");
        }

        var host = builder.Build();
        var connectionFactory = useKeyed ?
            host.Services.GetRequiredKeyedService(ReflectionHelpers.ProducerConnectionFactoryStringKeyStringValueType.Value!, "messaging") :
            host.Services.GetRequiredService(ReflectionHelpers.ProducerConnectionFactoryStringKeyStringValueType.Value!);

        ProducerConfig config = GetProducerConfig(connectionFactory)!;

        Assert.Equal(CommonHelpers.TestingEndpoint, config.BootstrapServers);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionStringCanBeSetInCode(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:messaging", "unused")
        ]);

        static void SetConnectionString(KafkaProducerSettings settings) => settings.ConnectionString = CommonHelpers.TestingEndpoint;
        if (useKeyed)
        {
            builder.AddKeyedKafkaProducer<string, string>("messaging", configureSettings: SetConnectionString);
        }
        else
        {
            builder.AddKafkaProducer<string, string>("messaging", configureSettings: SetConnectionString);
        }

        var host = builder.Build();
        var connectionFactory = useKeyed ?
            host.Services.GetRequiredKeyedService(ReflectionHelpers.ProducerConnectionFactoryStringKeyStringValueType.Value!, "messaging") :
            host.Services.GetRequiredService(ReflectionHelpers.ProducerConnectionFactoryStringKeyStringValueType.Value!);

        ProducerConfig config = GetProducerConfig(connectionFactory)!;

        Assert.Equal(CommonHelpers.TestingEndpoint, config.BootstrapServers);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionNameWinsOverConfigSection(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var key = useKeyed ? "messaging" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(ProducerConformanceTests.CreateConfigKey("Aspire:Confluent:Kafka:Producer", key, "ConnectionString"), "unused"),
            new KeyValuePair<string, string?>("ConnectionStrings:messaging", CommonHelpers.TestingEndpoint)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedKafkaProducer<string, string>("messaging");
        }
        else
        {
            builder.AddKafkaProducer<string, string>("messaging");
        }

        var host = builder.Build();
        var connectionFactory = useKeyed ?
            host.Services.GetRequiredKeyedService(ReflectionHelpers.ProducerConnectionFactoryStringKeyStringValueType.Value!, "messaging") :
            host.Services.GetRequiredService(ReflectionHelpers.ProducerConnectionFactoryStringKeyStringValueType.Value!);

        ProducerConfig config = GetProducerConfig(connectionFactory)!;

        Assert.Equal(CommonHelpers.TestingEndpoint, config.BootstrapServers);
    }

    [Fact]
    public void ProducerConfigOptionsFromConfig()
    {
        static Stream CreateStreamFromString(string data) => new MemoryStream(Encoding.UTF8.GetBytes(data));

        using var jsonStream = CreateStreamFromString("""
        {
          "Aspire": {
            "Confluent": {
              "Kafka": {
                "Producer": {
                  "Config": {
                    "BootstrapServers": "localhost:9092",
                    "Acks": "All",
                    "SaslUsername": "user",
                    "SaslPassword": "password",
                    "SaslMechanism": "Plain",
                    "SecurityProtocol": "Plaintext"
                  }
                }
              }
            }
          }
        }
        """);

        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Configuration.AddJsonStream(jsonStream);

        builder.AddKafkaProducer<string, string>("messaging");

        var host = builder.Build();
        var connectionFactory = host.Services.GetRequiredService(ReflectionHelpers.ProducerConnectionFactoryStringKeyStringValueType.Value!);

        ProducerConfig config = GetProducerConfig(connectionFactory)!;

        Assert.Equal(Acks.All, config.Acks);
        Assert.Equal("user", config.SaslUsername);
        Assert.Equal("password", config.SaslPassword);
        Assert.Equal(SaslMechanism.Plain, config.SaslMechanism);
        Assert.Equal(SecurityProtocol.Plaintext, config.SecurityProtocol);
    }

    private static ProducerConfig? GetProducerConfig(object o) => ReflectionHelpers.ProducerConnectionFactoryStringKeyStringValueType.Value!.GetProperty("Config")!.GetValue(o) as ProducerConfig;
}

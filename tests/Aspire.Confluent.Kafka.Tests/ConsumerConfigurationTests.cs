// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Confluent.Kafka.Tests;

public class ConsumerConfigurationTests
{
    [ConditionalTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadsFromConnectionStringsCorrectly(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var key = useKeyed ? "messaging" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:messaging", CommonHelpers.TestingEndpoint),
            new KeyValuePair<string, string?>(ProducerConformanceTests.CreateConfigKey("Aspire:Confluent:Kafka:Consumer", key, "Config:GroupId"), "unused")
        ]);

        if (useKeyed)
        {
            builder.AddKeyedKafkaConsumer<string, string>("messaging");
        }
        else
        {
            builder.AddKafkaConsumer<string, string>("messaging");
        }

        var host = builder.Build();
        var connectionFactory = useKeyed
            ? host.Services.GetRequiredKeyedService(ReflectionHelpers.ConsumerConnectionFactoryStringKeyStringValueType.Value!, "messaging")
            : host.Services.GetRequiredService(ReflectionHelpers.ConsumerConnectionFactoryStringKeyStringValueType.Value!);

        ConsumerConfig config = GetConsumerConfig(connectionFactory)!;

        Assert.Equal(CommonHelpers.TestingEndpoint, config.BootstrapServers);
    }

    [ConditionalTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionStringCanBeSetInCode(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var key = useKeyed ? "messaging" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:messaging", "unused"),
            new KeyValuePair<string, string?>(ProducerConformanceTests.CreateConfigKey("Aspire:Confluent:Kafka:Consumer", key, "Config:GroupId"), "unused")
        ]);

        static void SetConnectionString(KafkaConsumerSettings settings) => settings.ConnectionString = CommonHelpers.TestingEndpoint;
        if (useKeyed)
        {
            builder.AddKeyedKafkaConsumer<string, string>("messaging", configureSettings: SetConnectionString);
        }
        else
        {
            builder.AddKafkaConsumer<string, string>("messaging", configureSettings: SetConnectionString);
        }

        var host = builder.Build();
        var connectionFactory = useKeyed
            ? host.Services.GetRequiredKeyedService(ReflectionHelpers.ConsumerConnectionFactoryStringKeyStringValueType.Value!, "messaging")
            : host.Services.GetRequiredService(ReflectionHelpers.ConsumerConnectionFactoryStringKeyStringValueType.Value!);

        ConsumerConfig config = GetConsumerConfig(connectionFactory)!;

        Assert.Equal(CommonHelpers.TestingEndpoint, config.BootstrapServers);
    }

    [ConditionalTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionNameWinsOverConfigSection(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var key = useKeyed ? "messaging" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(ProducerConformanceTests.CreateConfigKey("Aspire:Confluent:Kafka:Consumer", key, "ConnectionString"), "unused"),
            new KeyValuePair<string, string?>("ConnectionStrings:messaging", CommonHelpers.TestingEndpoint),
            new KeyValuePair<string, string?>(ProducerConformanceTests.CreateConfigKey("Aspire:Confluent:Kafka:Consumer", key, "Config:GroupId"), "unused")
        ]);

        if (useKeyed)
        {
            builder.AddKeyedKafkaConsumer<string, string>("messaging");
        }
        else
        {
            builder.AddKafkaConsumer<string, string>("messaging");
        }

        var host = builder.Build();
        var connectionFactory = useKeyed
            ? host.Services.GetRequiredKeyedService(ReflectionHelpers.ConsumerConnectionFactoryStringKeyStringValueType.Value!, "messaging")
            : host.Services.GetRequiredService(ReflectionHelpers.ConsumerConnectionFactoryStringKeyStringValueType.Value!);

        ConsumerConfig config = GetConsumerConfig(connectionFactory)!;

        Assert.Equal(CommonHelpers.TestingEndpoint, config.BootstrapServers);
    }

    [Fact]
    public void ConsumerConfigOptionsFromConfig()
    {
        static Stream CreateStreamFromString(string data) => new MemoryStream(Encoding.UTF8.GetBytes(data));

        using var jsonStream = CreateStreamFromString("""
        {
          "Aspire": {
            "Confluent": {
              "Kafka": {
                "Consumer": {
                  "Config": {
                    "BootstrapServers": "localhost:9092",
                    "AutoOffsetReset": "Earliest",
                    "GroupId": "consumer-group",
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

        builder.AddKafkaConsumer<string, string>("messaging");

        var host = builder.Build();
        var connectionFactory = host.Services.GetRequiredService(ReflectionHelpers.ConsumerConnectionFactoryStringKeyStringValueType.Value!);

        ConsumerConfig config = GetConsumerConfig(connectionFactory)!;

        Assert.Equal(AutoOffsetReset.Earliest, config.AutoOffsetReset);
        Assert.Equal("consumer-group", config.GroupId);
        Assert.Equal("user", config.SaslUsername);
        Assert.Equal("password", config.SaslPassword);
        Assert.Equal(SaslMechanism.Plain, config.SaslMechanism);
        Assert.Equal(SecurityProtocol.Plaintext, config.SecurityProtocol);
    }

    private static ConsumerConfig? GetConsumerConfig(object o) => ReflectionHelpers.ConsumerConnectionFactoryStringKeyStringValueType.Value!.GetProperty("Config")!.GetValue(o) as ConsumerConfig;
}

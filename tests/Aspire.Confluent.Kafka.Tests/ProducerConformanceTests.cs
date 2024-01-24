// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.ConformanceTests;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Confluent.Kafka.Tests;

public class ProducerConformanceTests : ConformanceTests<IProducer<string, string>, KafkaProducerSettings>
{
    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    protected override string ActivitySourceName => throw new NotImplementedException();

    protected override string[] RequiredLogCategories => [
        "Aspire.Confluent.Kafka"
        ];

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
    {
        configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[1]
        {
            new KeyValuePair<string, string?>(CreateConfigKey("Aspire:Confluent:Kafka:Producer", key, "ConnectionString"),
                           "localhost:9092")
        });
    }

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<KafkaProducerSettings>? configure = null, string? key = null)
    {
        if (key is null)
        {
            builder.AddKafkaProducer<string, string>("messaging", configure);
        }
        else
        {
            builder.AddKeyedKafkaProducer<string, string>(key, configure);
        }
    }

    protected override void SetHealthCheck(KafkaProducerSettings options, bool enabled) => options.HealthChecks = enabled;

    protected override void SetMetrics(KafkaProducerSettings options, bool enabled) => options.Metrics = enabled;

    protected override void SetTracing(KafkaProducerSettings options, bool enabled)
    {
        throw new NotImplementedException();
    }

    protected override void TriggerActivity(IProducer<string, string> service)
    {
        service.Produce("test", new Message<string, string> { Key = "test", Value = "test" });
        service.Flush(TimeSpan.FromMilliseconds(1000));
    }

    protected override bool SupportsKeyedRegistrations => true;

    protected override string ValidJsonConfig => """
                {
                    "Aspire": {
                        "Confluent": {
                            "Kafka": {
                                "Producer": {
                                    "ConnectionString": "localhost:9092",
                                    "HealthChecks": true,
                                    "Metrics": true
                                }
                            }
                        }
                    }
                }
                """;
    protected override (string json, string error)[] InvalidJsonToErrorMessage => new[]
        {
            ("""{"Aspire": { "Confluent":{ "Kafka": { "Producer": { "Metrics": 0}}}}}""", "Value is \"integer\" but should be \"boolean\""),
            ("""{"Aspire": { "Confluent":{ "Kafka": { "Producer": { "HealthChecks": 0}}}}}""", "Value is \"integer\" but should be \"boolean\"")
        };
}

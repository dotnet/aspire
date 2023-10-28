// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.ConformanceTests;
using Microsoft.DotNet.XUnitExtensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;

namespace Aspire.RabbitMQ.Client.Tests;

public class ConformanceTests : ConformanceTests<IConnection, RabbitMQClientSettings>
{
    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    // IConnectionMultiplexer can be created only via call to ConnectionMultiplexer.Connect
    protected override bool CanCreateClientWithoutConnectingToServer => false;

    protected override bool CanConnectToServer => AspireRabbitMQHelpers.CanConnectToServer;

    protected override bool SupportsKeyedRegistrations => true;

    protected override string[] RequiredLogCategories => Array.Empty<string>();

    protected override string ActivitySourceName => "";

    protected override string JsonSchemaPath => "src/Components/Aspire.RabbitMQ.Client/ConfigurationSchema.json";

    protected override string ValidJsonConfig => """
        {
          "Aspire": {
            "RabbitMQ": {
              "Client": {
                "ConnectionFactory": {
                  "AutomaticRecoveryEnabled": false,
                  "ConsumerDispatchConcurrency": 2,
                  "Ssl": {
                    "AcceptablePolicyErrors": "None",
                    "Enabled": false,
                    "Version": "Tls13"
                  }
                },
                "ConnectionString": "amqp://localhost:5672",
                "MaxConnectRetryCount": 10,
                "HealthChecks": true,
                "Tracing": false
              }
            }
          }
        }
        """;

    protected override (string json, string error)[] InvalidJsonToErrorMessage => new[]
        {
            ("""{"Aspire": { "RabbitMQ": { "Client":{ "ConnectionFactory": "YOUR_OPTION"}}}}""", "Value is \"string\" but should be \"object\""),
            ("""{"Aspire": { "RabbitMQ": { "Client":{ "ConnectionFactory": { "AmqpUriSslProtocols": "Fast"}}}}}""", "Value should match one of the values specified by the enum"),
            ("""{"Aspire": { "RabbitMQ": { "Client":{ "ConnectionFactory": { "Ssl":{ "AcceptablePolicyErrors": "Fast"}}}}}}""", "Value should match one of the values specified by the enum"),
            ("""{"Aspire": { "RabbitMQ": { "Client":{ "ConnectionFactory": { "Ssl":{ "Version": "Fast"}}}}}}""", "Value should match one of the values specified by the enum"),
            ("""{"Aspire": { "RabbitMQ": { "Client":{ "ConnectionFactory": { "RequestedConnectionTimeout": "3S"}}}}}""", "Value does not match format \"duration\"")
        };

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null) =>
        AspireRabbitMQHelpers.PopulateConfiguration(configuration, key);

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<RabbitMQClientSettings>? configure = null, string? key = null)
    {
        if (key is null)
        {
            builder.AddRabbitMQ("rabbit", configure);
        }
        else
        {
            builder.AddKeyedRabbitMQ(key, configure);
        }
    }

    protected override void SetHealthCheck(RabbitMQClientSettings settings, bool enabled)
        => settings.HealthChecks = enabled;

    protected override void DisableRetries(RabbitMQClientSettings options)
    {
        options.MaxConnectRetryCount = 0;
    }

    protected override void SetTracing(RabbitMQClientSettings settings, bool enabled)
        => settings.Tracing = enabled;

    protected override void SetMetrics(RabbitMQClientSettings settings, bool enabled)
        => throw new NotImplementedException();

    protected override void TriggerActivity(IConnection service)
    {
        var channel = service.CreateModel();
        channel.QueueDeclare("test-queue", exclusive: false);
        channel.BasicPublish(
            exchange: "",
            routingKey: "test-queue",
            basicProperties: null,
            body: "hello world"u8.ToArray());
    }

    protected override void SetupConnectionInformationIsDelayValidated()
    {
        throw new SkipTestException("RabbitMQ connects to localhost by default if the connection information isn't available.");
    }
}

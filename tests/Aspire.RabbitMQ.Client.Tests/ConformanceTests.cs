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
                "ConnectionString": "YOUR_ENDPOINT",
                "HealthChecks": true,
                "Tracing": false
              }
            }
          }
        }
        """;

    protected override (string json, string error)[] InvalidJsonToErrorMessage => new[]
        {
            ("""{"Aspire": { "RabbitMQ": { "Client":{ "ClientFactory": "YOUR_OPTION"}}}}""", "Value is \"string\" but should be \"object\""),
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

    protected override void SetTracing(RabbitMQClientSettings settings, bool enabled)
        => settings.Tracing = enabled;

    protected override void SetMetrics(RabbitMQClientSettings settings, bool enabled)
        => throw new NotImplementedException();

    protected override void TriggerActivity(IConnection service)
    {
        // TODO
        //var channel = service.CreateModel();

        //string id = Guid.NewGuid().ToString();
        //database.StringSet(id, "hello");
        //database.KeyDelete(id);
    }

    protected override void SetupConnectionInformationIsDelayValidated()
    {
        throw new SkipTestException("RabbitMQ connects to localhost by default if the connection information isn't available.");
    }
}

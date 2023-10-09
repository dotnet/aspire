// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.ConformanceTests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace Aspire.StackExchange.Redis.Tests;

public class ConformanceTests : ConformanceTests<IConnectionMultiplexer, StackExchangeRedisSettings>
{
    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    // IConnectionMultiplexer can be created only via call to ConnectionMultiplexer.Connect
    protected override bool CanCreateClientWithoutConnectingToServer => false;

    protected override bool CanConnectToServer => AspireRedisHelpers.CanConnectToServer;

    protected override bool SupportsKeyedRegistrations => true;

    protected override string[] RequiredLogCategories => new string[] { "Aspire.StackExchange.Redis" };

    // https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/e4cb523a4a3592e1a1adf30f3596025bfd8978e3/src/OpenTelemetry.Instrumentation.StackExchangeRedis/StackExchangeRedisConnectionInstrumentation.cs#L34
    protected override string ActivitySourceName => "OpenTelemetry.Instrumentation.StackExchangeRedis";

    protected override string JsonSchemaPath => "src/Components/Aspire.StackExchange.Redis/ConfigurationSchema.json";

    protected override string ValidJsonConfig => """
        {
          "Aspire": {
            "StackExchange": {
              "Redis": {
                "ConnectionString": "YOUR_ENDPOINT",
                "HealthChecks": true,
                "Tracing": false,
                "ConfigurationOptions": {
                  "CheckCertificateRevocation": true,
                  "ConnectTimeout": 5,
                  "HeartbeatInterval": "PT5S",
                  "Ssl" : true,
                  "SslProtocols" : "Tls11"
                }
              }
            }
          }
        }
        """;

    protected override (string json, string error)[] InvalidJsonToErrorMessage => new[]
        {
            ("""{"Aspire": { "StackExchange": { "Redis":{ "ConfigurationOptions": "YOUR_OPTION"}}}}""", "Value is \"string\" but should be \"object\""),
            ("""{"Aspire": { "StackExchange": { "Redis":{ "ConfigurationOptions": { "Proxy": "Fast"}}}}}""", "Value should match one of the values specified by the enum"),
            ("""{"Aspire": { "StackExchange": { "Redis":{ "ConfigurationOptions": { "SslProtocols": "Fast"}}}}}""", "Value should match one of the values specified by the enum"),
            ("""{"Aspire": { "StackExchange": { "Redis":{ "ConfigurationOptions": { "HeartbeatInterval": "3S"}}}}}""", "Value does not match format \"duration\"")
        };

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null) =>
        AspireRedisHelpers.PopulateConfiguration(configuration, key);

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<StackExchangeRedisSettings>? configure = null, string? key = null)
    {
        if (key is null)
        {
            builder.AddRedis("redis", configure);
        }
        else
        {
            builder.AddKeyedRedis(key, configure);
        }
    }

    protected override void SetHealthCheck(StackExchangeRedisSettings settings, bool enabled)
        => settings.HealthChecks = enabled;

    protected override void SetTracing(StackExchangeRedisSettings settings, bool enabled)
        => settings.Tracing = enabled;

    protected override void SetMetrics(StackExchangeRedisSettings settings, bool enabled)
        => throw new NotImplementedException();

    protected override void TriggerActivity(IConnectionMultiplexer service)
    {
        var database = service.GetDatabase();

        string id = Guid.NewGuid().ToString();
        database.StringSet(id, "hello");
        database.KeyDelete(id);
    }
}

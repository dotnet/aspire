// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.ConformanceTests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NATS.Client.Core;

namespace Aspire.NATS.Net.Tests;

public class ConformanceTests : ConformanceTests<INatsConnection, NatsClientSettings>
{
    private const string ConnectionSting = "nats://apire-host:4222";

    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;
    protected override string ActivitySourceName => "Nats.Client";
    protected override string[] RequiredLogCategories => ["NATS.Client"];
    protected override void RegisterComponent(HostApplicationBuilder builder, Action<NatsClientSettings>? configure = null, string? key = null)
    {
        if (key is null)
        {
            builder.AddNatsClient("nats", configure);
        }
        else
        {
            builder.AddKeyedNatsClient(key, configure);
        }
    }

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
        => configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[1]
        {
            new KeyValuePair<string, string?>(CreateConfigKey("Aspire:Nats:Client", key, "ConnectionString"), ConnectionSting)
        });

    protected override bool CanCreateClientWithoutConnectingToServer => false;

    protected override void TriggerActivity(INatsConnection service)
    {
    }

    protected override void SetHealthCheck(NatsClientSettings options, bool enabled)
        => options.HealthChecks = enabled;

    protected override void SetTracing(NatsClientSettings options, bool enabled)
        => throw new NotImplementedException();

    protected override void SetMetrics(NatsClientSettings options, bool enabled)
        => throw new NotImplementedException();

    protected override string ValidJsonConfig => """
                                                 {
                                                   "Aspire": {
                                                     "Nats": {
                                                       "Client": {
                                                         "ConnectionString": "YOUR_CONNECTION_STRING",
                                                         "HealthChecks": false,
                                                         "Tracing": true,
                                                         "Metrics": true
                                                       }
                                                     }
                                                   }
                                                 }
                                                 """;
}

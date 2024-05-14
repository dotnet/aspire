// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.ConformanceTests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Milvus.Client;
using Xunit;

namespace Aspire.Milvus.Client.Tests;

public class ConformanceTests : ConformanceTests<MilvusClient, MilvusClientSettings>
{
    protected override bool SupportsKeyedRegistrations => true;

    protected override bool CanCreateClientWithoutConnectingToServer => false;

    protected override bool CanConnectToServer => AspireMilvusHelpers.CanConnectToServer;

    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    protected override string[] RequiredLogCategories => Array.Empty<string>();

    protected override string ActivitySourceName => "";

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<MilvusClientSettings>? configure = null, string? key = null)
    {
        if (key is null)
        {
            builder.AddMilvusClient("milvus", configure);
        }
        else
        {
            builder.AddKeyedMilvusClient(key, configure);
        }
    }

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
        => configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[2]
        {
            new KeyValuePair<string, string?>(CreateConfigKey("Aspire:Milvus:Client", key, "Endpoint"), "http://localhost:19530"),
            new KeyValuePair<string, string?>($"ConnectionStrings:{key}","Endpoint=http://localhost:19530;Key=pass")
        });

    protected override void TriggerActivity(MilvusClient service)
    {
    }

    protected override void SetHealthCheck(MilvusClientSettings options, bool enabled) => throw new NotImplementedException();

    protected override void SetTracing(MilvusClientSettings options, bool enabled) => throw new NotImplementedException();

    protected override void SetMetrics(MilvusClientSettings options, bool enabled) => throw new NotImplementedException();

    protected override string ValidJsonConfig => """
                                                 {
                                                   "Aspire": {
                                                     "Milvus": {
                                                       "Client": {
                                                         "Endpoint": "http://localhost:19530"
                                                       }
                                                     }
                                                   }
                                                 }
                                                 """;

    protected override (string json, string error)[] InvalidJsonToErrorMessage => new[]
        {
            ("""{"Aspire": { "Milvus":{ "Client": { "Endpoint": 3 }}}}""", "Value is \"integer\" but should be \"string\""),
            ("""{"Aspire": { "Milvus":{ "Client": { "Endpoint": "hello" }}}}""", "Value does not match format \"uri\"")
        };

    [Fact]
    public void CanAddMultipleKeyedServices()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:milvus1", "Endpoint=http://localhost1:19530;Key=pass"),
            new KeyValuePair<string, string?>("ConnectionStrings:milvus2", "Endpoint=http://localhost2:19531;Key=pass"),
            new KeyValuePair<string, string?>("ConnectionStrings:milvus3", "Endpoint=http://localhost3:19532;Key=pass"),
        ]);

        builder.AddMilvusClient("milvus1");
        builder.AddKeyedMilvusClient("milvus2");
        builder.AddKeyedMilvusClient("milvus3");

        using var host = builder.Build();

        var client1 = host.Services.GetRequiredService<MilvusClient>();
        var client2 = host.Services.GetRequiredKeyedService<MilvusClient>("milvus2");
        var client3 = host.Services.GetRequiredKeyedService<MilvusClient>("milvus3");

        Assert.NotSame(client1, client2);
        Assert.NotSame(client1, client3);
        Assert.NotSame(client2, client3);
    }
}

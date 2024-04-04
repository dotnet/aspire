// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.ConformanceTests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Qdrant.Client;

namespace Aspire.Qdrant.Client.Tests;
public class ConformanceTests : ConformanceTests<QdrantClient, QdrantClientSettings>
{
    protected override bool SupportsKeyedRegistrations => true;

    protected override bool CanCreateClientWithoutConnectingToServer => false;

    protected override bool CanConnectToServer => AspireQdrantHelpers.CanConnectToServer;

    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    protected override string[] RequiredLogCategories => Array.Empty<string>();

    protected override string ActivitySourceName => "";

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<QdrantClientSettings>? configure = null, string? key = null)
    {
        if (key is null)
        {
            builder.AddQdrantClient("qdrant", configure);
        }
        else
        {
            builder.AddKeyedQdrantClient(key, configure);
        }
    }

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
        => configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[2]
        {
            new KeyValuePair<string, string?>(CreateConfigKey("Aspire:Qdrant:Client", key, "Endpoint"), "http://localhost:6334"),
            new KeyValuePair<string, string?>($"ConnectionStrings:{key}","Endpoint=http://localhost:6334;Key=pass")
        });

    protected override void TriggerActivity(QdrantClient service)
    {
    }

    protected override void SetHealthCheck(QdrantClientSettings options, bool enabled) => throw new NotImplementedException();

    protected override void SetTracing(QdrantClientSettings options, bool enabled) => throw new NotImplementedException();

    protected override void SetMetrics(QdrantClientSettings options, bool enabled) => throw new NotImplementedException();

    protected override string ValidJsonConfig => """
                                                 {
                                                   "Aspire": {
                                                     "Qdrant": {
                                                       "Client": {
                                                         "Endpoint": "http://localhost:6334"
                                                       }
                                                     }
                                                   }
                                                 }
                                                 """;

    protected override (string json, string error)[] InvalidJsonToErrorMessage => new[]
        {
            ("""{"Aspire": { "Qdrant":{ "Client": { "Endpoint": 3 }}}}""", "Value is \"integer\" but should be \"string\""),
            ("""{"Aspire": { "Qdrant":{ "Client": { "Endpoint": "hello" }}}}""", "Value does not match format \"uri\"")
        };
}

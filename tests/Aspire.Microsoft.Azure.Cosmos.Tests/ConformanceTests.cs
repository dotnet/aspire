// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.ConformanceTests;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Microsoft.Azure.Cosmos.Tests;

public class ConformanceTests : ConformanceTests<CosmosClient, AzureCosmosDBSettings>
{
    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    protected override string ActivitySourceName => "Azure.Cosmos.Operation";

    protected override string[] RequiredLogCategories => Array.Empty<string>();

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
        => configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[1]
        {
            new KeyValuePair<string, string?>(CreateConfigKey("Aspire:Microsoft:Azure:Cosmos", key, "ConnectionString"),
                "AccountEndpoint=https://example.documents.azure.com:443/;AccountKey=fake;")
        });

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<AzureCosmosDBSettings>? configure = null, string? key = null)
    {
        if (key is null)
        {
            builder.AddAzureCosmosDbClient("cosmosdb", configure);
        }
        else
        {
            builder.AddKeyedAzureCosmosDbClient(key, configure);
        }
    }

    protected override void SetHealthCheck(AzureCosmosDBSettings options, bool enabled)
        => throw new NotImplementedException();

    protected override void SetTracing(AzureCosmosDBSettings options, bool enabled)
        => options.Tracing = enabled;

    protected override void SetMetrics(AzureCosmosDBSettings options, bool enabled)
        => throw new NotImplementedException();

    protected override string ValidJsonConfig => """
        {
          "Aspire": {
            "Microsoft": {
              "Azure": {
                "Cosmos": {
                  "ConnectionString": "YOUR_CONNECTION_STRING",
                  "Tracing": true
                }
              }
            }
          }
        }
        """;

    protected override (string json, string error)[] InvalidJsonToErrorMessage => new[]
        {
            ("""{"Aspire": { "Microsoft":{ "Azure": { "Cosmos": { "AccountEndpoint": 3 }}}}}""", "Value is \"integer\" but should be \"string\""),
            ("""{"Aspire": { "Microsoft":{ "Azure": { "Cosmos": { "AccountEndpoint": "hello" }}}}}""", "Value does not match format \"uri\"")
        };

    protected override void TriggerActivity(CosmosClient service)
    {
        // TODO: Get rid of GetAwaiter().GetResult()
        service.ReadAccountAsync().GetAwaiter().GetResult();
    }
}

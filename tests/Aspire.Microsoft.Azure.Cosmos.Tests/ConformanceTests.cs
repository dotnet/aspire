// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.ConformanceTests;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Microsoft.Azure.Cosmos.Tests;

public class ConformanceTests : ConformanceTests<CosmosClient, AzureDataCosmosSettings>
{
    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Scoped;

    protected override string ActivitySourceName => "Azure.Cosmos.Operation";

    // TODO
    protected override string[] RequiredLogCategories => Array.Empty<string>();

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
        => configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[1]
        {
            new KeyValuePair<string, string?>(CreateConfigKey("Aspire.Microsoft.Azure.Cosmos", key, "ConnectionString"),
                "AccountEndpoint=https://example.documents.azure.com:443/;AccountKey=fake;")
        });

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<AzureDataCosmosSettings>? configure = null, string? key = null)
        => builder.AddAzureCosmosDB("cosmosdb", configure);

    protected override void SetHealthCheck(AzureDataCosmosSettings options, bool enabled)
        => throw new NotImplementedException();

    protected override void SetTracing(AzureDataCosmosSettings options, bool enabled)
        => options.Tracing = enabled;

    protected override void SetMetrics(AzureDataCosmosSettings options, bool enabled)
        => options.Metrics = enabled;

    protected override string JsonSchemaPath
        => "src/Components/Aspire.Microsoft.Azure.Cosmos/ConfigurationSchema.json";

    protected override string ValidJsonConfig => """
        {
          "Aspire": {
            "Microsoft": {
              "Azure": {
                "Cosmos": {
                  "ConnectionString": "YOUR_CONNECTION_STRING",
                  "HealthChecks": false,
                  "Tracing": true,
                  "Metrics": true
                }
              }
            }
          }
        }
        """;

    protected override void TriggerActivity(CosmosClient service)
    {
        // TODO: Get rid of GetAwaiter().GetResult()
        service.ReadAccountAsync().GetAwaiter().GetResult();
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.ConformanceTests;
using Azure.Identity;
using Azure.Search.Documents.Indexes;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Azure.AI.Search.Tests;

public class ConformanceTests : ConformanceTests<SearchIndexClient, AzureAISearchSettings>
{
    protected const string Endpoint = "https://aspireaisearchtests.search.windows.net/";

    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    protected override string[] RequiredLogCategories => new string[] {
        "Azure.Core",
        "Azure.Identity"
    };

    protected override bool SupportsKeyedRegistrations => true;

    protected override string JsonSchemaPath => "src/Components/Aspire.Azure.AI.Search/ConfigurationSchema.json";

    protected override string ValidJsonConfig => """
        {
          "Aspire": {
            "Azure": {
              "AI": {
                "Search": {
                  "Endpoint": "http://YOUR_URI",
                  "Tracing": true,
                  "ClientOptions": {
                    "ConnectionIdleTimeout": "PT1S",
                    "EnableCrossEntityTransactions": true,
                    "RetryOptions": {
                      "Mode": "Fixed",
                      "MaxDelay": "PT3S"  
                    }
                  }
                }
              }
            }
          }
        }
        """;

    protected override (string json, string error)[] InvalidJsonToErrorMessage => new[]
        {
            ("""{"Aspire": { "Azure": { "AI":{ "Search": {"Endpoint": "YOUR_URI"}}}}}""", "Value does not match format \"uri\""),
            ("""{"Aspire": { "Azure": { "AI":{ "Search": {"Endpoint": "http://YOUR_URI", "Tracing": "false"}}}}}""", "Value is \"string\" but should be \"boolean\""),
        };

    protected override string ActivitySourceName => "Azure.Search.Documents.SearchIndexClient";

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
        => configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[]
        {
            new(CreateConfigKey("Aspire:Azure:AI:Search", key, "Endpoint"), Endpoint)
        });

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<AzureAISearchSettings>? configure = null, string? key = null)
    {
        if (key is null)
        {
            builder.AddAzureAISearch("aisearch", ConfigureCredentials);
        }
        else
        {
            builder.AddKeyedAzureAISearch(key, ConfigureCredentials);
        }

        void ConfigureCredentials(AzureAISearchSettings settings)
        {
            if (CanConnectToServer)
            {
                settings.Credential = new DefaultAzureCredential();
            }

            configure?.Invoke(settings);
        }
    }

    [Fact]
    public void TracingEnablesTheRightActivitySource()
        => RemoteExecutor.Invoke(() => ActivitySourceTest(key: null)).Dispose();

    [Fact]
    public void TracingEnablesTheRightActivitySource_Keyed()
        => RemoteExecutor.Invoke(() => ActivitySourceTest(key: "key")).Dispose();

    protected override void SetHealthCheck(AzureAISearchSettings options, bool enabled)
        => throw new NotImplementedException();

    protected override void SetMetrics(AzureAISearchSettings options, bool enabled)
        => throw new NotImplementedException();

    protected override void SetTracing(AzureAISearchSettings options, bool enabled)
        => options.Tracing = enabled;

    protected override void TriggerActivity(SearchIndexClient service)
        => service.GetIndex("my-index");
}

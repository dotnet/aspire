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

namespace Aspire.Azure.Search.Documents.Tests;

public class ConformanceTests : ConformanceTests<SearchIndexClient, AzureSearchSettings>
{
    protected const string Endpoint = "https://aspireazuresearchtests.search.windows.net/";

    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    protected override string[] RequiredLogCategories => [
        "Azure.Core",
        "Azure.Identity"
    ];

    protected override bool SupportsKeyedRegistrations => true;

    protected override string ValidJsonConfig => """
        {
          "Aspire": {
            "Azure": {
              "Search": {
                "Documents": {
                  "Endpoint": "http://YOUR_URI",
                  "Tracing": true,
                  "ClientOptions": {
                    "Retry": {
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

    protected override (string json, string error)[] InvalidJsonToErrorMessage =>
        [
            ("""{"Aspire": { "Azure": { "Search":{ "Documents": {"Endpoint": "YOUR_URI"}}}}}""", "Value does not match format \"uri\""),
            ("""{"Aspire": { "Azure": { "Search":{ "Documents": {"Endpoint": "http://YOUR_URI", "Tracing": "false"}}}}}""", "Value is \"string\" but should be \"boolean\""),
        ];

    protected override string ActivitySourceName => "Azure.Search.Documents.SearchIndexClient";

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
        => configuration.AddInMemoryCollection(
        [
            new(CreateConfigKey("Aspire:Azure:Search:Documents", key, "Endpoint"), Endpoint)
        ]);

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<AzureSearchSettings>? configure = null, string? key = null)
    {
        if (key is null)
        {
            builder.AddAzureSearch("search", ConfigureCredentials);
        }
        else
        {
            builder.AddKeyedAzureSearch(key, ConfigureCredentials);
        }

        void ConfigureCredentials(AzureSearchSettings settings)
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

    protected override void SetHealthCheck(AzureSearchSettings options, bool enabled)
        => options.HealthChecks = enabled;

    protected override void SetMetrics(AzureSearchSettings options, bool enabled)
        => throw new NotImplementedException();

    protected override void SetTracing(AzureSearchSettings options, bool enabled)
        => options.Tracing = enabled;

    protected override void TriggerActivity(SearchIndexClient service)
        => service.GetIndex("my-index");
}

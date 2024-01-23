// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.ConformanceTests;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Azure.AI.OpenAI.Tests;

public class ConformanceTests : ConformanceTests<OpenAIClient, AzureOpenAISettings>
{
    protected const string Endpoint = "https://aspireopenaitests.openai.azure.com/";

    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    protected override string[] RequiredLogCategories => new string[] {
        "Azure.Core"
    };

    protected override bool SupportsKeyedRegistrations => true;

    protected override string ValidJsonConfig => """
        {
          "Aspire": {
            "Azure": {
              "AI": {
                "OpenAI": {
                  "Endpoint": "http://YOUR_URI",
                  "Tracing": true,
                  "ClientOptions": {
                    "ConnectionIdleTimeout": "PT1S",
                    "EnableCrossEntityTransactions": true,
                    "RetryOptions": {
                      "Mode": "Fixed",
                      "MaxDelay": "PT3S"  
                    },
                    "TransportType": "AmqpWebSockets"
                  }
                }
              }
            }
          }
        }
        """;

    protected override (string json, string error)[] InvalidJsonToErrorMessage => new[]
        {
            ("""{"Aspire": { "Azure": { "AI":{ "OpenAI": {"Endpoint": "YOUR_URI"}}}}}""", "Value does not match format \"uri\""),
            ("""{"Aspire": { "Azure": { "AI":{ "OpenAI": {"Endpoint": "http://YOUR_URI", "Tracing": "false"}}}}}""", "Value is \"string\" but should be \"boolean\""),
        };

    protected override string ActivitySourceName => "Azure.AI.OpenAI.OpenAIClient";

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
        => configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[]
        {
            new(CreateConfigKey("Aspire:Azure:AI:OpenAI", key, "Endpoint"), Endpoint)
        });

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<AzureOpenAISettings>? configure = null, string? key = null)
    {
        if (key is null)
        {
            builder.AddAzureOpenAI("openai", ConfigureCredentials);
        }
        else
        {
            builder.AddKeyedAzureOpenAI(key, ConfigureCredentials);
        }

        void ConfigureCredentials(AzureOpenAISettings settings)
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

    protected override void SetHealthCheck(AzureOpenAISettings options, bool enabled)
        => throw new NotImplementedException();

    protected override void SetMetrics(AzureOpenAISettings options, bool enabled)
        => throw new NotImplementedException();

    protected override void SetTracing(AzureOpenAISettings options, bool enabled)
        => options.Tracing = enabled;

    protected override void TriggerActivity(OpenAIClient service)
        => service.GetCompletions(new CompletionsOptions { DeploymentName = "dummy-gpt" });
}

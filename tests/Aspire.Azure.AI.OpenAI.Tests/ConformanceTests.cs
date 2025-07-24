// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.ConformanceTests;
using Aspire.TestUtilities;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Azure.AI.OpenAI.Tests;

public class ConformanceTests : ConformanceTests<AzureOpenAIClient, AzureOpenAISettings>
{
    protected const string Endpoint = "https://aspireopenaitests.openai.azure.com/";

    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    protected override string[] RequiredLogCategories => [
        // since we don't have a way to connect to the server, we can't test the actual calls
        "Azure.Identity"
    ];

    protected override bool SupportsKeyedRegistrations => true;

    protected override string? ConfigurationSectionName => "Aspire:Azure:AI:OpenAI";

    protected override string ValidJsonConfig => """
        {
          "Aspire": {
            "Azure": {
              "AI": {
                "OpenAI": {
                  "Endpoint": "http://YOUR_URI",
                  "DisableTracing": false,
                  "DisableMetrics": false,
                  "ClientOptions": {
                    "NetworkTimeout": "00:00:02"
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
            ("""{"Aspire": { "Azure": { "AI":{ "OpenAI": {"Endpoint": "http://YOUR_URI", "DisableTracing": "true"}}}}}""", "Value is \"string\" but should be \"boolean\""),
        };

    protected override string ActivitySourceName => "OpenAI.ChatClient";

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
        => configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[]
        {
            new(CreateConfigKey("Aspire:Azure:AI:OpenAI", key, "Endpoint"), Endpoint)
        });

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<AzureOpenAISettings>? configure = null, string? key = null)
    {
        if (key is null)
        {
            builder.AddAzureOpenAIClient("openai", ConfigureCredentials);
        }
        else
        {
            builder.AddKeyedAzureOpenAIClient(key, ConfigureCredentials);
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
        => RemoteExecutor.Invoke(() => ActivitySourceTest(key: null), EnableTelemetry()).Dispose();

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspire/issues/9916")]
    public void TracingEnablesTheRightActivitySource_Keyed()
        => RemoteExecutor.Invoke(() => ActivitySourceTest(key: "key"), EnableTelemetry()).Dispose();

    protected override void SetHealthCheck(AzureOpenAISettings options, bool enabled)
        => throw new NotImplementedException();

    protected override void SetMetrics(AzureOpenAISettings options, bool enabled)
        => options.DisableMetrics = !enabled;

    protected override void SetTracing(AzureOpenAISettings options, bool enabled)
        => options.DisableTracing = !enabled;

    protected override void TriggerActivity(AzureOpenAIClient service)
        => service.GetChatClient("dummy").CompleteChat("dummy gpt");

    private static RemoteInvokeOptions EnableTelemetry()
        => new()
        {
            RuntimeConfigurationOptions = { { "OpenAI.Experimental.EnableOpenTelemetry", true } }
        };
}

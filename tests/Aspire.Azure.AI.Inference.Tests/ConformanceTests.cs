// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.ConformanceTests;
using Azure.AI.Inference;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Azure.AI.Inference.Tests;
public class ConformanceTests : ConformanceTests<ChatCompletionsClient, ChatCompletionsClientSettings>
{
    private const string Endpoint = "https://fakeendpoint";

    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    protected override string ActivitySourceName => "Azure.AI.Inference.ChatCompletionsClient";

    protected override string[] RequiredLogCategories => ["Azure.Identity"];

    protected override string? ConfigurationSectionName => "Aspire:Azure:AI:Inference";

    protected override string ValidJsonConfig => """
        {
          "Aspire": {
            "Azure": {
              "AI": {
                "Inference": {
                  "Endpoint": "http://YOUR_URI",
                  "Key": "YOUR_KEY",
                  "DeploymentId": "DEPLOYMENT_ID",
                  "DisableTracing": false,
                  "DisableMetrics": false
                }
              }
            }
          }
        }
        """;

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
        => configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[]
        {
            new(CreateConfigKey("Aspire:Azure:AI:Inference", key, "Endpoint"), Endpoint)
        });

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<ChatCompletionsClientSettings>? configure = null, string? key = null)
    {
        if (key is null)
        {
            builder.AddAzureChatCompletionsClient("inference", ConfigureCredentials);
        }
        else
        {
            builder.AddAzureChatCompletionsClient(key, ConfigureCredentials);
        }

        void ConfigureCredentials(ChatCompletionsClientSettings settings)
        {
            if (CanConnectToServer)
            {
                settings.TokenCredential = new DefaultAzureCredential();
            }

            configure?.Invoke(settings);
        }
    }

    protected override void SetHealthCheck(ChatCompletionsClientSettings options, bool enabled)
        => throw new NotImplementedException();

    protected override void SetMetrics(ChatCompletionsClientSettings options, bool enabled)
        => options.DisableMetrics = !enabled;

    protected override void SetTracing(ChatCompletionsClientSettings options, bool enabled)
        => options.DisableTracing = !enabled;

    protected override void TriggerActivity(ChatCompletionsClient service)
    {
        service.Complete(new ChatCompletionsOptions { Messages = [new ChatRequestUserMessage("dummy")] });
    }
}

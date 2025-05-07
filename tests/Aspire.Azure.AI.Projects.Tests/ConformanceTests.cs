// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.ConformanceTests;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Azure.AI.Projects.Tests;

public class ConformanceTests : ConformanceTests<AIProjectClient, AzureAIProjectSettings>
{
    private const string ConnectionString = "fake-endpoint.api.azureml.ms;2375c413-6855-548c-bc16-d1326ab8ca77;rg-name;project-name";

    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    protected override string ActivitySourceName => "";

    protected override string[] RequiredLogCategories => [
        // since we don't have a way to connect to the server, we can't test the actual calls
        "Azure.Identity"
    ];

    protected override bool SupportsKeyedRegistrations => true;

    protected override string? ConfigurationSectionName => "Aspire:Azure:AI:Projects";

    protected override string ValidJsonConfig => """
        {
          "Aspire": {
            "Azure": {
              "AI": {
                "Project": {
                  "Endpoint": "http://YOUR_URI",
                  "SubscriptionId": "YOUR_SUBSCRIPTION_ID",
                  "ResourceGroupName": "YOUR_RESOURCE_GROUP_NAME",
                  "ProjectName": "YOUR_PROJECT_NAME",
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
            new(CreateConfigKey("Aspire:Azure:AI:Projects", key, "ConnectionString"), ConnectionString)
        });

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<AzureAIProjectSettings>? configure = null, string? key = null)
    {
        if (key is null)
        {
            builder.AddAzureAIProjectClient("openai", ConfigureCredentials);
        }
        else
        {
            builder.AddKeyedAzureAIProjectClient(key, ConfigureCredentials);
        }

        void ConfigureCredentials(AzureAIProjectSettings settings)
        {
            if (CanConnectToServer)
            {
                settings.Credential = new DefaultAzureCredential();
            }

            configure?.Invoke(settings);
        }
    }

    protected override void SetHealthCheck(AzureAIProjectSettings options, bool enabled)
        => throw new NotImplementedException();

    protected override void SetMetrics(AzureAIProjectSettings options, bool enabled)
        => throw new NotSupportedException();

    protected override void SetTracing(AzureAIProjectSettings options, bool enabled)
        => options.DisableTracing = !enabled;

    protected override void TriggerActivity(AIProjectClient service)
    {
        service.GetAgentsClient().CreateRun("fake thread", "fake assistant");
    }
}

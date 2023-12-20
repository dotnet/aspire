// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.AI.OpenAI;
using Aspire.Components.ConformanceTests;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Azure.Messaging.ServiceBus.Tests;

public abstract class ConformanceTests : ConformanceTests<OpenAIClient, AzureOpenAISettings>
{
    // Fake connection string for cases when credentials are unavailable and need to switch to raw connection string
    protected const string ServiceUri = "https://aspireopenaitests.openai.azure.com/";
    protected const string Key = "fake";

    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    protected override string[] RequiredLogCategories => new string[] { "Azure.AI.OpenAI" };

    protected override bool SupportsKeyedRegistrations => true;

    protected override string JsonSchemaPath => "src/Components/Aspire.Azure.AI.OpenAI/ConfigurationSchema.json";

    protected override string ValidJsonConfig => """
        {
          "Aspire": {
            "Azure": {
              "AI": {
                "OpenAI": {
                  "ServiceUri": "http://YOUR_URI",
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
            ("""{"Aspire": { "Azure": { "AI":{ "OpenAI": {"ServiceUri": "YOUR_URI"}}}}}""", "Value does not match format \"uri\""),
            ("""{"Aspire": { "Azure": { "AI":{ "OpenAI": {"ServiceUri": "http://YOUR_URI", "Trace": "false"}}}}}""", "Value is \"string\" but should be \"boolean\""),
            ("""{"Aspire": { "Azure": { "AI":{ "OpenAI": {"ClientOptions": {"CustomEndpointAddress": "EndPoint"}}}}}}""", "Value does not match format \"uri\""),
            ("""{"Aspire": { "Azure": { "AI":{ "OpenAI": {"ClientOptions": {"EnableCrossEntityTransactions": "false"}}}}}}""", "Value is \"string\" but should be \"boolean\""),
            ("""{"Aspire": { "Azure": { "AI":{ "OpenAI": {"ClientOptions": {"RetryOptions": {"Mode": "Fast"}}}}}}}""", "Value should match one of the values specified by the enum"),
            ("""{"Aspire": { "Azure": { "AI":{ "OpenAI": {"ClientOptions": {"RetryOptions": {"TryTimeout": "3S"}}}}}}}""", "Value does not match format \"duration\""),
            ("""{"Aspire": { "Azure": { "AI":{ "OpenAI": {"ClientOptions": {"TransportType": "HTTP"}}}}}}""", "Value should match one of the values specified by the enum")
        };

    // When credentials are not available, we switch to using raw connection string (otherwise we get CredentialUnavailableException)
    protected KeyValuePair<string, string?> GetMainConfigEntry(string? key)
        => CanConnectToServer
                ? new(CreateConfigKey("Aspire:Azure:AI:OpenAI", key, nameof(AzureOpenAISettings.ServiceUri)), ServiceUri)
                : new(CreateConfigKey("Aspire:Azure:AI:OpenAI", key, nameof(AzureOpenAISettings.Key)), Key);

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<AzureOpenAISettings>? configure = null, string? key = null)
    {
        if (key is null)
        {
            builder.AddAzureAIOpenAI("openai", ConfigureCredentials);
        }
        else
        {
            builder.AddKeyedAzureAIOpenAI(key, ConfigureCredentials);
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

    protected override void SetMetrics(AzureOpenAISettings options, bool enabled)
        => throw new NotImplementedException();

    protected override void SetTracing(AzureOpenAISettings options, bool enabled)
        => options.Tracing = enabled;
}

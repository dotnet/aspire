// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.ConformanceTests;
using Azure.Identity;
using Azure.Messaging.WebPubSub;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Azure.Messaging.WebPubSub.Tests;

public abstract class ConformanceTests : ConformanceTests<WebPubSubServiceClient, AzureMessagingWebPubSubSettings>
{
    public const string Endpoint = "https://aspirewebpubsubtests.webpubsub.azure.com/";
    public const string ReverseProxyEndpoint = "https://reverse.com";
    // Fake connection string for cases when credentials are unavailable and need to switch to raw connection string
    protected const string ConnectionString = "Endpoint=https://aspirewebpubsubtests.webpubsub.azure.com/;AccessKey=fake;";

    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    protected override string ActivitySourceName => "Azure.Messaging.WebPubSub.*";

    protected override string[] RequiredLogCategories => new string[] { "Azure.Messaging.WebPubSub" };

    protected override bool SupportsKeyedRegistrations => true;

    protected override string ValidJsonConfig => """
        {
          "Aspire": {
            "Azure": {
              "Messaging": {
                "WebPubSub": {
                  "Endpoint": "YOUR_ENDPOINT",
                  "HealthChecks": true,
                  "ClientOptions": {
                    "Retry": {
                      "Mode": "Fixed",s
                      "MaxDelay": "PT3S"  
                    },
                    "ReverseProxyEndpoint": "https://reverse.com"
                  }
                }
              }
            }
          }
        }
        """;

    protected override (string json, string error)[] InvalidJsonToErrorMessage => new[]
        {
            ("""{"Aspire": { "Azure": { "Messaging":{ "WebPubSub": {"ClientOptions": {"ReverseProxyEndpoint": "EndPoint"}}}}}}""", "Value does not match format \"uri\""),
            ("""{"Aspire": { "Azure": { "Messaging":{ "WebPubSub": {"HealthChecks": "hello"}}}}}""", "Value is \"string\" but should be \"boolean\""),
            ("""{"Aspire": { "Azure": { "Messaging":{ "WebPubSub": {"ClientOptions": {"RetryOptions": {"Mode": "Fast"}}}}}}}""", "Value should match one of the values specified by the enum"),
            ("""{"Aspire": { "Azure": { "Messaging":{ "WebPubSub": {"ClientOptions": {"RetryOptions": {"MaxDelay": "3S"}}}}}}}""", "Value does not match format \"duration\""),
        };

    // When credentials are not available, we switch to using raw connection string (otherwise we get CredentialUnavailableException)
    protected KeyValuePair<string, string?> GetMainConfigEntry(string? key)
        => CanConnectToServer
                ? new(CreateConfigKey("Aspire:Azure:Messaging:WebPubSub", key, nameof(AzureMessagingWebPubSubSettings.Endpoint)), Endpoint)
                : new(CreateConfigKey("Aspire:Azure:Messaging:WebPubSub", key, nameof(AzureMessagingWebPubSubSettings.ConnectionString)), ConnectionString);

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<AzureMessagingWebPubSubSettings>? configure = null, string? key = null)
    {
        if (key is null)
        {
            builder.AddAzureWebPubSubHub("wps", "hub1", ConfigureCredentials);
        }
        else
        {
            builder.AddKeyedAzureWebPubSubHub(key, "hub1", ConfigureCredentials);
        }

        void ConfigureCredentials(AzureMessagingWebPubSubSettings settings)
        {
            if (CanConnectToServer)
            {
                settings.Credential = new DefaultAzureCredential();
            }
            configure?.Invoke(settings);
        }
    }

    protected override void SetMetrics(AzureMessagingWebPubSubSettings options, bool enabled)
        => throw new NotImplementedException();

    protected override void SetTracing(AzureMessagingWebPubSubSettings options, bool enabled)
        => options.DisableTracing = !enabled;
}

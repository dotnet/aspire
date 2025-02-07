// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.ConformanceTests;
using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Storage.Blobs;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Azure.Messaging.EventHubs.Tests;

public class ConformanceTests : ConformanceTests<EventProcessorClient, AzureMessagingEventHubsProcessorSettings>
{
    // Fake connection string for cases when credentials are unavailable and need to switch to raw connection string
    protected const string ConnectionString = "Endpoint=sb://aspireeventhubstests.servicebus.windows.net/;" +
                                              "SharedAccessKeyName=fake;SharedAccessKey=fake;EntityPath=MyHub";

    private const string BlobsConnectionString = "https://fake.blob.core.windows.net";

    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    protected override string ActivitySourceName => "Aspire.Azure.Messaging.EventHubs.EventProcessorClient";

    protected override string[] RequiredLogCategories => ["Aspire.Azure.Messaging.EventHubs"];

    protected override bool SupportsKeyedRegistrations => true;

    protected override string? ConfigurationSectionName => "Aspire:Azure:Messaging:EventHubs:EventProcessorClient";

    protected override string ValidJsonConfig => """
        {
          "Aspire": {
            "Azure": {
              "Messaging": {
                "EventHubs": {
                  "EventProcessorClient": {
                      "DisableHealthChecks": false,
                      "BlobClientServiceKey": "blobs",
                      "ClientOptions": {
                        "ConnectionIdleTimeout": "00:01",
                        "EnableCrossEntityTransactions": true,
                        "RetryOptions": {
                          "Mode": "Fixed",
                          "MaxDelay": "00:03"
                        },
                        "TransportType": "AmqpWebSockets"
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
             ("""{"Aspire": { "Azure": { "Messaging" :{ "EventHubs": { "EventHubConsumerClient": { "DisableHealthChecks": "true"}}}}}}""", "Value is \"string\" but should be \"boolean\""),
        ];

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
        => configuration.AddInMemoryCollection(
        [
            new("Aspire:Azure:Messaging:EventHubs:EventProcessorClient:ConnectionString", ConnectionString)
        ]);

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<AzureMessagingEventHubsProcessorSettings>? configure = null, string? key = null)
    {
        if (key is null)
        {
            builder.AddAzureEventProcessorClient("ehps", ConfigureCredentials);
        }
        else
        {
            builder.AddKeyedAzureEventProcessorClient(key, ConfigureCredentials);
        }

        var blobClient = new BlobServiceClient(new Uri(BlobsConnectionString), new DefaultAzureCredential());
        builder.Services.AddKeyedSingleton("blobs", blobClient);

        void ConfigureCredentials(AzureMessagingEventHubsProcessorSettings settings)
        {
            if (CanConnectToServer)
            {
                settings.Credential = new DefaultAzureCredential();
            }
            settings.BlobClientServiceKey = "blobs";
            configure?.Invoke(settings);
        }
    }

    [Fact]
    public void TracingEnablesTheRightActivitySource()
        => RemoteExecutor.Invoke(() => ActivitySourceTest(key: null), EnableTelemetry()).Dispose();

    [Fact]
    public void TracingEnablesTheRightActivitySource_Keyed()
        => RemoteExecutor.Invoke(() => ActivitySourceTest(key: "key"), EnableTelemetry()).Dispose();

    protected override void SetHealthCheck(AzureMessagingEventHubsProcessorSettings options, bool enabled)
        => options.DisableHealthChecks = !enabled;

    protected override void SetMetrics(AzureMessagingEventHubsProcessorSettings options, bool enabled)
        => throw new NotImplementedException();

    protected override void SetTracing(AzureMessagingEventHubsProcessorSettings options, bool enabled)
        => options.DisableTracing = !enabled;

    protected override void TriggerActivity(EventProcessorClient service)
    {
        //
    }

    private static RemoteInvokeOptions EnableTelemetry()
    {
        return new RemoteInvokeOptions();
    }
}

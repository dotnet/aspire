// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Aspire.Components.ConformanceTests;
using Azure.Core;
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
    private static readonly Lazy<bool> s_canConnectToServer = new(GetCanConnect);

    public const string ServiceUri = "https://fake.blob.core.windows.net/";

    // Fake connection string for cases when credentials are unavailable and need to switch to raw connection string
    protected const string ConnectionString = "Endpoint=sb://aspireeventhubstests.servicebus.windows.net/;" +
                                              "SharedAccessKeyName=fake;SharedAccessKey=fake;EntityPath=MyHub";

    private const string BlobsConnectionString = "https://fake.blob.core.windows.net";

    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    protected override string ActivitySourceName => "Azure.Messaging.EventHubs.EventProcessorClient";

    protected override string[] RequiredLogCategories => ["Azure.Messaging.EventHubs"];

    protected override bool CanConnectToServer => s_canConnectToServer.Value;

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

        var mockTransport = new MockTransport([CreateResponse("""{}"""), CreateResponse("""{}""")]);
        var blobClient = new BlobServiceClient(new Uri(BlobsConnectionString), new DefaultAzureCredential(), new BlobClientOptions() { Transport = mockTransport });
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

        MockResponse CreateResponse(string content)
        {
            var buffer = Encoding.UTF8.GetBytes(content);
            var response = new MockResponse(201)
            {
                ClientRequestId = Guid.NewGuid().ToString(),
                ContentStream = new MemoryStream(buffer),
            };

            response.AddHeader(new HttpHeader("Content-Type", "application/json; charset=utf-8"));

            return response;
        }
    }

    [Fact]
    public void TracingEnablesTheRightActivitySource()
        => RemoteExecutor.Invoke(() => ActivitySourceTest(key: null), EnableTracingForAzureSdk()).Dispose();

    [Fact]
    public void TracingEnablesTheRightActivitySource_Keyed()
        => RemoteExecutor.Invoke(() => ActivitySourceTest(key: "key"), EnableTracingForAzureSdk()).Dispose();

    protected override void SetHealthCheck(AzureMessagingEventHubsProcessorSettings options, bool enabled)
        => options.DisableHealthChecks = !enabled;

    protected override void SetMetrics(AzureMessagingEventHubsProcessorSettings options, bool enabled)
        => throw new NotImplementedException();

    protected override void SetTracing(AzureMessagingEventHubsProcessorSettings options, bool enabled)
        => options.DisableTracing = !enabled;

    protected override void TriggerActivity(EventProcessorClient service)
    {
        service.ProcessEventAsync += (_) => Task.CompletedTask;
        service.ProcessErrorAsync += (_) => Task.CompletedTask;
        try
        {
            service.StartProcessing();
        }
        catch (Exception)
        {
            // Expected exception
        }
    }

    private static RemoteInvokeOptions EnableTracingForAzureSdk()
        => new()
        {
            RuntimeConfigurationOptions = { { "Azure.Experimental.EnableActivitySource", true } }
        };

    private static bool GetCanConnect()
    {
        BlobClientOptions clientOptions = new();
        clientOptions.Retry.MaxRetries = 0; // don't enable retries (test runs few times faster)
        BlobServiceClient tableClient = new(new Uri(ServiceUri), new DefaultAzureCredential(), clientOptions);

        try
        {
            tableClient.GetBlobContainers().AsPages(pageSizeHint: 1).FirstOrDefault();

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}

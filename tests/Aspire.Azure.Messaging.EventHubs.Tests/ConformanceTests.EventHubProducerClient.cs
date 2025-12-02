// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Azure.Messaging.EventHubs.Tests;

public class ConformanceTests_EventHubProducerClient : ConformanceTestsBase<EventHubProducerClient, AzureMessagingEventHubsProducerSettings>
{
    public ConformanceTests_EventHubProducerClient(ITestOutputHelper? output = null)
        : base(output)
    {
    }

    protected override void SetHealthCheck(AzureMessagingEventHubsProducerSettings options, bool enabled)
        => options.DisableHealthChecks = !enabled;

    protected override void SetMetrics(AzureMessagingEventHubsProducerSettings options, bool enabled)
        => throw new NotImplementedException();

    protected override void SetTracing(AzureMessagingEventHubsProducerSettings options, bool enabled)
        => options.DisableTracing = !enabled;

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<AzureMessagingEventHubsProducerSettings>? configure = null, string? key = null)
    {
        if (key is null)
        {
            builder.AddAzureEventHubProducerClient("ehprc", settings => ConfigureCredentials(configure, settings));
        }
        else
        {
            builder.AddKeyedAzureEventHubProducerClient(key, settings => ConfigureCredentials(configure, settings));
        }

        void ConfigureCredentials(Action<AzureMessagingEventHubsProducerSettings>? configure, AzureMessagingEventHubsProducerSettings settings)
        {
            if (CanConnectToServer)
            {
                settings.Credential = new DefaultAzureCredential();
            }
            configure?.Invoke(settings);
        }
    }

    protected override void TriggerActivity(EventHubProducerClient service)
    {
        try
        {
            var binaryData = BinaryData.FromString("Hello, from /test sent via producerClient");
            service.SendAsync([new EventData(binaryData)]).GetAwaiter().GetResult();
        }
        catch (Exception)
        {
            // Expected exception
        }
    }

    // At the time of writing this seems to be the only client with telemetry
    // c.f. https://learn.microsoft.com/dotnet/api/overview/azure/messaging.eventhubs-readme?view=azure-dotnet#key-concepts

    [Fact]
    public void TracingEnablesTheRightActivitySource()
        => RemoteInvokeWithLogging(() => new ConformanceTests_EventHubProducerClient().ActivitySourceTest(key: null), Output, EnableTracingForAzureSdk());

    [Fact]
    public void TracingEnablesTheRightActivitySource_Keyed()
        => RemoteInvokeWithLogging(() => new ConformanceTests_EventHubProducerClient().ActivitySourceTest(key: "key"), Output, EnableTracingForAzureSdk());
}

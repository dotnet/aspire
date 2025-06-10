// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Hosting;

namespace Aspire.Azure.Messaging.EventHubs.Tests;

public class ConformanceTests_EventHubsBufferedProducerClient : ConformanceTestsBase<EventHubBufferedProducerClient, AzureMessagingEventHubsBufferedProducerSettings>
{
    protected override void SetHealthCheck(AzureMessagingEventHubsBufferedProducerSettings options, bool enabled)
        => options.DisableHealthChecks = !enabled;

    protected override void SetMetrics(AzureMessagingEventHubsBufferedProducerSettings options, bool enabled)
        => throw new NotImplementedException();

    protected override void SetTracing(AzureMessagingEventHubsBufferedProducerSettings options, bool enabled)
        => options.DisableTracing = !enabled;

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<AzureMessagingEventHubsBufferedProducerSettings>? configure = null, string? key = null)
    {
        if (key is null)
        {
            builder.AddAzureEventHubBufferedProducerClient("ehprc", settings => ConfigureCredentials(configure, settings));
        }
        else
        {
            builder.AddKeyedAzureEventHubBufferedProducerClient(key, settings => ConfigureCredentials(configure, settings));
        }

        void ConfigureCredentials(Action<AzureMessagingEventHubsBufferedProducerSettings>? configure, AzureMessagingEventHubsBufferedProducerSettings settings)
        {
            if (CanConnectToServer)
            {
                settings.Credential = new DefaultAzureCredential();
            }
            configure?.Invoke(settings);
        }
    }

    protected override void TriggerActivity(EventHubBufferedProducerClient service)
    {
        try
        {
            var binaryData = BinaryData.FromString("Hello, from /test sent via bufferedProducerClient");
            service.EnqueueEventAsync(new EventData(binaryData)).GetAwaiter().GetResult();
        }
        catch (Exception)
        {
            // Expected exception
        }
    }
}

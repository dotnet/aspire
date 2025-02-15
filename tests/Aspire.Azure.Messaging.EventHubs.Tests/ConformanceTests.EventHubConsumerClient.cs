// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Identity;
using Azure.Messaging.EventHubs.Consumer;
using Microsoft.Extensions.Hosting;

namespace Aspire.Azure.Messaging.EventHubs.Tests;

public class ConformanceTests_EventHubConsumerClient : ConformanceTestsBase<EventHubConsumerClient, AzureMessagingEventHubsConsumerSettings>
{
    protected override void SetHealthCheck(AzureMessagingEventHubsConsumerSettings options, bool enabled)
        => options.DisableHealthChecks = !enabled;

    protected override void SetMetrics(AzureMessagingEventHubsConsumerSettings options, bool enabled)
        => throw new NotImplementedException();

    protected override void SetTracing(AzureMessagingEventHubsConsumerSettings options, bool enabled)
        => options.DisableTracing = !enabled;

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<AzureMessagingEventHubsConsumerSettings>? configure = null, string? key = null)
    {
        if (key is null)
        {
            builder.AddAzureEventHubConsumerClient("ehprc", settings => ConfigureCredentials(configure, settings));
        }
        else
        {
            builder.AddKeyedAzureEventHubConsumerClient(key, settings => ConfigureCredentials(configure, settings));
        }

        void ConfigureCredentials(Action<AzureMessagingEventHubsConsumerSettings>? configure, AzureMessagingEventHubsConsumerSettings settings)
        {
            if (CanConnectToServer)
            {
                settings.Credential = new DefaultAzureCredential();
            }
            configure?.Invoke(settings);
        }
    }

    protected override void TriggerActivity(EventHubConsumerClient service)
    {
        try
        {
            _ = service.ReadEventsAsync().ToBlockingEnumerable().ToArray();
        }
        catch (Exception)
        {
            // Expected exception
        }
    }
}

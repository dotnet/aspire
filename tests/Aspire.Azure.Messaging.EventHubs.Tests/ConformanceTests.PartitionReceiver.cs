// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Identity;
using Azure.Messaging.EventHubs.Primitives;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Aspire.Azure.Messaging.EventHubs.Tests;

public class ConformanceTests_PartitionReceiver : ConformanceTestsBase<PartitionReceiver, AzureMessagingEventHubsPartitionReceiverSettings>
{
    protected override void SetHealthCheck(AzureMessagingEventHubsPartitionReceiverSettings options, bool enabled)
        => options.DisableHealthChecks = !enabled;

    protected override void SetMetrics(AzureMessagingEventHubsPartitionReceiverSettings options, bool enabled)
        => throw new NotImplementedException();

    protected override void SetTracing(AzureMessagingEventHubsPartitionReceiverSettings options, bool enabled)
        => options.DisableTracing = !enabled;

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
    => configuration.AddInMemoryCollection(
    [
        new($"Aspire:Azure:Messaging:EventHubs:{typeof(PartitionReceiver).Name}:ConnectionString", AspireEventHubsExtensionsTests.EhConnectionString),
        new($"Aspire:Azure:Messaging:EventHubs:{typeof(PartitionReceiver).Name}:PartitionId", "2")
    ]);

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<AzureMessagingEventHubsPartitionReceiverSettings>? configure = null, string? key = null)
    {
        if (key is null)
        {
            builder.AddAzurePartitionReceiverClient("ehprc", settings => ConfigureCredentials(configure, settings));
        }
        else
        {
            builder.AddKeyedAzurePartitionReceiverClient(key, settings => ConfigureCredentials(configure, settings));
        }

        AspireEventHubsExtensionsTests.InjectMockBlobClient(builder);

        void ConfigureCredentials(Action<AzureMessagingEventHubsPartitionReceiverSettings>? configure, AzureMessagingEventHubsPartitionReceiverSettings settings)
        {
            if (CanConnectToServer)
            {
                settings.Credential = new DefaultAzureCredential();
            }
            configure?.Invoke(settings);
        }
    }

    protected override void TriggerActivity(PartitionReceiver service)
    {
        try
        {
            _ = service.ReceiveBatchAsync(1).GetAwaiter().GetResult();
        }
        catch (Exception)
        {
            // Expected exception
        }
    }
}

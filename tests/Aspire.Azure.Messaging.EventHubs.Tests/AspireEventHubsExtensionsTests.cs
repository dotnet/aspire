// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Primitives;
using Azure.Messaging.EventHubs.Producer;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Azure.Messaging.EventHubs.Tests;

public class AspireEventHubsExtensionsTests
{
    private const string AspireEventHubsSection = "Aspire:Azure:Messaging:EventHubs:";
    private const string EhConnectionString = "Endpoint=sb://aspireeventhubstests.servicebus.windows.net/;" +
                                              "SharedAccessKeyName=fake;SharedAccessKey=fake;EntityPath=MyHub";
    public const string FullyQualifiedNamespace = "aspireeventhubstests.servicebus.windows.net";
    private const string BlobsConnectionString = "https://fake.blob.core.windows.net";

    private const int EventHubProducerClientIndex = 0;
    private const int EventHubConsumerClientIndex = 1;
    private const int EventProcessorClientIndex = 2;
    private const int PartitionReceiverIndex = 3;

    private static readonly Action<HostApplicationBuilder, string, Action<AzureMessagingEventHubsSettings>?>[] s_keyedClientAdders =
    [
        (builder, key, settings) => builder.AddKeyedAzureEventHubProducerClient(key, settings),
        (builder, key, settings) => builder.AddKeyedAzureEventHubConsumerClient(key, settings),
        (builder, key, settings) => builder.AddKeyedAzureEventProcessorClient(key, settings),
        (builder, key, settings) => builder.AddKeyedAzurePartitionReceiverClient(key, settings)
    ];

    private static readonly Action<HostApplicationBuilder, string, Action<AzureMessagingEventHubsSettings>?>[] s_clientAdders =
    [
        (builder, name, settings) => builder.AddAzureEventHubProducerClient(name, settings),
        (builder, name, settings) => builder.AddAzureEventHubConsumerClient(name, settings),
        (builder, name, settings) => builder.AddAzureEventProcessorClient(name, settings),
        (builder, name, settings) => builder.AddAzurePartitionReceiverClient(name, settings)
    ];

    private static readonly Type[] s_clientTypes =
    [
        typeof(EventHubProducerClient),
        typeof(EventHubConsumerClient),
        typeof(EventProcessorClient),
        typeof(PartitionReceiver)
    ];

    private static void ConfigureBlobServiceClient(bool useKeyed, IServiceCollection services)
    {
        var blobClient = new BlobServiceClient(new Uri(BlobsConnectionString), new DefaultAzureCredential());
        if (useKeyed)
        {
            services.AddKeyedSingleton("blobs", blobClient);
        }
        else
        {
            services.AddSingleton(blobClient);
        }
    }

    [Theory]
    [InlineData(false, EventProcessorClientIndex)]
    [InlineData(true, EventProcessorClientIndex)]
    public void ProcessorClientShouldNotTryCreateContainerWithBlobContainerSpecified(bool useKeyed, int clientIndex)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        ConfigureBlobServiceClient(useKeyed, builder.Services);

        var key = useKeyed ? "eh" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "BlobClientServiceKey"), useKeyed ? "blobs" : null),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "BlobContainerName"), "checkpoints"),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    AspireEventHubsSection + s_clientTypes[clientIndex].Name, key, "PartitionId"), "foo"),
            new KeyValuePair<string, string?>("ConnectionStrings:eh", EhConnectionString),
        ]);

        if (useKeyed)
        {
            s_keyedClientAdders[clientIndex](builder, "eh", null);
        }
        else
        {
            s_clientAdders[clientIndex](builder, "eh", null);
        }

        using var host = builder.Build();

        RetrieveAndAssert(useKeyed, clientIndex, host);
    }

    [Theory]
    [InlineData(false, EventHubProducerClientIndex)]
    [InlineData(true, EventHubProducerClientIndex)]
    [InlineData(false, EventHubConsumerClientIndex)]
    [InlineData(true, EventHubConsumerClientIndex)]
    [InlineData(false, EventProcessorClientIndex)]
    [InlineData(true, EventProcessorClientIndex)]
    [InlineData(false, PartitionReceiverIndex)]
    [InlineData(true, PartitionReceiverIndex)]
    public void ReadsFromConnectionStringsCorrectly(bool useKeyed, int clientIndex)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        ConfigureBlobServiceClient(useKeyed, builder.Services);

        var key = useKeyed ? "eh" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "BlobClientServiceKey"), useKeyed ? "blobs" : null),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "BlobContainerName"), "checkpoints"),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    AspireEventHubsSection + s_clientTypes[clientIndex].Name, key, "PartitionId"), "foo"),
            new KeyValuePair<string, string?>("ConnectionStrings:eh", EhConnectionString)
        ]);

        if (useKeyed)
        {
            s_keyedClientAdders[clientIndex](builder, "eh", null);
        }
        else
        {
            s_clientAdders[clientIndex](builder, "eh", null);
        }

        using var host = builder.Build();
        RetrieveAndAssert(useKeyed, clientIndex, host);
    }

    [Theory]
    [InlineData(false, EventHubProducerClientIndex)]
    [InlineData(true, EventHubProducerClientIndex)]
    [InlineData(false, EventHubConsumerClientIndex)]
    [InlineData(true, EventHubConsumerClientIndex)]
    [InlineData(false, EventProcessorClientIndex)]
    [InlineData(true, EventProcessorClientIndex)]
    [InlineData(false, PartitionReceiverIndex)]
    [InlineData(true, PartitionReceiverIndex)]
    public void ConnectionStringCanBeSetInCode(bool useKeyed, int clientIndex)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        ConfigureBlobServiceClient(useKeyed, builder.Services);

        var key = useKeyed ? "eh" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "BlobClientServiceKey"), useKeyed? "blobs" : null),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "BlobContainerName"), "checkpoints"),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    AspireEventHubsSection + s_clientTypes[clientIndex].Name, key, "PartitionId"), "foo")
        ]);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:eh", EhConnectionString)
        ]);

        if (useKeyed)
        {
            s_keyedClientAdders[clientIndex](builder, "eh", settings => settings.ConnectionString = EhConnectionString);
        }
        else
        {
            s_clientAdders[clientIndex](builder, "eh", settings => settings.ConnectionString = EhConnectionString);
        }

        using var host = builder.Build();
        RetrieveAndAssert(useKeyed, clientIndex, host);
    }

    [Theory]
    [InlineData(false, EventHubProducerClientIndex)]
    [InlineData(true, EventHubProducerClientIndex)]
    [InlineData(false, EventHubConsumerClientIndex)]
    [InlineData(true, EventHubConsumerClientIndex)]
    [InlineData(false, EventProcessorClientIndex)]
    [InlineData(true, EventProcessorClientIndex)]
    [InlineData(false, PartitionReceiverIndex)]
    [InlineData(true, PartitionReceiverIndex)]
    public void ConnectionNameWinsOverConfigSection(bool useKeyed, int clientIndex)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        ConfigureBlobServiceClient(useKeyed, builder.Services);

        var key = useKeyed ? "eh" : null;
        builder.Configuration.AddInMemoryCollection([
            // component settings
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "ConnectionString"), "unused"),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "BlobContainerName"), "checkpoints"),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "BlobClientServiceKey"), useKeyed ? "blobs" : null),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "PartitionId"), "foo"),

            // ambient connection strings
            new KeyValuePair<string, string?>("ConnectionStrings:eh", EhConnectionString),
        ]);

        if (useKeyed)
        {
            s_keyedClientAdders[clientIndex](builder, "eh", null);
        }
        else
        {
            s_clientAdders[clientIndex](builder, "eh", null);
        }

        using var host = builder.Build();
        RetrieveAndAssert(useKeyed, clientIndex, host);
    }

    private static void RetrieveAndAssert(bool useKeyed, int clientIndex, IHost host)
    {
        var client = RetrieveClient(useKeyed, clientIndex, host);

        AssertFullyQualifiedNamespace(client);
    }

    private static object RetrieveClient(bool useKeyed, int clientIndex, IHost host)
    {
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService(s_clientTypes[clientIndex], "eh") :
            host.Services.GetRequiredService(s_clientTypes[clientIndex]);

        return client;
    }

    private static void AssertFullyQualifiedNamespace(object client)
    {
        Assert.Equal(FullyQualifiedNamespace, client switch
        {
            EventHubProducerClient producer => producer.FullyQualifiedNamespace,
            EventHubConsumerClient consumer => consumer.FullyQualifiedNamespace,
            EventProcessorClient processor => processor.FullyQualifiedNamespace,
            PartitionReceiver receiver => receiver.FullyQualifiedNamespace,
            _ => throw new InvalidOperationException()
        });
    }

    [Theory]
    [InlineData(false, EventHubProducerClientIndex)]
    [InlineData(true, EventHubProducerClientIndex)]
    [InlineData(false, EventHubConsumerClientIndex)]
    [InlineData(true, EventHubConsumerClientIndex)]
    [InlineData(false, EventProcessorClientIndex)]
    [InlineData(true, EventProcessorClientIndex)]
    [InlineData(false, PartitionReceiverIndex)]
    [InlineData(true, PartitionReceiverIndex)]
    public void NamespaceWorksInConnectionStrings(bool useKeyed, int clientIndex)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        ConfigureBlobServiceClient(useKeyed, builder.Services);

        var key = useKeyed ? "eh" : null;

        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "EventHubName"), "MyHub"),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "BlobClientServiceKey"), useKeyed ? "blobs" : null),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "BlobContainerName"), "checkpoints"),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "PartitionId"), "foo"),

            new KeyValuePair<string, string?>("ConnectionStrings:eh", EhConnectionString),
        ]);

        if (useKeyed)
        {
            s_keyedClientAdders[clientIndex](builder, "eh", null);
        }
        else
        {
            s_clientAdders[clientIndex](builder, "eh", null);
        }

        using var host = builder.Build();
        RetrieveAndAssert(useKeyed, clientIndex, host);
    }

    public static string CreateConfigKey(string prefix, string? key, string suffix)
        => string.IsNullOrEmpty(key) ? $"{prefix}:{suffix}" : $"{prefix}:{key}:{suffix}";
}

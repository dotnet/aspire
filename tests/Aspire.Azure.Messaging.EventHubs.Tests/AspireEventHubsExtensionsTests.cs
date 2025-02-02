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
    private const int EventBufferedProducerClientIndex = 4;

    private static readonly Action<HostApplicationBuilder, string, Action<AzureMessagingEventHubsSettings>?>[] s_keyedClientAdders =
    [
        (builder, key, settings) => builder.AddKeyedAzureEventHubProducerClient(key, settings),
        (builder, key, settings) => builder.AddKeyedAzureEventHubConsumerClient(key, settings),
        (builder, key, settings) => builder.AddKeyedAzureEventProcessorClient(key, settings),
        (builder, key, settings) => builder.AddKeyedAzurePartitionReceiverClient(key, settings),
        (builder, key, settings) => builder.AddKeyedAzureEventHubBufferedProducerClient(key, settings),
    ];

    private static readonly Action<HostApplicationBuilder, string, Action<AzureMessagingEventHubsSettings>?>[] s_clientAdders =
    [
        (builder, name, settings) => builder.AddAzureEventHubProducerClient(name, settings),
        (builder, name, settings) => builder.AddAzureEventHubConsumerClient(name, settings),
        (builder, name, settings) => builder.AddAzureEventProcessorClient(name, settings),
        (builder, name, settings) => builder.AddAzurePartitionReceiverClient(name, settings),
        (builder, name, settings) => builder.AddAzureEventHubBufferedProducerClient(name, settings),
    ];

    private static readonly Type[] s_clientTypes =
    [
        typeof(EventHubProducerClient),
        typeof(EventHubConsumerClient),
        typeof(EventProcessorClient),
        typeof(PartitionReceiver),
        typeof(EventHubBufferedProducerClient)
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
    [InlineData(false, EventBufferedProducerClientIndex)]
    [InlineData(true, EventBufferedProducerClientIndex)]
    public void BindsClientOptionsFromConfigurationWithNamespace(bool useKeyed, int clientIndex)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        ConfigureBlobServiceClient(useKeyed, builder.Services);

        var key = useKeyed ? "eh" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "ClientOptions:Identifier"), "customidentifier"),
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
                    AspireEventHubsSection + s_clientTypes[clientIndex].Name, key, "PartitionId"), "foo"),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "FullyQualifiedNamespace"), FullyQualifiedNamespace),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "EventHubName"), "MyHub"),
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

        var assignedIdentifier = RetrieveClient(key, clientIndex, host) switch
        {
            EventProcessorClient processor => processor.Identifier,
            EventHubConsumerClient consumer => consumer.Identifier,
            EventHubProducerClient producer => producer.Identifier,
            PartitionReceiver receiver => receiver.Identifier,
            EventHubBufferedProducerClient producer => producer.Identifier,
            _ => null
        };

        Assert.NotNull(assignedIdentifier);
        Assert.Equal("customidentifier", assignedIdentifier);
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
    [InlineData(false, EventBufferedProducerClientIndex)]
    [InlineData(true, EventBufferedProducerClientIndex)]
    public void BindsClientOptionsFromConfigurationWithConnectionString(bool useKeyed, int clientIndex)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        ConfigureBlobServiceClient(useKeyed, builder.Services);

        var key = useKeyed ? "eh" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "ClientOptions:Identifier"), "customidentifier"),
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

        var assignedIdentifier = RetrieveClient(key, clientIndex, host) switch
        {
            EventProcessorClient processor => processor.Identifier,
            EventHubConsumerClient consumer => consumer.Identifier,
            EventHubProducerClient producer => producer.Identifier,
            PartitionReceiver receiver => receiver.Identifier,
            EventHubBufferedProducerClient producer => producer.Identifier,
            _ => null
        };

        Assert.NotNull(assignedIdentifier);
        Assert.Equal("customidentifier", assignedIdentifier);
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
    [InlineData(false, EventBufferedProducerClientIndex)]
    [InlineData(true, EventBufferedProducerClientIndex)]
    public void BindsClientOptionsFromConfigurationWithConnectionStringAndEventHubName(bool useKeyed, int clientIndex)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        ConfigureBlobServiceClient(useKeyed, builder.Services);

        var key = useKeyed ? "eh" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "ClientOptions:Identifier"), "customidentifier"),
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
                    AspireEventHubsSection + s_clientTypes[clientIndex].Name, key, "PartitionId"), "foo"),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "EventHubName"), "MyHub"),
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

        var assignedIdentifier = RetrieveClient(key, clientIndex, host) switch
        {
            EventProcessorClient processor => processor.Identifier,
            EventHubConsumerClient consumer => consumer.Identifier,
            EventHubProducerClient producer => producer.Identifier,
            PartitionReceiver receiver => receiver.Identifier,
            EventHubBufferedProducerClient producer => producer.Identifier,
            _ => null
        };

        Assert.NotNull(assignedIdentifier);
        Assert.Equal("customidentifier", assignedIdentifier);
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
    [InlineData(false, EventBufferedProducerClientIndex)]
    [InlineData(true, EventBufferedProducerClientIndex)]
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
    [InlineData(false, EventBufferedProducerClientIndex)]
    [InlineData(true, EventBufferedProducerClientIndex)]
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
    [InlineData(false, EventBufferedProducerClientIndex)]
    [InlineData(true, EventBufferedProducerClientIndex)]
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
        var client = RetrieveClient(useKeyed ? "eh" : null, clientIndex, host);

        AssertFullyQualifiedNamespace(FullyQualifiedNamespace, client);
    }

    private static object RetrieveClient(object? key, int clientIndex, IHost host)
    {
        var client = key is not null ?
            host.Services.GetRequiredKeyedService(s_clientTypes[clientIndex], key) :
            host.Services.GetRequiredService(s_clientTypes[clientIndex]);

        return client;
    }

    private static void AssertFullyQualifiedNamespace(string expectedNamespace, object client)
    {
        Assert.Equal(expectedNamespace, client switch
        {
            EventHubProducerClient producer => producer.FullyQualifiedNamespace,
            EventHubConsumerClient consumer => consumer.FullyQualifiedNamespace,
            EventProcessorClient processor => processor.FullyQualifiedNamespace,
            PartitionReceiver receiver => receiver.FullyQualifiedNamespace,
            EventHubBufferedProducerClient  producer => producer.FullyQualifiedNamespace,
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
    [InlineData(false, EventBufferedProducerClientIndex)]
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

    [Theory]
    [InlineData(EventHubProducerClientIndex)]
    [InlineData(EventHubConsumerClientIndex)]
    [InlineData(EventProcessorClientIndex)]
    [InlineData(PartitionReceiverIndex)]
    [InlineData(EventBufferedProducerClientIndex)]
    public void CanAddMultipleKeyedServices(int clientIndex)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:eh1", EhConnectionString),
            new KeyValuePair<string, string?>($"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}:BlobContainerName", "checkpoints"),
            new KeyValuePair<string, string?>($"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}:PartitionId", "foo"),

            new KeyValuePair<string, string?>("ConnectionStrings:eh2", EhConnectionString.Replace("aspireeventhubstests", "aspireeventhubstests2")),
            new KeyValuePair<string, string?>($"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}:eh2:BlobContainerName", "checkpoints"),
            new KeyValuePair<string, string?>($"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}:eh2:PartitionId", "foo"),

            new KeyValuePair<string, string?>("ConnectionStrings:eh3", EhConnectionString.Replace("aspireeventhubstests", "aspireeventhubstests3")),
            new KeyValuePair<string, string?>($"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}:eh3:BlobContainerName", "checkpoints"),
            new KeyValuePair<string, string?>($"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}:eh3:PartitionId", "foo"),
        ]);

        ConfigureBlobServiceClient(useKeyed: false, builder.Services);

        s_clientAdders[clientIndex](builder, "eh1", null);
        s_keyedClientAdders[clientIndex](builder, "eh2", null);
        s_keyedClientAdders[clientIndex](builder, "eh3", null);

        using var host = builder.Build();

        // Unkeyed services don't work with keyed services. See https://github.com/dotnet/aspire/issues/3890
        //var client1 = RetrieveClient(key: null, clientIndex, host);
        var client2 = RetrieveClient(key: "eh2", clientIndex, host);
        var client3 = RetrieveClient(key: "eh3", clientIndex, host);

        //Assert.NotSame(client1, client2);
        //Assert.NotSame(client1, client3);
        Assert.NotSame(client2, client3);

        //AssertFullyQualifiedNamespace("aspireeventhubstests.servicebus.windows.net", client1);
        AssertFullyQualifiedNamespace("aspireeventhubstests2.servicebus.windows.net", client2);
        AssertFullyQualifiedNamespace("aspireeventhubstests3.servicebus.windows.net", client3);
    }

    public static string CreateConfigKey(string prefix, string? key, string suffix)
        => string.IsNullOrEmpty(key) ? $"{prefix}:{suffix}" : $"{prefix}:{key}:{suffix}";
}

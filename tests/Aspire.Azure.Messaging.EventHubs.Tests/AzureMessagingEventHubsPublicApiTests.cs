// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core.Extensions;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Primitives;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Azure.Messaging.EventHubs.Tests;

public class AzureMessagingEventHubsPublicApiTests
{
    [Fact]
    public void AddAzureEventProcessorClientShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string connectionName = "EventHubs";

        var action = () => builder.AddAzureEventProcessorClient(
            connectionName,
            default(Action<AzureMessagingEventHubsProcessorSettings>?),
            default(Action<IAzureClientBuilder<EventProcessorClient, EventProcessorClientOptions>>?));

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddAzureEventProcessorClientShouldThrowWhenConnectionNameIsNullOrEmpty(bool isNull)
    {
        IHostApplicationBuilder builder = new HostApplicationBuilder();
        var connectionName = isNull ? null! : string.Empty;

        var action = () => builder.AddAzureEventProcessorClient(
            connectionName,
            default(Action<AzureMessagingEventHubsProcessorSettings>?),
            default(Action<IAzureClientBuilder<EventProcessorClient, EventProcessorClientOptions>>?));

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(connectionName), exception.ParamName);
    }

    [Fact]
    public void AddKeyedAzureEventProcessorClientShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string name = "EventHubs";

        var action = () => builder.AddKeyedAzureEventProcessorClient(
            name,
            default(Action<AzureMessagingEventHubsProcessorSettings>?),
            default(Action<IAzureClientBuilder<EventProcessorClient, EventProcessorClientOptions>>?));

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddKeyedAzureEventProcessorClientShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        IHostApplicationBuilder builder = new HostApplicationBuilder();
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddKeyedAzureEventProcessorClient(
            name,
            default(Action<AzureMessagingEventHubsProcessorSettings>?),
            default(Action<IAzureClientBuilder<EventProcessorClient, EventProcessorClientOptions>>?));

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void AddAzurePartitionReceiverClientShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string connectionName = "EventHubs";

        var action = () => builder.AddAzurePartitionReceiverClient(
            connectionName,
            default(Action<AzureMessagingEventHubsPartitionReceiverSettings>?),
            default(Action<IAzureClientBuilder<PartitionReceiver, PartitionReceiverOptions>>?));

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddAzurePartitionReceiverClientShouldThrowWhenConnectionNameIsNullOrEmpty(bool isNull)
    {
        IHostApplicationBuilder builder = new HostApplicationBuilder();
        var connectionName = isNull ? null! : string.Empty;

        var action = () => builder.AddAzurePartitionReceiverClient(
            connectionName,
            default(Action<AzureMessagingEventHubsPartitionReceiverSettings>?),
            default(Action<IAzureClientBuilder<PartitionReceiver, PartitionReceiverOptions>>?));

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(connectionName), exception.ParamName);
    }

    [Fact]
    public void AddKeyedAzurePartitionReceiverClientShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string name = "EventHubs";

        var action = () => builder.AddKeyedAzurePartitionReceiverClient(
            name,
            default(Action<AzureMessagingEventHubsPartitionReceiverSettings>?),
            default(Action<IAzureClientBuilder<PartitionReceiver, PartitionReceiverOptions>>?));

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddKeyedAzurePartitionReceiverClientShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        IHostApplicationBuilder builder = new HostApplicationBuilder();
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddKeyedAzurePartitionReceiverClient(
            name,
            default(Action<AzureMessagingEventHubsPartitionReceiverSettings>?),
            default(Action<IAzureClientBuilder<PartitionReceiver, PartitionReceiverOptions>>?));

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void AddAzureEventHubProducerClientShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string connectionName = "EventHubs";

        var action = () => builder.AddAzureEventHubProducerClient(
            connectionName,
            default(Action<AzureMessagingEventHubsProducerSettings>?),
            default(Action<IAzureClientBuilder<EventHubProducerClient, EventHubProducerClientOptions>>?));

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddAzureEventHubProducerClientShouldThrowWhenConnectionNameIsNullOrEmpty(bool isNull)
    {
        IHostApplicationBuilder builder = new HostApplicationBuilder();
        var connectionName = isNull ? null! : string.Empty;

        var action = () => builder.AddAzureEventHubProducerClient(
            connectionName,
            default(Action<AzureMessagingEventHubsProducerSettings>?),
            default(Action<IAzureClientBuilder<EventHubProducerClient, EventHubProducerClientOptions>>?));

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(connectionName), exception.ParamName);
    }

    [Fact]
    public void AddKeyedAzureEventHubProducerClientShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string name = "EventHubs";

        var action = () => builder.AddKeyedAzureEventHubProducerClient(
            name,
            default(Action<AzureMessagingEventHubsProducerSettings>?),
            default(Action<IAzureClientBuilder<EventHubProducerClient, EventHubProducerClientOptions>>?));

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddKeyedAzureEventHubProducerClientShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        IHostApplicationBuilder builder = new HostApplicationBuilder();
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddKeyedAzureEventHubProducerClient(
            name,
            default(Action<AzureMessagingEventHubsProducerSettings>?),
            default(Action<IAzureClientBuilder<EventHubProducerClient, EventHubProducerClientOptions>>?));

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void AddAzureEventHubBufferedProducerClientShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string connectionName = "EventHubs";

        var action = () => builder.AddAzureEventHubBufferedProducerClient(
            connectionName,
            default(Action<AzureMessagingEventHubsBufferedProducerSettings>?),
            default(Action<IAzureClientBuilder<EventHubBufferedProducerClient, EventHubBufferedProducerClientOptions>>?));

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddAzureEventHubBufferedProducerClientShouldThrowWhenConnectionNameIsNullOrEmpty(bool isNull)
    {
        IHostApplicationBuilder builder = new HostApplicationBuilder();
        var connectionName = isNull ? null! : string.Empty;

        var action = () => builder.AddAzureEventHubBufferedProducerClient(
            connectionName,
            default(Action<AzureMessagingEventHubsBufferedProducerSettings>?),
            default(Action<IAzureClientBuilder<EventHubBufferedProducerClient, EventHubBufferedProducerClientOptions>>?));

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(connectionName), exception.ParamName);
    }

    [Fact]
    public void AddKeyedAzureEventHubBufferedProducerClientShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string name = "EventHubs";

        var action = () => builder.AddKeyedAzureEventHubBufferedProducerClient(
            name,
            default(Action<AzureMessagingEventHubsBufferedProducerSettings>?),
            default(Action<IAzureClientBuilder<EventHubBufferedProducerClient, EventHubBufferedProducerClientOptions>>?));

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddKeyedAzureEventHubBufferedProducerClientShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        IHostApplicationBuilder builder = new HostApplicationBuilder();
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddKeyedAzureEventHubBufferedProducerClient(
            name,
            default(Action<AzureMessagingEventHubsBufferedProducerSettings>?),
            default(Action<IAzureClientBuilder<EventHubBufferedProducerClient, EventHubBufferedProducerClientOptions>>?));

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void AddAzureEventHubConsumerClientShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string connectionName = "EventHubs";

        var action = () => builder.AddAzureEventHubConsumerClient(
            connectionName,
            default(Action<AzureMessagingEventHubsConsumerSettings>?),
            default(Action<IAzureClientBuilder<EventHubConsumerClient, EventHubConsumerClientOptions>>?));

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddAzureEventHubConsumerClientShouldThrowWhenConnectionNameIsNullOrEmpty(bool isNull)
    {
        IHostApplicationBuilder builder = new HostApplicationBuilder();
        var connectionName = isNull ? null! : string.Empty;

        var action = () => builder.AddAzureEventHubConsumerClient(
            connectionName,
            default(Action<AzureMessagingEventHubsConsumerSettings>?),
            default(Action<IAzureClientBuilder<EventHubConsumerClient, EventHubConsumerClientOptions>>?));

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(connectionName), exception.ParamName);
    }

    [Fact]
    public void AddKeyedAzureEventHubConsumerClientShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string name = "EventHubs";

        var action = () => builder.AddKeyedAzureEventHubConsumerClient(
            name,
            default(Action<AzureMessagingEventHubsConsumerSettings>?),
            default(Action<IAzureClientBuilder<EventHubConsumerClient, EventHubConsumerClientOptions>>?));

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddKeyedAzureEventHubConsumerClientShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        IHostApplicationBuilder builder = new HostApplicationBuilder();
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddKeyedAzureEventHubConsumerClient(
            name,
            default(Action<AzureMessagingEventHubsConsumerSettings>?),
            default(Action<IAzureClientBuilder<EventHubConsumerClient, EventHubConsumerClientOptions>>?));

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.Azure.Tests;

public class ResourceWithAzureFunctionsConfigTests
{
    [Fact]
    public void AzureStorageResource_ImplementsIResourceWithAzureFunctionsConfig()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var storageResource = builder.AddAzureStorage("storage").Resource;

        // Act & Assert
        Assert.IsAssignableFrom<IResourceWithAzureFunctionsConfig>(storageResource);
    }

    [Fact]
    public void AzureBlobStorageResource_ImplementsIResourceWithAzureFunctionsConfig()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var storageResource = builder.AddAzureStorage("storage");
        var blobResource = storageResource.AddBlobs("blobs").Resource;

        // Act & Assert
        Assert.IsAssignableFrom<IResourceWithAzureFunctionsConfig>(blobResource);
    }

    [Fact]
    public void AzureQueueStorageResource_ImplementsIResourceWithAzureFunctionsConfig()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var storageResource = builder.AddAzureStorage("storage");
        var queueResource = storageResource.AddQueues("queues").Resource;

        // Act & Assert
        Assert.IsAssignableFrom<IResourceWithAzureFunctionsConfig>(queueResource);
    }

    [Fact]
    public void AzureCosmosDBResource_ImplementsIResourceWithAzureFunctionsConfig()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var cosmosResource = builder.AddAzureCosmosDB("cosmos").Resource;

        // Act & Assert
        Assert.IsAssignableFrom<IResourceWithAzureFunctionsConfig>(cosmosResource);
    }

    [Fact]
    public void AzureCosmosDBDatabaseResource_ImplementsIResourceWithAzureFunctionsConfig()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var cosmosResource = builder.AddAzureCosmosDB("cosmos");
        var dbResource = cosmosResource.AddCosmosDatabase("database").Resource;

        // Act & Assert
        Assert.IsAssignableFrom<IResourceWithAzureFunctionsConfig>(dbResource);
    }

    [Fact]
    public void AzureCosmosDBContainerResource_ImplementsIResourceWithAzureFunctionsConfig()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var cosmosResource = builder.AddAzureCosmosDB("cosmos");
        var dbResource = cosmosResource.AddCosmosDatabase("database");
        var containerResource = dbResource.AddContainer("container", "/id").Resource;

        // Act & Assert
        Assert.IsAssignableFrom<IResourceWithAzureFunctionsConfig>(containerResource);
    }

    [Fact]
    public void AzureEventHubsResource_ImplementsIResourceWithAzureFunctionsConfig()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var eventHubsResource = builder.AddAzureEventHubs("eventhubs").Resource;

        // Act & Assert
        Assert.IsAssignableFrom<IResourceWithAzureFunctionsConfig>(eventHubsResource);
    }

    [Fact]
    public void AzureServiceBusResource_ImplementsIResourceWithAzureFunctionsConfig()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var serviceBusResource = builder.AddAzureServiceBus("servicebus").Resource;

        // Act & Assert
        Assert.IsAssignableFrom<IResourceWithAzureFunctionsConfig>(serviceBusResource);
    }

    [Fact]
    public void AzureStorageEmulator_AppliesCorrectConfigurationFormat()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage").RunAsEmulator().Resource;
        var target = new Dictionary<string, object>();

        // Act
        ((IResourceWithAzureFunctionsConfig)storage).ApplyAzureFunctionsConfiguration(target, "myconnection");

        // Assert
        Assert.True(target.ContainsKey("myconnection"));
        Assert.True(target.ContainsKey("Aspire__Azure__Storage__Blobs__myconnection__ConnectionString"));
        Assert.True(target.ContainsKey("Aspire__Azure__Storage__Queues__myconnection__ConnectionString"));
        Assert.True(target.ContainsKey("Aspire__Azure__Storage__Tables__myconnection__ConnectionString"));
    }

    [Fact]
    public void AzureStorage_AppliesCorrectConfigurationFormat()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var storage = builder.AddAzureStorage("storage").Resource;
        var target = new Dictionary<string, object>();

        // Act
        ((IResourceWithAzureFunctionsConfig)storage).ApplyAzureFunctionsConfiguration(target, "myconnection");

        // Assert
        Assert.True(target.ContainsKey("myconnection__blobServiceUri"));
        Assert.True(target.ContainsKey("myconnection__queueServiceUri"));
        Assert.True(target.ContainsKey("myconnection__tableServiceUri"));
        Assert.True(target.ContainsKey("Aspire__Azure__Storage__Blobs__myconnection__ServiceUri"));
        Assert.True(target.ContainsKey("Aspire__Azure__Storage__Queues__myconnection__ServiceUri"));
        Assert.True(target.ContainsKey("Aspire__Azure__Storage__Tables__myconnection__ServiceUri"));
    }

    [Fact]
    public void AzureBlobStorage_AppliesCorrectConfigurationFormat()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage").RunAsEmulator();
        var blobResource = storage.AddBlobs("blobs").Resource;
        var target = new Dictionary<string, object>();

        // Act
        ((IResourceWithAzureFunctionsConfig)blobResource).ApplyAzureFunctionsConfiguration(target, "myblobs");

        // Assert
        Assert.True(target.ContainsKey("myblobs"));
        Assert.True(target.ContainsKey("Aspire__Azure__Storage__Blobs__myblobs__ConnectionString"));
    }

    [Fact]
    public void AzureTableStorage_AppliesCorrectConfigurationFormat()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage").RunAsEmulator();
        var tableResource = storage.AddTables("tables").Resource;
        var target = new Dictionary<string, object>();

        // Act
        ((IResourceWithAzureFunctionsConfig)tableResource).ApplyAzureFunctionsConfiguration(target, "mytables");

        // Assert
        Assert.True(target.ContainsKey("mytables"));
        Assert.True(target.ContainsKey("Aspire__Azure__Storage__Tables__mytables__ConnectionString"));
    }

    [Fact]
    public void AzureQueueStorage_AppliesCorrectConfigurationFormat()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage").RunAsEmulator();
        var queueResource = storage.AddQueues("queues").Resource;
        var target = new Dictionary<string, object>();

        // Act
        ((IResourceWithAzureFunctionsConfig)queueResource).ApplyAzureFunctionsConfiguration(target, "myqueues");

        // Assert
        Assert.True(target.ContainsKey("myqueues"));
        Assert.True(target.ContainsKey("Aspire__Azure__Storage__Queues__myqueues__ConnectionString"));
    }

    [Fact]
    public void AzureCosmosDBEmulator_AppliesCorrectConfigurationFormat()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var cosmosResource = builder.AddAzureCosmosDB("cosmos").RunAsEmulator().Resource;
        var target = new Dictionary<string, object>();

        // Act
        ((IResourceWithAzureFunctionsConfig)cosmosResource).ApplyAzureFunctionsConfiguration(target, "mycosmosdb");

        // Assert
        Assert.True(target.ContainsKey("mycosmosdb"));
        Assert.True(target.ContainsKey("Aspire__Microsoft__Azure__Cosmos__mycosmosdb__ConnectionString"));
    }

    [Fact]
    public void AzureCosmosDB_WithAccessKey_AppliesCorrectConfigurationFormat()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var cosmosResource = builder.AddAzureCosmosDB("cosmos").WithAccessKeyAuthentication().Resource;
        var target = new Dictionary<string, object>();

        // Act
        ((IResourceWithAzureFunctionsConfig)cosmosResource).ApplyAzureFunctionsConfiguration(target, "mycosmosdb");

        // Assert
        Assert.True(target.ContainsKey("mycosmosdb"));
        Assert.True(target.ContainsKey("Aspire__Microsoft__Azure__Cosmos__mycosmosdb__ConnectionString"));
    }

    [Fact]
    public void AzureCosmosDB_WithEntraID_AppliesCorrectConfigurationFormat()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var cosmosResource = builder.AddAzureCosmosDB("cosmos").Resource;
        var target = new Dictionary<string, object>();

        // Act
        ((IResourceWithAzureFunctionsConfig)cosmosResource).ApplyAzureFunctionsConfiguration(target, "mycosmosdb");

        // Assert
        Assert.True(target.ContainsKey("mycosmosdb__accountEndpoint"));
        Assert.True(target.ContainsKey("Aspire__Microsoft__Azure__Cosmos__mycosmosdb__AccountEndpoint"));
    }

    [Fact]
    public void AzureEventHubsEmulator_AppliesCorrectConfigurationFormat()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var eventHubsResource = builder.AddAzureEventHubs("eventhubs").RunAsEmulator().Resource;
        var target = new Dictionary<string, object>();

        // Act
        ((IResourceWithAzureFunctionsConfig)eventHubsResource).ApplyAzureFunctionsConfiguration(target, "myeventhubs");

        // Assert
        Assert.True(target.ContainsKey("myeventhubs"));
        Assert.Contains("Aspire__Azure__Messaging__EventHubs__EventHubProducerClient__myeventhubs__ConnectionString", target.Keys);
        Assert.Contains("Aspire__Azure__Messaging__EventHubs__EventHubConsumerClient__myeventhubs__ConnectionString", target.Keys);
        Assert.Contains("Aspire__Azure__Messaging__EventHubs__EventProcessorClient__myeventhubs__ConnectionString", target.Keys);
        Assert.Contains("Aspire__Azure__Messaging__EventHubs__PartitionReceiver__myeventhubs__ConnectionString", target.Keys);
        Assert.Contains("Aspire__Azure__Messaging__EventHubs__EventHubBufferedProducerClient__myeventhubs__ConnectionString", target.Keys);
    }

    [Fact]
    public void AzureEventHubs_AppliesCorrectConfigurationFormat()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var eventHubsResource = builder.AddAzureEventHubs("eventhubs").Resource;
        var target = new Dictionary<string, object>();

        // Act
        ((IResourceWithAzureFunctionsConfig)eventHubsResource).ApplyAzureFunctionsConfiguration(target, "myeventhubs");

        // Assert
        Assert.True(target.ContainsKey("myeventhubs__fullyQualifiedNamespace"));
        Assert.Contains("Aspire__Azure__Messaging__EventHubs__EventHubProducerClient__myeventhubs__FullyQualifiedNamespace", target.Keys);
        Assert.Contains("Aspire__Azure__Messaging__EventHubs__EventHubConsumerClient__myeventhubs__FullyQualifiedNamespace", target.Keys);
        Assert.Contains("Aspire__Azure__Messaging__EventHubs__EventProcessorClient__myeventhubs__FullyQualifiedNamespace", target.Keys);
        Assert.Contains("Aspire__Azure__Messaging__EventHubs__PartitionReceiver__myeventhubs__FullyQualifiedNamespace", target.Keys);
        Assert.Contains("Aspire__Azure__Messaging__EventHubs__EventHubBufferedProducerClient__myeventhubs__FullyQualifiedNamespace", target.Keys);
    }

    [Fact]
    public void AzureServiceBus_AppliesCorrectConfigurationFormat()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var serviceBusResource = builder.AddAzureServiceBus("servicebus").Resource;
        var target = new Dictionary<string, object>();

        // Act
        ((IResourceWithAzureFunctionsConfig)serviceBusResource).ApplyAzureFunctionsConfiguration(target, "myservicebus");

        // Assert
        Assert.True(target.ContainsKey("myservicebus__fullyQualifiedNamespace"));
        Assert.Contains("Aspire__Azure__Messaging__ServiceBus__myservicebus__FullyQualifiedNamespace", target.Keys);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureStorageConnectionPropertiesTests
{
    [Fact]
    public void AzureStorageResourceGetConnectionPropertiesReturnsExpectedValues()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage");

        var properties = ((IResourceWithConnectionString)storage.Resource).GetConnectionProperties().ToArray();

        Assert.Collection(
            properties,
            property =>
            {
                Assert.Equal("BlobUri", property.Key);
                Assert.Equal("{storage.outputs.blobEndpoint}", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("QueueUri", property.Key);
                Assert.Equal("{storage.outputs.queueEndpoint}", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("TableUri", property.Key);
                Assert.Equal("{storage.outputs.tableEndpoint}", property.Value.ValueExpression);
            });
    }

    [Fact]
    public void AzureStorageResourceGetConnectionPropertiesReturnsConnectionStringForEmulator()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage").RunAsEmulator();

        var properties = ((IResourceWithConnectionString)storage.Resource).GetConnectionProperties().ToArray();

        Assert.Collection(
            properties,
            property =>
            {
                Assert.Equal("ConnectionString", property.Key);
                Assert.Equal(
                    "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint={storage.bindings.blob.scheme}://{storage.bindings.blob.host}:{storage.bindings.blob.port}/devstoreaccount1;QueueEndpoint={storage.bindings.queue.scheme}://{storage.bindings.queue.host}:{storage.bindings.queue.port}/devstoreaccount1;TableEndpoint={storage.bindings.table.scheme}://{storage.bindings.table.host}:{storage.bindings.table.port}/devstoreaccount1;",
                    property.Value.ValueExpression);
            });
    }

    [Fact]
    public void AzureBlobStorageResourceGetConnectionPropertiesReturnsExpectedValues()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage");
        var blobs = storage.AddBlobs("blobs");

        var properties = ((IResourceWithConnectionString)blobs.Resource).GetConnectionProperties().ToArray();

        Assert.Collection(
            properties,
            property =>
            {
                Assert.Equal("Uri", property.Key);
                Assert.Equal("{storage.outputs.blobEndpoint}", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("ConnectionString", property.Key);
                Assert.Equal("{storage.outputs.blobEndpoint}", property.Value.ValueExpression);
            });
    }

    [Fact]
    public void AzureQueueStorageResourceGetConnectionPropertiesReturnsExpectedValues()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage");
        var queues = storage.AddQueues("queues");

        var properties = ((IResourceWithConnectionString)queues.Resource).GetConnectionProperties().ToArray();

        Assert.Collection(
            properties,
            property =>
            {
                Assert.Equal("Uri", property.Key);
                Assert.Equal("{storage.outputs.queueEndpoint}", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("ConnectionString", property.Key);
                Assert.Equal("{storage.outputs.queueEndpoint}", property.Value.ValueExpression);
            });
    }

    [Fact]
    public void AzureTableStorageResourceGetConnectionPropertiesReturnsExpectedValues()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage");
        var tables = storage.AddTables("tables");

        var properties = ((IResourceWithConnectionString)tables.Resource).GetConnectionProperties().ToArray();

        Assert.Collection(
            properties,
            property =>
            {
                Assert.Equal("Uri", property.Key);
                Assert.Equal("{storage.outputs.tableEndpoint}", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("ConnectionString", property.Key);
                Assert.Equal("{storage.outputs.tableEndpoint}", property.Value.ValueExpression);
            });
    }
}

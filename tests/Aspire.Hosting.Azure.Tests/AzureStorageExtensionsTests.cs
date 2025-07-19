// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Azure.Provisioning.Storage;
using Microsoft.Extensions.DependencyInjection;
using static Aspire.Hosting.Utils.AzureManifestUtils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureStorageExtensionsTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    public void AzureStorageUseEmulatorCallbackWithWithDataBindMountResultsInBindMountAnnotationWithDefaultPath(bool? isReadOnly)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage").RunAsEmulator(configureContainer: builder =>
        {
            if (isReadOnly.HasValue)
            {
                builder.WithDataBindMount(isReadOnly: isReadOnly.Value);
            }
            else
            {
                builder.WithDataBindMount();
            }
        });

        var volumeAnnotation = storage.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();
        Assert.Equal(Path.Combine(builder.AppHostDirectory, ".azurite", "storage"), volumeAnnotation.Source);
        Assert.Equal("/data", volumeAnnotation.Target);
        Assert.Equal(ContainerMountType.BindMount, volumeAnnotation.Type);
        Assert.Equal(isReadOnly ?? false, volumeAnnotation.IsReadOnly);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    public void AzureStorageUseEmulatorCallbackWithWithDataBindMountResultsInBindMountAnnotation(bool? isReadOnly)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage").RunAsEmulator(configureContainer: builder =>
        {
            if (isReadOnly.HasValue)
            {
                builder.WithDataBindMount("mydata", isReadOnly: isReadOnly.Value);
            }
            else
            {
                builder.WithDataBindMount("mydata");
            }
        });

        var volumeAnnotation = storage.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();
        Assert.Equal(Path.Combine(builder.AppHostDirectory, "mydata"), volumeAnnotation.Source);
        Assert.Equal("/data", volumeAnnotation.Target);
        Assert.Equal(ContainerMountType.BindMount, volumeAnnotation.Type);
        Assert.Equal(isReadOnly ?? false, volumeAnnotation.IsReadOnly);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    public void AzureStorageUseEmulatorCallbackWithWithDataVolumeResultsInVolumeAnnotationWithDefaultName(bool? isReadOnly)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage").RunAsEmulator(configureContainer: builder =>
        {
            if (isReadOnly.HasValue)
            {
                builder.WithDataVolume(isReadOnly: isReadOnly.Value);
            }
            else
            {
                builder.WithDataVolume();
            }
        });

        var volumeAnnotation = storage.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();
        Assert.Equal($"{builder.GetVolumePrefix()}-storage-data", volumeAnnotation.Source);
        Assert.Equal("/data", volumeAnnotation.Target);
        Assert.Equal(ContainerMountType.Volume, volumeAnnotation.Type);
        Assert.Equal(isReadOnly ?? false, volumeAnnotation.IsReadOnly);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    public void AzureStorageUseEmulatorCallbackWithWithDataVolumeResultsInVolumeAnnotation(bool? isReadOnly)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage").RunAsEmulator(configureContainer: builder =>
        {
            if (isReadOnly.HasValue)
            {
                builder.WithDataVolume("mydata", isReadOnly: isReadOnly.Value);
            }
            else
            {
                builder.WithDataVolume("mydata");
            }
        });

        var volumeAnnotation = storage.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();
        Assert.Equal("mydata", volumeAnnotation.Source);
        Assert.Equal("/data", volumeAnnotation.Target);
        Assert.Equal(ContainerMountType.Volume, volumeAnnotation.Type);
        Assert.Equal(isReadOnly ?? false, volumeAnnotation.IsReadOnly);
    }

    [Fact]
    public void AzureStorageUserEmulatorUseBlobQueueTablePortMethodsMutateEndpoints()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage").RunAsEmulator(configureContainer: builder =>
        {
            builder.WithBlobPort(9001);
            builder.WithQueuePort(9002);
            builder.WithTablePort(9003);
        });

        Assert.Collection(
            storage.Resource.Annotations.OfType<EndpointAnnotation>(),
            e => Assert.Equal(9001, e.Port),
            e => Assert.Equal(9002, e.Port),
            e => Assert.Equal(9003, e.Port));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AddAzureStorage_WithApiVersionCheck_ShouldSetSkipApiVersionCheck(bool enableApiVersionCheck)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage").RunAsEmulator(x => x.WithApiVersionCheck(enableApiVersionCheck));

        var args = await ArgumentEvaluator.GetArgumentListAsync(storage.Resource);

        Assert.All(["azurite", "-l", "/data", "--blobHost", "0.0.0.0", "--queueHost", "0.0.0.0", "--tableHost", "0.0.0.0"], x => args.Contains(x));

        if (enableApiVersionCheck)
        {
            Assert.Contains("--skipApiVersionCheck", args);
        }
        else
        {
            Assert.DoesNotContain("--skipApiVersionCheck", args);
        }
    }

    [Fact]
    public async Task AddAzureStorage_RunAsEmulator_SetSkipApiVersionCheck()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage").RunAsEmulator();

        var args = await ArgumentEvaluator.GetArgumentListAsync(storage.Resource);

        Assert.Contains("--skipApiVersionCheck", args);
    }

    [Fact]
    public async Task AddBlobs_ConnectionString_resolved_expected_RunAsEmulator()
    {
        const string expected = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;";

        using var builder = TestDistributedApplicationBuilder.Create();

        var storage = builder.AddAzureStorage("storage").RunAsEmulator(e =>
        {
            e.WithEndpoint("blob", e => e.AllocatedEndpoint = new(e, "localhost", 10000));
            e.WithEndpoint("queue", e => e.AllocatedEndpoint = new(e, "localhost", 10001));
            e.WithEndpoint("table", e => e.AllocatedEndpoint = new(e, "localhost", 10002));
        });

        Assert.True(storage.Resource.IsContainer());

        var blobs = storage.GetBlobService();

        Assert.Equal(expected, await ((IResourceWithConnectionString)blobs.Resource).ConnectionStringExpression.GetValueAsync(default));
    }

    [Fact]
    public async Task AddBlobs_ConnectionString_resolved_expected()
    {
        const string blobsConnectionString = "https://myblob";

        using var builder = TestDistributedApplicationBuilder.Create();

        var storagesku = builder.AddParameter("storagesku");
        var storage = builder.AddAzureStorage("storage");
        storage.Resource.Outputs["blobEndpoint"] = blobsConnectionString;

        var blobs = storage.GetBlobService();

        Assert.Equal(blobsConnectionString, await ((IResourceWithConnectionString)blobs.Resource).ConnectionStringExpression.GetValueAsync(default));
    }

    [Fact]
    public void AddBlobs_ConnectionString_unresolved_expected()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var storage = builder.AddAzureStorage("storage");
        var blobs = storage.GetBlobService();

        Assert.Equal("{storage.outputs.blobEndpoint}", blobs.Resource.ConnectionStringExpression.ValueExpression);
    }

    [Fact]
    public async Task AddBlobContainer_ConnectionString_resolved_expected_RunAsEmulator()
    {
        const string blobContainerName = "my-blob-container";

        using var builder = TestDistributedApplicationBuilder.Create();

        var storage = builder.AddAzureStorage("storage").RunAsEmulator(e =>
        {
            e.WithEndpoint("blob", e => e.AllocatedEndpoint = new(e, "localhost", 10000));
            e.WithEndpoint("queue", e => e.AllocatedEndpoint = new(e, "localhost", 10001));
            e.WithEndpoint("table", e => e.AllocatedEndpoint = new(e, "localhost", 10002));
        });

        Assert.True(storage.Resource.IsContainer());

        var blobs = storage.GetBlobService();
        var blobContainer = storage.AddBlobContainer(name: "myContainer", blobContainerName);

        string? blobConnectionString = await ((IResourceWithConnectionString)blobs.Resource).ConnectionStringExpression.GetValueAsync(default);
        string? blobContainerConnectionString = await ((IResourceWithConnectionString)blobContainer.Resource).ConnectionStringExpression.GetValueAsync(default);

        Assert.NotNull(blobConnectionString);
        Assert.Contains(blobConnectionString, blobContainerConnectionString);
        Assert.Contains($"ContainerName={blobContainerName}", blobContainerConnectionString);
    }

    [Fact]
    public async Task AddBlobContainer_ConnectionString_resolved_expected()
    {
        const string blobContainerName = "my-blob-container";

        using var builder = TestDistributedApplicationBuilder.Create();

        var storagesku = builder.AddParameter("storagesku");
        var storage = builder.AddAzureStorage("storage");
        storage.Resource.Outputs["blobEndpoint"] = "https://myblob";

        var blobs = storage.GetBlobService();
        var blobContainer = storage.AddBlobContainer(name: "myContainer", blobContainerName);

        string? blobsConnectionString = await ((IResourceWithConnectionString)blobs.Resource).ConnectionStringExpression.GetValueAsync(default);
        string expected = $"Endpoint={blobsConnectionString};ContainerName={blobContainerName}";

        Assert.Equal(expected, await ((IResourceWithConnectionString)blobContainer.Resource).ConnectionStringExpression.GetValueAsync(default));
    }

    [Fact]
    public void AddBlobContainer_ConnectionString_unresolved_expected()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var storage = builder.AddAzureStorage("storage");
        var blobContainer = storage.AddBlobContainer(name: "myContainer");

        Assert.Equal("Endpoint={storage.outputs.blobEndpoint};ContainerName=myContainer", blobContainer.Resource.ConnectionStringExpression.ValueExpression);
    }

    [Fact]
    public async Task AddQueues_ConnectionString_resolved_expected_RunAsEmulator()
    {
        const string expected = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;";

        using var builder = TestDistributedApplicationBuilder.Create();

        var storage = builder.AddAzureStorage("storage").RunAsEmulator(e =>
        {
            e.WithEndpoint("blob", e => e.AllocatedEndpoint = new(e, "localhost", 10000));
            e.WithEndpoint("queue", e => e.AllocatedEndpoint = new(e, "localhost", 10001));
            e.WithEndpoint("table", e => e.AllocatedEndpoint = new(e, "localhost", 10002));
        });

        Assert.True(storage.Resource.IsContainer());

        var queues = storage.GetQueueService();

        Assert.Equal(expected, await ((IResourceWithConnectionString)queues.Resource).GetConnectionStringAsync());
    }

    [Fact]
    public async Task AddQueues_ConnectionString_resolved_expected()
    {
        const string connectionString = "https://myblob";

        using var builder = TestDistributedApplicationBuilder.Create();

        var storagesku = builder.AddParameter("storagesku");
        var storage = builder.AddAzureStorage("storage");
        storage.Resource.Outputs["queueEndpoint"] = connectionString;

        var queues = storage.GetQueueService();

        Assert.Equal(connectionString, await ((IResourceWithConnectionString)queues.Resource).GetConnectionStringAsync());
    }

    [Fact]
    public void AddQueues_ConnectionString_unresolved_expected()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var storage = builder.AddAzureStorage("storage");
        var queues = storage.GetQueueService();

        Assert.Equal("{storage.outputs.queueEndpoint}", queues.Resource.ConnectionStringExpression.ValueExpression);
    }

    [Fact]
    public async Task AddQueue_ConnectionString_resolved_expected_RunAsEmulator()
    {
        const string queueName = "my-queue";

        using var builder = TestDistributedApplicationBuilder.Create();

        var storage = builder.AddAzureStorage("storage").RunAsEmulator(e =>
        {
            e.WithEndpoint("blob", e => e.AllocatedEndpoint = new(e, "localhost", 10000));
            e.WithEndpoint("queue", e => e.AllocatedEndpoint = new(e, "localhost", 10001));
            e.WithEndpoint("table", e => e.AllocatedEndpoint = new(e, "localhost", 10002));
        });

        Assert.True(storage.Resource.IsContainer());

        var queues = storage.GetQueueService();
        var queue = storage.AddQueue(name: "myqueue", queueName);

        string? connectionString = await ((IResourceWithConnectionString)queues.Resource).GetConnectionStringAsync();
        string expected = $"{connectionString};QueueName={queueName}";

        Assert.Equal(expected, await ((IResourceWithConnectionString)queue.Resource).GetConnectionStringAsync());
    }

    [Fact]
    public async Task AddQueue_ConnectionString_resolved_expected()
    {
        const string queueName = "my-queue";

        using var builder = TestDistributedApplicationBuilder.Create();

        var storagesku = builder.AddParameter("storagesku");
        var storage = builder.AddAzureStorage("storage");
        storage.Resource.Outputs["queueEndpoint"] = "https://myqueue";

        var queues = storage.GetQueueService();
        var queue = storage.AddQueue(name: "myqueue", queueName);

        string? connectionString = await ((IResourceWithConnectionString)queues.Resource).GetConnectionStringAsync();
        string expected = $"Endpoint={connectionString};QueueName={queueName}";

        Assert.Equal(expected, await ((IResourceWithConnectionString)queue.Resource).GetConnectionStringAsync());
    }

    [Fact]
    public void AddQueue_ConnectionString_unresolved_expected()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var storage = builder.AddAzureStorage("storage");
        var queues = storage.GetQueueService();
        var queue = storage.AddQueue(name: "myqueue");

        Assert.Equal("Endpoint={storage.outputs.queueEndpoint};QueueName=myqueue", queue.Resource.ConnectionStringExpression.ValueExpression);
    }

    [Fact]
    public async Task ResourceNamesBicepValid()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage");

        var blob = storage.AddBlobContainer(name: "myContainer", blobContainerName: "my-blob-container");
        var queues = storage.GetQueueService();
        var queue = storage.AddQueue(name: "myqueue", queueName: "my-queue");
        var tables = storage.GetTableService();

        var manifest = await AzureManifestUtils.GetManifestWithBicep(storage.Resource);

        await Verify(manifest.BicepText, extension: "bicep");
    }

    [Fact]
    public async Task AddAzureStorageEmulator()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var storage = builder.AddAzureStorage("storage").RunAsEmulator(e =>
        {
            e.WithEndpoint("blob", e => e.AllocatedEndpoint = new(e, "localhost", 10000));
            e.WithEndpoint("queue", e => e.AllocatedEndpoint = new(e, "localhost", 10001));
            e.WithEndpoint("table", e => e.AllocatedEndpoint = new(e, "localhost", 10002));
        });

        Assert.True(storage.Resource.IsContainer());

        var blob = storage.GetBlobService();
        var queue = storage.GetQueueService();
        var table = storage.GetTableService();

        EndpointReference GetEndpointReference(string name, int port)
            => new(storage.Resource, new EndpointAnnotation(ProtocolType.Tcp, name: name, targetPort: port));

        var blobqs = AzureStorageEmulatorConnectionString.Create(blobEndpoint: GetEndpointReference("blob", 10000)).ValueExpression;
        var queueqs = AzureStorageEmulatorConnectionString.Create(queueEndpoint: GetEndpointReference("queue", 10001)).ValueExpression;
        var tableqs = AzureStorageEmulatorConnectionString.Create(tableEndpoint: GetEndpointReference("table", 10002)).ValueExpression;

        Assert.Equal(blobqs, blob.Resource.ConnectionStringExpression.ValueExpression);
        Assert.Equal(queueqs, queue.Resource.ConnectionStringExpression.ValueExpression);
        Assert.Equal(tableqs, table.Resource.ConnectionStringExpression.ValueExpression);

        string Resolve(string? qs, string name, int port) =>
            qs!.Replace("{storage.bindings." + name + ".host}", "127.0.0.1")
               .Replace("{storage.bindings." + name + ".scheme}", "http")
               .Replace("{storage.bindings." + name + ".port}", port.ToString());

        Assert.Equal(Resolve(blobqs, "blob", 10000), await ((IResourceWithConnectionString)blob.Resource).GetConnectionStringAsync());
        Assert.Equal(Resolve(queueqs, "queue", 10001), await ((IResourceWithConnectionString)queue.Resource).GetConnectionStringAsync());
        Assert.Equal(Resolve(tableqs, "table", 10002), await ((IResourceWithConnectionString)table.Resource).GetConnectionStringAsync());
    }

    [Fact]
    public async Task AddAzureStorageViaRunMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var storagesku = builder.AddParameter("storagesku");
        var storage = builder.AddAzureStorage("storage")
            .ConfigureInfrastructure(infrastructure =>
            {
                var sa = infrastructure.GetProvisionableResources().OfType<StorageAccount>().Single();
                sa.Sku = new StorageSku()
                {
                    Name = storagesku.AsProvisioningParameter(infrastructure)
                };
            });

        storage.Resource.Outputs["blobEndpoint"] = "https://myblob";
        storage.Resource.Outputs["queueEndpoint"] = "https://myqueue";
        storage.Resource.Outputs["tableEndpoint"] = "https://mytable";

        // Check storage resource.
        Assert.Equal("storage", storage.Resource.Name);

        var storageManifest = await AzureManifestUtils.GetManifestWithBicep(storage.Resource);

        var expectedStorageManifest = """
            {
              "type": "azure.bicep.v0",
              "path": "storage.module.bicep",
              "params": {
                "storagesku": "{storagesku.value}"
              }
            }
            """;
        Assert.Equal(expectedStorageManifest, storageManifest.ManifestNode.ToString());

        await Verify(storageManifest.BicepText, extension: "bicep");

        // Check blob resource.
        var blob = storage.GetBlobService();

        var connectionStringBlobResource = (IResourceWithConnectionString)blob.Resource;

        Assert.Equal("https://myblob", await connectionStringBlobResource.GetConnectionStringAsync());
        var expectedBlobManifest = """
            {
              "type": "value.v0",
              "connectionString": "{storage.outputs.blobEndpoint}"
            }
            """;
        var blobManifest = await ManifestUtils.GetManifest(blob.Resource);
        Assert.Equal(expectedBlobManifest, blobManifest.ToString());

        // Check queue resource.
        var queue = storage.GetQueueService();

        var connectionStringQueueResource = (IResourceWithConnectionString)queue.Resource;

        Assert.Equal("https://myqueue", await connectionStringQueueResource.GetConnectionStringAsync());
        var expectedQueueManifest = """
            {
              "type": "value.v0",
              "connectionString": "{storage.outputs.queueEndpoint}"
            }
            """;
        var queueManifest = await ManifestUtils.GetManifest(queue.Resource);
        Assert.Equal(expectedQueueManifest, queueManifest.ToString());

        // Check table resource.
        var table = storage.GetTableService();

        var connectionStringTableResource = (IResourceWithConnectionString)table.Resource;

        Assert.Equal("https://mytable", await connectionStringTableResource.GetConnectionStringAsync());
        var expectedTableManifest = """
            {
              "type": "value.v0",
              "connectionString": "{storage.outputs.tableEndpoint}"
            }
            """;
        var tableManifest = await ManifestUtils.GetManifest(table.Resource);
        Assert.Equal(expectedTableManifest, tableManifest.ToString());
    }

    [Fact]
    public async Task AddAzureStorageViaRunModeAllowSharedKeyAccessOverridesDefaultFalse()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var storagesku = builder.AddParameter("storagesku");
        var storage = builder.AddAzureStorage("storage")
            .ConfigureInfrastructure(infrastructure =>
            {
                var sa = infrastructure.GetProvisionableResources().OfType<StorageAccount>().Single();
                sa.Sku = new StorageSku()
                {
                    Name = storagesku.AsProvisioningParameter(infrastructure)
                };
                sa.AllowSharedKeyAccess = true;
            });

        storage.Resource.Outputs["blobEndpoint"] = "https://myblob";
        storage.Resource.Outputs["queueEndpoint"] = "https://myqueue";
        storage.Resource.Outputs["tableEndpoint"] = "https://mytable";

        // Check storage resource.
        Assert.Equal("storage", storage.Resource.Name);

        var storageManifest = await AzureManifestUtils.GetManifestWithBicep(storage.Resource);

        var expectedStorageManifest = """
            {
              "type": "azure.bicep.v0",
              "path": "storage.module.bicep",
              "params": {
                "storagesku": "{storagesku.value}"
              }
            }
            """;
        Assert.Equal(expectedStorageManifest, storageManifest.ManifestNode.ToString());

        await Verify(storageManifest.BicepText, extension: "bicep");

        // Check blob resource.
        var blob = storage.GetBlobService();

        var connectionStringBlobResource = (IResourceWithConnectionString)blob.Resource;

        Assert.Equal("https://myblob", await connectionStringBlobResource.GetConnectionStringAsync());
        var expectedBlobManifest = """
            {
              "type": "value.v0",
              "connectionString": "{storage.outputs.blobEndpoint}"
            }
            """;
        var blobManifest = await ManifestUtils.GetManifest(blob.Resource);
        Assert.Equal(expectedBlobManifest, blobManifest.ToString());

        // Check queue resource.
        var queue = storage.GetQueueService();

        var connectionStringQueueResource = (IResourceWithConnectionString)queue.Resource;

        Assert.Equal("https://myqueue", await connectionStringQueueResource.GetConnectionStringAsync());
        var expectedQueueManifest = """
            {
              "type": "value.v0",
              "connectionString": "{storage.outputs.queueEndpoint}"
            }
            """;
        var queueManifest = await ManifestUtils.GetManifest(queue.Resource);
        Assert.Equal(expectedQueueManifest, queueManifest.ToString());

        // Check table resource.
        var table = storage.GetTableService();

        var connectionStringTableResource = (IResourceWithConnectionString)table.Resource;

        Assert.Equal("https://mytable", await connectionStringTableResource.GetConnectionStringAsync());
        var expectedTableManifest = """
            {
              "type": "value.v0",
              "connectionString": "{storage.outputs.tableEndpoint}"
            }
            """;
        var tableManifest = await ManifestUtils.GetManifest(table.Resource);
        Assert.Equal(expectedTableManifest, tableManifest.ToString());
    }

    [Fact]
    public async Task AddAzureStorageViaPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var storagesku = builder.AddParameter("storagesku");
        var storage = builder.AddAzureStorage("storage")
            .ConfigureInfrastructure(infrastructure =>
            {
                var sa = infrastructure.GetProvisionableResources().OfType<StorageAccount>().Single();
                sa.Sku = new StorageSku()
                {
                    Name = storagesku.AsProvisioningParameter(infrastructure)
                };
            });

        var blob = storage.GetBlobService();
        var queue = storage.GetQueueService();
        var table = storage.GetTableService();

        storage.Resource.Outputs["blobEndpoint"] = "https://myblob";
        storage.Resource.Outputs["queueEndpoint"] = "https://myqueue";
        storage.Resource.Outputs["tableEndpoint"] = "https://mytable";

        // Check storage resource.
        Assert.Equal("storage", storage.Resource.Name);

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var storageManifest = await GetManifestWithBicep(model, storage.Resource);

        var expectedStorageManifest = """
            {
              "type": "azure.bicep.v0",
              "path": "storage.module.bicep",
              "params": {
                "storagesku": "{storagesku.value}"
              }
            }
            """;
        Assert.Equal(expectedStorageManifest, storageManifest.ManifestNode.ToString());

        await Verify(storageManifest.BicepText, extension: "bicep");

        var storageRoles = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == "storage-roles");
        var storageRolesManifest = await GetManifestWithBicep(storageRoles, skipPreparer: true);
        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param storage_outputs_name string

            param principalType string

            param principalId string

            resource storage 'Microsoft.Storage/storageAccounts@2024-01-01' existing = {
              name: storage_outputs_name
            }

            resource storage_StorageBlobDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(storage.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
                principalType: principalType
              }
              scope: storage
            }

            resource storage_StorageTableDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(storage.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3')
                principalType: principalType
              }
              scope: storage
            }

            resource storage_StorageQueueDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(storage.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88')
                principalType: principalType
              }
              scope: storage
            }
            """;
        output.WriteLine(storageRolesManifest.BicepText);
        Assert.Equal(expectedBicep, storageRolesManifest.BicepText);

        // Check blob resource.

        var connectionStringBlobResource = (IResourceWithConnectionString)blob.Resource;

        Assert.Equal("https://myblob", await connectionStringBlobResource.GetConnectionStringAsync());
        var expectedBlobManifest = """
            {
              "type": "value.v0",
              "connectionString": "{storage.outputs.blobEndpoint}"
            }
            """;
        var blobManifest = await ManifestUtils.GetManifest(blob.Resource);
        Assert.Equal(expectedBlobManifest, blobManifest.ToString());

        // Check queue resource.
        var connectionStringQueueResource = (IResourceWithConnectionString)queue.Resource;

        Assert.Equal("https://myqueue", await connectionStringQueueResource.GetConnectionStringAsync());
        var expectedQueueManifest = """
            {
              "type": "value.v0",
              "connectionString": "{storage.outputs.queueEndpoint}"
            }
            """;
        var queueManifest = await ManifestUtils.GetManifest(queue.Resource);
        Assert.Equal(expectedQueueManifest, queueManifest.ToString());

        // Check table resource.
        var connectionStringTableResource = (IResourceWithConnectionString)table.Resource;

        Assert.Equal("https://mytable", await connectionStringTableResource.GetConnectionStringAsync());
        var expectedTableManifest = """
            {
              "type": "value.v0",
              "connectionString": "{storage.outputs.tableEndpoint}"
            }
            """;
        var tableManifest = await ManifestUtils.GetManifest(table.Resource);
        Assert.Equal(expectedTableManifest, tableManifest.ToString());
    }

    [Fact]
    public async Task AddAzureStorageViaPublishModeEnableAllowSharedKeyAccessOverridesDefaultFalse()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var storagesku = builder.AddParameter("storagesku");
        var storage = builder.AddAzureStorage("storage")
            .ConfigureInfrastructure(infrastructure =>
            {
                var sa = infrastructure.GetProvisionableResources().OfType<StorageAccount>().Single();
                sa.Sku = new StorageSku()
                {
                    Name = storagesku.AsProvisioningParameter(infrastructure)
                };
                sa.AllowSharedKeyAccess = true;
            });

        storage.Resource.Outputs["blobEndpoint"] = "https://myblob";
        storage.Resource.Outputs["queueEndpoint"] = "https://myqueue";
        storage.Resource.Outputs["tableEndpoint"] = "https://mytable";

        // Check storage resource.
        Assert.Equal("storage", storage.Resource.Name);

        var storageManifest = await AzureManifestUtils.GetManifestWithBicep(storage.Resource);

        var expectedStorageManifest = """
            {
              "type": "azure.bicep.v0",
              "path": "storage.module.bicep",
              "params": {
                "storagesku": "{storagesku.value}"
              }
            }
            """;

        Assert.Equal(expectedStorageManifest, storageManifest.ManifestNode.ToString());

        await Verify(storageManifest.BicepText, extension: "bicep");

        // Check blob resource.
        var blob = storage.GetBlobService();

        var connectionStringBlobResource = (IResourceWithConnectionString)blob.Resource;

        Assert.Equal("https://myblob", await connectionStringBlobResource.GetConnectionStringAsync());
        var expectedBlobManifest = """
            {
              "type": "value.v0",
              "connectionString": "{storage.outputs.blobEndpoint}"
            }
            """;
        var blobManifest = await ManifestUtils.GetManifest(blob.Resource);
        Assert.Equal(expectedBlobManifest, blobManifest.ToString());

        // Check queue resource.
        var queue = storage.GetQueueService();

        var connectionStringQueueResource = (IResourceWithConnectionString)queue.Resource;

        Assert.Equal("https://myqueue", await connectionStringQueueResource.GetConnectionStringAsync());
        var expectedQueueManifest = """
            {
              "type": "value.v0",
              "connectionString": "{storage.outputs.queueEndpoint}"
            }
            """;
        var queueManifest = await ManifestUtils.GetManifest(queue.Resource);
        Assert.Equal(expectedQueueManifest, queueManifest.ToString());

        // Check table resource.
        var table = storage.GetTableService();

        var connectionStringTableResource = (IResourceWithConnectionString)table.Resource;

        Assert.Equal("https://mytable", await connectionStringTableResource.GetConnectionStringAsync());
        var expectedTableManifest = """
            {
              "type": "value.v0",
              "connectionString": "{storage.outputs.tableEndpoint}"
            }
            """;
        var tableManifest = await ManifestUtils.GetManifest(table.Resource);
        Assert.Equal(expectedTableManifest, tableManifest.ToString());
    }

    [Fact]
    public void GetBlobService_Default_Name()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var storage = builder.AddAzureStorage("storage");
        var blobService = storage.GetBlobService();

        Assert.Equal("storage-blobs", blobService.Resource.Name);
    }

    [Fact]
    public void GetQueueService_Default_Name()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var storage = builder.AddAzureStorage("storage");
        var queueService = storage.GetQueueService();

        Assert.Equal("storage-queues", queueService.Resource.Name);
    }

    [Fact]
    public void GetTableService_Default_Name()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var storage = builder.AddAzureStorage("storage");
        var tableService = storage.GetTableService();

        Assert.Equal("storage-tables", tableService.Resource.Name);
    }

    [Fact]
    public async Task AddMultipleStorageServiceGeneratesSingleResource()
    {
        // Ensures the bicep file doesn't contain duplicate service resources
        // and blobs/queues are associated to the last created one.

        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage");

        var blobService1 = storage.GetBlobService();
        var container1 = storage.AddBlobContainer(name: "container1");

        var blobService2 = storage.GetBlobService();
        var container2 = storage.AddBlobContainer(name: "container2");

        var queueService1 = storage.GetQueueService();
        var queue1 = storage.AddQueue(name: "queue1");

        var queueService2 = storage.GetQueueService();
        var queue2 = storage.AddQueue(name: "queue2");

        var tableService1 = storage.GetTableService();
        var tableService2 = storage.GetTableService();

        var manifest = await AzureManifestUtils.GetManifestWithBicep(storage.Resource);

        await Verify(manifest.BicepText, extension: "bicep");
    }

    [Fact]
    public void GetBlobServiceReturnsExistingResourceWhenNamesMatch()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage");
        var blobContainer = storage.AddBlobContainer("blobcontainer");
        var blobStorageResource = builder.Resources.OfType<AzureBlobStorageResource>().FirstOrDefault();

        var blobService = storage.GetBlobService();

        Assert.NotNull(blobStorageResource);
        Assert.Equal("storage-blobs", blobService.Resource.Name);
        Assert.Equal(blobService.Resource, blobStorageResource);
    }

    [Fact]
    public void GetQueueServiceReturnsExistingResourceWhenNamesMatch()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage");
        var queue = storage.AddQueue("queue");
        var queueStorageResource = builder.Resources.OfType<AzureQueueStorageResource>().FirstOrDefault();

        var queueService = storage.GetQueueService();

        Assert.NotNull(queueStorageResource);
        Assert.Equal("storage-queues", queueService.Resource.Name);
        Assert.Equal(queueService.Resource, queueStorageResource);
    }

    [Fact]
    public void GetTableServiceReturnsExistingResourceWhenNamesMatch()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage");

        // There is no method to add a table directly, so we create a table service with the expected name instead.
        var tableService1 = storage.GetTableService();

        var tableService = storage.GetTableService();

        Assert.NotNull(tableService1);
        Assert.Equal("storage-tables", tableService1.Resource.Name);
        Assert.Equal(tableService.Resource, tableService1.Resource);
    }

    [Fact]
    public async Task AddBlobs_Preserves_BackwardCompatibility()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage");

#pragma warning disable CS0618 // Type or member is obsolete
        var blobService1 = storage.AddBlobs("blobService1");
        var container1 = storage.AddBlobContainer(name: "container1");

        var blobService2 = storage.AddBlobs("blobService2");
        var container2 = storage.AddBlobContainer(name: "container2");
#pragma warning restore CS0618 // Type or member is obsolete

        var blobService3 = storage.GetBlobService();

        var manifest = await GetManifestWithBicep(storage.Resource);

        Assert.Equal("storage-blobs", blobService3.Resource.Name);
        Assert.Equal("blobService1", blobService1.Resource.Name);
        Assert.Equal("blobService2", blobService2.Resource.Name);
        
        await Verify(manifest.BicepText, extension: "bicep");
    }

    [Fact]
    public async Task AddQueues_Preserves_BackwardCompatibility()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage");

#pragma warning disable CS0618 // Type or member is obsolete
        var queueService1 = storage.AddQueues("queueService1");
        var queue1 = storage.AddQueue(name: "queue1");

        var queueService2 = storage.AddQueues("queueService2");
        var queue2 = storage.AddQueue(name: "queue2");
#pragma warning restore CS0618 // Type or member is obsolete

        var queueService3 = storage.GetQueueService();

        var manifest = await GetManifestWithBicep(storage.Resource);

        Assert.Equal("storage-queues", queueService3.Resource.Name);
        Assert.Equal("queueService1", queueService1.Resource.Name);
        Assert.Equal("queueService2", queueService2.Resource.Name);

        await Verify(manifest.BicepText, extension: "bicep");
    }

    [Fact]
    public async Task AddTables_Preserves_BackwardCompatibility()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage");

#pragma warning disable CS0618 // Type or member is obsolete
        var tableService1 = storage.AddTables("tableService1");
        var tableService2 = storage.AddTables("tableService2");
#pragma warning restore CS0618 // Type or member is obsolete

        var tableService3 = storage.GetTableService();

        var manifest = await GetManifestWithBicep(storage.Resource);

        Assert.Equal("storage-tables", tableService3.Resource.Name);
        Assert.Equal("tableService1", tableService1.Resource.Name);
        Assert.Equal("tableService2", tableService2.Resource.Name);

        await Verify(manifest.BicepText, extension: "bicep");
    }
}

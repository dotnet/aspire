// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureStorageExtensionsTests
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

        var blobs = storage.AddBlobs("blob");

        Assert.Equal(expected, await ((IResourceWithConnectionString)blobs.Resource).GetConnectionStringAsync());
    }

    [Fact]
    public async Task AddBlobs_ConnectionString_resolved_expected()
    {
        const string blobsConnectionString = "https://myblob";

        using var builder = TestDistributedApplicationBuilder.Create();

        var storagesku = builder.AddParameter("storagesku");
        var storage = builder.AddAzureStorage("storage");
        storage.Resource.Outputs["blobEndpoint"] = blobsConnectionString;

        var blobs = storage.AddBlobs("blob");

        Assert.Equal(blobsConnectionString, await ((IResourceWithConnectionString)blobs.Resource).GetConnectionStringAsync());
    }

    [Fact]
    public void AddBlobs_ConnectionString_unresolved_expected()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var storage = builder.AddAzureStorage("storage");
        var blobs = storage.AddBlobs("blob");

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

        var blobs = storage.AddBlobs("blob");
        var blobContainer = blobs.AddBlobContainer(name: "myContainer", blobContainerName);

        string? blobConnectionString = await ((IResourceWithConnectionString)blobs.Resource).GetConnectionStringAsync();
        string? blobContainerConnectionString = await ((IResourceWithConnectionString)blobContainer.Resource).GetConnectionStringAsync();

        Assert.NotNull(blobConnectionString);
        Assert.Contains(blobConnectionString, await ((IResourceWithConnectionString)blobContainer.Resource).GetConnectionStringAsync());
        Assert.Contains($"ContainerName={blobContainerName}", await ((IResourceWithConnectionString)blobContainer.Resource).GetConnectionStringAsync());

        Assert.NotNull(blobContainerConnectionString);
        Assert.DoesNotContain($"ContainerName", await ((IResourceWithConnectionString)blobContainer.Resource).GetConnectionStringAsync());
        Assert.DoesNotContain(blobContainerName, await ((IResourceWithConnectionString)blobContainer.Resource).GetConnectionStringAsync());
    }

    [Fact]
    public async Task AddBlobContainer_ConnectionString_resolved_expected()
    {
        const string blobContainerName = "my-blob-container";

        using var builder = TestDistributedApplicationBuilder.Create();

        var storagesku = builder.AddParameter("storagesku");
        var storage = builder.AddAzureStorage("storage");
        storage.Resource.Outputs["blobEndpoint"] = "https://myblob";

        var blobs = storage.AddBlobs("blob");
        var blobContainer = blobs.AddBlobContainer(name: "myContainer", blobContainerName);

        string? blobsConnectionString = await ((IResourceWithConnectionString)blobs.Resource).GetConnectionStringAsync();
        string expected = $"Endpoint={blobsConnectionString};ContainerName={blobContainerName}";

        Assert.Equal(expected, await ((IResourceWithConnectionString)blobContainer.Resource).GetConnectionStringAsync());
    }

    [Fact]
    public void AddBlobContainer_ConnectionString_unresolved_expected()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var storage = builder.AddAzureStorage("storage");
        var blobs = storage.AddBlobs("blob");
        var blobContainer = blobs.AddBlobContainer(name: "myContainer");

        Assert.Equal("Endpoint={storage.outputs.blobEndpoint};ContainerName=myContainer", blobContainer.Resource.ConnectionStringExpression.ValueExpression);
    }

    [Fact]
    public async Task ResourceNamesBicepValid()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage");

        var blobs = storage.AddBlobs("myblobs");
        var blob = blobs.AddBlobContainer(name: "myContainer", blobContainerName: "my-blob-container");
        var queues = storage.AddQueues("myqueues");
        var tables = storage.AddTables("mytables");

        var manifest = await AzureManifestUtils.GetManifestWithBicep(storage.Resource);

        await Verifier.Verify(manifest.BicepText, extension: "bicep")
            .UseHelixAwareDirectory("Snapshots");
    }
}

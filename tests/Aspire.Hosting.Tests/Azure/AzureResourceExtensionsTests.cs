// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Lifecycle;
using Xunit;

namespace Aspire.Hosting.Tests.Azure;

public class AzureResourceExtensionsTests
{
    [Fact]
    public void AzureStorageUserEmulatorCallbackWithUsePersistenceResultsInVolumeAnnotation()
    {
        var builder = DistributedApplication.CreateBuilder();
        var storage = builder.AddAzureStorage("storage").RunAsEmulator(configureContainer: builder =>
        {
            builder.UsePersistence("mydata");
        });

        var computedPath = Path.GetFullPath("mydata");

        var volumeAnnotation = storage.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();
        Assert.Equal(computedPath, volumeAnnotation.Source);
        Assert.Equal("/data", volumeAnnotation.Target);
        Assert.Equal(ContainerMountType.Bind, volumeAnnotation.Type);
    }

    [Fact]
    public async Task AzureStorageUserEmulatorUseBlobQueueTablePortMethodsMutateEndpoints()
    {
        var builder = DistributedApplication.CreateBuilder();
        var storage = builder.AddAzureStorage("storage").RunAsEmulator(configureContainer: builder =>
        {
            builder.UseBlobPort(9001);
            builder.UseQueuePort(9002);
            builder.UseTablePort(9003);
        });

        // Throw before ApplicationExecutor starts doing real work
        builder.Services.AddLifecycleHook<TestUtils.ThrowLifecycleHook>();

        var app = builder.Build();

        // Don't want to actually start an app
        await Assert.ThrowsAnyAsync<Exception>(() => app.StartAsync());

        Assert.Collection(
            storage.Resource.Annotations.OfType<EndpointAnnotation>(),
            e => Assert.Equal(9001, e.Port),
            e => Assert.Equal(9002, e.Port),
            e => Assert.Equal(9003, e.Port));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(8081)]
    [InlineData(9007)]
    public async Task AddAzureCosmosDBWithEmulatorGetsExpectedPort(int? port = null)
    {
        var builder = DistributedApplication.CreateBuilder();

        var cosmos = builder.AddAzureCosmosDB("cosmos");

        cosmos.RunAsEmulator(container =>
        {
            container.UseGatewayPort(port);
        });

        // Throw before ApplicationExecutor starts doing real work
        builder.Services.AddLifecycleHook<TestUtils.ThrowLifecycleHook>();

        var app = builder.Build();

        // Don't want to actually start an app
        await Assert.ThrowsAnyAsync<Exception>(() => app.StartAsync());

        var endpointAnnotation = cosmos.Resource.Annotations.OfType<EndpointAnnotation>().FirstOrDefault();
        Assert.NotNull(endpointAnnotation);

        var actualPort = endpointAnnotation.Port;
        Assert.Equal(port, actualPort);
    }

    [Theory]
    [InlineData("2.3.97-preview")]
    [InlineData("1.0.7")]
    public void AddAzureCosmosDBWithEmulatorGetsExpectedImageTag(string imageTag)
    {
        var builder = DistributedApplication.CreateBuilder();

        var cosmos = builder.AddAzureCosmosDB("cosmos");

        cosmos.RunAsEmulator(container =>
        {
            container.WithImageTag(imageTag);
        });

        var containerImageAnnotation = cosmos.Resource.Annotations.OfType<ContainerImageAnnotation>().FirstOrDefault();
        Assert.NotNull(containerImageAnnotation);

        var actualTag = containerImageAnnotation.Tag;
        Assert.Equal(imageTag ?? "latest", actualTag);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREAZURE003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.Tests;

public class AzurePrivateEndpointExtensionsTests
{
    [Fact]
    public void AddAzurePrivateEndpoint_CreatesResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var vnet = builder.AddAzureVirtualNetwork("myvnet");
        var subnet = vnet.AddSubnet("pesubnet", "10.0.1.0/24");
        var storage = builder.AddAzureStorage("storage");
        var blobs = storage.AddBlobs("blobs");

        var pe = builder.AddAzurePrivateEndpoint(subnet, blobs);

        Assert.NotNull(pe);
        Assert.Equal("pesubnet-blobs-pe", pe.Resource.Name);
        Assert.IsType<AzurePrivateEndpointResource>(pe.Resource);
        Assert.Same(subnet.Resource, pe.Resource.Subnet);
        Assert.Same(blobs.Resource, pe.Resource.Target);
    }

    [Fact]
    public void AddAzurePrivateEndpoint_AddsAnnotationToParentStorage()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var vnet = builder.AddAzureVirtualNetwork("myvnet");
        var subnet = vnet.AddSubnet("pesubnet", "10.0.1.0/24");
        var storage = builder.AddAzureStorage("storage");
        var blobs = storage.AddBlobs("blobs");

        // Before adding PE, no annotation
        Assert.Empty(storage.Resource.Annotations.OfType<PrivateEndpointTargetAnnotation>());

        builder.AddAzurePrivateEndpoint(subnet, blobs);

        // After adding PE, annotation should be on parent storage
        var annotation = storage.Resource.Annotations.OfType<PrivateEndpointTargetAnnotation>().SingleOrDefault();
        Assert.NotNull(annotation);
    }

    [Fact]
    public void AddAzurePrivateEndpoint_ForQueues_AddsAnnotationToParentStorage()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var vnet = builder.AddAzureVirtualNetwork("myvnet");
        var subnet = vnet.AddSubnet("pesubnet", "10.0.1.0/24");
        var storage = builder.AddAzureStorage("storage");
        var queues = storage.AddQueues("queues");

        builder.AddAzurePrivateEndpoint(subnet, queues);

        var annotation = storage.Resource.Annotations.OfType<PrivateEndpointTargetAnnotation>().SingleOrDefault();
        Assert.NotNull(annotation);
    }

    [Fact]
    public async Task AddAzurePrivateEndpoint_GeneratesBicep()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var vnet = builder.AddAzureVirtualNetwork("myvnet");
        var subnet = vnet.AddSubnet("pesubnet", "10.0.1.0/24");
        var storage = builder.AddAzureStorage("storage");
        var blobs = storage.AddBlobs("blobs");

        var pe = builder.AddAzurePrivateEndpoint(subnet, blobs);

        var manifest = await AzureManifestUtils.GetManifestWithBicep(pe.Resource);

        await Verify(manifest.BicepText, extension: "bicep");
    }

    [Fact]
    public async Task AddAzurePrivateEndpoint_ForQueues_GeneratesBicep()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var vnet = builder.AddAzureVirtualNetwork("myvnet");
        var subnet = vnet.AddSubnet("pesubnet", "10.0.1.0/24");
        var storage = builder.AddAzureStorage("storage");
        var queues = storage.AddQueues("queues");

        var pe = builder.AddAzurePrivateEndpoint(subnet, queues);

        var manifest = await AzureManifestUtils.GetManifestWithBicep(pe.Resource);

        await Verify(manifest.BicepText, extension: "bicep");
    }

    [Fact]
    public void AddAzurePrivateEndpoint_InRunMode_DoesNotAddToBuilder()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);

        var vnet = builder.AddAzureVirtualNetwork("myvnet");
        var subnet = vnet.AddSubnet("pesubnet", "10.0.1.0/24");
        var storage = builder.AddAzureStorage("storage");
        var blobs = storage.AddBlobs("blobs");

        var pe = builder.AddAzurePrivateEndpoint(subnet, blobs);

        // In run mode, the PE resource should not be added to the builder's resources
        Assert.DoesNotContain(pe.Resource, builder.Resources);
    }

    [Fact]
    public void AzureBlobStorageResource_ImplementsIAzurePrivateEndpointTarget()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var storage = builder.AddAzureStorage("storage");
        var blobs = storage.AddBlobs("blobs");

        Assert.IsAssignableFrom<IAzurePrivateEndpointTarget>(blobs.Resource);

        var target = (IAzurePrivateEndpointTarget)blobs.Resource;
        Assert.Equal(["blob"], target.GetPrivateLinkGroupIds());
        Assert.Equal("privatelink.blob.core.windows.net", target.GetPrivateDnsZoneName());
    }

    [Fact]
    public void AzureQueueStorageResource_ImplementsIAzurePrivateEndpointTarget()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var storage = builder.AddAzureStorage("storage");
        var queues = storage.AddQueues("queues");

        Assert.IsAssignableFrom<IAzurePrivateEndpointTarget>(queues.Resource);

        var target = (IAzurePrivateEndpointTarget)queues.Resource;
        Assert.Equal(["queue"], target.GetPrivateLinkGroupIds());
        Assert.Equal("privatelink.queue.core.windows.net", target.GetPrivateDnsZoneName());
    }
}

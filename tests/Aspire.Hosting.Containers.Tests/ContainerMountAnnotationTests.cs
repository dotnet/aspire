// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Containers.Tests;

public class ContainerMountAnnotationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void CtorThrowsArgumentNullExceptionIfSourceIsMissingForBindMount(string? source)
    {
#pragma warning disable CA1507 // Use nameof to express symbol names: false positive here, the parameter name being tested isn't the parameter to the test method
        Assert.Throws<ArgumentNullException>("source", () => new ContainerMountAnnotation(source, "/usr/foo", ContainerMountType.BindMount, false));
#pragma warning restore CA1507
    }

    [Fact]
    public void CtorThrowsArgumentExceptionIfBindMountSourceIsNotRooted()
    {
        Assert.Throws<ArgumentException>("source", () => new ContainerMountAnnotation("usr/foo", "/usr/foo", ContainerMountType.BindMount, false));
    }

    [Fact]
    public void CtorThrowsArgumentExceptionIfAnonymousVolumeIsReadOnly()
    {
        Assert.Throws<ArgumentException>("isReadOnly", () => new ContainerMountAnnotation(null, "/usr/foo", ContainerMountType.Volume, true));
    }

    [Fact]
    public void CtorAllowsDockerSocketAsBindMountSource()
    {
        var annotation = new ContainerMountAnnotation("/var/run/docker.sock", "/var/run/docker.sock", ContainerMountType.BindMount, false);
        Assert.Equal("/var/run/docker.sock", annotation.Source);
        Assert.Equal("/var/run/docker.sock", annotation.Target);
        Assert.Equal(ContainerMountType.BindMount, annotation.Type);
        Assert.False(annotation.IsReadOnly);
    }

    [Fact]
    public void OptionsPropertyRoundTrips()
    {
        var ann = new ContainerMountAnnotation("myvol", "/data", ContainerMountType.Volume, false)
        {
            Options = "uid=999,gid=999"
        };
        Assert.Equal("uid=999,gid=999", ann.Options);
    }

    [Fact]
    public void WithMountOptionsExtensionSetsOptions()
    {
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("c1", "alpine:latest")
            .WithVolume("vol1", "/data", isReadOnly: false)
            .WithMountOptions("/data", "uid=999");

        var mount = container.Resource.Annotations.OfType<ContainerMountAnnotation>().Single(m => m.Target == "/data");
        Assert.Equal("uid=999", mount.Options);
    }

    [Fact]
    public void WithMountOptionsThrowsWhenBuilderIsNull()
    {
        IResourceBuilder<ContainerResource> builder = null!;
        Assert.Throws<ArgumentNullException>(() => builder.WithMountOptions("/data", "uid=999"));
    }

    [Fact]
    public void WithMountOptionsThrowsWhenTargetPathIsNull()
    {
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("c1", "alpine:latest");
        Assert.Throws<ArgumentException>(() => container.WithMountOptions(null!, "uid=999"));
    }

    [Fact]
    public void WithMountOptionsThrowsWhenTargetPathIsEmpty()
    {
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("c1", "alpine:latest");
        Assert.Throws<ArgumentException>(() => container.WithMountOptions("", "uid=999"));
    }

    [Fact]
    public void WithMountOptionsThrowsWhenOptionsIsNull()
    {
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("c1", "alpine:latest");
        Assert.Throws<ArgumentException>(() => container.WithMountOptions("/data", null!));
    }

    [Fact]
    public void WithMountOptionsThrowsWhenOptionsIsEmpty()
    {
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("c1", "alpine:latest");
        Assert.Throws<ArgumentException>(() => container.WithMountOptions("/data", ""));
    }

    [Fact]
    public void WithMountOptionsThrowsWhenNoMountExists()
    {
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("c1", "alpine:latest");
        
        var ex = Assert.Throws<InvalidOperationException>(() => container.WithMountOptions("/nonexistent", "uid=999"));
        Assert.Equal("No container mount with target '/nonexistent' was found on resource 'c1'.", ex.Message);
    }
}

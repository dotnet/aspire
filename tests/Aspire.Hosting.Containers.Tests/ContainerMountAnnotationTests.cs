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
    public void CtorAllowsRelativePathWhenBasePathIsProvided()
    {
        var annotation = new ContainerMountAnnotation("./certs", "/app/certs", ContainerMountType.BindMount, isReadOnly: false, basePath: "/app/host");
        Assert.Equal("./certs", annotation.Source);
        Assert.Equal("/app/certs", annotation.Target);
        Assert.Equal(ContainerMountType.BindMount, annotation.Type);
        Assert.False(annotation.IsReadOnly);
        Assert.Equal("/app/host", annotation.BasePath);
    }

    [Fact]
    public void CtorAllowsRelativePathWithParentDirectoryWhenBasePathIsProvided()
    {
        var annotation = new ContainerMountAnnotation("../data/certs", "/app/certs", ContainerMountType.BindMount, isReadOnly: true, basePath: "/app/host");
        Assert.Equal("../data/certs", annotation.Source);
        Assert.Equal("/app/certs", annotation.Target);
        Assert.Equal(ContainerMountType.BindMount, annotation.Type);
        Assert.True(annotation.IsReadOnly);
        Assert.Equal("/app/host", annotation.BasePath);
    }

    [Fact]
    public void CtorSetsBasePathToNullByDefault()
    {
        var annotation = new ContainerMountAnnotation("/absolute/path", "/target", ContainerMountType.BindMount, false);
        Assert.Null(annotation.BasePath);
    }

    [Fact]
    public void CtorWithBasePathOverloadAllowsRelativePathWithNullBasePath()
    {
        // The constructor with basePath parameter allows relative paths even with null basePath
        // This is useful for Docker Compose scenarios where paths are relative to the compose file
        var annotation = new ContainerMountAnnotation("relative/path", "/target", ContainerMountType.BindMount, isReadOnly: false, basePath: null);
        Assert.Equal("relative/path", annotation.Source);
        Assert.Null(annotation.BasePath);
    }

    [Fact]
    public void CtorWithAbsolutePathAndBasePathIsValid()
    {
        // When an absolute path is provided with a base path, it's still valid (basePath will be ignored when resolving)
        var annotation = new ContainerMountAnnotation("/absolute/path", "/target", ContainerMountType.BindMount, isReadOnly: false, basePath: "/app/host");
        Assert.Equal("/absolute/path", annotation.Source);
        Assert.Equal("/app/host", annotation.BasePath);
    }
}

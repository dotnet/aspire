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
    public void CtorAllowsRelativePathWhenIsSourceRelativeIsTrue()
    {
        var annotation = new ContainerMountAnnotation("./certs", "/app/certs", ContainerMountType.BindMount, isReadOnly: false, isSourceRelative: true);
        Assert.Equal("./certs", annotation.Source);
        Assert.Equal("/app/certs", annotation.Target);
        Assert.Equal(ContainerMountType.BindMount, annotation.Type);
        Assert.False(annotation.IsReadOnly);
        Assert.True(annotation.IsSourceRelative);
    }

    [Fact]
    public void CtorAllowsRelativePathWithParentDirectoryWhenIsSourceRelativeIsTrue()
    {
        var annotation = new ContainerMountAnnotation("../data/certs", "/app/certs", ContainerMountType.BindMount, isReadOnly: true, isSourceRelative: true);
        Assert.Equal("../data/certs", annotation.Source);
        Assert.Equal("/app/certs", annotation.Target);
        Assert.Equal(ContainerMountType.BindMount, annotation.Type);
        Assert.True(annotation.IsReadOnly);
        Assert.True(annotation.IsSourceRelative);
    }

    [Fact]
    public void CtorSetsIsSourceRelativeToFalseByDefault()
    {
        var annotation = new ContainerMountAnnotation("/absolute/path", "/target", ContainerMountType.BindMount, false);
        Assert.False(annotation.IsSourceRelative);
    }

    [Fact]
    public void CtorWithIsSourceRelativeFalseStillRequiresRootedPath()
    {
        Assert.Throws<ArgumentException>("source", () => new ContainerMountAnnotation("relative/path", "/target", ContainerMountType.BindMount, isReadOnly: false, isSourceRelative: false));
    }

    [Fact]
    public void CtorWithAbsolutePathAndIsSourceRelativeTrueIsValid()
    {
        // When isSourceRelative is true with an absolute path, it's still valid
        var annotation = new ContainerMountAnnotation("/absolute/path", "/target", ContainerMountType.BindMount, isReadOnly: false, isSourceRelative: true);
        Assert.Equal("/absolute/path", annotation.Source);
        Assert.True(annotation.IsSourceRelative);
    }
}

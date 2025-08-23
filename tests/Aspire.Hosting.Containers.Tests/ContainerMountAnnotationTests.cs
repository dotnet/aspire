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
    public void NewOverloadSetsProperties()
    {
        var directoryMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | 
                           UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                           UnixFileMode.OtherRead | UnixFileMode.OtherExecute;
        var fileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | 
                      UnixFileMode.GroupRead | 
                      UnixFileMode.OtherRead;

        var annotation = new ContainerMountAnnotation(
            "myvol", 
            "/data", 
            ContainerMountType.Volume, 
            false,
            userId: 999,
            groupId: 999,
            directoryMode: directoryMode,
            fileMode: fileMode);

        Assert.Equal("myvol", annotation.Source);
        Assert.Equal("/data", annotation.Target);
        Assert.Equal(ContainerMountType.Volume, annotation.Type);
        Assert.False(annotation.IsReadOnly);
        Assert.Equal(999, annotation.UserId);
        Assert.Equal(999, annotation.GroupId);
        Assert.Equal(directoryMode, annotation.DirectoryMode);
        Assert.Equal(fileMode, annotation.FileMode);
    }

    [Fact]
    public void LegacyConstructorKeepsPropertiesNull()
    {
        var annotation = new ContainerMountAnnotation("myvol", "/data", ContainerMountType.Volume, false);
        
        Assert.Equal("myvol", annotation.Source);
        Assert.Equal("/data", annotation.Target);
        Assert.Equal(ContainerMountType.Volume, annotation.Type);
        Assert.False(annotation.IsReadOnly);
        Assert.Null(annotation.UserId);
        Assert.Null(annotation.GroupId);
        Assert.Null(annotation.DirectoryMode);
        Assert.Null(annotation.FileMode);
    }

    [Fact]
    public void ValidationRejectsNonPermissionBitsInDirectoryMode()
    {
        var invalidMode = UnixFileMode.UserRead | UnixFileMode.StickyBit;

        var ex = Assert.Throws<ArgumentException>("directoryMode", () => new ContainerMountAnnotation(
            "myvol", "/data", ContainerMountType.Volume, false, null, null, invalidMode, null));
        
        Assert.Contains("DirectoryMode must contain only permission bits", ex.Message);
    }

    [Fact]
    public void ValidationRejectsNonPermissionBitsInFileMode()
    {
        var invalidMode = UnixFileMode.UserRead | UnixFileMode.StickyBit;

        var ex = Assert.Throws<ArgumentException>("fileMode", () => new ContainerMountAnnotation(
            "myvol", "/data", ContainerMountType.Volume, false, null, null, null, invalidMode));
        
        Assert.Contains("FileMode must contain only permission bits", ex.Message);
    }

    [Fact]
    public void ValidationAllowsValidPermissionBits()
    {
        var validMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | 
                       UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
                       UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute;

        // Should not throw
        var annotation = new ContainerMountAnnotation(
            "myvol", "/data", ContainerMountType.Volume, false, 999, 999, validMode, validMode);
        
        Assert.Equal(validMode, annotation.DirectoryMode);
        Assert.Equal(validMode, annotation.FileMode);
    }
}

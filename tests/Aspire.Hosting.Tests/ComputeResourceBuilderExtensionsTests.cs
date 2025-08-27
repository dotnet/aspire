// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Tests;

[SuppressMessage("Experimental", "ASPIRECOMPUTE001", Justification = "Testing experimental API")]
public class ComputeResourceBuilderExtensionsTests
{
    [Fact]
    public void WithVolumeAddsContainerMountAnnotationToContainerResource()
    {
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("mycontainer", "nginx")
            .WithVolume("myvolume", "/app/data", isReadOnly: true);

        var annotations = container.Resource.Annotations.OfType<ContainerMountAnnotation>();
        var annotation = Assert.Single(annotations);
        
        Assert.Equal("myvolume", annotation.Source);
        Assert.Equal("/app/data", annotation.Target);
        Assert.Equal(ContainerMountType.Volume, annotation.Type);
        Assert.True(annotation.IsReadOnly);
    }

    [Fact]
    public void WithVolumeAddsContainerMountAnnotationToProjectResource()
    {
        var builder = DistributedApplication.CreateBuilder();
        var project = builder.AddProject<Projects.TestProject_AppHost>("myproject")
            .WithVolume("shared-data", "/app/shared");

        var annotations = project.Resource.Annotations.OfType<ContainerMountAnnotation>();
        var annotation = Assert.Single(annotations);
        
        Assert.Equal("shared-data", annotation.Source);
        Assert.Equal("/app/shared", annotation.Target);
        Assert.Equal(ContainerMountType.Volume, annotation.Type);
        Assert.False(annotation.IsReadOnly);
    }

    [Fact]
    public void WithVolumeAddsContainerMountAnnotationToExecutableResource()
    {
        var builder = DistributedApplication.CreateBuilder();
        var executable = builder.AddExecutable("myexecutable", "dotnet", "/app", "myapp.dll")
            .WithVolume("logs", "/app/logs", isReadOnly: false);

        var annotations = executable.Resource.Annotations.OfType<ContainerMountAnnotation>();
        var annotation = Assert.Single(annotations);
        
        Assert.Equal("logs", annotation.Source);
        Assert.Equal("/app/logs", annotation.Target);
        Assert.Equal(ContainerMountType.Volume, annotation.Type);
        Assert.False(annotation.IsReadOnly);
    }

    [Fact]
    public void WithVolumeAnonymousAddsContainerMountAnnotationToContainerResource()
    {
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("mycontainer", "nginx")
            .WithVolume("/app/data");

        var annotations = container.Resource.Annotations.OfType<ContainerMountAnnotation>();
        var annotation = Assert.Single(annotations);
        
        Assert.Null(annotation.Source);
        Assert.Equal("/app/data", annotation.Target);
        Assert.Equal(ContainerMountType.Volume, annotation.Type);
        Assert.False(annotation.IsReadOnly);
    }

    [Fact]
    public void WithVolumeAnonymousAddsContainerMountAnnotationToProjectResource()
    {
        var builder = DistributedApplication.CreateBuilder();
        var project = builder.AddProject<Projects.TestProject_AppHost>("myproject")
            .WithVolume("/tmp/cache");

        var annotations = project.Resource.Annotations.OfType<ContainerMountAnnotation>();
        var annotation = Assert.Single(annotations);
        
        Assert.Null(annotation.Source);
        Assert.Equal("/tmp/cache", annotation.Target);
        Assert.Equal(ContainerMountType.Volume, annotation.Type);
        Assert.False(annotation.IsReadOnly);
    }

    [Fact]
    public void WithVolumeAnonymousAddsContainerMountAnnotationToExecutableResource()
    {
        var builder = DistributedApplication.CreateBuilder();
        var executable = builder.AddExecutable("myexecutable", "dotnet", "/app", "myapp.dll")
            .WithVolume("/tmp/temp");

        var annotations = executable.Resource.Annotations.OfType<ContainerMountAnnotation>();
        var annotation = Assert.Single(annotations);
        
        Assert.Null(annotation.Source);
        Assert.Equal("/tmp/temp", annotation.Target);
        Assert.Equal(ContainerMountType.Volume, annotation.Type);
        Assert.False(annotation.IsReadOnly);
    }

    [Fact]
    public void WithVolumeThrowsArgumentNullExceptionForNullBuilder()
    {
        IResourceBuilder<ContainerResource>? builder = null;
        
        var ex = Assert.Throws<ArgumentNullException>(() => builder!.WithVolume("vol", "/data"));
        Assert.Equal("builder", ex.ParamName);
    }

    [Fact]
    public void WithVolumeThrowsArgumentNullExceptionForNullTarget()
    {
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("mycontainer", "nginx");
        
        var ex = Assert.Throws<ArgumentNullException>(() => container.WithVolume("vol", null!));
        Assert.Equal("target", ex.ParamName);
    }

    [Fact]
    public void WithVolumeAnonymousThrowsArgumentNullExceptionForNullBuilder()
    {
        IResourceBuilder<ContainerResource>? builder = null;
        
        var ex = Assert.Throws<ArgumentNullException>(() => builder!.WithVolume("/data"));
        Assert.Equal("builder", ex.ParamName);
    }

    [Fact]
    public void WithVolumeAnonymousThrowsArgumentNullExceptionForNullTarget()
    {
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("mycontainer", "nginx");
        
        var ex = Assert.Throws<ArgumentNullException>(() => container.WithVolume(null!));
        Assert.Equal("target", ex.ParamName);
    }

    [Fact]
    public void WithVolumeThrowsForAnonymousReadOnlyVolume()
    {
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("mycontainer", "nginx");
        
        var ex = Assert.Throws<ArgumentException>(() => container.WithVolume(null, "/data", isReadOnly: true));
        Assert.Equal("isReadOnly", ex.ParamName);
    }

    [Fact]
    public void MultipleWithVolumeCallsAddMultipleAnnotations()
    {
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("mycontainer", "nginx")
            .WithVolume("volume1", "/data1")
            .WithVolume("volume2", "/data2", isReadOnly: true)
            .WithVolume("/anonymous");

        var annotations = container.Resource.Annotations.OfType<ContainerMountAnnotation>().ToList();
        Assert.Equal(3, annotations.Count);
        
        Assert.Equal("volume1", annotations[0].Source);
        Assert.Equal("/data1", annotations[0].Target);
        Assert.False(annotations[0].IsReadOnly);
        
        Assert.Equal("volume2", annotations[1].Source);
        Assert.Equal("/data2", annotations[1].Target);
        Assert.True(annotations[1].IsReadOnly);
        
        Assert.Null(annotations[2].Source);
        Assert.Equal("/anonymous", annotations[2].Target);
        Assert.False(annotations[2].IsReadOnly);
    }

    [Fact]
    public void WithVolumeWorksWithSharedVolumeNames()
    {
        var builder = DistributedApplication.CreateBuilder();
        var container1 = builder.AddContainer("container1", "nginx")
            .WithVolume("shared", "/data");
        var container2 = builder.AddContainer("container2", "nginx")
            .WithVolume("shared", "/backup");

        var annotations1 = container1.Resource.Annotations.OfType<ContainerMountAnnotation>();
        var annotation1 = Assert.Single(annotations1);
        
        var annotations2 = container2.Resource.Annotations.OfType<ContainerMountAnnotation>();
        var annotation2 = Assert.Single(annotations2);
        
        // Both should have the same volume name
        Assert.Equal("shared", annotation1.Source);
        Assert.Equal("shared", annotation2.Source);
        
        // But different mount points
        Assert.Equal("/data", annotation1.Target);
        Assert.Equal("/backup", annotation2.Target);
    }
}
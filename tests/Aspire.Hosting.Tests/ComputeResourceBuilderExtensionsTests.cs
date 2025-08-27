// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable IDE0005 // Using directive is unnecessary

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Tests;

[SuppressMessage("Experimental", "ASPIRECOMPUTE001", Justification = "Testing experimental API")]
public class ComputeResourceBuilderExtensionsTests
{
    [Fact]
    public void WithVolumeAddsContainerMountAnnotationToProjectResource()
    {
        var builder = DistributedApplication.CreateBuilder();
        var project = builder.AddProject("myproject", "/path/to/project")
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
    public void WithVolumeAnonymousAddsContainerMountAnnotationToProjectResource()
    {
        var builder = DistributedApplication.CreateBuilder();
        var project = builder.AddProject("myproject", "/path/to/project")
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
        IResourceBuilder<ExecutableResource>? builder = null;
        
        var ex = Assert.Throws<ArgumentNullException>(() => builder!.WithVolume("vol", "/data"));
        Assert.Equal("builder", ex.ParamName);
    }

    [Fact]
    public void WithVolumeThrowsArgumentNullExceptionForNullTarget()
    {
        var builder = DistributedApplication.CreateBuilder();
        var executable = builder.AddExecutable("myexecutable", "dotnet", "/app", "myapp.dll");
        
        var ex = Assert.Throws<ArgumentNullException>(() => executable.WithVolume("vol", null!));
        Assert.Equal("target", ex.ParamName);
    }

    [Fact]
    public void WithVolumeAnonymousThrowsArgumentNullExceptionForNullBuilder()
    {
        IResourceBuilder<ExecutableResource>? builder = null;
        
        var ex = Assert.Throws<ArgumentNullException>(() => builder!.WithVolume("/data"));
        Assert.Equal("builder", ex.ParamName);
    }

    [Fact]
    public void WithVolumeAnonymousThrowsArgumentNullExceptionForNullTarget()
    {
        var builder = DistributedApplication.CreateBuilder();
        var executable = builder.AddExecutable("myexecutable", "dotnet", "/app", "myapp.dll");
        
        var ex = Assert.Throws<ArgumentNullException>(() => executable.WithVolume(null!));
        Assert.Equal("target", ex.ParamName);
    }

    [Fact]
    public void WithVolumeThrowsForAnonymousReadOnlyVolume()
    {
        var builder = DistributedApplication.CreateBuilder();
        var executable = builder.AddExecutable("myexecutable", "dotnet", "/app", "myapp.dll");
        
        var ex = Assert.Throws<ArgumentException>(() => executable.WithVolume(null, "/data", isReadOnly: true));
        Assert.Equal("isReadOnly", ex.ParamName);
    }

    [Fact]
    public void MultipleWithVolumeCallsAddMultipleAnnotations()
    {
        var builder = DistributedApplication.CreateBuilder();
        var executable = builder.AddExecutable("myexecutable", "dotnet", "/app", "myapp.dll")
            .WithVolume("volume1", "/data1")
            .WithVolume("volume2", "/data2", isReadOnly: true)
            .WithVolume("/anonymous");

        var annotations = executable.Resource.Annotations.OfType<ContainerMountAnnotation>().ToList();
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
        var project1 = builder.AddProject("project1", "/path/to/project1")
            .WithVolume("shared", "/data");
        var project2 = builder.AddProject("project2", "/path/to/project2") 
            .WithVolume("shared", "/backup");

        var annotations1 = project1.Resource.Annotations.OfType<ContainerMountAnnotation>();
        var annotation1 = Assert.Single(annotations1);
        
        var annotations2 = project2.Resource.Annotations.OfType<ContainerMountAnnotation>();
        var annotation2 = Assert.Single(annotations2);
        
        // Both should have the same volume name
        Assert.Equal("shared", annotation1.Source);
        Assert.Equal("shared", annotation2.Source);
        
        // But different mount points
        Assert.Equal("/data", annotation1.Target);
        Assert.Equal("/backup", annotation2.Target);
    }

    [Fact] 
    public void ContainerResourcesStillUseExistingExtensions()
    {
        var builder = DistributedApplication.CreateBuilder();
        
        // This should use ContainerResourceBuilderExtensions.WithVolume, not the new ComputeResourceBuilderExtensions.WithVolume
        // We can test this by ensuring that both work without issues
        var container = builder.AddContainer("mycontainer", "nginx");
        
        // Container resources should use ContainerResourceBuilderExtensions explicitly to avoid ambiguity
        var containerWithVolume = ContainerResourceBuilderExtensions.WithVolume(
            container, "myvolume", "/app/data", isReadOnly: true);

        var annotations = containerWithVolume.Resource.Annotations.OfType<ContainerMountAnnotation>();
        var annotation = Assert.Single(annotations);
        
        Assert.Equal("myvolume", annotation.Source);
        Assert.Equal("/app/data", annotation.Target);
        Assert.Equal(ContainerMountType.Volume, annotation.Type);
        Assert.True(annotation.IsReadOnly);
    }
}
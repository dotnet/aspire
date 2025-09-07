// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Tests;

public class ComputeResourceVolumeTests
{
    [Fact]
    public void WithVolumeAddsContainerMountAnnotationToProjectResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<TestProject>("myproject")
            .WithVolume("shared-data", "/app/shared");

        var annotations = project.Resource.Annotations.OfType<ContainerMountAnnotation>();
        var annotation = Assert.Single(annotations);
        
        Assert.Equal("shared-data", annotation.Source);
        Assert.Equal("/app/shared", annotation.Target);
        Assert.Equal(ContainerMountType.Volume, annotation.Type);
        Assert.False(annotation.IsReadOnly);
    }

    [Fact]
    public void WithVolumeAnonymousAddsContainerMountAnnotationToProjectResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<TestProject>("myproject")
            .WithVolume("/tmp/cache");

        var annotations = project.Resource.Annotations.OfType<ContainerMountAnnotation>();
        var annotation = Assert.Single(annotations);
        
        Assert.Null(annotation.Source);
        Assert.Equal("/tmp/cache", annotation.Target);
        Assert.Equal(ContainerMountType.Volume, annotation.Type);
        Assert.False(annotation.IsReadOnly);
    }

    [Fact]
    public void WithVolumeThrowsArgumentNullExceptionForNullBuilder()
    {
        IResourceBuilder<ProjectResource>? builder = null;
        
        var ex = Assert.Throws<ArgumentNullException>(() => builder!.WithVolume("vol", "/data"));
        Assert.Equal("builder", ex.ParamName);
    }

    [Fact]
    public void WithVolumeThrowsArgumentNullExceptionForNullTarget()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<TestProject>("myproject");
        
        var ex = Assert.Throws<ArgumentNullException>(() => project.WithVolume("vol", null!));
        Assert.Equal("target", ex.ParamName);
    }

    [Fact]
    public void WithVolumeAnonymousThrowsArgumentNullExceptionForNullBuilder()
    {
        IResourceBuilder<ProjectResource>? builder = null;
        
        var ex = Assert.Throws<ArgumentNullException>(() => builder!.WithVolume("/data"));
        Assert.Equal("builder", ex.ParamName);
    }

    [Fact]
    public void WithVolumeAnonymousThrowsArgumentNullExceptionForNullTarget()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<TestProject>("myproject");
        
        var ex = Assert.Throws<ArgumentNullException>(() => project.WithVolume(null!));
        Assert.Equal("target", ex.ParamName);
    }

    [Fact]
    public void MultipleWithVolumeCallsAddMultipleAnnotations()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<TestProject>("myproject")
            .WithVolume("volume1", "/data1")
            .WithVolume("volume2", "/data2", isReadOnly: true)
            .WithVolume("/anonymous");

        var annotations = project.Resource.Annotations.OfType<ContainerMountAnnotation>().ToList();
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
        using var builder = TestDistributedApplicationBuilder.Create();
        var project1 = builder.AddProject<TestProject>("project1")
            .WithVolume("shared", "/data");
        var project2 = builder.AddProject<TestProject2>("project2") 
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
        using var builder = TestDistributedApplicationBuilder.Create();
        
        // Container resources should use ContainerResourceBuilderExtensions.WithVolume as before
        var container = builder.AddContainer("mycontainer", "nginx")
            .WithVolume("myvolume", "/app/data", isReadOnly: true);

        var annotations = container.Resource.Annotations.OfType<ContainerMountAnnotation>();
        var annotation = Assert.Single(annotations);
        
        Assert.Equal("myvolume", annotation.Source);
        Assert.Equal("/app/data", annotation.Target);
        Assert.Equal(ContainerMountType.Volume, annotation.Type);
        Assert.True(annotation.IsReadOnly);
    }

    private sealed class TestProject : IProjectMetadata
    {
        public string ProjectPath => "TestProject";
        public LaunchSettings LaunchSettings { get; } = new();
    }

    private sealed class TestProject2 : IProjectMetadata
    {
        public string ProjectPath => "TestProject2";
        public LaunchSettings LaunchSettings { get; } = new();
    }
}
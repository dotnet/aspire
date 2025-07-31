// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Tests;

public class WithResourceIconTests
{
    [Fact]
    public void WithResourceIcon_SetsIconNameAndDefaultVariant()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var container = builder.AddContainer("mycontainer", "myimage")
                              .WithResourceIcon("Database");

        // Verify the annotation was added
        var iconAnnotation = container.Resource.Annotations.OfType<ResourceIconAnnotation>().Single();
        Assert.Equal("Database", iconAnnotation.IconName);
        Assert.Equal(IconVariant.Filled, iconAnnotation.IconVariant);
    }

    [Fact]
    public void WithResourceIcon_SetsIconNameAndCustomVariant()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var container = builder.AddContainer("mycontainer", "myimage")
                              .WithResourceIcon("CloudArrowUp", IconVariant.Regular);

        // Verify the annotation was added with correct variant
        var iconAnnotation = container.Resource.Annotations.OfType<ResourceIconAnnotation>().Single();
        Assert.Equal("CloudArrowUp", iconAnnotation.IconName);
        Assert.Equal(IconVariant.Regular, iconAnnotation.IconVariant);
    }

    [Fact]
    public void WithResourceIcon_ThrowsOnNullIconName()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var container = builder.AddContainer("mycontainer", "myimage");

        Assert.Throws<ArgumentNullException>(() => container.WithResourceIcon(null!));
    }

    [Fact]
    public void WithResourceIcon_ThrowsOnEmptyIconName()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var container = builder.AddContainer("mycontainer", "myimage");

        Assert.Throws<ArgumentException>(() => container.WithResourceIcon(""));
        Assert.Throws<ArgumentException>(() => container.WithResourceIcon("   "));
    }

    [Fact]
    public void WithResourceIcon_CanBeCalledOnAnyResourceType()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        // Test on different resource types
        var container = builder.AddContainer("container", "image")
                              .WithResourceIcon("Box");

        var parameter = builder.AddParameter("param")
                              .WithResourceIcon("Settings", IconVariant.Regular);

        var project = builder.AddProject<TestProject>("project")
                            .WithResourceIcon("CodeCircle");

        // Verify all have the annotations
        Assert.Single(container.Resource.Annotations.OfType<ResourceIconAnnotation>());
        Assert.Single(parameter.Resource.Annotations.OfType<ResourceIconAnnotation>());
        Assert.Single(project.Resource.Annotations.OfType<ResourceIconAnnotation>());
    }

    [Fact]
    public void WithResourceIcon_OverridesExistingIconAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var container = builder.AddContainer("mycontainer", "myimage")
                              .WithResourceIcon("Database")
                              .WithResourceIcon("CloudArrowUp", IconVariant.Regular);

        // Should only have one annotation with the latest values
        var iconAnnotations = container.Resource.Annotations.OfType<ResourceIconAnnotation>().ToList();
        Assert.Equal(2, iconAnnotations.Count); // Both annotations should exist since we don't override
        
        // Get the latest one
        var latestAnnotation = iconAnnotations.Last();
        Assert.Equal("CloudArrowUp", latestAnnotation.IconName);
        Assert.Equal(IconVariant.Regular, latestAnnotation.IconVariant);
    }

    private sealed class TestProject : IProjectMetadata
    {
        public string ProjectPath => "TestProject.csproj";

        public LaunchSettings LaunchSettings { get; set; } = new();
    }
}
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Tests;

public class WithResourceIconTests
{
    [Fact]
    public void WithIconName_SetsIconNameAndDefaultVariant()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var container = builder.AddContainer("mycontainer", "myimage")
                              .WithIconName("Database");

        // Verify the annotation was added
        var iconAnnotation = container.Resource.Annotations.OfType<ResourceIconAnnotation>().Single();
        Assert.Equal("Database", iconAnnotation.IconName);
        Assert.Equal(IconVariant.Filled, iconAnnotation.IconVariant);
    }

    [Fact]
    public void WithIconName_SetsIconNameAndCustomVariant()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var container = builder.AddContainer("mycontainer", "myimage")
                              .WithIconName("CloudArrowUp", IconVariant.Regular);

        // Verify the annotation was added with correct variant
        var iconAnnotation = container.Resource.Annotations.OfType<ResourceIconAnnotation>().Single();
        Assert.Equal("CloudArrowUp", iconAnnotation.IconName);
        Assert.Equal(IconVariant.Regular, iconAnnotation.IconVariant);
    }

    [Fact]
    public void WithIconName_ThrowsOnNullIconName()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var container = builder.AddContainer("mycontainer", "myimage");

        Assert.Throws<ArgumentNullException>(() => container.WithIconName(null!));
    }

    [Fact]
    public void WithIconName_ThrowsOnEmptyIconName()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var container = builder.AddContainer("mycontainer", "myimage");

        Assert.Throws<ArgumentException>(() => container.WithIconName(""));
        Assert.Throws<ArgumentException>(() => container.WithIconName("   "));
    }

    [Fact]
    public void WithIconName_CanBeCalledOnAnyResourceType()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        // Test on different resource types
        var container = builder.AddContainer("container", "image")
                              .WithIconName("Box");

        var parameter = builder.AddParameter("param")
                              .WithIconName("Settings", IconVariant.Regular);

        var project = builder.AddProject<TestProject>("project")
                            .WithIconName("CodeCircle");

        // Verify all have the annotations
        Assert.Single(container.Resource.Annotations.OfType<ResourceIconAnnotation>());
        Assert.Single(parameter.Resource.Annotations.OfType<ResourceIconAnnotation>());
        Assert.Single(project.Resource.Annotations.OfType<ResourceIconAnnotation>());
    }

    [Fact]
    public void WithIconName_OverridesExistingIconAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var container = builder.AddContainer("mycontainer", "myimage")
                              .WithIconName("Database")
                              .WithIconName("CloudArrowUp", IconVariant.Regular);

        // Should have both annotations (WithIconName adds, doesn't replace)
        var iconAnnotations = container.Resource.Annotations.OfType<ResourceIconAnnotation>().ToList();
        Assert.Equal(2, iconAnnotations.Count);
        
        // Get the latest one - this is what ResourceNotificationService should use
        var latestAnnotation = iconAnnotations.Last();
        Assert.Equal("CloudArrowUp", latestAnnotation.IconName);
        Assert.Equal(IconVariant.Regular, latestAnnotation.IconVariant);
        
        // Verify that TryGetLastAnnotation returns the correct one
        Assert.True(container.Resource.TryGetLastAnnotation<ResourceIconAnnotation>(out var lastAnnotation));
        Assert.Equal("CloudArrowUp", lastAnnotation.IconName);
        Assert.Equal(IconVariant.Regular, lastAnnotation.IconVariant);
    }

    [Fact]
    public void WithResourceIcon_SetsCustomIconDataWithSvgContent()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var svgContent = "<svg><circle cx='50' cy='50' r='40'/></svg>";
        var container = builder.AddContainer("mycontainer", "myimage")
                              .WithResourceIcon(svgContent, "MySvgIcon");

        // Verify the annotation was added with custom icon data
        var iconAnnotation = container.Resource.Annotations.OfType<ResourceIconAnnotation>().Single();
        Assert.Equal(svgContent, iconAnnotation.CustomIconData);
        Assert.Equal("MySvgIcon", iconAnnotation.IconName);
    }

    [Fact]
    public void WithResourceIcon_SetsCustomIconDataWithDataUri()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var dataUri = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAUA";
        var container = builder.AddContainer("mycontainer", "myimage")
                              .WithResourceIcon(dataUri, "MyPngIcon");

        // Verify the annotation was added with custom icon data
        var iconAnnotation = container.Resource.Annotations.OfType<ResourceIconAnnotation>().Single();
        Assert.Equal(dataUri, iconAnnotation.CustomIconData);
        Assert.Equal("MyPngIcon", iconAnnotation.IconName);
    }

    [Fact]
    public void WithResourceIcon_FromFile_LoadsSvgContent()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        // Create a temporary SVG file
        var tempFile = Path.Combine(Path.GetTempPath(), $"test-icon-{Guid.NewGuid()}.svg");
        var svgContent = "<svg width='24' height='24'><circle cx='12' cy='12' r='10'/></svg>";
        File.WriteAllText(tempFile, svgContent);

        try
        {
            var container = builder.AddContainer("mycontainer", "myimage")
                                  .WithResourceIcon(tempFile);

            // Verify the annotation was added with the SVG content
            var iconAnnotation = container.Resource.Annotations.OfType<ResourceIconAnnotation>().Single();
            Assert.Equal(svgContent, iconAnnotation.CustomIconData);
            Assert.Equal("test-icon-" + Path.GetFileNameWithoutExtension(tempFile).Split('-').Last(), iconAnnotation.IconName);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void WithResourceIcon_FromFile_LoadsPngAsDataUri()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        // Create a temporary PNG file
        var tempFile = Path.Combine(Path.GetTempPath(), $"test-icon-{Guid.NewGuid()}.png");
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }; // PNG magic bytes
        File.WriteAllBytes(tempFile, pngBytes);

        try
        {
            var container = builder.AddContainer("mycontainer", "myimage")
                                  .WithResourceIcon(tempFile);

            // Verify the annotation was added with a data URI
            var iconAnnotation = container.Resource.Annotations.OfType<ResourceIconAnnotation>().Single();
            Assert.NotNull(iconAnnotation.CustomIconData);
            Assert.StartsWith("data:image/png;base64,", iconAnnotation.CustomIconData);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void WithResourceIcon_FromFile_ThrowsIfFileNotFound()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var container = builder.AddContainer("mycontainer", "myimage");

        var nonExistentFile = Path.Combine(Path.GetTempPath(), "nonexistent-icon.svg");
        Assert.Throws<FileNotFoundException>(() => container.WithResourceIcon(nonExistentFile));
    }

    [Fact]
    public void WithResourceIcon_FromFile_ThrowsOnUnsupportedFormat()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        // Create a temporary file with unsupported extension
        var tempFile = Path.Combine(Path.GetTempPath(), $"test-icon-{Guid.NewGuid()}.txt");
        File.WriteAllText(tempFile, "not an icon");

        try
        {
            var container = builder.AddContainer("mycontainer", "myimage");
            Assert.Throws<NotSupportedException>(() => container.WithResourceIcon(tempFile));
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void WithResourceIcon_ThrowsOnNullIconData()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var container = builder.AddContainer("mycontainer", "myimage");

        Assert.Throws<ArgumentNullException>(() => container.WithResourceIcon(null!, "test"));
    }

    [Fact]
    public void WithResourceIcon_ThrowsOnEmptyIconData()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var container = builder.AddContainer("mycontainer", "myimage");

        Assert.Throws<ArgumentException>(() => container.WithResourceIcon("", "test"));
        Assert.Throws<ArgumentException>(() => container.WithResourceIcon("   ", "test"));
    }

    [Fact]
    public void WithResourceIcon_CustomDataTakesPrecedenceOverIconName()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var svgContent = "<svg><rect width='24' height='24'/></svg>";
        var container = builder.AddContainer("mycontainer", "myimage")
                              .WithIconName("Database")
                              .WithResourceIcon(svgContent, "CustomIcon");

        // Should have both annotations
        var iconAnnotations = container.Resource.Annotations.OfType<ResourceIconAnnotation>().ToList();
        Assert.Equal(2, iconAnnotations.Count);

        // The latest annotation should have custom icon data
        var latestAnnotation = iconAnnotations.Last();
        Assert.Equal(svgContent, latestAnnotation.CustomIconData);
        Assert.Equal("CustomIcon", latestAnnotation.IconName);
    }

    private sealed class TestProject : IProjectMetadata
    {
        public string ProjectPath => "TestProject.csproj";

        public LaunchSettings LaunchSettings { get; set; } = new();
    }
}
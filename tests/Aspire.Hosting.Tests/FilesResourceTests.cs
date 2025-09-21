// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Tests;

public class FilesResourceTests
{
    [Fact]
    public void AddFilesResourceWithNoInitialFiles()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddFiles("emptyfiles");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var filesResource = Assert.Single(appModel.Resources.OfType<FilesResource>());

        Assert.Equal("emptyfiles", filesResource.Name);
        Assert.Empty(filesResource.Files);
    }

    [Fact]
    public void WithSourceAddsSourceToResource()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddFiles("myfiles")
                  .WithSource("/path/to/directory")
                  .WithSource("/path/to/file.txt");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var filesResource = Assert.Single(appModel.Resources.OfType<FilesResource>());

        Assert.Equal("myfiles", filesResource.Name);
        Assert.Equal(2, filesResource.Files.Count());
        Assert.Contains("/path/to/directory", filesResource.Files);
        Assert.Contains("/path/to/file.txt", filesResource.Files);
    }

    [Fact]
    public void FilesResourceImplementsIResourceWithFiles()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddFiles("myfiles").WithSource("/path/to/file1.txt");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var filesResource = Assert.Single(appModel.Resources.OfType<FilesResource>());

        Assert.IsAssignableFrom<IResourceWithFiles>(filesResource);
        var resourceWithFiles = (IResourceWithFiles)filesResource;
        Assert.Single(resourceWithFiles.Files, "/path/to/file1.txt");
    }

    [Fact]
    public void FilesResourceImplementsIResourceWithoutLifetime()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddFiles("myfiles");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var filesResource = Assert.Single(appModel.Resources.OfType<FilesResource>());

        Assert.IsAssignableFrom<IResourceWithoutLifetime>(filesResource);
    }

    [Fact]
    public void WithSourceAddsFilesCallbackAnnotation()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddFiles("myfiles")
                  .WithSource("/path/to/directory");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var filesResource = Assert.Single(appModel.Resources.OfType<FilesResource>());

        var callbackAnnotation = Assert.Single(filesResource.Annotations.OfType<Aspire.Hosting.ApplicationModel.FilesCallbackAnnotation>());
        Assert.NotNull(callbackAnnotation.Callback);
    }

    [Fact]
    public void ResourceFileHasCorrectProperties()
    {
        var fullPath = "/full/path/to/file.txt";
        var relativePath = "folder/file.txt";
        
        var resourceFile = new Aspire.Hosting.ApplicationModel.ResourceFile(fullPath, relativePath);
        
        Assert.Equal(fullPath, resourceFile.FullPath);
        Assert.Equal(relativePath, resourceFile.RelativePath);
    }

    [Fact]
    public void ResourceFileConstructorThrowsOnNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => new Aspire.Hosting.ApplicationModel.ResourceFile(null!, "relative"));
        Assert.Throws<ArgumentNullException>(() => new Aspire.Hosting.ApplicationModel.ResourceFile("full", null!));
    }

    [Fact]
    public void FilesResourceAddFileThrowsOnNull()
    {
        var filesResource = new FilesResource("test", []);

        Assert.Throws<ArgumentNullException>(() => filesResource.AddFile(null!));
    }

    [Fact]
    public void FilesResourceAddFilesThrowsOnNull()
    {
        var filesResource = new FilesResource("test", []);

        Assert.Throws<ArgumentNullException>(() => filesResource.AddFiles(null!));
    }
}
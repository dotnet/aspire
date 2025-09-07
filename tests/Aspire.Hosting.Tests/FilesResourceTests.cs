// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Tests;

public class FilesResourceTests
{
    [Fact]
    public void AddFilesResourceWithMultipleFiles()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var files = new[] { "/path/to/file1.txt", "/path/to/file2.txt" };

        appBuilder.AddFiles("myfiles", files);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var filesResource = Assert.Single(appModel.Resources.OfType<FilesResource>());

        Assert.Equal("myfiles", filesResource.Name);
        Assert.Equal(files, filesResource.Files);
    }

    [Fact]
    public void AddFilesResourceWithSingleFile()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var filePath = "/path/to/file.txt";

        appBuilder.AddFiles("myfile", filePath);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var filesResource = Assert.Single(appModel.Resources.OfType<FilesResource>());

        Assert.Equal("myfile", filesResource.Name);
        Assert.Single(filesResource.Files, filePath);
    }

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
    public void WithFileAddsFileToResource()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddFiles("myfiles")
                  .WithFile("/path/to/file1.txt")
                  .WithFile("/path/to/file2.txt");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var filesResource = Assert.Single(appModel.Resources.OfType<FilesResource>());

        Assert.Equal("myfiles", filesResource.Name);
        Assert.Equal(2, filesResource.Files.Count());
        Assert.Contains("/path/to/file1.txt", filesResource.Files);
        Assert.Contains("/path/to/file2.txt", filesResource.Files);
    }

    [Fact]
    public void WithFilesAddsMultipleFilesToResource()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var additionalFiles = new[] { "/path/to/file3.txt", "/path/to/file4.txt" };

        appBuilder.AddFiles("myfiles", "/path/to/file1.txt")
                  .WithFiles(additionalFiles);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var filesResource = Assert.Single(appModel.Resources.OfType<FilesResource>());

        Assert.Equal("myfiles", filesResource.Name);
        Assert.Equal(3, filesResource.Files.Count());
        Assert.Contains("/path/to/file1.txt", filesResource.Files);
        Assert.Contains("/path/to/file3.txt", filesResource.Files);
        Assert.Contains("/path/to/file4.txt", filesResource.Files);
    }

    [Fact]
    public void FilesResourceImplementsIResourceWithFiles()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var files = new[] { "/path/to/file1.txt" };

        appBuilder.AddFiles("myfiles", files);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var filesResource = Assert.Single(appModel.Resources.OfType<FilesResource>());

        Assert.IsAssignableFrom<IResourceWithFiles>(filesResource);
        var resourceWithFiles = (IResourceWithFiles)filesResource;
        Assert.Equal(files, resourceWithFiles.Files);
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
    public void FilesProducedEventCreatesCorrectly()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var filesResource = new FilesResource("test", ["/path/to/file.txt"]);
        var files = new[] { "/path/to/output1.txt", "/path/to/output2.txt" };

        using var app = appBuilder.Build();
        var services = app.Services;

        var filesProducedEvent = new FilesProducedEvent(filesResource, services, files);

        Assert.Equal(filesResource, filesProducedEvent.Resource);
        Assert.Equal(services, filesProducedEvent.Services);
        Assert.Equal(files, filesProducedEvent.Files);
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
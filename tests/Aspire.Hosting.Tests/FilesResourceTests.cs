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
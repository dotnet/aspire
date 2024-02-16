// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aspire.Hosting.Tests;

public class DistributedApplicationBuilderTests
{
    [Theory]
    [InlineData(new string[0], DistributedApplicationOperation.Run)]
    [InlineData(new string[] { "--publisher", "manifest" }, DistributedApplicationOperation.Publish)]
    public void BuilderExecutionContextExposesCorrectOperation(string[] args, DistributedApplicationOperation operation)
    {
        var builder = DistributedApplication.CreateBuilder(args);
        Assert.Equal(operation, builder.ExecutionContext.Operation);
    }

    [Fact]
    public void BuilderAddsDefaultServices()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var app = appBuilder.Build();

        Assert.NotNull(app.Services.GetRequiredKeyedService<IDistributedApplicationPublisher>("manifest"));
        Assert.NotNull(app.Services.GetRequiredKeyedService<IDistributedApplicationPublisher>("dcp"));

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        Assert.Empty(appModel.Resources);

        var lifecycles = app.Services.GetServices<IDistributedApplicationLifecycleHook>();
        Assert.Single(lifecycles.Where(h => h.GetType().Name == "DcpDistributedApplicationLifecycleHook"));
        Assert.Equal(4, lifecycles.Count());

        var options = app.Services.GetRequiredService<IOptions<PublishingOptions>>();
        Assert.Null(options.Value.Publisher);
        Assert.Null(options.Value.OutputPath);
    }

    [Fact]
    public void BuilderAddsResourceToAddModel()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddResource(new TestResource());
        var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var resource = Assert.Single(appModel.Resources);
        Assert.IsType<TestResource>(resource);
    }

    [Fact]
    public void BuilderConfiguresPublishingOptionsFromCommandLine()
    {
        var appBuilder = DistributedApplication.CreateBuilder(["--publisher", "manifest", "--output-path", "/tmp/"]);
        var app = appBuilder.Build();

        var publishOptions = app.Services.GetRequiredService<IOptions<PublishingOptions>>();
        Assert.Equal("manifest", publishOptions.Value.Publisher);
        Assert.Equal("/tmp/", publishOptions.Value.OutputPath);
    }

    [Fact]
    public void BuilderConfiguresPublishingOptionsFromConfig()
    {
        var appBuilder = DistributedApplication.CreateBuilder(["--publisher", "manifest", "--output-path", "/tmp/"]);
        appBuilder.Configuration["Publishing:Publisher"] = "docker";
        appBuilder.Configuration["Publishing:OutputPath"] = "/path/";
        var app = appBuilder.Build();

        var publishOptions = app.Services.GetRequiredService<IOptions<PublishingOptions>>();
        Assert.Equal("docker", publishOptions.Value.Publisher);
        Assert.Equal("/path/", publishOptions.Value.OutputPath);
    }

    [Fact]
    public void AppHostDirectoryAvailableViaConfig()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var appHostDirectory = appBuilder.AppHostDirectory;
        var app = appBuilder.Build();

        var config = app.Services.GetRequiredService<IConfiguration>();
        Assert.Equal(appHostDirectory, config["AppHost:Directory"]);
    }

    [Fact]
    public void AddResource_DuplicateResourceNames_SameCasing_Error()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddResource(new ContainerResource("Test"));

        var ex = Assert.Throws<DistributedApplicationException>(() => appBuilder.AddResource(new ContainerResource("Test")));
        Assert.Equal("Cannot add resource of type 'Aspire.Hosting.ApplicationModel.ContainerResource' with name 'Test' because resource of type 'Aspire.Hosting.ApplicationModel.ContainerResource' with that name already exists. Resource names are case-insensitive.", ex.Message);
    }

    [Fact]
    public void AddResource_DuplicateResourceNames_MixedCasing_Error()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddResource(new ContainerResource("Test"));

        var ex = Assert.Throws<DistributedApplicationException>(() => appBuilder.AddResource(new ContainerResource("TEST")));
        Assert.Equal("Cannot add resource of type 'Aspire.Hosting.ApplicationModel.ContainerResource' with name 'TEST' because resource of type 'Aspire.Hosting.ApplicationModel.ContainerResource' with that name already exists. Resource names are case-insensitive.", ex.Message);
    }

    [Fact]
    public void Build_DuplicateResourceNames_MixedCasing_Error()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.Resources.Add(new ContainerResource("Test"));
        appBuilder.Resources.Add(new ContainerResource("Test"));

        var ex = Assert.Throws<DistributedApplicationException>(appBuilder.Build);
        Assert.Equal("Multiple resources with the name 'Test'. Resource names are case-insensitive.", ex.Message);
    }

    [Fact]
    public void Build_DuplicateResourceNames_SameCasing_Error()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.Resources.Add(new ContainerResource("Test"));
        appBuilder.Resources.Add(new ContainerResource("TEST"));

        var ex = Assert.Throws<DistributedApplicationException>(appBuilder.Build);
        Assert.Equal("Multiple resources with the name 'Test'. Resource names are case-insensitive.", ex.Message);
    }

    private sealed class TestResource : IResource
    {
        public string Name => nameof(TestResource);

        public ResourceMetadataCollection Annotations => throw new NotImplementedException();
    }
}

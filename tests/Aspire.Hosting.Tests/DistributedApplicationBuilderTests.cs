// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aspire.Hosting.Tests;

public class DistributedApplicationBuilderTests
{
    [Fact]
    public void AddingTwoResourcesWithSameNameThrows()
    {
        var ex = Assert.Throws<DistributedApplicationException>(() =>
        {
            var builder = DistributedApplication.CreateBuilder();
            builder.AddRedisContainer("x");
            builder.AddPostgresContainer("x");
        });

        Assert.Equal("Cannot add resource of type 'Aspire.Hosting.ApplicationModel.PostgresContainerResource' with name 'x' because resource of type 'Aspire.Hosting.ApplicationModel.RedisContainerResource' with that name already exists.", ex.Message);
    }

    [Fact]
    public void BuilderAddsDefaultServices()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var app = appBuilder.Build();

        Assert.NotNull(app.Services.GetRequiredKeyedService<IDistributedApplicationPublisher>("manifest"));
        Assert.NotNull(app.Services.GetRequiredKeyedService<IDistributedApplicationPublisher>("dcp"));

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        Assert.Null(appModel.Name);
        Assert.Empty(appModel.Resources);

        var lifecycles = app.Services.GetServices<IDistributedApplicationLifecycleHook>();
        Assert.Single(lifecycles.Where(h => h.GetType().Name == "DcpDistributedApplicationLifecycleHook"));
        Assert.Equal(3, lifecycles.Count());

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
        Assert.Null(appModel.Name);
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

    private sealed class TestResource : IResource
    {
        public string Name => throw new NotImplementedException();

        public ResourceMetadataCollection Annotations => throw new NotImplementedException();
    }
}

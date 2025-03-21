// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aspire.Hosting.Tests;

public class WithUrlsTests
{
    [Fact]
    public void WithUrlsAddsAnnotationForAsyncCallback()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        Func<ResourceUrlsCallbackContext, Task> callback = c => Task.CompletedTask;

        var projectA = builder.AddProject<ProjectA>("projecta")
                              .WithUrls(callback);

        var urlsCallback = projectA.Resource.Annotations.OfType<ResourceUrlsCallbackAnnotation>()
            .Where(a => a.Callback == callback).FirstOrDefault();

        Assert.NotNull(urlsCallback);
    }

    [Fact]
    public void WithUrlsAddsAnnotationForSyncCallback()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var projectA = builder.AddProject<ProjectA>("projecta");

        Assert.Empty(projectA.Resource.Annotations.OfType<ResourceUrlsCallbackAnnotation>());

        projectA.WithUrls(c => { });

        Assert.NotEmpty(projectA.Resource.Annotations.OfType<ResourceUrlsCallbackAnnotation>());
    }

    [Fact]
    public async Task WithUrlsCallsCallbackAfterEndpointsAllocated()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var called = false;
        var projectA = builder.AddProject<ProjectA>("projecta")
            .WithUrls(c => called = true);

        var tcs = new TaskCompletionSource();
        builder.Eventing.Subscribe<AfterEndpointsAllocatedEvent>((e, ct) =>
        {
            // Should not be called until after event handlers for AfterEndpointsAllocatedEvent
            Assert.False(called);
            return Task.CompletedTask;
        });
        builder.Eventing.Subscribe<BeforeResourceStartedEvent>(projectA.Resource, (e, ct) =>
        {
            // Should be called by the time resource is started
            Assert.True(called);
            tcs.SetResult();
            return Task.CompletedTask;
        });

        var app = await builder.BuildAsync();
        await app.StartAsync();

        await tcs.Task;

        await app.StopAsync();
    }

    [Fact]
    public async Task WithUrlsProvidesLoggerInstanceOnCallbackContextAllocated()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        ILogger logger = NullLogger.Instance;
        var projectA = builder.AddProject<ProjectA>("projecta")
            .WithUrls(c => logger = c.Logger);

        var tcs = new TaskCompletionSource();
        builder.Eventing.Subscribe<BeforeResourceStartedEvent>(projectA.Resource, (e, ct) =>
        {
            tcs.SetResult();
            return Task.CompletedTask;
        });

        var app = await builder.BuildAsync();
        await app.StartAsync();

        await tcs.Task;

        Assert.NotNull(logger);
        Assert.True(logger is not NullLogger);

        await app.StopAsync();
    }

    [Fact]
    public async Task WithUrlsAddsUrlAnnotations()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var projectA = builder.AddProject<ProjectA>("projecta")
            .WithUrls(c => c.Urls.Add(new() { Url = "https://example.com", DisplayText = "Example" }));

        var tcs = new TaskCompletionSource();
        builder.Eventing.Subscribe<BeforeResourceStartedEvent>(projectA.Resource, (e, ct) =>
        {
            tcs.SetResult();
            return Task.CompletedTask;
        });

        var app = await builder.BuildAsync();
        await app.StartAsync();
        await tcs.Task;

        var urls = projectA.Resource.Annotations.OfType<ResourceUrlAnnotation>();
        Assert.Single(urls, u => u.Url == "https://example.com" && u.DisplayText == "Example");

        await app.StopAsync();
    }

    [Fact]
    public async Task WithUrlAddsUrlAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var projectA = builder.AddProject<ProjectA>("projecta")
            .WithUrl("https://example.com", "Example");

        var tcs = new TaskCompletionSource();
        builder.Eventing.Subscribe<BeforeResourceStartedEvent>(projectA.Resource, (e, ct) =>
        {
            tcs.SetResult();
            return Task.CompletedTask;
        });

        var app = await builder.BuildAsync();
        await app.StartAsync();
        await tcs.Task;

        var urls = projectA.Resource.Annotations.OfType<ResourceUrlAnnotation>();
        Assert.Single(urls, u => u.Url == "https://example.com" && u.DisplayText == "Example");

        await app.StopAsync();
    }

    [Fact]
    public async Task EndpointsResultInUrls()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var projectA = builder.AddProject<ProjectA>("projecta")
            .WithHttpEndpoint(name: "test");

        var tcs = new TaskCompletionSource();
        builder.Eventing.Subscribe<BeforeResourceStartedEvent>(projectA.Resource, (e, ct) =>
        {
            tcs.SetResult();
            return Task.CompletedTask;
        });

        var app = await builder.BuildAsync();
        await app.StartAsync();
        await tcs.Task;

        var urls = projectA.Resource.Annotations.OfType<ResourceUrlAnnotation>();
        Assert.Single(urls, u => u.Url.StartsWith("http://localhost") && u.Endpoint?.EndpointName == "test");

        await app.StopAsync();
    }

    [Fact]
    public async Task ProjectLaunchProfileRelativeLaunchUrlIsAddedToEndpointUrl()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var projectA = builder.AddProject<ProjectB>("projectb");

        var tcs = new TaskCompletionSource();
        builder.Eventing.Subscribe<BeforeResourceStartedEvent>(projectA.Resource, (e, ct) =>
        {
            tcs.SetResult();
            return Task.CompletedTask;
        });

        var app = await builder.BuildAsync();
        await app.StartAsync();
        await tcs.Task;

        var urls = projectA.Resource.Annotations.OfType<ResourceUrlAnnotation>();
        Assert.Single(urls, u => u.Url.EndsWith("/sub-path") && u.Endpoint?.EndpointName == "http");

        await app.StopAsync();
    }

    [Fact]
    public async Task ProjectLaunchProfileAbsoluteLaunchUrlIsUsedAsEndpointUrl()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var projectA = builder.AddProject<ProjectB>("projectb", launchProfileName: "custom");

        var tcs = new TaskCompletionSource();
        builder.Eventing.Subscribe<BeforeResourceStartedEvent>(projectA.Resource, (e, ct) =>
        {
            tcs.SetResult();
            return Task.CompletedTask;
        });

        var app = await builder.BuildAsync();
        await app.StartAsync();
        await tcs.Task;

        var urls = projectA.Resource.Annotations.OfType<ResourceUrlAnnotation>();
        Assert.Single(urls, u => u.Url == "http://custom.localhost:23456/home" && u.Endpoint?.EndpointName == "http");

        await app.StopAsync();
    }

    [Fact]
    public async Task WithUrlForEndpointUpdatesUrlForEndpoint()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var projectA = builder.AddProject<ProjectA>("projecta")
            .WithHttpEndpoint(name: "test")
            .WithUrlForEndpoint("test", u =>
            {
                u.Url = "https://example.com";
                u.DisplayText = "Link Text";
                u.DisplayOrder = 1000;
            });

        var tcs = new TaskCompletionSource();
        builder.Eventing.Subscribe<BeforeResourceStartedEvent>(projectA.Resource, (e, ct) =>
        {
            tcs.SetResult();
            return Task.CompletedTask;
        });

        var app = await builder.BuildAsync();
        await app.StartAsync();
        await tcs.Task;

        var urls = projectA.Resource.Annotations.OfType<ResourceUrlAnnotation>();
        Assert.Single(urls, u =>
            u.Url == "https://example.com"
            && u.DisplayText == "Link Text"
            && u.Endpoint?.EndpointName == "test"
            && u.DisplayOrder == 1000);

        await app.StopAsync();
    }

    [Fact]
    public async Task WithUrlForEndpointDoesNotThrowOrCallCallbackIfEndpointNotFound()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var called = false;
        var projectA = builder.AddProject<ProjectA>("projecta")
            .WithHttpEndpoint(name: "test")
            .WithUrlForEndpoint("non-existant", u =>
            {
                called = true;
            });

        var tcs = new TaskCompletionSource();
        builder.Eventing.Subscribe<BeforeResourceStartedEvent>(projectA.Resource, (e, ct) =>
        {
            tcs.SetResult();
            return Task.CompletedTask;
        });

        var app = await builder.BuildAsync();
        await app.StartAsync();
        await tcs.Task;

        Assert.False(called);

        await app.StopAsync();
    }

    private sealed class ProjectA : IProjectMetadata
    {
        public string ProjectPath => "projectA";

        public LaunchSettings LaunchSettings { get; } = new();
    }

    private sealed class ProjectB : IProjectMetadata
    {
        public string ProjectPath => "project";

        public LaunchSettings LaunchSettings { get; } = new()
        {
            Profiles = new Dictionary<string, LaunchProfile>
            {
                ["http"] = new()
                {
                    CommandName = "Project",
                    ApplicationUrl = "http://localhost:23456",
                    LaunchUrl = "/sub-path"
                },
                ["custom"] = new()
                {
                    CommandName = "Project",
                    ApplicationUrl = "http://localhost:23456",
                    LaunchUrl = "http://custom.localhost:23456/home"
                }
            }
        };
    }
}

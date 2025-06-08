// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
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
    public async Task WithUrlsCallsCallbackAfterBeforeResourceStartedEvent()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var called = false;
        var projectA = builder.AddProject<ProjectA>("projecta")
            .WithUrls(c => called = true);

        var tcs = new TaskCompletionSource();
        builder.Eventing.Subscribe<ResourceEndpointsAllocatedEvent>(projectA.Resource, (e, ct) =>
        {
            // Should not be called at this point
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
    public async Task WithUrlsProvidesServiceProviderInstanceOnCallbackContextAllocated()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var tcs = new TaskCompletionSource<IServiceProvider>();

        var projectA = builder.AddProject<ProjectA>("projecta")
            .WithUrls(c =>
            {
                try
                {
                    tcs.TrySetResult(c.ExecutionContext.ServiceProvider);
                }
                catch (InvalidOperationException ex)
                {
                    tcs.TrySetException(ex);
                }
            });

        var app = await builder.BuildAsync();

        await app.StartAsync();

        Assert.NotNull(await tcs.Task);

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
    public async Task WithUrlInterpolatedStringAddsUrlAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var projectA = builder.AddProject<ProjectA>("projecta")
            .WithHttpsEndpoint();
        projectA.WithUrl($"{projectA.Resource.GetEndpoint("https")}/test", "Example");

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
        var endpointUrl = urls.First(u => u.Endpoint is not null);
        Assert.Collection(urls,
            u => Assert.True(u.Url == endpointUrl.Url && u.DisplayText is null),
            u => Assert.True(u.Url.EndsWith("/test") && u.DisplayText == "Example")
        );

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
    public async Task EndpointUrlsAreInitiallyInactive()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var servicea = builder.AddProject<Projects.ServiceA>("servicea")
            .WithUrlForEndpoint("http", u => u.Url = "https://example.com");

        var httpEndpoint = servicea.Resource.GetEndpoint("http");

        var app = await builder.BuildAsync();
        var rns = app.Services.GetRequiredService<ResourceNotificationService>();
        ImmutableArray<UrlSnapshot> initialUrlSnapshot = default;
        var cts = new CancellationTokenSource();
        var watchTask = Task.Run(async () =>
        {
            await foreach (var notification in rns.WatchAsync(cts.Token).WithCancellation(cts.Token))
            {
                if (notification.Snapshot.Urls.Length > 0 && initialUrlSnapshot == default)
                {
                    initialUrlSnapshot = notification.Snapshot.Urls;
                    break;
                }
            }
        });

        await app.StartAsync();

        await watchTask.DefaultTimeout(TestConstants.LongTimeoutTimeSpan);
        cts.Cancel();

        await app.StopAsync();

        Assert.Single(initialUrlSnapshot, s => s.Name == httpEndpoint.EndpointName && s.IsInactive && s.Url == "https://example.com");
    }

    [Fact]
    public async Task MultipleUrlsForSingleEndpointAreIncludedInUrlSnapshot()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var servicea = builder.AddProject<Projects.ServiceA>("servicea");
        var httpEndpoint = servicea.Resource.GetEndpoint("http");
        servicea.WithUrl($"{httpEndpoint}/one", "Example 1");
        servicea.WithUrl($"{httpEndpoint}/two", "Example 2");

        var app = await builder.BuildAsync();
        var rns = app.Services.GetRequiredService<ResourceNotificationService>();
        ImmutableArray<UrlSnapshot> initialUrlSnapshot = default;
        var cts = new CancellationTokenSource();
        var watchTask = Task.Run(async () =>
        {
            await foreach (var notification in rns.WatchAsync(cts.Token).WithCancellation(cts.Token))
            {
                if (notification.Snapshot.Urls.Length > 0 && initialUrlSnapshot == default)
                {
                    initialUrlSnapshot = notification.Snapshot.Urls;
                    break;
                }
            }
        });

        await app.StartAsync();

        await watchTask.DefaultTimeout(TestConstants.LongTimeoutTimeSpan);
        cts.Cancel();

        await app.StopAsync();

        Assert.Collection(initialUrlSnapshot,
            s => Assert.True(s.Name == httpEndpoint.EndpointName && s.DisplayProperties.DisplayName == ""), // <-- this is the default URL added for the endpoint
            s => Assert.True(s.Name == httpEndpoint.EndpointName && s.Url.EndsWith("/one") && s.DisplayProperties.DisplayName == "Example 1"),
            s => Assert.True(s.Name == httpEndpoint.EndpointName && s.Url.EndsWith("/two") && s.DisplayProperties.DisplayName == "Example 2")
        );
    }

    [Fact]
    public async Task UrlsAreInExepctedStateForResourcesGivenTheirLifecycle()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var servicea = builder.AddProject<Projects.ServiceA>("servicea")
            .WithUrl("https://example.com/project");

        var custom = builder.AddResource(new CustomResource("custom"))
            .WithHttpEndpoint()
            .WithUrl("https://example.com/custom")
            .WithInitialState(new()
            {
                ResourceType = "Custom",
                CreationTimeStamp = DateTime.UtcNow,
                State = KnownResourceStates.NotStarted,
                Properties = []
            });

        builder.Eventing.Subscribe<InitializeResourceEvent>(custom.Resource, async (e, ct) =>
        {
            // Mark all the endpoints on custom resource as allocated so that the URLs are initialized
            if (custom.Resource.TryGetEndpoints(out var endpoints))
            {
                var startingPort = 1234;
                foreach (var endpoint in endpoints)
                {
                    endpoint.AllocatedEndpoint = new(endpoint, endpoint.TargetHost, endpoint.Port ?? endpoint.TargetPort ?? startingPort++);
                }
            }

            // Publish the ResourceEndpointsAllocatedEvent for the resource
            await e.Eventing.PublishAsync(new ResourceEndpointsAllocatedEvent(custom.Resource, e.Services), EventDispatchBehavior.BlockingConcurrent, ct);

            // Publish the BeforeResourceStartedEvent for the resource
            await e.Eventing.PublishAsync(new BeforeResourceStartedEvent(custom.Resource, e.Services), EventDispatchBehavior.BlockingSequential, ct);

            // Mark all the endpoint URLs as active (this makes them visible in the dashboard)
            await e.Notifications.PublishUpdateAsync(custom.Resource, s => s with
            {
                Urls = [.. s.Urls.Select(u => u with { IsInactive = false })]
            });

            // Move resource to the running state
            await e.Services.GetRequiredService<ResourceNotificationService>()
                .PublishUpdateAsync(e.Resource, s => s with
                {
                    StartTimeStamp = DateTime.UtcNow,
                    State = KnownResourceStates.Running
                });
        });

        var app = await builder.BuildAsync();

        var rns = app.Services.GetRequiredService<ResourceNotificationService>();
        var projectInitialized = false;
        var projectEndpointsAllocated = false;
        var projectRunning = false;
        var customInitialized = false;
        var customEndpointsAllocated = false;
        var customRunning = false;
        var cts = new CancellationTokenSource();
        var watchTask = Task.Run(async () =>
        {
            await foreach (var notification in rns.WatchAsync(cts.Token).WithCancellation(cts.Token))
            {
                if (notification.Resource == servicea.Resource && notification.Snapshot.Urls.Length > 0)
                {
                    if (!projectInitialized)
                    {
                        var urls = notification.Snapshot.Urls;
                        // Endpoint URL should not be present yet, just the static URL
                        var url = Assert.Single(urls);
                        Assert.False(url.IsInactive);
                        Assert.Null(url.Name);
                        Assert.Equal("https://example.com/project", url.Url);
                        projectInitialized = true;
                    }
                    else if (!projectEndpointsAllocated && notification.Snapshot.Urls.Length == 2)
                    {
                        var urls = notification.Snapshot.Urls;
                        Assert.Equal(2, urls.Length);
                        Assert.Collection(urls,
                            // Endpoint URL should be inactive initially
                            s => { Assert.True(s.IsInactive); Assert.NotNull(s.Name); Assert.StartsWith("http://localhost", s.Url); },
                            // Non-endpoint URL should be active
                            s => { Assert.False(s.IsInactive); Assert.Null(s.Name); Assert.Equal("https://example.com/project", s.Url); }
                        );
                        projectEndpointsAllocated = true;
                    }
                    else if (!projectRunning && notification.Snapshot.State == KnownResourceStates.Running &&
                             notification.Snapshot.Urls[^1].IsInactive == false)
                    {
                        var urls = notification.Snapshot.Urls;
                        Assert.Equal(2, urls.Length);
                        Assert.Collection(urls,
                            // Endpoint URL should be active now
                            s => { Assert.False(s.IsInactive); Assert.NotNull(s.Name); Assert.StartsWith("http://localhost", s.Url); },
                            // Non-endpoint URL should still be active
                            s => { Assert.False(s.IsInactive); Assert.Null(s.Name); Assert.Equal("https://example.com/project", s.Url); }
                        );
                        projectRunning = true;
                    }
                }
                else if (notification.Resource == custom.Resource && notification.Snapshot.Urls.Length > 0)
                {
                    if (!customInitialized)
                    {
                        var urls = notification.Snapshot.Urls;
                        // Endpoint URL should not be present yet, just the static URL
                        var url = Assert.Single(urls);
                        Assert.False(url.IsInactive);
                        Assert.Null(url.Name);
                        Assert.Equal("https://example.com/custom", url.Url);
                        customInitialized = true;
                    }
                    else if (!customEndpointsAllocated && notification.Snapshot.Urls.Length == 2)
                    {
                        var urls = notification.Snapshot.Urls;
                        Assert.Equal(2, urls.Length);
                        Assert.Collection(urls,
                            // Endpoint URL should be inactive initially
                            s => { Assert.True(s.IsInactive); Assert.NotNull(s.Name); Assert.StartsWith("http://localhost", s.Url); },
                            // Non-endpoint URL should be active
                            s => { Assert.False(s.IsInactive); Assert.Null(s.Name); Assert.Equal("https://example.com/custom", s.Url); }
                        );
                        customEndpointsAllocated = true;
                    }
                    else if (!customRunning && notification.Snapshot.State == KnownResourceStates.Running &&
                             notification.Snapshot.Urls[^1].IsInactive == false)
                    {
                        var urls = notification.Snapshot.Urls;
                        Assert.Equal(2, urls.Length);
                        Assert.Collection(urls,
                            // Endpoint URL should be active now
                            s => { Assert.False(s.IsInactive); Assert.NotNull(s.Name); Assert.StartsWith("http://localhost", s.Url); },
                            // Non-endpoint URL should be active
                            s => { Assert.False(s.IsInactive); Assert.Null(s.Name); Assert.Equal("https://example.com/custom", s.Url); }
                        );
                        customRunning = true;
                    }
                }

                if (projectRunning && customRunning)
                {
                    break;
                }
            }
        });

        await app.StartAsync();

        await app.ResourceNotifications.WaitForResourceAsync(servicea.Resource.Name, KnownResourceStates.Running).DefaultTimeout(TestConstants.LongTimeoutTimeSpan);
        await app.ResourceNotifications.WaitForResourceAsync(custom.Resource.Name, KnownResourceStates.Running).DefaultTimeout(TestConstants.LongTimeoutTimeSpan);

        await watchTask.DefaultTimeout(TestConstants.LongTimeoutTimeSpan);
        cts.Cancel();

        await app.StopAsync();
    }

    [Fact]
    public async Task UrlsAreMarkedAsInternalDependingOnDisplayLocation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        builder.AddProject<Projects.ServiceA>("servicea")
            .WithUrls(c =>
            {
                c.Urls.Add(new() { Url = "http://example.com/", DisplayLocation = UrlDisplayLocation.SummaryAndDetails });
                c.Urls.Add(new() { Url = "http://example.com/internal", DisplayLocation = UrlDisplayLocation.DetailsOnly });
                c.Urls.Add(new() { Url = "http://example.com/out-of-range", DisplayLocation = (UrlDisplayLocation)100 });
            });

        var app = await builder.BuildAsync();

        var rns = app.Services.GetRequiredService<ResourceNotificationService>();
        ImmutableArray<UrlSnapshot> urlSnapshot = default;
        var cts = new CancellationTokenSource();
        var watchTask = Task.Run(async () =>
        {
            await foreach (var notification in rns.WatchAsync(cts.Token).WithCancellation(cts.Token))
            {
                if (string.Equals(notification.Snapshot.State?.Text, KnownResourceStates.Running))
                {
                    if (notification.Snapshot.Urls.Length > 1 && urlSnapshot == default)
                    {
                        urlSnapshot = notification.Snapshot.Urls;
                        break;
                    }
                }
            }
        });

        await app.StartAsync();

        await rns.WaitForResourceAsync("servicea", KnownResourceStates.Running).DefaultTimeout(TestConstants.LongTimeoutTimeSpan);
        await watchTask.DefaultTimeout(TestConstants.LongTimeoutTimeSpan);
        cts.Cancel();

        await app.StopAsync();

        Assert.Collection(urlSnapshot,
            url => { Assert.Equal("http", url.Name); Assert.False(url.IsInternal); },
            url => { Assert.Equal("http://example.com/", url.Url); Assert.False(url.IsInternal); },
            url => { Assert.Equal("http://example.com/internal", url.Url); Assert.True(url.IsInternal); },
            url => { Assert.Equal("http://example.com/out-of-range", url.Url); Assert.False(url.IsInternal); }
        );
    }

    [Fact]
    public async Task WithUrlForEndpointUpdateDoesNotThrowOrCallCallbackIfEndpointNotFound()
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

    [Fact]
    public async Task WithUrlForEndpointAddDoesNotThrowOrCallCallbackIfEndpointNotFound()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var called = false;
        var projectA = builder.AddProject<ProjectA>("projecta")
            .WithHttpEndpoint(name: "test")
            .WithUrlForEndpoint("non-existant", ep =>
            {
                called = true;
                return new() { Url = "https://example.com" };
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

    [Fact]
    public async Task WithUrlForEndpointUpdateTurnsRelativeUrlIntoAbsoluteUrl()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var projectA = builder.AddProject<ProjectA>("projecta")
            .WithHttpEndpoint(name: "test")
            .WithUrlForEndpoint("test", url =>
            {
                url.Url = "/sub-path";
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

        var endpointUrl = projectA.Resource.Annotations.OfType<ResourceUrlAnnotation>().FirstOrDefault(u => u.Endpoint?.EndpointName == "test");

        Assert.NotNull(endpointUrl);
        Assert.True(endpointUrl.Url.StartsWith("http://localhost") && endpointUrl.Url.EndsWith("/sub-path"));

        await app.StopAsync();
    }

    [Fact]
    public async Task WithUrlForEndpointAddTurnsRelativeUrlIntoAbsoluteUrl()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var projectA = builder.AddProject<ProjectA>("projecta")
            .WithHttpEndpoint(name: "test")
            .WithUrlForEndpoint("test", ep =>
            {
                return new() { Url = "/sub-path" };
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

        var endpointUrl = projectA.Resource.Annotations.OfType<ResourceUrlAnnotation>().FirstOrDefault(u => u.Endpoint?.EndpointName == "test" && u.Url.EndsWith("/sub-path"));

        Assert.NotNull(endpointUrl);
        Assert.True(endpointUrl.Url.StartsWith("http://localhost") && endpointUrl.Url.EndsWith("/sub-path"));

        await app.StopAsync();
    }

    [Fact]
    public async Task WithUrlsTurnsRelativeEndpointUrlsIntoAbsoluteUrls()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var projectA = builder.AddProject<ProjectA>("projecta")
            .WithHttpEndpoint(name: "test")
            .WithUrls(c =>
            {
                c.Urls.Add(new() { Endpoint = c.GetEndpoint("test"), Url = "/sub-path" });
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

        var endpointUrl = projectA.Resource.Annotations.OfType<ResourceUrlAnnotation>().FirstOrDefault(u => u.Endpoint?.EndpointName == "test" && u.Url.EndsWith("/sub-path"));

        Assert.NotNull(endpointUrl);
        Assert.True(endpointUrl.Url.StartsWith("http://localhost") && endpointUrl.Url.EndsWith("/sub-path"));

        await app.StopAsync();
    }

    private sealed class CustomResource(string name) : Resource(name), IResourceWithEndpoints
    {

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

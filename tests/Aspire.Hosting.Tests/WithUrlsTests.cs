// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
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
        builder.Eventing.Subscribe<AfterEndpointsAllocatedEvent>((e, ct) =>
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

        var exceptionTcs = new TaskCompletionSource<Exception?>();

        var projectA = builder.AddProject<ProjectA>("projecta")
            .WithUrls(c =>
            {
                try
                {
                    var sp = c.ExecutionContext.ServiceProvider;
                    exceptionTcs.TrySetResult(null);
                }
                catch (InvalidOperationException ex)
                {
                    exceptionTcs.TrySetException(ex);
                }
            });

        var app = await builder.BuildAsync();

        await app.StartAsync();

        var exception = await exceptionTcs.Task;

        if (exception is not null)
        {
            throw new Xunit.Sdk.XunitException("Exception occurred in WithUrls callback.", exception);
        }

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
    public async Task NonEndpointUrlsAreInactiveUntilResourceRunning()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        builder.AddProject<Projects.ServiceA>("servicea")
            .WithUrl("https://example.com");

        var app = await builder.BuildAsync();

        var rns = app.Services.GetRequiredService<ResourceNotificationService>();
        ImmutableArray<UrlSnapshot> initialUrlSnapshot = default;
        ImmutableArray<UrlSnapshot> urlSnapshotAfterRunning = default;
        var cts = new CancellationTokenSource();
        var watchTask = Task.Run(async () =>
        {
            await foreach (var notification in rns.WatchAsync(cts.Token).WithCancellation(cts.Token))
            {
                if (notification.Snapshot.Urls.Length > 0 && initialUrlSnapshot == default)
                {
                    initialUrlSnapshot = notification.Snapshot.Urls;
                    continue;
                }

                if (string.Equals(notification.Snapshot.State?.Text, KnownResourceStates.Running))
                {
                    if (notification.Snapshot.Urls.Length > 0 && urlSnapshotAfterRunning == default)
                    {
                        urlSnapshotAfterRunning = notification.Snapshot.Urls;
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

        Assert.All(initialUrlSnapshot, s => Assert.True(s.IsInactive));
        Assert.Single(urlSnapshotAfterRunning, s => !s.IsInactive && s.Url == "https://example.com");
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

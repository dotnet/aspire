// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Maui;
using Aspire.Hosting.Maui.Annotations;
using Aspire.Hosting.Maui.Lifecycle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Tests;

/// <summary>
/// Tests for the MAUI build queue that serializes builds per-project.
/// These tests construct the MauiBuildQueueEventSubscriber directly and register
/// only that subscriber, avoiding the Android/iOS environment subscribers that
/// depend on services not available in unit tests.
/// </summary>
public class MauiBuildQueueTests
{
    [Fact]
    public void BuildQueueAnnotation_SemaphoreInitializedToOne()
    {
        using var annotation = new MauiBuildQueueAnnotation();
        Assert.Equal(1, annotation.BuildSemaphore.CurrentCount);
    }

    [Fact]
    public void BuildQueueAnnotation_Dispose_ReleasesSemaphore()
    {
        var annotation = new MauiBuildQueueAnnotation();
        annotation.Dispose();

        Assert.Throws<ObjectDisposedException>(() => annotation.BuildSemaphore.Wait(0));
    }

    [Fact]
    public void BuildQueueAnnotation_AddedByAddMauiProject()
    {
        var parent = new MauiProjectResource("mauiapp", "/fake/path.csproj");
        parent.Annotations.Add(new MauiBuildQueueAnnotation());
        Assert.True(parent.TryGetLastAnnotation<MauiBuildQueueAnnotation>(out _));
    }

    [Fact]
    public async Task SingleResource_AcquiresSemaphore()
    {
        await using var env = await BuildQueueTestEnvironment.CreateAsync();

        await env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.Android, env.Services),
            CancellationToken.None);

        Assert.True(env.Parent.TryGetLastAnnotation<MauiBuildQueueAnnotation>(out var annotation));
        Assert.Equal(0, annotation!.BuildSemaphore.CurrentCount);
    }

    [Fact]
    public async Task SecondResource_BlocksUntilBuildCompletes()
    {
        await using var env = await BuildQueueTestEnvironment.CreateAsync();

        await env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.Android, env.Services),
            CancellationToken.None);

        var secondTask = Task.Run(() => env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.MacCatalyst, env.Services),
            CancellationToken.None));

        await Task.Delay(300);
        Assert.False(secondTask.IsCompleted, "Second resource should be blocked by the queue.");

        // Simulate MSBuild completing — write "Build succeeded" to the resource log.
        env.SimulateBuildComplete(env.Android);

        await secondTask.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SecondResource_ShowsQueuedState()
    {
        await using var env = await BuildQueueTestEnvironment.CreateAsync();

        await env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.Android, env.Services),
            CancellationToken.None);

        var queuedSeen = new TaskCompletionSource<bool>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        _ = Task.Run(async () =>
        {
            await foreach (var evt in env.NotificationService.WatchAsync(cts.Token))
            {
                if (evt.Resource.Name == env.MacCatalyst.Name && evt.Snapshot.State?.Text == "Queued")
                {
                    queuedSeen.TrySetResult(true);
                    return;
                }
            }
        }, cts.Token);

        _ = Task.Run(() => env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.MacCatalyst, env.Services),
            CancellationToken.None));

        var result = await queuedSeen.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(result);

        await env.NotificationService.PublishUpdateAsync(env.Android, s => s with
        {
            State = new ResourceStateSnapshot(KnownResourceStates.Running, KnownResourceStateStyles.Success)
        });
    }

    [Fact]
    public async Task SingleResource_ShowsBuildingState()
    {
        await using var env = await BuildQueueTestEnvironment.CreateAsync();

        var buildingSeen = new TaskCompletionSource<bool>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        _ = Task.Run(async () =>
        {
            await foreach (var evt in env.NotificationService.WatchAsync(cts.Token))
            {
                if (evt.Resource.Name == env.Android.Name && evt.Snapshot.State?.Text == "Building")
                {
                    buildingSeen.TrySetResult(true);
                    return;
                }
            }
        }, cts.Token);

        _ = Task.Run(() => env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.Android, env.Services),
            CancellationToken.None));

        var result = await buildingSeen.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(result);
    }

    [Fact]
    public async Task ResourcesFromDifferentProjects_RunConcurrently()
    {
        await using var env = await BuildQueueTestEnvironment.CreateWithTwoProjectsAsync();

        var task1 = env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.Android, env.Services),
            CancellationToken.None);

        var task2 = env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.Android2!, env.Services),
            CancellationToken.None);

        await Task.WhenAll(task1, task2).WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task FailedResource_ReleasesQueue()
    {
        await using var env = await BuildQueueTestEnvironment.CreateAsync();

        await env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.Android, env.Services),
            CancellationToken.None);

        var secondTask = Task.Run(() => env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.MacCatalyst, env.Services),
            CancellationToken.None));

        await Task.Delay(200);
        Assert.False(secondTask.IsCompleted);

        await env.NotificationService.PublishUpdateAsync(env.Android, s => s with
        {
            State = new ResourceStateSnapshot(KnownResourceStates.FailedToStart, KnownResourceStateStyles.Error)
        });

        await secondTask.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CancelledQueuedResource_DoesNotDeadlock()
    {
        await using var env = await BuildQueueTestEnvironment.CreateAsync();

        await env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.Android, env.Services),
            CancellationToken.None);

        using var cts = new CancellationTokenSource();
        var secondTask = Task.Run(() => env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.MacCatalyst, env.Services),
            cts.Token));

        await Task.Delay(200);

        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => secondTask.WaitAsync(TimeSpan.FromSeconds(5)));

        await env.NotificationService.PublishUpdateAsync(env.Android, s => s with
        {
            State = new ResourceStateSnapshot(KnownResourceStates.Finished, KnownResourceStateStyles.Success)
        });

        var thirdTask = env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.IOSSimulator, env.Services),
            CancellationToken.None);

        await thirdTask.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ThreeResources_ExecuteInSequence()
    {
        await using var env = await BuildQueueTestEnvironment.CreateAsync();
        var completionOrder = new List<string>();

        var task1 = Task.Run(async () =>
        {
            await env.Eventing.PublishAsync(
                new BeforeResourceStartedEvent(env.Android, env.Services),
                CancellationToken.None);
            lock (completionOrder) { completionOrder.Add("android"); }
        });

        await task1.WaitAsync(TimeSpan.FromSeconds(5));

        var task2 = Task.Run(async () =>
        {
            await env.Eventing.PublishAsync(
                new BeforeResourceStartedEvent(env.MacCatalyst, env.Services),
                CancellationToken.None);
            lock (completionOrder) { completionOrder.Add("maccatalyst"); }
        });

        await Task.Delay(100);

        var task3 = Task.Run(async () =>
        {
            await env.Eventing.PublishAsync(
                new BeforeResourceStartedEvent(env.IOSSimulator, env.Services),
                CancellationToken.None);
            lock (completionOrder) { completionOrder.Add("ios"); }
        });

        await Task.Delay(200);

        Assert.Single(completionOrder);
        Assert.False(task2.IsCompleted);
        Assert.False(task3.IsCompleted);

        env.SimulateBuildComplete(env.Android);

        await task2.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(2, completionOrder.Count);

        env.SimulateBuildComplete(env.MacCatalyst);

        await task3.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(3, completionOrder.Count);
    }

    [Fact]
    public async Task NonMauiResource_IsNotAffected()
    {
        await using var env = await BuildQueueTestEnvironment.CreateAsync();

        await env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.Android, env.Services),
            CancellationToken.None);

        // Parent MauiProjectResource is NOT IMauiPlatformResource — should not block
        var parentTask = env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.Parent, env.Services),
            CancellationToken.None);

        await parentTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    /// <summary>
    /// Test environment that creates resources manually and registers only the
    /// <see cref="MauiBuildQueueEventSubscriber"/>, avoiding the Android/iOS
    /// environment subscribers that require services unavailable in unit tests.
    /// </summary>
    private sealed class BuildQueueTestEnvironment : IAsyncDisposable
    {
        public required DistributedApplication App { get; init; }
        public required MauiProjectResource Parent { get; init; }
        public required MauiAndroidEmulatorResource Android { get; init; }
        public required MauiMacCatalystPlatformResource MacCatalyst { get; init; }
        public required MauiiOSSimulatorResource IOSSimulator { get; init; }
        public MauiAndroidEmulatorResource? Android2 { get; init; }

        public IServiceProvider Services => App.Services;
        public IDistributedApplicationEventing Eventing => App.Services.GetRequiredService<IDistributedApplicationEventing>();
        public ResourceNotificationService NotificationService => App.Services.GetRequiredService<ResourceNotificationService>();
        public ResourceLoggerService LoggerService => App.Services.GetRequiredService<ResourceLoggerService>();

        /// <summary>
        /// Simulates MSBuild completing by writing "Build succeeded" to the resource's log stream.
        /// </summary>
        public void SimulateBuildComplete(IResource resource)
        {
            var logger = LoggerService.GetLogger(resource);
            logger.LogInformation("Build succeeded.");
        }

        public static async Task<BuildQueueTestEnvironment> CreateAsync()
        {
            var appBuilder = DistributedApplication.CreateBuilder();

            var parent = new MauiProjectResource("mauiapp", "/fake/path.csproj");
            parent.Annotations.Add(new MauiBuildQueueAnnotation());
            appBuilder.CreateResourceBuilder(parent);

            var android = new MauiAndroidEmulatorResource("android", parent);
            appBuilder.AddResource(android);

            var macCatalyst = new MauiMacCatalystPlatformResource("maccatalyst", parent);
            appBuilder.AddResource(macCatalyst);

            var iosSimulator = new MauiiOSSimulatorResource("ios-simulator", parent);
            appBuilder.AddResource(iosSimulator);

            var app = appBuilder.Build();
            await InitializeSubscriberAsync(app);

            return new BuildQueueTestEnvironment
            {
                App = app,
                Parent = parent,
                Android = android,
                MacCatalyst = macCatalyst,
                IOSSimulator = iosSimulator
            };
        }

        public static async Task<BuildQueueTestEnvironment> CreateWithTwoProjectsAsync()
        {
            var appBuilder = DistributedApplication.CreateBuilder();

            var parent1 = new MauiProjectResource("mauiapp1", "/fake/path1.csproj");
            parent1.Annotations.Add(new MauiBuildQueueAnnotation());
            appBuilder.CreateResourceBuilder(parent1);
            var android1 = new MauiAndroidEmulatorResource("android1", parent1);
            appBuilder.AddResource(android1);

            var parent2 = new MauiProjectResource("mauiapp2", "/fake/path2.csproj");
            parent2.Annotations.Add(new MauiBuildQueueAnnotation());
            appBuilder.CreateResourceBuilder(parent2);
            var android2 = new MauiAndroidEmulatorResource("android2", parent2);
            appBuilder.AddResource(android2);

            var macCatalyst = new MauiMacCatalystPlatformResource("maccatalyst", parent1);
            appBuilder.AddResource(macCatalyst);

            var iosSimulator = new MauiiOSSimulatorResource("ios-simulator", parent1);
            appBuilder.AddResource(iosSimulator);

            var app = appBuilder.Build();
            await InitializeSubscriberAsync(app);

            return new BuildQueueTestEnvironment
            {
                App = app,
                Parent = parent1,
                Android = android1,
                MacCatalyst = macCatalyst,
                IOSSimulator = iosSimulator,
                Android2 = android2
            };
        }

        private static async Task InitializeSubscriberAsync(DistributedApplication app)
        {
            var notificationService = app.Services.GetRequiredService<ResourceNotificationService>();
            var loggerService = app.Services.GetRequiredService<ResourceLoggerService>();
            var eventing = app.Services.GetRequiredService<IDistributedApplicationEventing>();
            var execContext = app.Services.GetRequiredService<DistributedApplicationExecutionContext>();

            var subscriber = new MauiBuildQueueEventSubscriber(notificationService, loggerService);
            await subscriber.SubscribeAsync(eventing, execContext, CancellationToken.None);
        }

        public async ValueTask DisposeAsync()
        {
            await App.DisposeAsync();
        }
    }
}

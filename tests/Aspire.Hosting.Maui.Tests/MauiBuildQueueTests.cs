// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
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
/// Uses a <see cref="TestableBuildQueueSubscriber"/> that overrides
/// <see cref="MauiBuildQueueEventSubscriber.RunBuildAsync"/> with a
/// controllable <see cref="TaskCompletionSource"/> per resource.
/// </summary>
public class MauiBuildQueueTests
{
    [Fact]
    public void BuildQueueAnnotation_SemaphoreInitializedToOne()
    {
        var annotation = new MauiBuildQueueAnnotation();
        Assert.Equal(1, annotation.BuildSemaphore.CurrentCount);
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

        // Start the event but don't complete the build yet — semaphore should be held.
        var eventTask = Task.Run(() => env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.Android, env.Services),
            CancellationToken.None));

        await Task.Delay(200);

        Assert.True(env.Parent.TryGetLastAnnotation<MauiBuildQueueAnnotation>(out var annotation));
        Assert.Equal(0, annotation!.BuildSemaphore.CurrentCount);

        env.Subscriber.CompleteBuild(env.Android);
        await eventTask.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SingleResource_ReleasesSemaphoreAfterBuild()
    {
        await using var env = await BuildQueueTestEnvironment.CreateAsync();

        env.Subscriber.CompleteBuildImmediately(env.Android);

        await env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.Android, env.Services),
            CancellationToken.None);

        Assert.True(env.Parent.TryGetLastAnnotation<MauiBuildQueueAnnotation>(out var annotation));
        Assert.Equal(1, annotation!.BuildSemaphore.CurrentCount);
    }

    [Fact]
    public async Task SecondResource_BlocksUntilBuildCompletes()
    {
        await using var env = await BuildQueueTestEnvironment.CreateAsync();

        var task1 = Task.Run(() => env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.Android, env.Services),
            CancellationToken.None));

        await Task.Delay(200);

        var task2 = Task.Run(() => env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.MacCatalyst, env.Services),
            CancellationToken.None));

        await Task.Delay(300);
        Assert.False(task2.IsCompleted, "Second resource should be blocked by the queue.");

        // Complete first build — second should start.
        env.Subscriber.CompleteBuild(env.Android);
        await task1.WaitAsync(TimeSpan.FromSeconds(5));

        // Complete second build.
        env.Subscriber.CompleteBuild(env.MacCatalyst);
        await task2.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SecondResource_ShowsQueuedState()
    {
        await using var env = await BuildQueueTestEnvironment.CreateAsync();

        var task1 = Task.Run(() => env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.Android, env.Services),
            CancellationToken.None));

        await Task.Delay(200);

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

        var task2 = Task.Run(() => env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.MacCatalyst, env.Services),
            CancellationToken.None));

        var result = await queuedSeen.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(result);

        // Clean up: complete both builds and await their tasks.
        env.Subscriber.CompleteBuild(env.Android);
        await task1.WaitAsync(TimeSpan.FromSeconds(5));
        env.Subscriber.CompleteBuild(env.MacCatalyst);
        await task2.WaitAsync(TimeSpan.FromSeconds(5));
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

        var eventTask = Task.Run(() => env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.Android, env.Services),
            CancellationToken.None));

        var result = await buildingSeen.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(result);

        env.Subscriber.CompleteBuild(env.Android);
        await eventTask.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ResourcesFromDifferentProjects_RunConcurrently()
    {
        await using var env = await BuildQueueTestEnvironment.CreateWithTwoProjectsAsync();

        // Start both events WITHOUT completing builds — both should enter Building concurrently.
        var task1 = Task.Run(() => env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.Android, env.Services),
            CancellationToken.None));

        var task2 = Task.Run(() => env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.Android2!, env.Services),
            CancellationToken.None));

        await Task.Delay(300);

        // Both should be building simultaneously — neither should have completed,
        // proving they are NOT serialized across different projects.
        Assert.False(task1.IsCompleted, "Task1 should still be building.");
        Assert.False(task2.IsCompleted, "Task2 should still be building.");

        // Complete both builds.
        env.Subscriber.CompleteBuild(env.Android);
        env.Subscriber.CompleteBuild(env.Android2!);

        await Task.WhenAll(task1, task2).WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task FailedBuild_ReleasesQueueAndThrows()
    {
        await using var env = await BuildQueueTestEnvironment.CreateAsync();

        env.Subscriber.FailBuild(env.Android, "Compilation error");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => env.Eventing.PublishAsync(
                new BeforeResourceStartedEvent(env.Android, env.Services),
                CancellationToken.None));

        // Semaphore should be released even after failure.
        Assert.True(env.Parent.TryGetLastAnnotation<MauiBuildQueueAnnotation>(out var annotation));
        Assert.Equal(1, annotation!.BuildSemaphore.CurrentCount);
    }

    [Fact]
    public async Task CancelledQueuedResource_DoesNotDeadlock()
    {
        await using var env = await BuildQueueTestEnvironment.CreateAsync();

        var task1 = Task.Run(() => env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.Android, env.Services),
            CancellationToken.None));

        await Task.Delay(200);

        using var cts = new CancellationTokenSource();
        var task2 = Task.Run(() => env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.MacCatalyst, env.Services),
            cts.Token));

        await Task.Delay(200);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => task2.WaitAsync(TimeSpan.FromSeconds(5)));

        // Complete first build — semaphore should still work for a third resource.
        env.Subscriber.CompleteBuild(env.Android);
        await task1.WaitAsync(TimeSpan.FromSeconds(5));

        env.Subscriber.CompleteBuildImmediately(env.IOSSimulator);
        var task3 = env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.IOSSimulator, env.Services),
            CancellationToken.None);
        await task3.WaitAsync(TimeSpan.FromSeconds(5));
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

        await Task.Delay(200);

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

        Assert.Empty(completionOrder);
        Assert.False(task2.IsCompleted);
        Assert.False(task3.IsCompleted);

        env.Subscriber.CompleteBuild(env.Android);
        await task1.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Single(completionOrder);
        Assert.Equal("android", completionOrder[0]);

        env.Subscriber.CompleteBuild(env.MacCatalyst);
        await task2.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(2, completionOrder.Count);
        Assert.Equal("maccatalyst", completionOrder[1]);

        env.Subscriber.CompleteBuild(env.IOSSimulator);
        await task3.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(3, completionOrder.Count);
        Assert.Equal("ios", completionOrder[2]);
    }

    [Fact]
    public async Task NonMauiResource_IsNotAffected()
    {
        await using var env = await BuildQueueTestEnvironment.CreateAsync();

        var task1 = Task.Run(() => env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.Android, env.Services),
            CancellationToken.None));

        await Task.Delay(200);

        // Parent MauiProjectResource is NOT IMauiPlatformResource — should pass through.
        var parentTask = env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.Parent, env.Services),
            CancellationToken.None);

        await parentTask.WaitAsync(TimeSpan.FromSeconds(2));

        env.Subscriber.CompleteBuild(env.Android);
        await task1.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ResourceRestart_CanBuildSameResourceTwice()
    {
        await using var env = await BuildQueueTestEnvironment.CreateAsync();

        // First build
        env.Subscriber.CompleteBuildImmediately(env.Android);
        await env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.Android, env.Services),
            CancellationToken.None);

        // Semaphore released after first build
        Assert.True(env.Parent.TryGetLastAnnotation<MauiBuildQueueAnnotation>(out var annotation));
        Assert.Equal(1, annotation!.BuildSemaphore.CurrentCount);

        // Second build of same resource (restart scenario)
        env.Subscriber.CompleteBuildImmediately(env.Android);
        await env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.Android, env.Services),
            CancellationToken.None);

        Assert.Equal(1, annotation.BuildSemaphore.CurrentCount);
    }

    [Fact]
    public async Task MissingBuildQueueAnnotation_SkipsQueue()
    {
        // Create a parent without MauiBuildQueueAnnotation
        var appBuilder = DistributedApplication.CreateBuilder();
        var parent = new MauiProjectResource("mauiapp-no-annotation", "/fake/path.csproj");
        appBuilder.CreateResourceBuilder(parent);

        var android = new MauiAndroidEmulatorResource("android-no-annotation", parent);
        appBuilder.AddResource(android);

        var app = appBuilder.Build();
        var notificationService = app.Services.GetRequiredService<ResourceNotificationService>();
        var loggerService = app.Services.GetRequiredService<ResourceLoggerService>();
        var eventing = app.Services.GetRequiredService<IDistributedApplicationEventing>();
        var execContext = app.Services.GetRequiredService<DistributedApplicationExecutionContext>();

        var subscriber = new TestableBuildQueueSubscriber(notificationService, loggerService);
        await subscriber.SubscribeAsync(eventing, execContext, CancellationToken.None);

        // Should return immediately — no annotation means no queue
        await eventing.PublishAsync(
            new BeforeResourceStartedEvent(android, app.Services),
            CancellationToken.None);

        await app.DisposeAsync();
    }

    [Fact]
    public async Task MissingBuildInfoAnnotation_SkipsBuildAndReleasesQueue()
    {
        // Use the real subscriber (not testable) to exercise the RunBuildAsync path
        // where MauiBuildInfoAnnotation is absent.
        var appBuilder = DistributedApplication.CreateBuilder();
        var parent = new MauiProjectResource("mauiapp", "/fake/path.csproj");
        parent.Annotations.Add(new MauiBuildQueueAnnotation());
        appBuilder.CreateResourceBuilder(parent);

        var android = new MauiAndroidEmulatorResource("android", parent);
        appBuilder.AddResource(android);

        var app = appBuilder.Build();
        var notificationService = app.Services.GetRequiredService<ResourceNotificationService>();
        var loggerService = app.Services.GetRequiredService<ResourceLoggerService>();
        var eventing = app.Services.GetRequiredService<IDistributedApplicationEventing>();
        var execContext = app.Services.GetRequiredService<DistributedApplicationExecutionContext>();

        // Use real subscriber — android has no MauiBuildInfoAnnotation
        var subscriber = new MauiBuildQueueEventSubscriber(notificationService, loggerService);
        await subscriber.SubscribeAsync(eventing, execContext, CancellationToken.None);

        // Should complete without error — build is skipped, semaphore released
        await eventing.PublishAsync(
            new BeforeResourceStartedEvent(android, app.Services),
            CancellationToken.None);

        Assert.True(parent.TryGetLastAnnotation<MauiBuildQueueAnnotation>(out var annotation));
        Assert.Equal(1, annotation!.BuildSemaphore.CurrentCount);

        await app.DisposeAsync();
    }

    [Fact]
    public async Task UnexpectedException_ReleasesSemaphore()
    {
        await using var env = await BuildQueueTestEnvironment.CreateAsync();

        env.Subscriber.FailBuildWith(env.Android, new ArgumentException("Unexpected error"));

        await Assert.ThrowsAsync<ArgumentException>(
            () => env.Eventing.PublishAsync(
                new BeforeResourceStartedEvent(env.Android, env.Services),
                CancellationToken.None));

        // Semaphore should be released even for unexpected exception types.
        Assert.True(env.Parent.TryGetLastAnnotation<MauiBuildQueueAnnotation>(out var annotation));
        Assert.Equal(1, annotation!.BuildSemaphore.CurrentCount);
    }

    [Fact]
    public void BuildInfoAnnotation_StoresAllProperties()
    {
        var annotation = new MauiBuildInfoAnnotation(
            "/path/to/project.csproj",
            "/path/to",
            "net10.0-android",
            "Debug");

        Assert.Equal("/path/to/project.csproj", annotation.ProjectPath);
        Assert.Equal("/path/to", annotation.WorkingDirectory);
        Assert.Equal("net10.0-android", annotation.TargetFramework);
        Assert.Equal("Debug", annotation.Configuration);
    }

    [Fact]
    public void BuildInfoAnnotation_NullableProperties()
    {
        var annotation = new MauiBuildInfoAnnotation(
            "/path/to/project.csproj",
            "/path/to",
            targetFramework: null,
            configuration: null);

        Assert.Null(annotation.TargetFramework);
        Assert.Null(annotation.Configuration);
    }

    [Fact]
    public async Task CancelQueuedResource_CompletesGracefullyAndDoesNotAcquireSemaphore()
    {
        await using var env = await BuildQueueTestEnvironment.CreateAsync();

        // Hold the semaphore with Android.
        var task1 = Task.Run(() => env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.Android, env.Services),
            CancellationToken.None));

        await Task.Delay(200);

        // Queue MacCatalyst — it should be stuck waiting.
        var task2 = Task.Run(() => env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.MacCatalyst, env.Services),
            CancellationToken.None));

        await Task.Delay(200);
        Assert.False(task2.IsCompleted, "MacCatalyst should be queued.");

        // Cancel the queued resource via the subscriber's CancelResource method.
        env.CancelResource(env.MacCatalyst.Name);

        // Should complete without throwing — the handler catches the cancellation
        // and sets a terminal state instead of propagating the exception.
        await task2.WaitAsync(TimeSpan.FromSeconds(5));

        // Semaphore should still be held (count 0) — cancellation of queued resource
        // should NOT release the semaphore because it never acquired it.
        Assert.True(env.Parent.TryGetLastAnnotation<MauiBuildQueueAnnotation>(out var annotation));
        Assert.Equal(0, annotation!.BuildSemaphore.CurrentCount);

        // Clean up: complete Android build.
        env.Subscriber.CompleteBuild(env.Android);
        await task1.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CancelBuildingResource_ReleasesSemaphore()
    {
        await using var env = await BuildQueueTestEnvironment.CreateAsync();

        // Start Android build (it will hold the semaphore and wait for TCS).
        var task1 = Task.Run(() => env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.Android, env.Services),
            CancellationToken.None));

        await Task.Delay(200);

        // Android should be building (semaphore held).
        Assert.True(env.Parent.TryGetLastAnnotation<MauiBuildQueueAnnotation>(out var annotation));
        Assert.Equal(0, annotation!.BuildSemaphore.CurrentCount);

        // Cancel via the subscriber — this cancels the CTS, which cancels the TCS via the registration.
        env.CancelResource(env.Android.Name);

        // Should complete without throwing — the handler catches the cancellation.
        await task1.WaitAsync(TimeSpan.FromSeconds(5));

        // Semaphore should be released after the building resource is cancelled.
        Assert.Equal(1, annotation.BuildSemaphore.CurrentCount);
    }

    [Fact]
    public async Task CancelQueuedResource_NextResourceProceeds()
    {
        await using var env = await BuildQueueTestEnvironment.CreateAsync();

        // Hold the semaphore with Android.
        var task1 = Task.Run(() => env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.Android, env.Services),
            CancellationToken.None));

        await Task.Delay(200);

        // Queue MacCatalyst and iOS.
        var task2 = Task.Run(() => env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.MacCatalyst, env.Services),
            CancellationToken.None));

        await Task.Delay(100);

        var task3 = Task.Run(() => env.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(env.IOSSimulator, env.Services),
            CancellationToken.None));

        await Task.Delay(200);

        // Cancel MacCatalyst.
        env.CancelResource(env.MacCatalyst.Name);
        // Should complete gracefully — no exception.
        await task2.WaitAsync(TimeSpan.FromSeconds(5));

        // Complete Android — iOS should proceed (MacCatalyst was cancelled without acquiring semaphore).
        env.Subscriber.CompleteBuild(env.Android);
        await task1.WaitAsync(TimeSpan.FromSeconds(5));

        // iOS should now be building.
        await Task.Delay(200);
        Assert.False(task3.IsCompleted, "iOS should be building now.");

        env.Subscriber.CompleteBuild(env.IOSSimulator);
        await task3.WaitAsync(TimeSpan.FromSeconds(5));
    }

    // ───────────────────────────────────────────────────────────────────
    // Test infrastructure
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// A subscriber that overrides <see cref="RunBuildAsync"/> with a controllable
    /// <see cref="TaskCompletionSource"/> so tests can decide when (and whether) each
    /// resource's build completes.
    /// </summary>
    private sealed class TestableBuildQueueSubscriber(
        ResourceNotificationService notificationService,
        ResourceLoggerService loggerService) : MauiBuildQueueEventSubscriber(notificationService, loggerService)
    {
        private readonly ConcurrentDictionary<string, TaskCompletionSource> _buildCompletions = new();
        private readonly ConcurrentDictionary<string, Exception> _buildFailures = new();

        /// <summary>Completes the build for the given resource, unblocking the event handler.</summary>
        public void CompleteBuild(IResource resource)
        {
            GetOrCreateCompletion(resource.Name).TrySetResult();
        }

        /// <summary>Pre-registers a resource whose build should complete immediately.</summary>
        public void CompleteBuildImmediately(IResource resource)
        {
            GetOrCreateCompletion(resource.Name).TrySetResult();
        }

        /// <summary>Pre-registers a resource whose build should fail with an exception.</summary>
        public void FailBuild(IResource resource, string message)
        {
            _buildFailures[resource.Name] = new InvalidOperationException(message);
            GetOrCreateCompletion(resource.Name).TrySetResult();
        }

        /// <summary>Pre-registers a resource whose build should fail with a specific exception.</summary>
        public void FailBuildWith(IResource resource, Exception exception)
        {
            _buildFailures[resource.Name] = exception;
            GetOrCreateCompletion(resource.Name).TrySetResult();
        }

        internal override async Task RunBuildAsync(IResource resource, ILogger logger, CancellationToken cancellationToken)
        {
            var tcs = GetOrCreateCompletion(resource.Name);

            try
            {
                using var reg = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
                await tcs.Task.ConfigureAwait(false);

                if (_buildFailures.TryRemove(resource.Name, out var ex))
                {
                    throw ex;
                }
            }
            finally
            {
                // Always clean up so the same resource can be restarted.
                _buildCompletions.TryRemove(resource.Name, out _);
            }
        }

        private TaskCompletionSource GetOrCreateCompletion(string name)
        {
            return _buildCompletions.GetOrAdd(name, _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
        }
    }

    /// <summary>
    /// Test environment that creates resources manually and registers only the
    /// <see cref="TestableBuildQueueSubscriber"/>, avoiding the Android/iOS
    /// environment subscribers that require services unavailable in unit tests.
    /// </summary>
    private sealed class BuildQueueTestEnvironment : IAsyncDisposable
    {
        public required DistributedApplication App { get; init; }
        public required MauiProjectResource Parent { get; init; }
        public required MauiAndroidEmulatorResource Android { get; init; }
        public required MauiMacCatalystPlatformResource MacCatalyst { get; init; }
        public required MauiiOSSimulatorResource IOSSimulator { get; init; }
        public required TestableBuildQueueSubscriber Subscriber { get; init; }
        public MauiAndroidEmulatorResource? Android2 { get; init; }

        public IServiceProvider Services => App.Services;
        public IDistributedApplicationEventing Eventing => App.Services.GetRequiredService<IDistributedApplicationEventing>();
        public ResourceNotificationService NotificationService => App.Services.GetRequiredService<ResourceNotificationService>();

        /// <summary>Cancels a resource via the annotation's CancelResource method.</summary>
        public bool CancelResource(string resourceName)
        {
            Parent.TryGetLastAnnotation<MauiBuildQueueAnnotation>(out var annotation);
            return annotation!.CancelResource(resourceName);
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
            var subscriber = await InitializeSubscriberAsync(app);

            return new BuildQueueTestEnvironment
            {
                App = app,
                Parent = parent,
                Android = android,
                MacCatalyst = macCatalyst,
                IOSSimulator = iosSimulator,
                Subscriber = subscriber
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
            var subscriber = await InitializeSubscriberAsync(app);

            return new BuildQueueTestEnvironment
            {
                App = app,
                Parent = parent1,
                Android = android1,
                MacCatalyst = macCatalyst,
                IOSSimulator = iosSimulator,
                Android2 = android2,
                Subscriber = subscriber
            };
        }

        private static async Task<TestableBuildQueueSubscriber> InitializeSubscriberAsync(DistributedApplication app)
        {
            var notificationService = app.Services.GetRequiredService<ResourceNotificationService>();
            var loggerService = app.Services.GetRequiredService<ResourceLoggerService>();
            var eventing = app.Services.GetRequiredService<IDistributedApplicationEventing>();
            var execContext = app.Services.GetRequiredService<DistributedApplicationExecutionContext>();

            var subscriber = new TestableBuildQueueSubscriber(notificationService, loggerService);
            await subscriber.SubscribeAsync(eventing, execContext, CancellationToken.None);
            return subscriber;
        }

        public async ValueTask DisposeAsync()
        {
            await App.DisposeAsync();
        }
    }
}

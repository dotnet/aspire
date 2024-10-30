// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Hosting.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Tests.Health;

public class ResourceHealthCheckServiceTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task ResourcesWithoutHealthCheck_HealthyWhenRunning()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        var resource = builder.AddResource(new ParentResource("resource"));

        await using var app = await builder.BuildAsync().DefaultTimeout();
        var rns = app.Services.GetRequiredService<ResourceNotificationService>();

        await app.StartAsync().DefaultTimeout();

        await rns.PublishUpdateAsync(resource.Resource, s => s with
        {
            State = new ResourceStateSnapshot(KnownResourceStates.Starting, null)
        }).DefaultTimeout();

        var startingEvent = await rns.WaitForResourceAsync("resource", e => e.Snapshot.State?.Text == KnownResourceStates.Starting).DefaultTimeout();
        Assert.Null(startingEvent.Snapshot.HealthStatus);

        await rns.PublishUpdateAsync(resource.Resource, s => s with
        {
            State = new ResourceStateSnapshot(KnownResourceStates.Running, null)
        });

        var healthyEvent = await rns.WaitForResourceHealthyAsync("resource").DefaultTimeout();
        Assert.Equal(HealthStatus.Healthy, healthyEvent.Snapshot.HealthStatus);

        await app.StopAsync().DefaultTimeout();
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/6385")]
    public async Task ResourcesWithHealthCheck_NotHealthyUntilCheckSucceeds()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        builder.Services.AddHealthChecks().AddCheck("healthcheck_a",  () => HealthCheckResult.Healthy());

        var resource = builder.AddResource(new ParentResource("resource"))
            .WithHealthCheck("healthcheck_a");

        await using var app = await builder.BuildAsync().DefaultTimeout();
        var rns = app.Services.GetRequiredService<ResourceNotificationService>();

        await app.StartAsync().DefaultTimeout();

        await rns.PublishUpdateAsync(resource.Resource, s => s with
        {
            State = new ResourceStateSnapshot(KnownResourceStates.Starting, null)
        }).DefaultTimeout();

        var startingEvent = await rns.WaitForResourceAsync("resource", e => e.Snapshot.State?.Text == KnownResourceStates.Starting).DefaultTimeout();
        Assert.Null(startingEvent.Snapshot.HealthStatus);

        await rns.PublishUpdateAsync(resource.Resource, s => s with
        {
            State = new ResourceStateSnapshot(KnownResourceStates.Running, null)
        });

        var runningEvent = await rns.WaitForResourceAsync("resource", e => e.Snapshot.State?.Text == KnownResourceStates.Running).DefaultTimeout();

        Assert.Equal(HealthStatus.Unhealthy, runningEvent.Snapshot.HealthStatus);
        await rns.WaitForResourceHealthyAsync("resource").DefaultTimeout();

        await app.StopAsync().DefaultTimeout();
    }

    [Fact]
    public async Task HealthCheckIntervalSlowsAfterSteadyHealthyState()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        AutoResetEvent? are = null;

        builder.Services.AddHealthChecks().AddCheck("resource_check", () =>
        {
            are?.Set();

            return HealthCheckResult.Healthy();
        });

        var resource = builder.AddResource(new ParentResource("resource"))
                              .WithHealthCheck("resource_check");

        using var app = builder.Build();
        var rns = app.Services.GetRequiredService<ResourceNotificationService>();

        var abortTokenSource = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.LongTimeoutDuration);

        await app.StartAsync(abortTokenSource.Token).DefaultTimeout();

        await rns.PublishUpdateAsync(resource.Resource, s => s with
        {
            State = KnownResourceStates.Running
        }).DefaultTimeout();
        await rns.WaitForResourceHealthyAsync(resource.Resource.Name, abortTokenSource.Token).DefaultTimeout();

        are = new AutoResetEvent(false);

        // Allow one event to through since it could be half way through.
        are.WaitOne();

        var stopwatch = Stopwatch.StartNew();
        are.WaitOne();
        stopwatch.Stop();

        // Delay is 30 seconds but we allow for a (ridiculous) 10 second margin of error.
        Assert.True(stopwatch.ElapsedMilliseconds > 20000);

        await app.StopAsync(abortTokenSource.Token).DefaultTimeout();
    }

    [Fact]
    public async Task HealthCheckIntervalDoesNotSlowBeforeSteadyHealthyState()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        AutoResetEvent? are = null;

        builder.Services.AddHealthChecks().AddCheck("resource_check", () =>
        {
            are?.Set();

            return HealthCheckResult.Unhealthy();
        });

        var resource = builder.AddResource(new ParentResource("resource"))
                              .WithHealthCheck("resource_check");

        using var app = builder.Build();
        var rns = app.Services.GetRequiredService<ResourceNotificationService>();

        var abortTokenSource = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.LongTimeoutDuration);

        await app.StartAsync(abortTokenSource.Token).DefaultTimeout();

        await rns.PublishUpdateAsync(resource.Resource, s => s with
        {
            State = KnownResourceStates.Running
        }).DefaultTimeout();
        await rns.WaitForResourceAsync(resource.Resource.Name, KnownResourceStates.Running, abortTokenSource.Token).DefaultTimeout();

        are = new AutoResetEvent(false);

        // Allow one event to through since it could be half way through.
        are.WaitOne();

        var stopwatch = Stopwatch.StartNew();
        are.WaitOne();
        stopwatch.Stop();

        // When not in a healthy state the delay should be ~3 seconds but
        // we'll check for 10 seconds to make sure we haven't got down
        // the 30 second slow path.
        Assert.True(stopwatch.ElapsedMilliseconds < 10000);

        await app.StopAsync(abortTokenSource.Token).DefaultTimeout();
    }

    [Fact]
    public async Task ResourcesWithoutHealthCheckAnnotationsGetReadyEventFired()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        var resource = builder.AddResource(new ParentResource("resource"));

        var blockAssert = new TaskCompletionSource<ResourceReadyEvent>();
        builder.Eventing.Subscribe<ResourceReadyEvent>(resource.Resource, (@event, ct) =>
        {
            blockAssert.SetResult(@event);
            return Task.CompletedTask;
        });

        using var app = builder.Build();
        var rns = app.Services.GetRequiredService<ResourceNotificationService>();
        var pendingStart = app.StartAsync();

        await rns.PublishUpdateAsync(resource.Resource, s => s with
        {
            State = new ResourceStateSnapshot(KnownResourceStates.Running, null)
        }).DefaultTimeout();

        var @event = await blockAssert.Task.DefaultTimeout();
        Assert.Equal(resource.Resource, @event.Resource);

        await pendingStart.DefaultTimeout();
        await app.StopAsync().DefaultTimeout();
    }

    [Fact]
    public async Task PoorlyImplementedHealthChecksDontCauseMonitoringLoopToCrashout()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var hitCount = 0;
        builder.Services.AddHealthChecks().AddCheck("resource_check", (check) =>
        {
            Interlocked.Increment(ref hitCount);
            throw new InvalidOperationException("Random failure instead of result!");
        });

        var resource = builder.AddResource(new ParentResource("resource"))
                              .WithHealthCheck("resource_check");

        using var app = builder.Build();
        var rns = app.Services.GetRequiredService<ResourceNotificationService>();

        var pendingStart = app.StartAsync().DefaultTimeout();

        await rns.PublishUpdateAsync(resource.Resource, s => s with
        {
            State = new ResourceStateSnapshot(KnownResourceStates.Running, null)
        }).DefaultTimeout();

        while (!pendingStart.IsCanceled)
        {
            if (hitCount > 2)
            {
                break;
            }
            await Task.Delay(100);
        }

        await pendingStart; // already has a timeout
        await app.StopAsync().DefaultTimeout();
    }

    [Fact]
    public async Task ResourceHealthCheckServiceDoesNotRunHealthChecksUnlessResourceIsRunning()
    {
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

        // The custom resource we are using for our test.
        var hitCount = 0;
        var checkStatus = HealthCheckResult.Unhealthy();
        builder.Services.AddHealthChecks().AddCheck("parent_test", () =>
        {
            Interlocked.Increment(ref hitCount);
            return checkStatus;
        });

        var parent = builder.AddResource(new ParentResource("parent"))
                            .WithHealthCheck("parent_test");

        // Handle ResourceReadyEvent and use it to control when we drop through to do our assert
        // on the health test being executed.
        var resourceReadyEventFired = new TaskCompletionSource<ResourceReadyEvent>();
        builder.Eventing.Subscribe<ResourceReadyEvent>(parent.Resource, (@event, ct) =>
        {
            resourceReadyEventFired.SetResult(@event);
            return Task.CompletedTask;
        });

        using var app = builder.Build();
        var pendingStart = app.StartAsync().DefaultTimeout(TestConstants.LongTimeoutDuration);
        var rns = app.Services.GetRequiredService<ResourceNotificationService>();

        // Verify that the health check does not get run before we move the resource into the
        // the running state. There isn't a great way to do this using a completition source
        // so I'm just going to spin for up to ten seconds to be sure that no local perf
        // issues lead to a false pass here.
        var giveUpAfter = DateTime.Now.AddSeconds(10);
        while (!pendingStart.IsCanceled)
        {
            Assert.Equal(0, hitCount);
            await Task.Delay(100);

            if (DateTime.Now > giveUpAfter)
            {
                break;
            }
        }

        await rns.PublishUpdateAsync(parent.Resource, s => s with
        {
            State = new ResourceStateSnapshot(KnownResourceStates.Running, null)
        }).DefaultTimeout();

        // Wait for the ResourceReadyEvent
        checkStatus = HealthCheckResult.Healthy();
        await Task.WhenAll([resourceReadyEventFired.Task]).DefaultTimeout();
        Assert.True(hitCount > 0);

        await pendingStart; // already has a timeout
        await app.StopAsync().DefaultTimeout();
    }

    [Fact]
    public async Task ResourceHealthCheckServiceOnlyRaisesResourceReadyOnce()
    {
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

        // The custom resource we are using for our test.
        var healthCheckHits = 0;
        builder.Services.AddHealthChecks().AddCheck("parent_test", () =>
        {
            Interlocked.Increment(ref healthCheckHits);
            return HealthCheckResult.Healthy();
        });

        var parent = builder.AddResource(new ParentResource("parent"))
                            .WithHealthCheck("parent_test");

        // Handle ResourceReadyEvent and use it to control when we drop through to do our assert
        // on the health test being executed.
        var eventHits = 0;
        var resourceReadyEventFired = new TaskCompletionSource<ResourceReadyEvent>();
        builder.Eventing.Subscribe<ResourceReadyEvent>(parent.Resource, (@event, ct) =>
        {
            Interlocked.Increment(ref eventHits);
            return Task.CompletedTask;
        });

        using var app = builder.Build();
        var pendingStart = app.StartAsync().DefaultTimeout();
        var rns = app.Services.GetRequiredService<ResourceNotificationService>();

        // Get the custom resource to a running state.
        await rns.PublishUpdateAsync(parent.Resource, s => s with
        {
            State = new ResourceStateSnapshot(KnownResourceStates.Running, null)
        }).DefaultTimeout();

        while (!pendingStart.IsCanceled)
        {
            // We wait for this hit count to reach 3
            // because it means that we've had a chance
            // to fire the ready event twice.
            if (healthCheckHits > 2)
            {
                break;
            }
            await Task.Delay(100);
        }

        Assert.Equal(1, eventHits);

        await pendingStart; // already has a timeout
        await app.StopAsync().DefaultTimeout();
    }

    [Fact]
    public async Task VerifyThatChildResourceWillBecomeHealthyOnceParentBecomesHealthy()
    {
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

        builder.Services.AddHealthChecks().AddCheck("parent_test", () => HealthCheckResult.Healthy());
        var parent = builder.AddResource(new ParentResource("parent"))
                            .WithHealthCheck("parent_test");

        var parentReady = new TaskCompletionSource<ResourceReadyEvent>();
        builder.Eventing.Subscribe<ResourceReadyEvent>(parent.Resource, (@event, ct) =>
        {
            parentReady.SetResult(@event);
            return Task.CompletedTask;
        });

        var child = builder.AddResource(new ChildResource("child", parent.Resource));

        var childReady = new TaskCompletionSource<ResourceReadyEvent>();
        builder.Eventing.Subscribe<ResourceReadyEvent>(child.Resource, (@event, ct) =>
        {
            childReady.SetResult(@event);
            return Task.CompletedTask;
        });

        using var app = builder.Build();
        var pendingStart = app.StartAsync();
        var rns = app.Services.GetRequiredService<ResourceNotificationService>();

        // Get the custom resource to a running state.
        await rns.PublishUpdateAsync(parent.Resource, s => s with
        {
            State = new ResourceStateSnapshot(KnownResourceStates.Running, null)
        }).DefaultTimeout();

        // ... only need to do this with custom resources, for containers this
        // is handled by app executor. When we get operators we won't need to do
        // this at all.
        await rns.PublishUpdateAsync(child.Resource, s => s with
        {
            State = new ResourceStateSnapshot(KnownResourceStates.Running, null)
        }).DefaultTimeout();

        var parentReadyEvent = await parentReady.Task.DefaultTimeout();
        Assert.Equal(parentReadyEvent.Resource, parent.Resource);

        var childReadyEvent = await childReady.Task.DefaultTimeout();
        Assert.Equal(childReadyEvent.Resource, child.Resource);

        await pendingStart; // already has a timeout
        await app.StopAsync().DefaultTimeout();
    }

    private sealed class ParentResource(string name) : Resource(name)
    {
    }

    private sealed class ChildResource(string name, ParentResource parent) : Resource(name), IResourceWithParent<ParentResource>
    {
        public ParentResource Parent => parent;
    }
}

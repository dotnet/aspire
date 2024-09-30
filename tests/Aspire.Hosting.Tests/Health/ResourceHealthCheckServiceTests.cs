// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Tests.Health;

public class ResourceHealthCheckServiceTests(ITestOutputHelper testOutputHelper)
{
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
        });

        var @event = await blockAssert.Task;
        Assert.Equal(resource.Resource, @event.Resource);

        await pendingStart;
        await app.StopAsync();
    }

    [Fact]
    public async Task PoorlyImplementedHealthChecksDontCauseMonitoringLoopToCrashout()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var hitCount = 0;
        builder.Services.AddHealthChecks().AddCheck("resource_check", (check) =>
        {
            hitCount++;
            throw new InvalidOperationException("Random failure instead of result!");
        });

        var resource = builder.AddResource(new ParentResource("resource"))
                              .WithHealthCheck("resource_check");

        using var app = builder.Build();
        var rns = app.Services.GetRequiredService<ResourceNotificationService>();

        var abortTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        var pendingStart = app.StartAsync(abortTokenSource.Token);

        await rns.PublishUpdateAsync(resource.Resource, s => s with
        {
            State = new ResourceStateSnapshot(KnownResourceStates.Running, null)
        });

        while (!abortTokenSource.Token.IsCancellationRequested)
        {
            if (hitCount > 2)
            {
                break;
            }
            await Task.Delay(100);
        }

        await pendingStart;
        await app.StopAsync();
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
            hitCount++;
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

        // Make sure that this test doesn't run longer than a minute (should finish in a second or two)
        // but allow enough time to debug things without having to adjust timings.
        var abortTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(120));

        using var app = builder.Build();
        var pendingStart = app.StartAsync(abortTokenSource.Token);
        var rns = app.Services.GetRequiredService<ResourceNotificationService>();

        // Verify that the health check does not get run before we move the resource into the
        // the running state. There isn't a great way to do this using a completition source
        // so I'm just going to spin for up to ten seconds to be sure that no local perf
        // issues lead to a false pass here.
        var giveUpAfter = DateTime.Now.AddSeconds(10);
        while (!abortTokenSource.Token.IsCancellationRequested)
        {
            Assert.Equal(0, hitCount);
            await Task.Delay(100);

            if (DateTime.Now > giveUpAfter)
            {
                break;
            }
        }
        Assert.False(abortTokenSource.IsCancellationRequested);

        await rns.PublishUpdateAsync(parent.Resource, s => s with
        {
            State = new ResourceStateSnapshot(KnownResourceStates.Running, null)
        });

        // Wait for the ResourceReadyEvent
        checkStatus = HealthCheckResult.Healthy();
        await Task.WhenAll([resourceReadyEventFired.Task]);
        Assert.True(hitCount > 0);

        await pendingStart;
        await app.StopAsync();
    }

    [Fact]
    public async Task ResourceHealthCheckServiceOnlyRaisesResourceReadyOnce()
    {
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

        // The custom resource we are using for our test.
        var healthCheckHits = 0;
        builder.Services.AddHealthChecks().AddCheck("parent_test", () =>
        {
            healthCheckHits++;
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
            eventHits++;
            return Task.CompletedTask;
        });

        // Make sure that this test doesn't run longer than a minute (should finish in a second or two)
        // but allow enough time to debug things without having to adjust timings.
        var abortTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(120));

        using var app = builder.Build();
        var pendingStart = app.StartAsync(abortTokenSource.Token);
        var rns = app.Services.GetRequiredService<ResourceNotificationService>();

        // Get the custom resource to a running state.
        await rns.PublishUpdateAsync(parent.Resource, s => s with
        {
            State = new ResourceStateSnapshot(KnownResourceStates.Running, null)
        });

        while (!abortTokenSource.Token.IsCancellationRequested)
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

        Assert.False(abortTokenSource.IsCancellationRequested);
        Assert.Equal(1, eventHits);

        await pendingStart;
        await app.StopAsync();
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
        });

        // ... only need to do this with custom resources, for containers this
        // is handled by app executor. When we get operators we won't need to do
        // this at all.
        await rns.PublishUpdateAsync(child.Resource, s => s with
        {
            State = new ResourceStateSnapshot(KnownResourceStates.Running, null)
        });

        var parentReadyEvent = await parentReady.Task;
        Assert.Equal(parentReadyEvent.Resource, parent.Resource);

        var childReadyEvent = await childReady.Task;
        Assert.Equal(childReadyEvent.Resource, child.Resource);

        await pendingStart;
        await app.StopAsync();
    }

    private sealed class ParentResource(string name) : Resource(name)
    {
    }

    private sealed class ChildResource(string name, ParentResource parent) : Resource(name), IResourceWithParent<ParentResource>
    {
        public ParentResource Parent => parent;
    }
}

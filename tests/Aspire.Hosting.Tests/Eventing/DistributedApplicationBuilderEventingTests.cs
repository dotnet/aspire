// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests.Eventing;

public class DistributedApplicationBuilderEventingTests
{
    [Fact]
    public void CanResolveIDistributedApplicationEventingFromDI()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        using var app = builder.Build();
        var eventing = app.Services.GetRequiredService<IDistributedApplicationEventing>();
        Assert.Equal(builder.Eventing, eventing);
    }

    [Fact]
    [RequiresDocker]
    public async Task ResourceEventsForContainersFireForSpecificResources()
    {
        var beforeResourceStartedEvent = new ManualResetEventSlim();
        var afterResourceCreatedEvent = new ManualResetEventSlim();

        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddRedis("redis");

        builder.Eventing.Subscribe<BeforeResourceStartedEvent>(redis.Resource, (e, ct) =>
        {
            Assert.NotNull(e.Services);
            Assert.NotNull(e.Resource);
            beforeResourceStartedEvent.Set();
            return Task.CompletedTask;
        });

        builder.Eventing.Subscribe<AfterResourceStartedEvent>(redis.Resource, (e, ct) =>
        {
            Assert.NotNull(e.Services);
            Assert.NotNull(e.Resource);
            afterResourceCreatedEvent.Set();
            return Task.CompletedTask;
        });

        using var app = builder.Build();
        await app.StartAsync();

        var allFired = ManualResetEvent.WaitAll(
            [beforeResourceStartedEvent.WaitHandle, afterResourceCreatedEvent.WaitHandle],
            TimeSpan.FromSeconds(10)
            );

        Assert.True(allFired);
        await app.StopAsync();
    }

    [Fact]
    [RequiresDocker]
    public async Task ResourceEventsForContainersFireForAllResources()
    {
        var countdownEvent = new CountdownEvent(4);

        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddRedis("redis1");
        builder.AddRedis("redis2");

        // Should be called twice ... once for each event.
        builder.Eventing.Subscribe<BeforeResourceStartedEvent>((e, ct) =>
        {
            Assert.NotNull(e.Services);
            Assert.NotNull(e.Resource);
            countdownEvent.Signal();
            return Task.CompletedTask;
        });

        // Should be called twice ... once for each event.
        builder.Eventing.Subscribe<AfterResourceStartedEvent>((e, ct) =>
        {
            Assert.NotNull(e.Services);
            Assert.NotNull(e.Resource);
            countdownEvent.Signal();
            return Task.CompletedTask;
        });

        using var app = builder.Build();
        await app.StartAsync();

        var fired = countdownEvent.Wait(TimeSpan.FromSeconds(10));

        Assert.True(fired);
        await app.StopAsync();
    }

    [Fact]
    public async Task LifeycleHookAnalogousEventsFire()
    {
        var beforeStartEventFired = new ManualResetEventSlim();
        var afterEndpointsAllocatedEventFired = new ManualResetEventSlim();
        var afterResourcesCreatedEventFired = new ManualResetEventSlim();

        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Eventing.Subscribe<BeforeStartEvent>((e, ct) =>
        {
            Assert.NotNull(e.Services);
            Assert.NotNull(e.Model);
            beforeStartEventFired.Set();
            return Task.CompletedTask;
        });
        builder.Eventing.Subscribe<AfterEndpointsAllocatedEvent>((e, ct) =>
        {
            Assert.NotNull(e.Services);
            Assert.NotNull(e.Model);
            afterEndpointsAllocatedEventFired.Set();
            return Task.CompletedTask;
        });
        builder.Eventing.Subscribe<AfterResourcesCreatedEvent>((e, ct) =>
        {
            Assert.NotNull(e.Services);
            Assert.NotNull(e.Model);
            afterResourcesCreatedEventFired.Set();
            return Task.CompletedTask;
        });

        using var app = builder.Build();
        await app.StartAsync();

        var allFired = ManualResetEvent.WaitAll(
            [beforeStartEventFired.WaitHandle, afterEndpointsAllocatedEventFired.WaitHandle, afterResourcesCreatedEventFired.WaitHandle],
            TimeSpan.FromSeconds(10)
            );

        Assert.True(allFired);
        await app.StopAsync();
    }
}

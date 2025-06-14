// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestUtilities;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests.Eventing;

public class DistributedApplicationBuilderEventingTests
{
    [Fact]
    public async Task EventsCanBePublishedBlockSequential()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var hitCount = 0;
        var blockAssertionTcs = new TaskCompletionSource();
        var blockFirstSubscriptionTcs = new TaskCompletionSource();

        builder.Eventing.Subscribe<DummyEvent>(async (@event, ct) =>
        {
            blockAssertionTcs.SetResult();
            Interlocked.Increment(ref hitCount);
            await blockFirstSubscriptionTcs.Task;
        });

        builder.Eventing.Subscribe<DummyEvent>((@event, ct) =>
        {
            Interlocked.Increment(ref hitCount);
            return Task.CompletedTask;
        });

        var pendingPublish = builder.Eventing.PublishAsync(new DummyEvent(), EventDispatchBehavior.BlockingSequential);

        await blockAssertionTcs.Task.DefaultTimeout();
        Assert.Equal(1, hitCount);
        blockFirstSubscriptionTcs.SetResult();
        await pendingPublish.DefaultTimeout();
        Assert.Equal(2, hitCount);
    }

    [Fact]
    public async Task EventsCanBePublishedBlockConcurrent()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var hitCount = 0;
        var blockAssertionSub1 = new TaskCompletionSource();
        var blockAssertionSub2 = new TaskCompletionSource();
        var blockSubscriptionCompletion = new TaskCompletionSource();

        builder.Eventing.Subscribe<DummyEvent>(async (@event, ct) =>
        {
            Interlocked.Increment(ref hitCount);
            blockAssertionSub1.SetResult();
            await blockSubscriptionCompletion.Task;
        });

        builder.Eventing.Subscribe<DummyEvent>(async (@event, ct) =>
        {
            Interlocked.Increment(ref hitCount);
            blockAssertionSub2.SetResult();
            await blockSubscriptionCompletion.Task;
        });

        var pendingPublish = builder.Eventing.PublishAsync(new DummyEvent(), EventDispatchBehavior.BlockingConcurrent);

        await Task.WhenAll(blockAssertionSub1.Task, blockAssertionSub2.Task).DefaultTimeout();
        Assert.Equal(2, hitCount);
        blockSubscriptionCompletion.SetResult();
        await pendingPublish.DefaultTimeout();
    }

    [Fact]
    public async Task EventsCanBePublishedNonBlockingConcurrent()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var hitCount = 0;
        var blockAssertionSub1 = new TaskCompletionSource();
        var blockAssertionSub2 = new TaskCompletionSource();
        var blockSubscriptionExecution = new TaskCompletionSource();

        builder.Eventing.Subscribe<DummyEvent>(async (@event, ct) =>
        {
            await blockSubscriptionExecution.Task;
            Interlocked.Increment(ref hitCount);
            blockAssertionSub1.SetResult();
        });

        builder.Eventing.Subscribe<DummyEvent>(async (@event, ct) =>
        {
            await blockSubscriptionExecution.Task;
            Interlocked.Increment(ref hitCount);
            blockAssertionSub2.SetResult();
        });

        await builder.Eventing.PublishAsync(new DummyEvent(), EventDispatchBehavior.NonBlockingConcurrent).DefaultTimeout();

        blockSubscriptionExecution.SetResult();
        await Task.WhenAll(blockAssertionSub1.Task, blockAssertionSub2.Task).DefaultTimeout();
        Assert.Equal(2, hitCount);
    }

    [Fact]
    public async Task EventsCanBePublishedNonBlockingSequential()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var hitCount = 0;
        var blockEventSub1 = new TaskCompletionSource();
        var blockEventSub2 = new TaskCompletionSource();
        var blockAssert1 = new TaskCompletionSource();
        var blockAssert2 = new TaskCompletionSource();
        var blockAssert3 = new TaskCompletionSource();

        builder.Eventing.Subscribe<DummyEvent>(async (@event, ct) =>
        {
            blockAssert1.SetResult();
            await blockEventSub1.Task;
            Interlocked.Increment(ref hitCount);
            blockAssert2.SetResult();
            await blockEventSub2.Task;
        });

        builder.Eventing.Subscribe<DummyEvent>((@event, ct) =>
        {
            Interlocked.Increment(ref hitCount);
            blockAssert3.SetResult();
            return Task.CompletedTask;
        });

        await builder.Eventing.PublishAsync(new DummyEvent(), EventDispatchBehavior.NonBlockingSequential).DefaultTimeout();

        // Make sure that we are zero when we enter
        // the first handler.
        await blockAssert1.Task.DefaultTimeout();
        Assert.Equal(0, hitCount);

        // Give the second handler a chance to run,
        // it shouldn't and hit count should
        // still be zero.
        await Task.Delay(1000);
        Assert.Equal(0, hitCount);

        // After we unblock the first sub
        // we update the hit count and verify
        // that it has moved to 1.
        blockEventSub1.SetResult();
        await blockAssert2.Task.DefaultTimeout();
        Assert.Equal(1, hitCount);
        blockEventSub2.SetResult();

        // Now block until the second handler has
        // run and make sure it has incremented.
        await blockAssert3.Task.DefaultTimeout();
        Assert.Equal(2, hitCount);
    }

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
        var beforeResourceStartedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddRedis("redis");

        builder.Eventing.Subscribe<BeforeResourceStartedEvent>(redis.Resource, (e, ct) =>
        {
            Assert.NotNull(e.Services);
            Assert.NotNull(e.Resource);
            beforeResourceStartedTcs.TrySetResult();
            return Task.CompletedTask;
        });

        using var app = builder.Build();
        await app.StartAsync().DefaultTimeout(TestConstants.DefaultOrchestratorTestTimeout);

        await beforeResourceStartedTcs.Task.DefaultTimeout();

        await app.StopAsync().DefaultTimeout(TestConstants.DefaultOrchestratorTestLongTimeout);
    }

    [Fact]
    [RequiresDocker]
    public async Task ResourceEventsForContainersFireForAllResources()
    {
        var countdownEvent = new CountdownEvent(2);

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

        using var app = builder.Build();
        await app.StartAsync().DefaultTimeout(TestConstants.DefaultOrchestratorTestTimeout);

        var fired = countdownEvent.Wait(TimeSpan.FromSeconds(10));

        Assert.True(fired);
        await app.StopAsync().DefaultTimeout(TestConstants.DefaultOrchestratorTestLongTimeout);
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
#pragma warning disable CS0618 // Type or member is obsolete
        builder.Eventing.Subscribe<AfterEndpointsAllocatedEvent>((e, ct) =>
        {
            Assert.NotNull(e.Services);
            Assert.NotNull(e.Model);
            afterEndpointsAllocatedEventFired.Set();
            return Task.CompletedTask;
        });
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
        builder.Eventing.Subscribe<AfterResourcesCreatedEvent>((e, ct) =>
        {
            Assert.NotNull(e.Services);
            Assert.NotNull(e.Model);
            afterResourcesCreatedEventFired.Set();
            return Task.CompletedTask;
        });
#pragma warning restore CS0618 // Type or member is obsolete

        using var app = builder.Build();
        await app.StartAsync();

        var allFired = ManualResetEvent.WaitAll(
            [beforeStartEventFired.WaitHandle, afterEndpointsAllocatedEventFired.WaitHandle, afterResourcesCreatedEventFired.WaitHandle],
            TimeSpan.FromSeconds(10)
            );

        Assert.True(allFired);
        await app.StopAsync();
    }

    public class DummyEvent : IDistributedApplicationEvent
    {
    }
}

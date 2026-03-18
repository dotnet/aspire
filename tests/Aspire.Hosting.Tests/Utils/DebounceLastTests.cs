// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Microsoft.Extensions.Time.Testing;

namespace Aspire.Hosting.Tests.Utils;

public class DebounceLastTests
{
    private static readonly TimeSpan s_delay = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan s_maxDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan s_testTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task RunnerIsCalledAfterDelay()
    {
        var clock = new FakeTimeProvider();
        var runnerCalled = new TaskCompletionSource<bool>();

        var debounce = new DebounceLast<int>(
            () =>
            {
                runnerCalled.SetResult(true);
                return Task.FromResult(42);
            },
            s_delay,
            s_maxDelay,
            clock);

        var resultTask = debounce.RunAsync();

        // Runner should not have been called yet.
        Assert.False(runnerCalled.Task.IsCompleted);

        clock.Advance(s_delay);

        var result = await resultTask.WaitAsync(s_testTimeout);
        Assert.Equal(42, result);
        Assert.True(await runnerCalled.Task);
    }

    [Fact]
    public async Task RunnerExceptionIsPropagatedToCallers()
    {
        var clock = new FakeTimeProvider();

        var debounce = new DebounceLast<int>(
            () => throw new InvalidOperationException("boom"),
            s_delay,
            s_maxDelay,
            clock);

        var t1 = debounce.RunAsync();
        var t2 = debounce.RunAsync();

        clock.Advance(s_delay);

        var ex1 = await Assert.ThrowsAsync<InvalidOperationException>(() => t1.WaitAsync(s_testTimeout));
        var ex2 = await Assert.ThrowsAsync<InvalidOperationException>(() => t2.WaitAsync(s_testTimeout));

        Assert.Equal("boom", ex1.Message);
        Assert.Same(ex1, ex2);
    }

    [Fact]
    public async Task RapidCallsAreDebounced()
    {
        var clock = new FakeTimeProvider();
        var callCount = 0;

        var debounce = new DebounceLast<int>(
            () =>
            {
                Interlocked.Increment(ref callCount);
                return Task.FromResult(99);
            },
            s_delay,
            s_maxDelay,
            clock);

        // Fire several calls in rapid succession, each extending the debounce window.
        var tasks = new List<Task<int>>();
        var smallDelay = s_delay / 10.0;

        for (var i = 0; i < 5; i++)
        {
            tasks.Add(debounce.RunAsync());
            clock.Advance(smallDelay);
        }

        // All tasks share the same debounce cycle; advance past the final fire-at.
        clock.Advance(s_delay);

        var results = await Task.WhenAll(tasks).WaitAsync(s_testTimeout);

        // Runner should be called exactly once.
        Assert.Equal(1, callCount);

        // All callers get the same result.
        Assert.All(results, r => Assert.Equal(99, r));
    }

    [Fact]
    public async Task MaxDelayIsRespectedWhenCallsKeepArriving()
    {
        var clock = new FakeTimeProvider();
        var runnerInvoked = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        var debounce = new DebounceLast<int>(
            () =>
            {
                runnerInvoked.TrySetResult(42);
                return Task.FromResult(42);
            },
            s_delay,
            s_maxDelay,
            clock);

        var firstResult = debounce.RunAsync();

        // Start a cycle and keep calling RunAsync at intervals shorter than the delay.
        // This continuously pushes _fireAt forward, but _threshold caps it.
        // Do enough calls that _threshold is exceeded.
        var oneThirdDelay = TimeSpan.FromTicks(s_delay.Ticks / 3);
        var invocations = (s_maxDelay.Ticks / oneThirdDelay.Ticks) * 2;

        for (var i = 0; i < invocations; i++)
        {
            clock.Advance(oneThirdDelay);
            await Task.Yield(); // Let the background task process the timer.
            _ = debounce.RunAsync();
        }

        Assert.True(runnerInvoked.Task.IsCompleted);
        Assert.True(firstResult.IsCompleted);
        Assert.Equal(42, await firstResult);
    }

    [Fact]
    public async Task InfrequentCallsResultInMultipleRunnerInvocations()
    {
        var clock = new FakeTimeProvider();
        var callCount = 0;

        var debounce = new DebounceLast<int>(
            () =>
            {
                var count = Interlocked.Increment(ref callCount);
                return Task.FromResult(count * 10);
            },
            s_delay,
            s_maxDelay,
            clock);

        // First call.
        var t1 = debounce.RunAsync();
        clock.Advance(s_delay);
        var r1 = await t1.WaitAsync(s_testTimeout);

        Assert.Equal(10, r1);
        Assert.Equal(1, callCount);

        // Second call, well after the first completed.
        var t2 = debounce.RunAsync();
        clock.Advance(s_delay);
        var r2 = await t2.WaitAsync(s_testTimeout);

        Assert.Equal(20, r2);
        Assert.Equal(2, callCount);

        // Third call.
        var t3 = debounce.RunAsync();
        clock.Advance(s_delay);
        var r3 = await t3.WaitAsync(s_testTimeout);

        Assert.Equal(30, r3);
        Assert.Equal(3, callCount);
    }

    [Fact]
    public async Task CallsDuringRunnerExecutionStartNewCycle()
    {
        var clock = new FakeTimeProvider();
        var invocationCount = 0;
        var releaseRunner = new TaskCompletionSource();

        var debounce = new DebounceLast<int>(
            async () =>
            {
                var n = Interlocked.Increment(ref invocationCount);
                if (n == 1)
                {
                    // First invocation blocks until we release it.
                    await releaseRunner.Task;
                }
                return n;
            },
            s_delay,
            s_maxDelay,
            clock);

        // First cycle.
        var t1 = debounce.RunAsync();
        clock.Advance(s_delay);

        // Wait until runner is actually executing (first invocation recorded).
        await WaitUntilAsync(() => Volatile.Read(ref invocationCount) >= 1);

        // While the runner is executing, a new call should start a fresh cycle.
        var t2 = debounce.RunAsync();

        // Release the first runner.
        releaseRunner.SetResult();
        var r1 = await t1.WaitAsync(s_testTimeout);
        Assert.Equal(1, r1);

        // Advance time for the second cycle to fire.
        clock.Advance(s_delay);
        var r2 = await t2.WaitAsync(s_testTimeout);
        Assert.Equal(2, r2);

        // Runner was called twice, once per cycle.
        Assert.Equal(2, invocationCount);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var cts = new CancellationTokenSource(s_testTimeout);
        while (!condition())
        {
            await Task.Delay(10, cts.Token);
        }
    }
}

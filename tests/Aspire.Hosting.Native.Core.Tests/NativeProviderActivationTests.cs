// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Native.Client;
using Aspire.Hosting.Native.Core.Tests.TestServices;

namespace Aspire.Hosting.Native.Core.Tests;

public class NativeProviderActivationTests
{
    [Fact]
    public async Task ExitedProviderDisposesProcessAfterDrainingOutput()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Skip("This process fixture uses a Unix shell.");
        }
        await using var host = await NativeProviderProcessTestHost.StartAsync(retainOutput: false);
        var handle = host.Process.SafeHandle;
        var activation = new NativeProviderActivation(host.Process, TimeProvider.System);
        await activation.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.True(handle.IsClosed);
    }

    [Fact]
    public async Task DescendantHeldOutputCannotOutliveProviderShutdownBudget()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Skip("This process fixture uses a Unix shell.");
        }
        await using var host = await NativeProviderProcessTestHost.StartAsync(retainOutput: true);
        var handle = host.Process.SafeHandle;
        var timerCreated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var clock = new AdjustableFakeTimeProvider
        {
            TimerCreated = dueTime =>
            {
                if (dueTime == TimeSpan.FromSeconds(15))
                {
                    timerCreated.TrySetResult();
                }
            }
        };
        var activation = new NativeProviderActivation(host.Process, clock);
        var disposal = activation.DisposeAsync().AsTask();
        await timerCreated.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        clock.Advance(TimeSpan.FromMilliseconds(14999));
        Assert.False(disposal.IsCompleted);
        clock.Advance(TimeSpan.FromMilliseconds(1));
        await Assert.ThrowsAsync<TimeoutException>(() =>
            disposal.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.True(handle.IsClosed);
        Assert.False(host.Descendant!.HasExited);
    }
}

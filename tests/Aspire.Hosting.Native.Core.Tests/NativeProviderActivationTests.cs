// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Native.Client;
using Aspire.Hosting.Native.Core.Tests.TestServices;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Hosting.Native.Core.Tests;

public class NativeProviderActivationTests
{
    [Fact]
    public async Task RepeatedSpawnFailuresDoNotConsumePendingProviderCapacity()
    {
        await using var host = await NativeClientTestHost.StartAsync();
        await using var client = await NativeClient.ConnectAsync(host.Endpoint, host.Token,
            TimeSpan.FromSeconds(15), TimeProvider.System, TestContext.Current.CancellationToken);
        var directory = Directory.CreateTempSubdirectory("aspire-native-provider-failure-");
        try
        {
            var missing = Path.Combine(directory.FullName, "missing-provider");
            for (var index = 0; index < 80; index++)
            {
                await Assert.ThrowsAsync<System.ComponentModel.Win32Exception>(() =>
                    NativeProviderActivation.StartAsync(client, "example.failure", missing, [],
                        TimeProvider.System, NullLogger.Instance, TestContext.Current.CancellationToken));
            }
            var authority = await client.ExpectIntegrationHostAsync("example.next", TestContext.Current.CancellationToken);
            Assert.True(await client.AbandonIntegrationHostRegistrationAsync(authority, TestContext.Current.CancellationToken));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

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

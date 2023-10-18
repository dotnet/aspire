// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.Tests.Helpers;
using Aspire.Hosting.Lifecycle;
using Xunit;

namespace Aspire.Hosting.Tests;

public class DistributedApplicationTests
{
    [Fact]
    public async void RegisteredLifecycleHookIsExecutedWhenRunAsynchronously()
    {
        var exceptionMessage = "Exception from lifecycle hook to prove it ran!";

        var testProgram = new TestProgram([]);
        testProgram.AppBuilder.Services.AddLifecycleHook<CallbackLifecycleHook>((sp) =>
        {
            return new CallbackLifecycleHook((appModel, cancellationToken) =>
            {
                Assert.NotNull(appModel);

                throw new DistributedApplicationException(exceptionMessage);
            });
        });

        var ex = await Assert.ThrowsAsync<DistributedApplicationException>(async () =>
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMinutes(1));
            await testProgram.RunAsync(cts.Token);
        });

        Assert.Equal(exceptionMessage, ex.Message);
    }

    [Fact]
    public async void MultipleRegisteredLifecycleHooksAreExecuted()
    {
        var exceptionMessage = "Exception from lifecycle hook to prove it ran!";

        var signal = (FirstHookExecuted: false, SecondHookExecuted: false);

        var testProgram = new TestProgram([]);

        // Lifecycle hook 1
        testProgram.AppBuilder.Services.AddLifecycleHook<CallbackLifecycleHook>((sp) =>
        {
            return new CallbackLifecycleHook((app, cancellationToken) =>
            {
                signal.FirstHookExecuted = true;
                return Task.CompletedTask;
            });
        });

        // Lifecycle hook 2
        testProgram.AppBuilder.Services.AddLifecycleHook<CallbackLifecycleHook>((sp) =>
        {
            return new CallbackLifecycleHook((app, cancellationToken) =>
            {
                signal.SecondHookExecuted = true;

                // We still want to throw on the second one to block startup.
                throw new DistributedApplicationException(exceptionMessage);
            });
        });

        var ex = await Assert.ThrowsAsync<DistributedApplicationException>(async () =>
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMinutes(1));
            await testProgram.RunAsync(cts.Token);
        });

        Assert.Equal(exceptionMessage, ex.Message);
        Assert.True(signal.FirstHookExecuted);
        Assert.True(signal.SecondHookExecuted);
    }

    [Fact]
    public void RegisteredLifecycleHookIsExecutedWhenRunSynchronously()
    {
        var exceptionMessage = "Exception from lifecycle hook to prove it ran!";

        var testProgram = new TestProgram([]);
        testProgram.AppBuilder.Services.AddLifecycleHook<CallbackLifecycleHook>((sp) =>
        {
            return new CallbackLifecycleHook((appModel, cancellationToken) =>
            {
                Assert.NotNull(appModel);

                throw new DistributedApplicationException(exceptionMessage);
            });
        });

        var ex = Assert.Throws<AggregateException>(testProgram.Run);
        Assert.IsType<DistributedApplicationException>(ex.InnerExceptions.First());
        Assert.Equal(exceptionMessage, ex.InnerExceptions.First().Message);
    }

    [LocalOnlyFact]
    public async void TestProjectStartsAndStopsCleanly()
    {
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMinutes(1));

        var testProgram = new TestProgram([]);
        var pendingRun = testProgram.RunAsync(cts.Token);

        // Make sure each service is running
        await testProgram.ServiceABuilder.HttpGetPidAsync("http", cts.Token);
        await testProgram.ServiceBBuilder.HttpGetPidAsync("http", cts.Token);
        await testProgram.ServiceCBuilder.HttpGetPidAsync("http", cts.Token);

        // Shut it all down.
        cts.Cancel();
        await pendingRun;
    }

    [LocalOnlyFact]
    public async void TestServicesWithMultipleReplicas()
    {
        // Start up the test project.
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMinutes(1));

        var replicaCount = 3;

        var testProgram = new TestProgram([]);
        testProgram.ServiceBBuilder.WithReplicas(replicaCount);

        var pendingRun = testProgram.RunAsync(cts.Token);

        // Make sure services A and C are running
        await testProgram.ServiceABuilder.HttpGetPidAsync("http", cts.Token);
        await testProgram.ServiceCBuilder.HttpGetPidAsync("http", cts.Token);

        // We should get 3 distinct PIDs from service B
        Dictionary<int, bool> pids = new();
        while (true)
        {
            var pid = await testProgram.ServiceBBuilder.HttpGetPidAsync("http", cts.Token);
            if (!string.IsNullOrEmpty(pid))
            {
                pids[int.Parse(pid, CultureInfo.InvariantCulture)] = true;
                if (pids.Count == replicaCount)
                {
                    break; // Success! We heard from all 3 replicas.
                }
            }
            await Task.Delay(100, cts.Token);
        }

        // Shut it all down.
        cts.Cancel();
        await pendingRun;
    }
}

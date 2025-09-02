// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Cli;
using Aspire.Hosting.Utils;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;

namespace Aspire.Cli.Tests;

public class CliOrphanDetectorTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task CliOrphanDetectorCompletesWhenNoPidEnvironmentVariablePresent()
    {
        var configuration = new ConfigurationBuilder().Build();

        var lifetime = new HostLifetimeStub(() => {});
        var detector = new CliOrphanDetector(configuration, lifetime, TimeProvider.System);

        // The detector should complete almost immediately because there is no
        // environment variable present that indicates that it is hitched to
        // .NET Aspire lifetime.
        await detector.StartAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CliOrphanDetectorCallsStopIfEnvironmentVariablePresentAndProcessNotRunning()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "ASPIRE_CLI_PID", "1111" } })    
            .Build();

        var stopSignalChannel = Channel.CreateUnbounded<bool>();
        var lifetime = new HostLifetimeStub(() => stopSignalChannel.Writer.TryWrite(true));

        var detector = new CliOrphanDetector(configuration, lifetime, TimeProvider.System);
        detector.IsProcessRunning = _ => false;

        // The detector should complete almost immediately because there is no
        // environment variable present that indicates that it is hitched to
        // .NET Aspire lifetime.
        await detector.StartAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(await stopSignalChannel.Reader.WaitToReadAsync());
    }

    [Fact]
    public async Task CliOrphanDetectorUsesTimestampDetectionWhenStartTimeProvided()
    {
        var expectedStartTime = DateTime.Now.AddMinutes(-5);
        var expectedStartTimeUnixSeconds = ((DateTimeOffset)expectedStartTime).ToUnixTimeSeconds();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> 
            { 
                { "ASPIRE_CLI_PID", "1111" },
                { "ASPIRE_CLI_STARTED", expectedStartTimeUnixSeconds.ToString() }
            })    
            .Build();

        var stopSignalChannel = Channel.CreateUnbounded<bool>();
        var lifetime = new HostLifetimeStub(() => stopSignalChannel.Writer.TryWrite(true));

        var detector = new CliOrphanDetector(configuration, lifetime, TimeProvider.System);
        detector.IsProcessRunningWithStartTime = (pid, startTime) => false;

        await detector.StartAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(await stopSignalChannel.Reader.WaitToReadAsync());
    }

    [Fact]
    public async Task CliOrphanDetectorFallsBackToPidOnlyWhenStartTimeInvalid()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> 
            { 
                { "ASPIRE_CLI_PID", "1111" },
                { "ASPIRE_CLI_STARTED", "invalid_start_time" }
            })    
            .Build();

        var stopSignalChannel = Channel.CreateUnbounded<bool>();
        var lifetime = new HostLifetimeStub(() => stopSignalChannel.Writer.TryWrite(true));

        var detector = new CliOrphanDetector(configuration, lifetime, TimeProvider.System);
        detector.IsProcessRunning = _ => false;

        await detector.StartAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(await stopSignalChannel.Reader.WaitToReadAsync());
    }

    [Fact]
    public async Task CliOrphanDetectorContinuesRunningWhenProcessAliveWithCorrectStartTime()
    {
        var expectedStartTime = DateTime.Now.AddMinutes(-5);
        var expectedStartTimeUnix = ((DateTimeOffset)expectedStartTime).ToUnixTimeSeconds();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> 
            { 
                { "ASPIRE_CLI_PID", "1111" },
                { "ASPIRE_CLI_STARTED", expectedStartTimeUnix.ToString() }
            })    
            .Build();
        var fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.Now);

        var stopSignalChannel = Channel.CreateUnbounded<bool>();
        var processRunningChannel = Channel.CreateUnbounded<int>();

        var lifetime = new HostLifetimeStub(() => stopSignalChannel.Writer.TryWrite(true));
        var detector = new CliOrphanDetector(configuration, lifetime, fakeTimeProvider);
        
        var processRunningCallCounter = 0;
        detector.IsProcessRunningWithStartTime = (pid, startTime) => {
            Assert.True(processRunningChannel.Writer.TryWrite(++processRunningCallCounter));
            return processRunningCallCounter < 3; // Process dies after 3 checks
        };

        await detector.StartAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));

        // Verify process is checked first time
        Assert.True(await processRunningChannel.Reader.WaitToReadAsync());
        fakeTimeProvider.Advance(TimeSpan.FromSeconds(1));

        // Second check
        Assert.True(await processRunningChannel.Reader.WaitToReadAsync());
        fakeTimeProvider.Advance(TimeSpan.FromSeconds(1));

        // Third check (process dies)
        Assert.True(await processRunningChannel.Reader.WaitToReadAsync());
        Assert.Equal(3, processRunningCallCounter);

        // Should stop the application
        Assert.True(await stopSignalChannel.Reader.WaitToReadAsync());
    }

    [Fact]
    public async Task CliOrphanDetectorStopsWhenProcessHasDifferentStartTime()
    {
        var expectedStartTime = DateTime.Now.AddMinutes(-5);
        var expectedStartTimeUnixString = ((DateTimeOffset)expectedStartTime).ToUnixTimeSeconds().ToString();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> 
            { 
                { "ASPIRE_CLI_PID", "1111" },
                { "ASPIRE_CLI_STARTED", expectedStartTimeUnixString }
            })    
            .Build();

        var stopSignalChannel = Channel.CreateUnbounded<bool>();
        var lifetime = new HostLifetimeStub(() => stopSignalChannel.Writer.TryWrite(true));

        var detector = new CliOrphanDetector(configuration, lifetime, TimeProvider.System);
        
        // Simulate process with different start time (PID reuse scenario)
        detector.IsProcessRunningWithStartTime = (pid, startTime) => {
            // Process exists but has different start time - indicates PID reuse
            return false;
        };

        await detector.StartAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(await stopSignalChannel.Reader.WaitToReadAsync());
    }

    [Fact]
    public async Task CliOrphanDetectorAfterTheProcessWasRunningForAWhileThenStops()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "ASPIRE_CLI_PID", "1111" } })    
            .Build();
        var fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.Now);

        var stopSignalChannel = Channel.CreateUnbounded<bool>();
        var processRunningChannel = Channel.CreateUnbounded<int>();

        var lifetime = new HostLifetimeStub(() => stopSignalChannel.Writer.TryWrite(true));
        var detector = new CliOrphanDetector(configuration, lifetime, fakeTimeProvider);
        
        var processRunningCallCounter = 0;
        detector.IsProcessRunning = pid => {
            Assert.True(processRunningChannel.Writer.TryWrite(++processRunningCallCounter));
            return processRunningCallCounter < 5;
        };

        // The detector should complete after about 5 seconds

        await detector.StartAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(await processRunningChannel.Reader.WaitToReadAsync());
        fakeTimeProvider.Advance(TimeSpan.FromSeconds(1));

        Assert.True(await processRunningChannel.Reader.WaitToReadAsync());
        fakeTimeProvider.Advance(TimeSpan.FromSeconds(1));

        Assert.True(await processRunningChannel.Reader.WaitToReadAsync());
        fakeTimeProvider.Advance(TimeSpan.FromSeconds(1));

        Assert.True(await processRunningChannel.Reader.WaitToReadAsync());
        fakeTimeProvider.Advance(TimeSpan.FromSeconds(1));

        Assert.True(await processRunningChannel.Reader.WaitToReadAsync());
        Assert.Equal(5, processRunningCallCounter);

        Assert.True(await stopSignalChannel.Reader.WaitToReadAsync());
    }

    [Fact]
    public async Task AppHostExitsWhenCliProcessPidDies()
    {
        using var fakeCliProcess = RemoteExecutor.Invoke(
        static () => Thread.Sleep(Timeout.Infinite),
        new RemoteInvokeOptions { CheckExitCode = false }
        );
        
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(testOutputHelper);
        builder.Configuration["ASPIRE_CLI_PID"] = fakeCliProcess.Process.Id.ToString();
        
        var resourcesCreatedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        builder.Eventing.Subscribe<AfterResourcesCreatedEvent>((e, ct) => {
            resourcesCreatedTcs.SetResult();
            return Task.CompletedTask;
        });

        using var app = builder.Build();
        var pendingRun = app.RunAsync();

        // Wait until the apphost is spun up and then kill off the stub
        // process so everything is torn down.
        await resourcesCreatedTcs.Task.WaitAsync(TimeSpan.FromSeconds(60));
        fakeCliProcess.Process.Kill();
        
        await pendingRun.WaitAsync(TimeSpan.FromSeconds(60));
    }
}

file sealed class HostLifetimeStub(Action stopImplementation) : IHostApplicationLifetime
{
    public CancellationToken ApplicationStarted => throw new NotImplementedException();

    public CancellationToken ApplicationStopped => throw new NotImplementedException();

    public CancellationToken ApplicationStopping => throw new NotImplementedException();

    public void StopApplication() => stopImplementation();
}
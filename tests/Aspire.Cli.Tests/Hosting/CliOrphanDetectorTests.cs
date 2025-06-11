// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading.Channels;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Cli;
using Aspire.Hosting.Utils;
using Aspire.TestUtilities;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;
using Xunit;

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

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [QuarantinedTest("https://github.com/dotnet/aspire/issues/7920")]
    public async Task AppHostExitsWhenCliProcessPidDies(int runNumber)
    {
        testOutputHelper.WriteLine($"=== Starting AppHostExitsWhenCliProcessPidDies run #{runNumber} ===");
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            testOutputHelper.WriteLine($"[Run {runNumber}] Creating fake CLI process...");
            using var fakeCliProcess = RemoteExecutor.Invoke(
                static () => Thread.Sleep(Timeout.Infinite),
                new RemoteInvokeOptions { CheckExitCode = false }
                );
            
            var cliProcessId = fakeCliProcess.Process.Id;
            testOutputHelper.WriteLine($"[Run {runNumber}] Created fake CLI process with PID: {cliProcessId}");
            testOutputHelper.WriteLine($"[Run {runNumber}] Process running: {!fakeCliProcess.Process.HasExited}");
            
            testOutputHelper.WriteLine($"[Run {runNumber}] Creating TestDistributedApplicationBuilder...");
            using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(testOutputHelper);
            builder.Configuration["ASPIRE_CLI_PID"] = cliProcessId.ToString();
            
            var resourcesCreatedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            builder.Eventing.Subscribe<AfterResourcesCreatedEvent>((e, ct) => {
                testOutputHelper.WriteLine($"[Run {runNumber}] AfterResourcesCreatedEvent fired at {stopwatch.Elapsed}");
                resourcesCreatedTcs.SetResult();
                return Task.CompletedTask;
            });

            testOutputHelper.WriteLine($"[Run {runNumber}] Building application...");
            using var app = builder.Build();
            
            testOutputHelper.WriteLine($"[Run {runNumber}] Starting application async at {stopwatch.Elapsed}...");
            var pendingRun = app.RunAsync();

            testOutputHelper.WriteLine($"[Run {runNumber}] Waiting for resources to be created (timeout: 10s)...");
            await resourcesCreatedTcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
            testOutputHelper.WriteLine($"[Run {runNumber}] Resources created after {stopwatch.Elapsed}");
            
            // Verify the CLI process is still running before killing it
            testOutputHelper.WriteLine($"[Run {runNumber}] CLI process status before kill: HasExited={fakeCliProcess.Process.HasExited}");
            if (fakeCliProcess.Process.HasExited)
            {
                testOutputHelper.WriteLine($"[Run {runNumber}] WARNING: CLI process already exited with code: {fakeCliProcess.Process.ExitCode}");
            }
            
            testOutputHelper.WriteLine($"[Run {runNumber}] Killing CLI process at {stopwatch.Elapsed}...");
            fakeCliProcess.Process.Kill();
            
            // Wait a moment for the kill to be processed
            await Task.Delay(100);
            testOutputHelper.WriteLine($"[Run {runNumber}] CLI process killed. HasExited={fakeCliProcess.Process.HasExited}");

            testOutputHelper.WriteLine($"[Run {runNumber}] Waiting for app to exit (timeout: 10s)...");
            await pendingRun.WaitAsync(TimeSpan.FromSeconds(10));
            testOutputHelper.WriteLine($"[Run {runNumber}] App exited successfully after {stopwatch.Elapsed}");
        }
        catch (Exception ex)
        {
            testOutputHelper.WriteLine($"[Run {runNumber}] EXCEPTION after {stopwatch.Elapsed}: {ex.GetType().Name}: {ex.Message}");
            testOutputHelper.WriteLine($"[Run {runNumber}] Stack trace: {ex.StackTrace}");
            throw;
        }
        finally
        {
            stopwatch.Stop();
            testOutputHelper.WriteLine($"=== Completed AppHostExitsWhenCliProcessPidDies run #{runNumber} in {stopwatch.Elapsed} ===");
        }
    }
}

file sealed class HostLifetimeStub(Action stopImplementation) : IHostApplicationLifetime
{
    public CancellationToken ApplicationStarted => throw new NotImplementedException();

    public CancellationToken ApplicationStopped => throw new NotImplementedException();

    public CancellationToken ApplicationStopping => throw new NotImplementedException();

    public void StopApplication() => stopImplementation();
}
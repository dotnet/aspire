// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;
using Aspire.Hosting.Cli;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aspire.Cli.Tests;

public class CliOrphanDetectorTests
{
    [Fact]
    public async Task CliOrphanDetectorCompletesWhenNoPidEnvironmentVariablePresent()
    {
        var lifetime = new HostLifetimeStub(() => {});
        var detector = new CliOrphanDetector(lifetime);
        detector.GetEnvironmentVariable = _ => null;

        // The detector should complete almost immediately because there is no
        // environment variable present that indicates that it is hitched to
        // .NET Aspire lifetime.
        await detector.StartAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CliOrphanDetectorCallsStopIfEnvironmentVariablePresentAndProcessNotRunning()
    {
        var stopSignalChannel = Channel.CreateUnbounded<bool>();
        var lifetime = new HostLifetimeStub(() => stopSignalChannel.Writer.TryWrite(true));

        var detector = new CliOrphanDetector(lifetime);
        detector.GetEnvironmentVariable = _ => "1111";
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
        var fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.Now);

        var stopSignalChannel = Channel.CreateUnbounded<bool>();
        var processRunningChannel = Channel.CreateUnbounded<int>();

        var lifetime = new HostLifetimeStub(() => stopSignalChannel.Writer.TryWrite(true));
        var detector = new CliOrphanDetector(lifetime);
        detector.TimeProvider = fakeTimeProvider;
        detector.GetEnvironmentVariable = _ => "1111";
        
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
}

file sealed class HostLifetimeStub(Action stopImplementation) : IHostApplicationLifetime
{
    public CancellationToken ApplicationStarted => throw new NotImplementedException();

    public CancellationToken ApplicationStopped => throw new NotImplementedException();

    public CancellationToken ApplicationStopping => throw new NotImplementedException();

    public void StopApplication() => stopImplementation();
}
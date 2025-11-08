// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting.Cli;

internal sealed class CliOrphanDetector(IConfiguration configuration, IHostApplicationLifetime lifetime, TimeProvider timeProvider) : BackgroundService
{
    internal Func<int, bool> IsProcessRunning { get; set; } = (int pid) =>
    {
        try
        {
            return !Process.GetProcessById(pid).HasExited;
        }
        catch (ArgumentException)
        {
            // If Process.GetProcessById throws it means the process in not running.
            return false;
        }
    };

    internal Func<int, long, bool> IsProcessRunningWithStartTime { get; set; } = (int pid, long expectedStartTimeUnix) =>
    {
        try
        {
            var process = Process.GetProcessById(pid);
            if (process.HasExited)
            {
                return false;
            }
            
            // Check if the process start time matches the expected start time exactly.
            var actualStartTimeUnix = ((DateTimeOffset)process.StartTime).ToUnixTimeSeconds();
            return actualStartTimeUnix == expectedStartTimeUnix;
        }
        catch
        {
            // If we can't get the process and/or can't get the start time, 
            // then we interpret both exceptions as the process not being there.
            return false;
        }
    };

    /// <summary>
    /// Test hook that is called before waiting for the next timer tick.
    /// This allows tests to synchronize without relying on timing delays.
    /// </summary>
    internal Func<Task>? OnBeforeTimerWaitAsync { get; set; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            if (configuration[KnownConfigNames.CliProcessId] is not { } pidString || !int.TryParse(pidString, out var pid))
            {
                // If there is no PID environment variable, we assume that the process is not a child process
                // of the .NET Aspire CLI and we won't continue monitoring.
                return;
            }

            // Try to get the CLI process start time for robust orphan detection
            long? expectedStartTimeUnix = null;
            if (configuration[KnownConfigNames.CliProcessStarted] is { } startTimeString && 
                long.TryParse(startTimeString, out var startTimeUnix))
            {
                expectedStartTimeUnix = startTimeUnix;
            }

            using var periodic = new PeriodicTimer(TimeSpan.FromSeconds(1), timeProvider);

            do
            {
                bool isProcessStillRunning;
                
                if (expectedStartTimeUnix.HasValue)
                {
                    // Use robust process checking with start time verification
                    isProcessStillRunning = IsProcessRunningWithStartTime(pid, expectedStartTimeUnix.Value);
                }
                else
                {
                    // Fall back to PID-only logic for backwards compatibility
                    isProcessStillRunning = IsProcessRunning(pid);
                }

                if (!isProcessStillRunning)
                {
                    lifetime.StopApplication();
                    return;
                }

                // Test hook: allow tests to synchronize before the timer wait
                if (OnBeforeTimerWaitAsync is not null)
                {
                    await OnBeforeTimerWaitAsync().ConfigureAwait(false);
                }
            } while (await periodic.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
        }
        catch (TaskCanceledException)
        {
            // This is expected when the app is shutting down.
        }
    }
}
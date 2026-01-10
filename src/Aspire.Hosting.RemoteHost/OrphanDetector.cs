// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting.RemoteHost;

internal sealed class OrphanDetector : BackgroundService
{
    private const string HostProcessId = "REMOTE_APP_HOST_PID";

    /// <summary>
    /// Called when the parent process has died.
    /// </summary>
    public Action? OnParentDied { get; set; }

    internal Func<int, bool> IsProcessRunning { get; set; } = (int pid) =>
    {
        try
        {
            return !Process.GetProcessById(pid).HasExited;
        }
        catch (ArgumentException)
        {
            // If Process.GetProcessById throws it means the process is not running.
            return false;
        }
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            if (Environment.GetEnvironmentVariable(HostProcessId) is not { } pidString || !int.TryParse(pidString, out var pid))
            {
                // If there is no PID environment variable, we assume that the process is not a child process
                // of the .NET Aspire CLI and we won't continue monitoring.
                Console.WriteLine("No parent PID specified, orphan detection disabled.");
                return;
            }

            Console.WriteLine($"Monitoring parent process PID: {pid}");

            using var periodic = new PeriodicTimer(TimeSpan.FromSeconds(1), TimeProvider.System);

            do
            {
                if (!IsProcessRunning(pid))
                {
                    Console.WriteLine($"Parent process {pid} is no longer running.");
                    OnParentDied?.Invoke();
                    return;
                }
            } while (await periodic.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException)
        {
            // This is expected when the app is shutting down.
            Console.WriteLine("Orphan detector stopped.");
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting.Cli;

internal sealed class CliOrphanDetector(IConfiguration configuration, IHostApplicationLifetime lifetime) : BackgroundService
{
    private const string CliProcessIdEnvironmentVariable = "ASPIRE_CLI_PID";

    internal TimeProvider TimeProvider { get; set; } = TimeProvider.System;

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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (configuration[CliProcessIdEnvironmentVariable] is not { } pidString || !int.TryParse(pidString, out var pid))
        {
            // If there is no PID environment variable, we assume that the process is not a child process
            // of the .NET Aspire CLI and we won't continue monitoring.
            return;
        }

        Console.WriteLine("CLI PROCESS ID IS: " + pid);

        using var periodic = new PeriodicTimer(TimeSpan.FromSeconds(1), TimeProvider);

        do
        {
            if (!IsProcessRunning(pid))
            {
                lifetime.StopApplication();
                return;
            }
        } while (await periodic.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }
}
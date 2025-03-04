// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Hosting;

namespace Aspire.Cli;

internal sealed class AppHostRunner(IHostLifetime hostLifetime)
{
    internal Func<int> GetCurrentProcessId { get; set; } = () => Environment.ProcessId;

    public async Task<int> RunAppHostAsync(FileInfo appHostProjectFile, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo("dotnet", $"run --project \"{appHostProjectFile.FullName}\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // The AppHost uses this environment variable to signal to the CliOrphanDetector which process
        // it should monitor in order to know when to stop the CLI. As long as the process still exists
        // the orphan detector will allow the CLI to keep running. If the environment variable does
        // not exist the orphan detector will exit.
        startInfo.EnvironmentVariables["ASPIRE_CLI_PID"] = GetCurrentProcessId().ToString(CultureInfo.InvariantCulture);

        using var process = new Process { StartInfo = startInfo };
        var started = process.Start();

        if (!started)
        {
            return ExitCodeConstants.FailedToDotnetRunAppHost;
        }

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        if (!process.HasExited)
        {
            process.Kill(false); // DCP should clean everything else up.
        }

        await hostLifetime.StopAsync(cancellationToken).ConfigureAwait(false);

        return process.ExitCode;
    }
}
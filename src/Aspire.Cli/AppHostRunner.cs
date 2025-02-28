// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace Aspire.Cli;

internal sealed class AppHostRunner(IHostLifetime hostLifetime)
{
    public async Task<int> RunAppHostAsync(FileInfo appHostProjectFile, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo("dotnet", $"run --project \"{appHostProjectFile.FullName}\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true
        };
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
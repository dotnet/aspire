// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;

namespace Aspire.Cli;

internal sealed class DotNetCliRunner
{
    internal Func<int> GetCurrentProcessId { get; set; } = () => Environment.ProcessId;

    public async Task<int> RunAsync(FileInfo projectFile, string[] args, CancellationToken cancellationToken)
    {
        string[] cliArgs = ["run", "--project", projectFile.FullName, "--", ..args];
        return await ExecuteAsync(cliArgs, cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> InstallTemplateAsync(string packageName, string version, bool force, CancellationToken cancellationToken)
    {
        string[] forceArgs = force ? ["--force"] : [];
        string[] cliArgs = ["new", "install", $"{packageName}::{version}", ..forceArgs];
        return await ExecuteAsync(cliArgs, cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> NewProjectAsync(CancellationToken cancellationToken)
    {
        string[] cliArgs = ["new", "aspire-starter", "--name", "HelloAspire"];
        return await ExecuteAsync(cliArgs, cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> ExecuteAsync(string[] args, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (args.Length > 0)
        {
            startInfo.Arguments = string.Join(" ", args);
        }

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
            process.Kill(false);
        }

        return process.ExitCode;
    }
}
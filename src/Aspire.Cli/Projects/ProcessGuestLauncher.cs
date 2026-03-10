// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Projects;

/// <summary>
/// Launches a guest language process by starting a local OS process.
/// </summary>
internal sealed class ProcessGuestLauncher : IGuestProcessLauncher
{
    private readonly string _language;
    private readonly ILogger _logger;

    public ProcessGuestLauncher(string language, ILogger logger)
    {
        _language = language;
        _logger = logger;
    }

    public async Task<(int ExitCode, OutputCollector? Output)> LaunchAsync(
        string command,
        string[] args,
        DirectoryInfo workingDirectory,
        IDictionary<string, string> environmentVariables,
        CancellationToken cancellationToken)
    {
        var resolvedCommand = PathLookupHelper.FindFullPathFromPath(command);
        if (resolvedCommand is null)
        {
            _logger.LogError("Command '{Command}' not found in PATH", command);
            var errorOutput = new OutputCollector();
            errorOutput.AppendError($"Command '{command}' not found. Please ensure it is installed and in your PATH.");
            return (-1, errorOutput);
        }

        _logger.LogDebug("Executing: {Command} {Args}", resolvedCommand, string.Join(" ", args));

        var startInfo = new ProcessStartInfo
        {
            FileName = resolvedCommand,
            WorkingDirectory = workingDirectory.FullName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        foreach (var (key, value) in environmentVariables)
        {
            startInfo.EnvironmentVariables[key] = value;
        }

        using var process = new Process { StartInfo = startInfo };

        var outputCollector = new OutputCollector();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data is not null)
            {
                _logger.LogDebug("{Language}({ProcessId}) stdout: {Line}", _language, process.Id, e.Data);
                outputCollector.AppendOutput(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data is not null)
            {
                _logger.LogDebug("{Language}({ProcessId}) stderr: {Line}", _language, process.Id, e.Data);
                outputCollector.AppendError(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);
        return (process.ExitCode, outputCollector);
    }
}

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
    private readonly Func<string, string?> _commandResolver;
    private readonly Action<string, string>? _liveOutputCallback;

    public ProcessGuestLauncher(string language, ILogger logger, Func<string, string?>? commandResolver = null, Action<string, string>? liveOutputCallback = null)
    {
        _language = language;
        _logger = logger;
        _commandResolver = commandResolver ?? PathLookupHelper.FindFullPathFromPath;
        _liveOutputCallback = liveOutputCallback;
    }

    public async Task<(int ExitCode, OutputCollector? Output)> LaunchAsync(
        string command,
        string[] args,
        DirectoryInfo workingDirectory,
        IDictionary<string, string> environmentVariables,
        CancellationToken cancellationToken)
    {
        if (!CommandPathResolver.TryResolveCommand(command, _commandResolver, out var resolvedCommand, out var errorMessage))
        {
            _logger.LogError("Command '{Command}' not found in PATH", command);
            var errorOutput = new OutputCollector();
            errorOutput.AppendError(errorMessage!);
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

        var outputCollector = new OutputCollector(fileLogger: null, liveOutputCallback: _liveOutputCallback);
        var stdoutCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stderrCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data is null)
            {
                stdoutCompleted.TrySetResult();
            }
            else
            {
                _logger.LogDebug("{Language}({ProcessId}) stdout: {Line}", _language, process.Id, e.Data);
                outputCollector.AppendOutput(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data is null)
            {
                stderrCompleted.TrySetResult();
            }
            else
            {
                _logger.LogDebug("{Language}({ProcessId}) stderr: {Line}", _language, process.Id, e.Data);
                outputCollector.AppendError(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);
        await Task.WhenAll(stdoutCompleted.Task, stderrCompleted.Task).WaitAsync(cancellationToken);
        return (process.ExitCode, outputCollector);
    }
}

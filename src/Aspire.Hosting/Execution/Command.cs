// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREHOSTINGVIRTUALSHELL001

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Execution;

/// <summary>
/// Implementation of <see cref="ICommand"/> that represents a prepared command.
/// </summary>
[Experimental("ASPIREHOSTINGVIRTUALSHELL001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
internal sealed class Command : ICommand
{
    private readonly VirtualShell _shell;
    private readonly string _resolvedPath;

    internal Command(VirtualShell shell, string fileName, IReadOnlyList<string> args, string resolvedPath)
    {
        _shell = shell;
        FileName = fileName;
        Arguments = args;
        _resolvedPath = resolvedPath;
    }

    /// <inheritdoc />
    public string FileName { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Arguments { get; }

    /// <inheritdoc />
    public async Task<ProcessResult> RunAsync(ProcessInput? stdin = null, bool capture = true, CancellationToken ct = default)
    {
        using var activity = _shell.ActivitySource.StartActivity(FileName);

        // Only build redacted command string when needed for diagnostics
        string? redactedCommand = null;
        if (activity is not null || _shell.LoggingEnabled)
        {
            var redactedArgs = _shell.RedactArgs(Arguments);
            redactedCommand = $"{FileName} {string.Join(" ", redactedArgs)}".Trim();

            activity?.SetTag("virtualshell.command", redactedCommand);
            activity?.SetTag("virtualshell.working_dir", _shell.State.WorkingDirectory);
            if (_shell.CurrentTag is not null)
            {
                activity?.SetTag("virtualshell.tag", _shell.CurrentTag);
            }

            if (_shell.LoggingEnabled)
            {
                _shell.Logger.LogDebug("Starting command: {Command} in {WorkingDirectory}",
                    redactedCommand, _shell.State.WorkingDirectory ?? ".");
            }
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await _shell.Runner.RunAsync(_resolvedPath, Arguments, _shell.State, stdin, capture, ct).ConfigureAwait(false);
            stopwatch.Stop();

            _shell.RecordMetrics(FileName, result, stopwatch.Elapsed.TotalMilliseconds);
            activity?.SetTag("virtualshell.exit_code", result.ExitCode);
            activity?.SetStatus(result.Success ? ActivityStatusCode.Ok : ActivityStatusCode.Error);

            if (_shell.LoggingEnabled)
            {
                if (result.Success)
                {
                    _shell.Logger.LogInformation("Command completed: {Command} exited {ExitCode} in {Duration}ms",
                        redactedCommand, result.ExitCode, stopwatch.Elapsed.TotalMilliseconds);
                }
                else
                {
                    var redactedStderr = _shell.Redact(result.Stderr);
                    _shell.Logger.LogWarning("Command failed: {Command} exited {ExitCode} ({Reason}) in {Duration}ms: {Stderr}",
                        redactedCommand, result.ExitCode, result.Reason, stopwatch.Elapsed.TotalMilliseconds, redactedStderr);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            if (_shell.LoggingEnabled)
            {
                _shell.Logger.LogError(ex, "Command threw exception: {Command} in {Duration}ms",
                    redactedCommand, stopwatch.Elapsed.TotalMilliseconds);
            }
            throw;
        }
    }

    /// <inheritdoc />
    public IProcessLines StartReading(ProcessInput? stdin = null)
    {
        if (_shell.LoggingEnabled)
        {
            var redactedArgs = _shell.RedactArgs(Arguments);
            var redactedCommand = $"{FileName} {string.Join(" ", redactedArgs)}".Trim();
            _shell.Logger.LogDebug("Starting streaming command: {Command} in {WorkingDirectory}",
                redactedCommand, _shell.State.WorkingDirectory ?? ".");
        }

        return _shell.Runner.StartReading(_resolvedPath, Arguments, _shell.State, stdin);
    }

    /// <inheritdoc />
    public IProcessPipes StartProcess()
    {
        if (_shell.LoggingEnabled)
        {
            var redactedArgs = _shell.RedactArgs(Arguments);
            var redactedCommand = $"{FileName} {string.Join(" ", redactedArgs)}".Trim();
            _shell.Logger.LogDebug("Starting pipe-access command: {Command} in {WorkingDirectory}",
                redactedCommand, _shell.State.WorkingDirectory ?? ".");
        }

        return _shell.Runner.StartProcess(_resolvedPath, Arguments, _shell.State);
    }

    /// <inheritdoc />
    public IProcessHandle Start(ProcessInput? stdin = null, ProcessOutput? stdout = null, ProcessOutput? stderr = null)
    {
        if (_shell.LoggingEnabled)
        {
            var redactedArgs = _shell.RedactArgs(Arguments);
            var redactedCommand = $"{FileName} {string.Join(" ", redactedArgs)}".Trim();
            _shell.Logger.LogDebug("Starting custom output command: {Command} in {WorkingDirectory}",
                redactedCommand, _shell.State.WorkingDirectory ?? ".");
        }

        return _shell.Runner.Start(_resolvedPath, Arguments, _shell.State, stdin, stdout ?? ProcessOutput.Null, stderr ?? ProcessOutput.Null);
    }
}

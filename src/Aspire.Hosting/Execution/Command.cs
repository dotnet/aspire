// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREHOSTINGVIRTUALSHELL001

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Execution;

/// <summary>
/// Represents a command that can be configured fluently and executed.
/// </summary>
public sealed class Command : ICommand
{
    private readonly ShellState _shellState;
    private readonly IExecutableResolver _resolver;
    private readonly IProcessRunner _runner;
    private readonly string _fileName;
    private readonly IReadOnlyList<string> _args;

    // Diagnostics context
    private readonly string? _tag;
    private readonly HashSet<string> _secretValues;
    private readonly HashSet<string> _secretEnvKeys;
    private readonly bool _loggingEnabled;
    private readonly ILogger _logger;
    private readonly VirtualShellActivitySource _activitySource;
    private readonly Histogram<double> _durationHistogram;
    private readonly Counter<long> _commandCounter;

    // Per-command settings
    private Stdin? _stdin;
    private bool _captureOutput = true;

    internal Command(
        ShellState shellState,
        IExecutableResolver resolver,
        IProcessRunner runner,
        string fileName,
        IReadOnlyList<string> args,
        string? tag,
        HashSet<string> secretValues,
        HashSet<string> secretEnvKeys,
        bool loggingEnabled,
        ILogger logger,
        VirtualShellActivitySource activitySource,
        Histogram<double> durationHistogram,
        Counter<long> commandCounter)
    {
        _shellState = shellState;
        _resolver = resolver;
        _runner = runner;
        _fileName = fileName;
        _args = args;
        _tag = tag;
        _secretValues = secretValues;
        _secretEnvKeys = secretEnvKeys;
        _loggingEnabled = loggingEnabled;
        _logger = logger;
        _activitySource = activitySource;
        _durationHistogram = durationHistogram;
        _commandCounter = commandCounter;
    }

    /// <inheritdoc />
    public ICommand WithStdin(Stdin stdin)
    {
        _stdin = stdin;
        return this;
    }

    /// <inheritdoc />
    public ICommand WithStdin() => WithStdin(Stdin.CreatePipe());

    /// <inheritdoc />
    public ICommand WithCaptureOutput(bool capture)
    {
        _captureOutput = capture;
        return this;
    }

    /// <inheritdoc />
    public async Task<ProcessResult> RunAsync(CancellationToken ct = default)
    {
        var spec = CreateSpec(captureByDefault: true);
        var exePath = _resolver.ResolveOrThrow(_fileName, _shellState);

        using var activity = _activitySource.StartActivity(_fileName);

        // Only build redacted command string when needed for diagnostics
        string? redactedCommand = null;
        if (activity is not null || _loggingEnabled)
        {
            var redactedArgs = RedactArgs(_args);
            redactedCommand = $"{_fileName} {string.Join(" ", redactedArgs)}".Trim();

            activity?.SetTag("virtualshell.command", redactedCommand);
            activity?.SetTag("virtualshell.working_dir", _shellState.WorkingDirectory);
            if (_tag is not null)
            {
                activity?.SetTag("virtualshell.tag", _tag);
            }

            if (_loggingEnabled)
            {
                _logger.LogDebug("Starting command: {Command} in {WorkingDirectory}",
                    redactedCommand, _shellState.WorkingDirectory ?? ".");
            }
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await _runner.RunAsync(exePath, _args, spec, _shellState, ct).ConfigureAwait(false);
            stopwatch.Stop();

            RecordMetrics(result, stopwatch.Elapsed.TotalMilliseconds);
            activity?.SetTag("virtualshell.exit_code", result.ExitCode);
            activity?.SetStatus(result.Success ? ActivityStatusCode.Ok : ActivityStatusCode.Error);

            if (_loggingEnabled)
            {
                if (result.Success)
                {
                    _logger.LogInformation("Command completed: {Command} exited {ExitCode} in {Duration}ms",
                        redactedCommand, result.ExitCode, stopwatch.Elapsed.TotalMilliseconds);
                }
                else
                {
                    var redactedStderr = Redact(result.Stderr);
                    _logger.LogWarning("Command failed: {Command} exited {ExitCode} ({Reason}) in {Duration}ms: {Stderr}",
                        redactedCommand, result.ExitCode, result.Reason, stopwatch.Elapsed.TotalMilliseconds, redactedStderr);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            if (_loggingEnabled)
            {
                _logger.LogError(ex, "Command threw exception: {Command} in {Duration}ms",
                    redactedCommand, stopwatch.Elapsed.TotalMilliseconds);
            }
            throw;
        }
    }

    /// <inheritdoc />
    public IRunningProcess Start()
    {
        var spec = CreateSpec(captureByDefault: false);
        var exePath = _resolver.ResolveOrThrow(_fileName, _shellState);

        if (_loggingEnabled)
        {
            var redactedArgs = RedactArgs(_args);
            var redactedCommand = $"{_fileName} {string.Join(" ", redactedArgs)}".Trim();
            _logger.LogDebug("Starting streaming command: {Command} in {WorkingDirectory}",
                redactedCommand, _shellState.WorkingDirectory ?? ".");
        }

        // For streaming, we don't wrap with activity/metrics - the caller manages the lifetime
        return _runner.Start(exePath, _args, spec, _shellState);
    }

    private ExecSpec CreateSpec(bool captureByDefault)
    {
        var spec = new ExecSpec
        {
            WorkingDirectory = _shellState.WorkingDirectory,
            CaptureOutput = _captureOutput && captureByDefault,
            Stdin = _stdin
        };

        return spec;
    }

    private void RecordMetrics(ProcessResult result, double durationMs)
    {
        var tags = new TagList
        {
            { "command", _fileName },
            { "exit_code", result.ExitCode },
            { "success", result.Success }
        };

        if (_tag is not null)
        {
            tags.Add("tag", _tag);
        }

        _durationHistogram.Record(durationMs, tags);
        _commandCounter.Add(1, tags);
    }

    private string Redact(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value ?? string.Empty;
        }

        if (_secretValues.Contains(value))
        {
            return "[REDACTED]";
        }

        return value;
    }

    private IReadOnlyList<string> RedactArgs(IReadOnlyList<string> args)
    {
        if (_secretValues.Count == 0)
        {
            return args; // No secrets, no allocation needed
        }

        var redacted = new string[args.Count];
        for (var i = 0; i < args.Count; i++)
        {
            redacted[i] = Redact(args[i]);
        }
        return redacted;
    }
}

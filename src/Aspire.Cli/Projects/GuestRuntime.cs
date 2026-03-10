// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Cli.Utils;
using Aspire.Hosting.Ats;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Projects;

/// <summary>
/// A data-driven runtime executor for guest language processes.
/// Interprets <see cref="RuntimeSpec"/> to install dependencies and execute AppHost processes.
/// </summary>
internal sealed class GuestRuntime
{
    private readonly RuntimeSpec _spec;
    private readonly ILogger _logger;
    private readonly Func<string, string?> _commandResolver;

    /// <summary>
    /// Creates a new GuestRuntime for the given runtime specification.
    /// </summary>
    /// <param name="spec">The runtime specification describing how to execute the guest language.</param>
    /// <param name="logger">Logger for debugging output.</param>
    /// <param name="commandResolver">Optional command resolver used to locate executables on PATH.</param>
    public GuestRuntime(RuntimeSpec spec, ILogger logger, Func<string, string?>? commandResolver = null)
    {
        _spec = spec;
        _logger = logger;
        _commandResolver = commandResolver ?? PathLookupHelper.FindFullPathFromPath;
    }

    /// <summary>
    /// Gets the language identifier from the runtime specification.
    /// </summary>
    public string Language => _spec.Language;

    /// <summary>
    /// Gets the display name from the runtime specification.
    /// </summary>
    public string DisplayName => _spec.DisplayName;

    /// <summary>
    /// Installs dependencies for the guest language project.
    /// </summary>
    /// <param name="directory">The project directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the exit code and captured output from the dependency installation command.</returns>
    public async Task<(int ExitCode, OutputCollector Output)> InstallDependenciesAsync(DirectoryInfo directory, CancellationToken cancellationToken)
    {
        var outputCollector = new OutputCollector();

        if (_spec.InstallDependencies is null)
        {
            _logger.LogDebug("No dependency installation configured for {Language}", _spec.Language);
            return (0, outputCollector);
        }

        if (!CommandPathResolver.TryResolveCommand(_spec.InstallDependencies.Command, _commandResolver, out var command, out var errorMessage))
        {
            _logger.LogError("Command '{Command}' not found in PATH", _spec.InstallDependencies.Command);
            outputCollector.AppendError(errorMessage!);
            return (-1, outputCollector);
        }

        var args = ReplacePlaceholders(_spec.InstallDependencies.Args, null, directory, null);

        _logger.LogDebug("Installing dependencies: {Command} {Args}", command, string.Join(" ", args));

        var startInfo = new ProcessStartInfo
        {
            FileName = command,
            WorkingDirectory = directory.FullName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Use ArgumentList for proper escaping of special characters
        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        // Add command-specific environment variables from the spec
        if (_spec.InstallDependencies.EnvironmentVariables is not null)
        {
            foreach (var (key, value) in _spec.InstallDependencies.EnvironmentVariables)
            {
                startInfo.EnvironmentVariables[key] = value;
            }
        }

        using var process = new Process { StartInfo = startInfo };

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data is not null)
            {
                _logger.LogDebug("{Language}({ProcessId}) stdout: {Line}", _spec.Language, process.Id, e.Data);
                outputCollector.AppendOutput(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data is not null)
            {
                _logger.LogDebug("{Language}({ProcessId}) stderr: {Line}", _spec.Language, process.Id, e.Data);
                outputCollector.AppendError(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        return (process.ExitCode, outputCollector);
    }

    /// <summary>
    /// Runs the AppHost guest process.
    /// </summary>
    /// <param name="appHostFile">The AppHost file to execute.</param>
    /// <param name="directory">The project directory.</param>
    /// <param name="environmentVariables">Environment variables to set for the process.</param>
    /// <param name="watchMode">Whether to run in watch mode for hot reload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple of the exit code and captured output.</returns>
    public async Task<(int ExitCode, OutputCollector Output)> RunAsync(
        FileInfo appHostFile,
        DirectoryInfo directory,
        IDictionary<string, string> environmentVariables,
        bool watchMode,
        CancellationToken cancellationToken)
    {
        // Use watch execute if watch mode is enabled and the spec supports it
        var commandSpec = watchMode && _spec.WatchExecute is not null
            ? _spec.WatchExecute
            : _spec.Execute;

        return await ExecuteCommandAsync(commandSpec, appHostFile, directory, environmentVariables, null, cancellationToken);
    }

    /// <summary>
    /// Runs the AppHost guest process for publishing.
    /// </summary>
    /// <param name="appHostFile">The AppHost file to execute.</param>
    /// <param name="directory">The project directory.</param>
    /// <param name="environmentVariables">Environment variables to set for the process.</param>
    /// <param name="publishArgs">Additional arguments for publishing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple of the exit code and captured output.</returns>
    public async Task<(int ExitCode, OutputCollector Output)> PublishAsync(
        FileInfo appHostFile,
        DirectoryInfo directory,
        IDictionary<string, string> environmentVariables,
        string[]? publishArgs,
        CancellationToken cancellationToken)
    {
        // Use publish execute if available, otherwise fall back to regular execute
        var commandSpec = _spec.PublishExecute ?? _spec.Execute;

        return await ExecuteCommandAsync(commandSpec, appHostFile, directory, environmentVariables, publishArgs, cancellationToken);
    }

    private async Task<(int ExitCode, OutputCollector Output)> ExecuteCommandAsync(
        CommandSpec commandSpec,
        FileInfo appHostFile,
        DirectoryInfo directory,
        IDictionary<string, string> environmentVariables,
        string[]? additionalArgs,
        CancellationToken cancellationToken)
    {
        if (!CommandPathResolver.TryResolveCommand(commandSpec.Command, _commandResolver, out var command, out var errorMessage))
        {
            _logger.LogError("Command '{Command}' not found in PATH", commandSpec.Command);
            var output = new OutputCollector();
            output.AppendError(errorMessage!);
            return (-1, output);
        }

        var args = ReplacePlaceholders(commandSpec.Args, appHostFile, directory, additionalArgs);

        _logger.LogDebug("Executing: {Command} {Args}", command, string.Join(" ", args));

        var startInfo = new ProcessStartInfo
        {
            FileName = command,
            WorkingDirectory = directory.FullName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Use ArgumentList for proper escaping of special characters
        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        // Add caller-provided environment variables
        foreach (var (key, value) in environmentVariables)
        {
            startInfo.EnvironmentVariables[key] = value;
        }

        // Add command-specific environment variables from the spec
        // These take precedence over caller-provided variables
        if (commandSpec.EnvironmentVariables is not null)
        {
            foreach (var (key, value) in commandSpec.EnvironmentVariables)
            {
                startInfo.EnvironmentVariables[key] = value;
            }
        }

        using var process = new Process { StartInfo = startInfo };

        var outputCollector = new OutputCollector();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data is not null)
            {
                _logger.LogDebug("{Language}({ProcessId}) stdout: {Line}", _spec.Language, process.Id, e.Data);
                outputCollector.AppendOutput(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data is not null)
            {
                _logger.LogDebug("{Language}({ProcessId}) stderr: {Line}", _spec.Language, process.Id, e.Data);
                outputCollector.AppendError(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);
        return (process.ExitCode, outputCollector);
    }

    /// <summary>
    /// Replaces placeholders in command arguments with actual values.
    /// </summary>
    private static string[] ReplacePlaceholders(
        string[] args,
        FileInfo? appHostFile,
        DirectoryInfo directory,
        string[]? additionalArgs)
    {
        var result = new List<string>();

        foreach (var arg in args)
        {
            var replaced = arg
                .Replace("{appHostFile}", appHostFile?.FullName ?? "")
                .Replace("{appHostDir}", directory.FullName);

            // Handle {args} placeholder - replace with additional args or empty
            if (replaced.Contains("{args}"))
            {
                if (additionalArgs is { Length: > 0 })
                {
                    replaced = replaced.Replace("{args}", string.Join(" ", additionalArgs));
                }
                else
                {
                    replaced = replaced.Replace("{args}", "");
                }
            }

            // Skip empty args that resulted from placeholder replacement
            if (!string.IsNullOrWhiteSpace(replaced))
            {
                result.Add(replaced);
            }
        }

        // If {args} wasn't in the template and we have additional args, append them
        if (additionalArgs is { Length: > 0 } && !args.Any(a => a.Contains("{args}")))
        {
            result.AddRange(additionalArgs);
        }

        return result.ToArray();
    }

}

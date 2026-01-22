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

    /// <summary>
    /// Creates a new GuestRuntime for the given runtime specification.
    /// </summary>
    /// <param name="spec">The runtime specification describing how to execute the guest language.</param>
    /// <param name="logger">Logger for debugging output.</param>
    public GuestRuntime(RuntimeSpec spec, ILogger logger)
    {
        _spec = spec;
        _logger = logger;
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
    /// <returns>The exit code from the dependency installation command.</returns>
    public async Task<int> InstallDependenciesAsync(DirectoryInfo directory, CancellationToken cancellationToken)
    {
        if (_spec.InstallDependencies is null)
        {
            _logger.LogDebug("No dependency installation configured for {Language}", _spec.Language);
            return 0;
        }

        var command = FindCommand(_spec.InstallDependencies.Command);
        if (command is null)
        {
            _logger.LogError("Command '{Command}' not found in PATH", _spec.InstallDependencies.Command);
            return -1;
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
        process.Start();
        await process.WaitForExitAsync(cancellationToken);

        return process.ExitCode;
    }

    /// <summary>
    /// Runs the AppHost guest process.
    /// </summary>
    /// <param name="appHostFile">The AppHost file to execute.</param>
    /// <param name="directory">The project directory.</param>
    /// <param name="environmentVariables">Environment variables to set for the process.</param>
    /// <param name="watchMode">Whether to run in watch mode for hot reload.</param>
    /// <param name="additionalArgs">Additional command-line arguments to pass to the AppHost.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple of the exit code and captured output.</returns>
    public async Task<(int ExitCode, OutputCollector Output)> RunAsync(
        FileInfo appHostFile,
        DirectoryInfo directory,
        IDictionary<string, string> environmentVariables,
        bool watchMode,
        string[]? additionalArgs,
        CancellationToken cancellationToken)
    {
        // Use watch execute if watch mode is enabled and the spec supports it
        var commandSpec = watchMode && _spec.WatchExecute is not null
            ? _spec.WatchExecute
            : _spec.Execute;

        return await ExecuteCommandAsync(commandSpec, appHostFile, directory, environmentVariables, additionalArgs, cancellationToken);
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
        var command = FindCommand(commandSpec.Command);
        if (command is null)
        {
            _logger.LogError("Command '{Command}' not found in PATH", commandSpec.Command);
            var output = new OutputCollector();
            output.AppendError($"Command '{commandSpec.Command}' not found. Please ensure it is installed and in your PATH.");
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

    /// <summary>
    /// Finds the full path to a command in PATH.
    /// </summary>
    private static string? FindCommand(string command)
    {
        return PathLookupHelper.FindFullPathFromPath(command);
    }
}

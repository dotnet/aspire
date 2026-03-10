// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        var args = ReplacePlaceholders(_spec.InstallDependencies.Args, null, directory, null);

        var environmentVariables = _spec.InstallDependencies.EnvironmentVariables ?? new Dictionary<string, string>();

        var launcher = CreateDefaultLauncher();
        var (exitCode, _) = await launcher.LaunchAsync(
            _spec.InstallDependencies.Command,
            args,
            directory,
            environmentVariables,
            cancellationToken);

        return exitCode;
    }

    /// <summary>
    /// Runs the AppHost guest process.
    /// </summary>
    /// <param name="appHostFile">The AppHost file to execute.</param>
    /// <param name="directory">The project directory.</param>
    /// <param name="environmentVariables">Environment variables to set for the process.</param>
    /// <param name="watchMode">Whether to run in watch mode for hot reload.</param>
    /// <param name="launcher">Strategy for launching the process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple of the exit code and captured output (null when launched via extension).</returns>
    public async Task<(int ExitCode, OutputCollector? Output)> RunAsync(
        FileInfo appHostFile,
        DirectoryInfo directory,
        IDictionary<string, string> environmentVariables,
        bool watchMode,
        IGuestProcessLauncher launcher,
        CancellationToken cancellationToken)
    {
        // Use watch execute if watch mode is enabled and the spec supports it
        var commandSpec = watchMode && _spec.WatchExecute is not null
            ? _spec.WatchExecute
            : _spec.Execute;

        return await ExecuteCommandAsync(commandSpec, appHostFile, directory, environmentVariables, null, launcher, cancellationToken);
    }

    /// <summary>
    /// Runs the AppHost guest process for publishing.
    /// </summary>
    /// <param name="appHostFile">The AppHost file to execute.</param>
    /// <param name="directory">The project directory.</param>
    /// <param name="environmentVariables">Environment variables to set for the process.</param>
    /// <param name="publishArgs">Additional arguments for publishing.</param>
    /// <param name="launcher">Strategy for launching the process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple of the exit code and captured output.</returns>
    public async Task<(int ExitCode, OutputCollector? Output)> PublishAsync(
        FileInfo appHostFile,
        DirectoryInfo directory,
        IDictionary<string, string> environmentVariables,
        string[]? publishArgs,
        IGuestProcessLauncher launcher,
        CancellationToken cancellationToken)
    {
        // Use publish execute if available, otherwise fall back to regular execute
        var commandSpec = _spec.PublishExecute ?? _spec.Execute;

        return await ExecuteCommandAsync(commandSpec, appHostFile, directory, environmentVariables, publishArgs, launcher, cancellationToken);
    }

    private async Task<(int ExitCode, OutputCollector? Output)> ExecuteCommandAsync(
        CommandSpec commandSpec,
        FileInfo appHostFile,
        DirectoryInfo directory,
        IDictionary<string, string> environmentVariables,
        string[]? additionalArgs,
        IGuestProcessLauncher launcher,
        CancellationToken cancellationToken)
    {
        var args = ReplacePlaceholders(commandSpec.Args, appHostFile, directory, additionalArgs);

        // Merge command-specific environment variables from the spec (they take precedence)
        var mergedEnvironment = new Dictionary<string, string>(environmentVariables);
        if (commandSpec.EnvironmentVariables is not null)
        {
            foreach (var (key, value) in commandSpec.EnvironmentVariables)
            {
                mergedEnvironment[key] = value;
            }
        }

        _logger.LogDebug("Launching: {Command} {Args}", commandSpec.Command, string.Join(" ", args));
        return await launcher.LaunchAsync(commandSpec.Command, args, directory, mergedEnvironment, cancellationToken);
    }

    /// <summary>
    /// Creates the default process-based launcher for this runtime.
    /// </summary>
    public ProcessGuestLauncher CreateDefaultLauncher() => new(_spec.Language, _logger);

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

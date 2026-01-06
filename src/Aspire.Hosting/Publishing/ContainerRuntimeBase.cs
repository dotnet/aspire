// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES003
#pragma warning disable ASPIRECONTAINERRUNTIME001
#pragma warning disable ASPIREHOSTINGVIRTUALSHELL001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Execution;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Publishing;

/// <summary>
/// Base class for container runtime implementations that provides common process execution,
/// logging, and error handling patterns.
/// </summary>
internal abstract class ContainerRuntimeBase<TLogger> : IContainerRuntime where TLogger : class
{
    private readonly ILogger<TLogger> _logger;
    private readonly IVirtualShell _shell;

    protected ContainerRuntimeBase(ILogger<TLogger> logger, IVirtualShell shell)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
    }

    /// <summary>
    /// Gets the logger instance for use in derived classes.
    /// </summary>
    protected ILogger<TLogger> Logger => _logger;

    /// <summary>
    /// Gets the virtual shell for process execution.
    /// </summary>
    protected IVirtualShell Shell => _shell;

    /// <summary>
    /// Gets the name of the container runtime executable (e.g., "docker", "podman").
    /// </summary>
    protected abstract string RuntimeExecutable { get; }

    public abstract string Name { get; }

    public abstract Task<bool> CheckIfRunningAsync(CancellationToken cancellationToken);

    public abstract Task BuildImageAsync(string contextPath, string dockerfilePath, ContainerImageBuildOptions? options, Dictionary<string, string?> buildArguments, Dictionary<string, string?> buildSecrets, string? stage, CancellationToken cancellationToken);

    public virtual async Task TagImageAsync(string localImageName, string targetImageName, CancellationToken cancellationToken)
    {
        var arguments = $"tag \"{localImageName}\" \"{targetImageName}\"";

        await ExecuteContainerCommandAsync(
            arguments,
            $"{Name} tag for {{LocalImageName}} -> {{TargetImageName}} failed with exit code {{ExitCode}}.",
            $"{Name} tag for {{LocalImageName}} -> {{TargetImageName}} succeeded.",
            $"{Name} tag failed with exit code {{0}}.",
            cancellationToken,
            localImageName, targetImageName).ConfigureAwait(false);
    }

    public virtual async Task RemoveImageAsync(string imageName, CancellationToken cancellationToken)
    {
        var arguments = $"rmi \"{imageName}\"";

        await ExecuteContainerCommandAsync(
            arguments,
            $"{Name} rmi for {{ImageName}} failed with exit code {{ExitCode}}.",
            $"{Name} rmi for {{ImageName}} succeeded.",
            $"{Name} rmi failed with exit code {{0}}.",
            cancellationToken,
            imageName).ConfigureAwait(false);
    }

    public virtual async Task PushImageAsync(IResource resource, CancellationToken cancellationToken)
    {
        var localImageName = resource.TryGetContainerImageName(out var imageName)
            ? imageName
            : resource.Name.ToLowerInvariant();

        var remoteImageName = await resource.GetFullRemoteImageNameAsync(cancellationToken).ConfigureAwait(false);

        await TagImageAsync(localImageName, remoteImageName, cancellationToken).ConfigureAwait(false);

        var arguments = $"push \"{remoteImageName}\"";

        await ExecuteContainerCommandAsync(
            arguments,
            $"{Name} push for {{ImageName}} failed with exit code {{ExitCode}}.",
            $"{Name} push for {{ImageName}} succeeded.",
            $"{Name} push failed with exit code {{0}}.",
            cancellationToken,
            remoteImageName).ConfigureAwait(false);
    }

    public virtual async Task LoginToRegistryAsync(string registryServer, string username, string password, CancellationToken cancellationToken)
    {
        var args = new[] { "login", registryServer, "--username", username, "--password-stdin" };

        _logger.LogDebug("Running {RuntimeName} with arguments: {Arguments}", RuntimeExecutable, string.Join(" ", args));
        _logger.LogDebug("Password length being passed to stdin: {PasswordLength}", password?.Length ?? 0);

        var result = await _shell.Command(RuntimeExecutable, args)
            .RunAsync(stdin: ProcessInput.FromText(password + "\n"), ct: cancellationToken).ConfigureAwait(false);

        result.LogOutput(_logger, RuntimeExecutable);

        if (result.ExitCode != 0)
        {
            _logger.LogError("{RuntimeName} login to {RegistryServer} failed with exit code {ExitCode}.", Name, registryServer, result.ExitCode);
            throw new DistributedApplicationException($"{Name} login failed with exit code {result.ExitCode}.");
        }

        _logger.LogInformation("{RuntimeName} login to {RegistryServer} succeeded.", Name, registryServer);
    }

    /// <summary>
    /// Executes a container runtime command with standard logging and error handling.
    /// </summary>
    /// <param name="arguments">The command arguments to pass to the container runtime.</param>
    /// <param name="errorLogTemplate">Log template for error messages (must include {ExitCode} placeholder).</param>
    /// <param name="successLogTemplate">Log template for success messages.</param>
    /// <param name="exceptionMessageTemplate">Exception message template (must include {ExitCode} placeholder).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="logArguments">Arguments to pass to the log templates.</param>
    private async Task ExecuteContainerCommandAsync(
        string arguments,
        string errorLogTemplate,
        string successLogTemplate,
        string exceptionMessageTemplate,
        CancellationToken cancellationToken,
        params object[] logArguments)
    {
        _logger.LogDebug("Running {RuntimeName} with arguments: {ArgumentList}", Name, arguments);

        var result = await _shell.Command($"{RuntimeExecutable} {arguments}").RunAsync(ct: cancellationToken).ConfigureAwait(false);

        result.LogOutput(_logger, RuntimeExecutable);

        if (result.ExitCode != 0)
        {
            var errorArgs = logArguments.Concat(new object[] { result.ExitCode }).ToArray();
            _logger.LogError(errorLogTemplate, errorArgs);
            throw new DistributedApplicationException(string.Format(System.Globalization.CultureInfo.InvariantCulture, exceptionMessageTemplate, result.ExitCode));
        }

        _logger.LogInformation(successLogTemplate, logArguments);
    }

    /// <summary>
    /// Executes a container runtime command and returns the exit code without throwing exceptions.
    /// </summary>
    /// <param name="arguments">The command arguments to pass to the container runtime.</param>
    /// <param name="errorLogTemplate">Log template for error messages (must include {ExitCode} placeholder).</param>
    /// <param name="successLogTemplate">Log template for success messages.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="logArguments">Arguments to pass to the log templates.</param>
    /// <param name="environmentVariables">Optional environment variables to set for the process.</param>
    /// <returns>The exit code of the process.</returns>
    protected async Task<int> ExecuteContainerCommandWithExitCodeAsync(
        string arguments,
        string errorLogTemplate,
        string successLogTemplate,
        CancellationToken cancellationToken,
        object[] logArguments,
        Dictionary<string, string>? environmentVariables = null)
    {
        _logger.LogDebug("Running {RuntimeName} with arguments: {ArgumentList}", Name, arguments);

        // Build the shell with environment variables if provided
        var shell = environmentVariables is not null
            ? _shell.Env(environmentVariables.ToDictionary(kvp => kvp.Key, kvp => (string?)kvp.Value))
            : _shell;

        var result = await shell.Command($"{RuntimeExecutable} {arguments}").RunAsync(ct: cancellationToken).ConfigureAwait(false);

        result.LogOutput(_logger, RuntimeExecutable);

        if (result.ExitCode != 0)
        {
            var errorArgs = logArguments.Concat(new object[] { result.ExitCode }).ToArray();
            _logger.LogError(errorLogTemplate, errorArgs);
            return result.ExitCode;
        }

        _logger.LogDebug(successLogTemplate, logArguments);
        return result.ExitCode;
    }

    /// <summary>
    /// Builds a string of build arguments for container build commands.
    /// </summary>
    /// <param name="buildArguments">The build arguments to include.</param>
    /// <returns>A string containing the formatted build arguments.</returns>
    protected static string BuildArgumentsString(Dictionary<string, string?> buildArguments)
    {
        var result = string.Empty;
        foreach (var buildArg in buildArguments)
        {
            result += buildArg.Value is not null
                ? $" --build-arg \"{buildArg.Key}={buildArg.Value}\""
                : $" --build-arg \"{buildArg.Key}\"";
        }
        return result;
    }

    /// <summary>
    /// Builds a string of build secrets for container build commands.
    /// </summary>
    /// <param name="buildSecrets">The build secrets to include.</param>
    /// <param name="requireValue">Whether to require a non-null value for secrets (default: false).</param>
    /// <returns>A string containing the formatted build secrets.</returns>
    protected static string BuildSecretsString(Dictionary<string, string?> buildSecrets, bool requireValue = false)
    {
        var result = string.Empty;
        foreach (var buildSecret in buildSecrets)
        {
            if (requireValue && buildSecret.Value is null)
            {
                result += $" --secret \"id={buildSecret.Key}\"";
            }
            else
            {
                result += $" --secret \"id={buildSecret.Key},env={buildSecret.Key.ToUpperInvariant()}\"";
            }
        }
        return result;
    }

    /// <summary>
    /// Builds a string for the target stage in container build commands.
    /// </summary>
    /// <param name="stage">The target stage to include.</param>
    /// <returns>A string containing the formatted target stage, or empty string if stage is null or empty.</returns>
    protected static string BuildStageString(string? stage)
    {
        return !string.IsNullOrEmpty(stage) ? $" --target \"{stage}\"" : string.Empty;
    }
}

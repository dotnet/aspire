// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using Aspire.Hosting.Dcp.Process;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Publishing;

/// <summary>
/// Base class for container runtime implementations that provides common process execution,
/// logging, and error handling patterns.
/// </summary>
internal abstract class ContainerRuntimeBase<TLogger> : IContainerRuntime where TLogger : class
{
    private readonly ILogger<TLogger> _logger;

    protected ContainerRuntimeBase(ILogger<TLogger> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the logger instance for use in derived classes.
    /// </summary>
    protected ILogger<TLogger> Logger => _logger;

    /// <summary>
    /// Gets the name of the container runtime executable (e.g., "docker", "podman").
    /// </summary>
    protected abstract string RuntimeExecutable { get; }

    public abstract string Name { get; }

    public abstract Task<bool> CheckIfRunningAsync(CancellationToken cancellationToken);

    public abstract Task BuildImageAsync(string contextPath, string dockerfilePath, string imageName, ContainerBuildOptions? options, Dictionary<string, string?> buildArguments, Dictionary<string, string?> buildSecrets, string? stage, CancellationToken cancellationToken);

    public virtual async Task TagImageAsync(string localImageName, string targetImageName, CancellationToken cancellationToken)
    {
        var arguments = $"tag \"{localImageName}\" \"{targetImageName}\"";
        
        await ExecuteContainerCommandAsync(
            arguments, 
            $"{Name} tag for {{LocalImageName}} -> {{TargetImageName}} failed with exit code {{ExitCode}}.",
            $"{Name} tag for {{LocalImageName}} -> {{TargetImageName}} succeeded.",
            $"{Name} tag failed with exit code {{ExitCode}}.",
            cancellationToken,
            localImageName, targetImageName).ConfigureAwait(false);
    }

    public virtual async Task PushImageAsync(string imageName, CancellationToken cancellationToken)
    {
        var arguments = $"push \"{imageName}\"";
        
        await ExecuteContainerCommandAsync(
            arguments, 
            $"{Name} push for {{ImageName}} failed with exit code {{ExitCode}}.",
            $"{Name} push for {{ImageName}} succeeded.",
            $"{Name} push failed with exit code {{ExitCode}}.",
            cancellationToken,
            imageName).ConfigureAwait(false);
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
    protected virtual async Task ExecuteContainerCommandAsync(
        string arguments, 
        string errorLogTemplate,
        string successLogTemplate,
        string exceptionMessageTemplate,
        CancellationToken cancellationToken,
        params object[] logArguments)
    {
        var spec = CreateProcessSpec(arguments);
        
        _logger.LogInformation("Running {RuntimeName} with arguments: {ArgumentList}", Name, spec.Arguments);
        var (pendingProcessResult, processDisposable) = ProcessUtil.Run(spec);

        await using (processDisposable)
        {
            var processResult = await pendingProcessResult
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            if (processResult.ExitCode != 0)
            {
                var errorArgs = logArguments.Concat(new object[] { processResult.ExitCode }).ToArray();
                _logger.LogError(errorLogTemplate, errorArgs);
                throw new DistributedApplicationException(string.Format(System.Globalization.CultureInfo.InvariantCulture, exceptionMessageTemplate, processResult.ExitCode));
            }

            _logger.LogInformation(successLogTemplate, logArguments);
        }
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
    protected virtual async Task<int> ExecuteContainerCommandWithExitCodeAsync(
        string arguments, 
        string errorLogTemplate,
        string successLogTemplate,
        CancellationToken cancellationToken,
        object[] logArguments,
        Dictionary<string, string>? environmentVariables = null)
    {
        var spec = CreateProcessSpec(arguments);
        
        // Add environment variables if provided
        if (environmentVariables is not null)
        {
            foreach (var (key, value) in environmentVariables)
            {
                spec.EnvironmentVariables[key] = value;
            }
        }
        
        _logger.LogInformation("Running {RuntimeName} with arguments: {ArgumentList}", Name, spec.Arguments);
        var (pendingProcessResult, processDisposable) = ProcessUtil.Run(spec);

        await using (processDisposable)
        {
            var processResult = await pendingProcessResult
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            if (processResult.ExitCode != 0)
            {
                var errorArgs = logArguments.Concat(new object[] { processResult.ExitCode }).ToArray();
                _logger.LogError(errorLogTemplate, errorArgs);
                return processResult.ExitCode;
            }

            _logger.LogInformation(successLogTemplate, logArguments);
            return processResult.ExitCode;
        }
    }

    /// <summary>
    /// Builds a string of build arguments for container build commands.
    /// </summary>
    /// <param name="buildArguments">The build arguments to include.</param>
    /// <returns>A string containing the formatted build arguments.</returns>
    protected virtual string BuildArgumentsString(Dictionary<string, string?> buildArguments)
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
    protected virtual string BuildSecretsString(Dictionary<string, string?> buildSecrets, bool requireValue = false)
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
    protected virtual string BuildStageString(string? stage)
    {
        return !string.IsNullOrEmpty(stage) ? $" --target \"{stage}\"" : string.Empty;
    }

    /// <summary>
    /// Creates a ProcessSpec for executing container runtime commands.
    /// </summary>
    /// <param name="arguments">The command arguments.</param>
    /// <returns>A configured ProcessSpec instance.</returns>
    protected virtual ProcessSpec CreateProcessSpec(string arguments)
    {
        return new ProcessSpec(RuntimeExecutable)
        {
            Arguments = arguments,
            OnOutputData = output =>
            {
                _logger.LogInformation("{RuntimeName} (stdout): {Output}", RuntimeExecutable, output);
            },
            OnErrorData = error =>
            {
                _logger.LogInformation("{RuntimeName} (stderr): {Error}", RuntimeExecutable, error);
            },
            ThrowOnNonZeroReturnCode = false,
            InheritEnv = true
        };
    }
}
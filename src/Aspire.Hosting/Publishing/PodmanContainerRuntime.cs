// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using Aspire.Hosting.Dcp.Process;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Publishing;

internal sealed class PodmanContainerRuntime(ILogger<PodmanContainerRuntime> logger) : IContainerRuntime
{
    public string Name => "Podman";
    private async Task<int> RunPodmanBuildAsync(string contextPath, string dockerfilePath, string imageName, ContainerBuildOptions? options, CancellationToken cancellationToken)
    {
        var arguments = $"build --file \"{dockerfilePath}\" --tag \"{imageName}\"";

        // Add platform support if specified
        if (options?.TargetPlatform is not null)
        {
            arguments += $" --platform \"{options.TargetPlatform.Value.ToRuntimePlatformString()}\"";
        }

        // Add format support if specified
        if (options?.ImageFormat is not null)
        {
            var format = options.ImageFormat.Value switch
            {
                ContainerImageFormat.Oci => "oci",
                ContainerImageFormat.Docker => "docker",
                _ => throw new ArgumentOutOfRangeException(nameof(options), options.ImageFormat, "Invalid container image format")
            };
            arguments += $" --format \"{format}\"";
        }

        // Add output support if specified
        if (!string.IsNullOrEmpty(options?.OutputPath))
        {
            // Extract resource name from imageName for the file name
            var resourceName = imageName.Split('/').Last().Split(':').First();
            arguments += $" --output \"{Path.Combine(options.OutputPath, resourceName)}.tar\"";
        }

        arguments += $" \"{contextPath}\"";

        var spec = new ProcessSpec("podman")
        {
            Arguments = arguments,
            OnOutputData = output =>
            {
                logger.LogInformation("podman build (stdout): {Output}", output);
            },
            OnErrorData = error =>
            {
                logger.LogInformation("podman build (stderr): {Error}", error);
            },
            ThrowOnNonZeroReturnCode = false,
            InheritEnv = true
        };

        logger.LogInformation("Running Podman CLI with arguments: {ArgumentList}", spec.Arguments);
        var (pendingProcessResult, processDisposable) = ProcessUtil.Run(spec);

        await using (processDisposable)
        {
            var processResult = await pendingProcessResult
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            if (processResult.ExitCode != 0)
            {
                logger.LogError("Podman build for {ImageName} failed with exit code {ExitCode}.", imageName, processResult.ExitCode);
                return processResult.ExitCode;
            }

            logger.LogInformation("Podman build for {ImageName} succeeded.", imageName);
            return processResult.ExitCode;
        }
    }

    public async Task BuildImageAsync(string contextPath, string dockerfilePath, string imageName, ContainerBuildOptions? options, CancellationToken cancellationToken)
    {
        var exitCode = await RunPodmanBuildAsync(
            contextPath,
            dockerfilePath,
            imageName,
            options,
            cancellationToken).ConfigureAwait(false);

        if (exitCode != 0)
        {
            throw new DistributedApplicationException($"Podman build failed with exit code {exitCode}.");
        }
    }

    public Task<bool> CheckIfRunningAsync(CancellationToken cancellationToken)
    {
        var spec = new ProcessSpec("podman")
        {
            Arguments = "container ls -n 1",
            OnOutputData = output =>
            {
                logger.LogInformation("podman container ls (stdout): {Output}", output);
            },
            OnErrorData = error =>
            {
                logger.LogInformation("podman container ls (stderr): {Error}", error);
            },
            ThrowOnNonZeroReturnCode = false,
            InheritEnv = true
        };

        logger.LogInformation("Running Podman CLI with arguments: {ArgumentList}", spec.Arguments);
        var (pendingProcessResult, processDisposable) = ProcessUtil.Run(spec);

        return CheckPodmanHealthAsync(pendingProcessResult, processDisposable, cancellationToken);

        async Task<bool> CheckPodmanHealthAsync(Task<ProcessResult> pending, IAsyncDisposable disposable, CancellationToken token)
        {
            await using (disposable)
            {
                var processResult = await pending.WaitAsync(token).ConfigureAwait(false);

                if (processResult.ExitCode != 0)
                {
                    logger.LogError("Podman container ls failed with exit code {ExitCode}.", processResult.ExitCode);
                    return false;
                }

                // Optionally, parse processResult.Output for more health checks.
                logger.LogInformation("Podman is running and healthy.");
                return true;
            }
        }
    }

    public async Task TagImageAsync(string localImageName, string targetImageName, CancellationToken cancellationToken)
    {
        var arguments = $"tag \"{localImageName}\" \"{targetImageName}\"";

        var spec = new ProcessSpec("podman")
        {
            Arguments = arguments,
            OnOutputData = output =>
            {
                logger.LogInformation("podman tag (stdout): {Output}", output);
            },
            OnErrorData = error =>
            {
                logger.LogInformation("podman tag (stderr): {Error}", error);
            },
            ThrowOnNonZeroReturnCode = false,
            InheritEnv = true
        };

        logger.LogInformation("Running Podman tag with arguments: {ArgumentList}", spec.Arguments);
        var (pendingProcessResult, processDisposable) = ProcessUtil.Run(spec);

        await using (processDisposable)
        {
            var processResult = await pendingProcessResult
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            if (processResult.ExitCode != 0)
            {
                logger.LogError("podman tag for {LocalImageName} -> {TargetImageName} failed with exit code {ExitCode}.", localImageName, targetImageName, processResult.ExitCode);
                throw new DistributedApplicationException($"Podman tag failed with exit code {processResult.ExitCode}.");
            }

            logger.LogInformation("podman tag for {LocalImageName} -> {TargetImageName} succeeded.", localImageName, targetImageName);
        }
    }

    public async Task PushImageAsync(string imageName, CancellationToken cancellationToken)
    {
        var arguments = $"push \"{imageName}\"";

        var spec = new ProcessSpec("podman")
        {
            Arguments = arguments,
            OnOutputData = output =>
            {
                logger.LogInformation("podman push (stdout): {Output}", output);
            },
            OnErrorData = error =>
            {
                logger.LogInformation("podman push (stderr): {Error}", error);
            },
            ThrowOnNonZeroReturnCode = false,
            InheritEnv = true
        };

        logger.LogInformation("Running Podman push with arguments: {ArgumentList}", spec.Arguments);
        var (pendingProcessResult, processDisposable) = ProcessUtil.Run(spec);

        await using (processDisposable)
        {
            var processResult = await pendingProcessResult
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            if (processResult.ExitCode != 0)
            {
                logger.LogError("podman push for {ImageName} failed with exit code {ExitCode}.", imageName, processResult.ExitCode);
                throw new DistributedApplicationException($"Podman push failed with exit code {processResult.ExitCode}.");
            }

            logger.LogInformation("podman push for {ImageName} succeeded.", imageName);
        }
    }
}

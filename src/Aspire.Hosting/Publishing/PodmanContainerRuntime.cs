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
        var arguments = $"build --file {dockerfilePath} --tag {imageName}";

        // Add platform support if specified
        if (!string.IsNullOrEmpty(options?.TargetPlatform))
        {
            arguments += $" --platform {options.TargetPlatform}";
        }

        // Add format support if specified 
        if (options?.ImageFormat.HasValue == true)
        {
            var format = options.ImageFormat.Value switch
            {
                ContainerImageFormat.OciTar => "oci",
                ContainerImageFormat.DockerTar => "docker-archive", 
                ContainerImageFormat.Docker => "docker",
                _ => throw new ArgumentOutOfRangeException(nameof(options), options.ImageFormat, "Invalid container image format")
            };
            arguments += $" --format {format}";
        }

        arguments += $" {contextPath}";

        // Note: Podman doesn't support --output like Docker buildx, so OutputPath is not directly supported
        // For Podman, users would need to save/export the image separately after building
        if (!string.IsNullOrEmpty(options?.OutputPath))
        {
            logger.LogWarning("OutputPath is not directly supported by Podman build. The image will be built and tagged, but not exported to the specified path.");
        }

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
            Arguments = "info",
            OnOutputData = output =>
            {
                logger.LogInformation("podman info (stdout): {Output}", output);
            },
            OnErrorData = error =>
            {
                logger.LogInformation("podman info (stderr): {Error}", error);
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
                    logger.LogError("Podman info failed with exit code {ExitCode}.", processResult.ExitCode);
                    return false;
                }

                // Optionally, parse processResult.Output for more health checks.
                logger.LogInformation("Podman is running and healthy.");
                return true;
            }
        }
    }
}
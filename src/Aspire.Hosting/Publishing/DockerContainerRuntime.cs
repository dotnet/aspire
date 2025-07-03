// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using Aspire.Hosting.Dcp.Process;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Publishing;

internal sealed class DockerContainerRuntime(ILogger<DockerContainerRuntime> logger) : IContainerRuntime
{
    public string Name => "Docker";
    private async Task<int> RunDockerBuildAsync(string contextPath, string dockerfilePath, string imageName, ContainerBuildOptions? options, CancellationToken cancellationToken)
    {
        var arguments = $"build --file {dockerfilePath} --tag {imageName}";

        // Add platform support if specified
        if (!string.IsNullOrEmpty(options?.TargetPlatform))
        {
            arguments += $" --platform {options.TargetPlatform}";
        }

        // Add output format support if specified
        if (options?.ImageFormat.HasValue == true || !string.IsNullOrEmpty(options?.OutputPath))
        {
            var outputType = options.ImageFormat switch
            {
                ContainerImageFormat.OciTar => "type=oci",
                ContainerImageFormat.DockerTar => "type=docker",
                ContainerImageFormat.Docker => "type=docker",
                null => "type=docker",
                _ => throw new ArgumentOutOfRangeException(nameof(options), options.ImageFormat, "Invalid container image format")
            };

            if (!string.IsNullOrEmpty(options?.OutputPath))
            {
                outputType += $",dest={options.OutputPath}";
            }

            arguments += $" --output {outputType}";
        }

        arguments += $" {contextPath}";

        var spec = new ProcessSpec("docker")
        {
            Arguments = arguments,
            OnOutputData = output =>
            {
                logger.LogInformation("docker build (stdout): {Output}", output);
            },
            OnErrorData = error =>
            {
                logger.LogInformation("docker build (stderr): {Error}", error);
            },
            ThrowOnNonZeroReturnCode = false,
            InheritEnv = true
        };

        logger.LogInformation("Running Docker CLI with arguments: {ArgumentList}", spec.Arguments);
        var (pendingProcessResult, processDisposable) = ProcessUtil.Run(spec);

        await using (processDisposable)
        {
            var processResult = await pendingProcessResult
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            if (processResult.ExitCode != 0)
            {
                logger.LogError("Docker build for {ImageName} failed with exit code {ExitCode}.", imageName, processResult.ExitCode);
                return processResult.ExitCode;
            }

            logger.LogInformation("Docker build for {ImageName} succeeded.", imageName);
            return processResult.ExitCode;
        }
    }

    public async Task BuildImageAsync(string contextPath, string dockerfilePath, string imageName, ContainerBuildOptions? options, CancellationToken cancellationToken)
    {
        var exitCode = await RunDockerBuildAsync(
            contextPath,
            dockerfilePath,
            imageName,
            options,
            cancellationToken).ConfigureAwait(false);

        if (exitCode != 0)
        {
            throw new DistributedApplicationException($"Docker build failed with exit code {exitCode}.");
        }
    }

    public Task<bool> CheckIfRunningAsync(CancellationToken cancellationToken)
    {
        var spec = new ProcessSpec("docker")
        {
            Arguments = "info",
            OnOutputData = output =>
            {
                logger.LogInformation("docker info (stdout): {Output}", output);
            },
            OnErrorData = error =>
            {
                logger.LogInformation("docker info (stderr): {Error}", error);
            },
            ThrowOnNonZeroReturnCode = false,
            InheritEnv = true
        };

        logger.LogInformation("Running Docker CLI with arguments: {ArgumentList}", spec.Arguments);
        var (pendingProcessResult, processDisposable) = ProcessUtil.Run(spec);

        return CheckDockerInfoAsync(pendingProcessResult, processDisposable, cancellationToken);

        async Task<bool> CheckDockerInfoAsync(Task<ProcessResult> pendingResult, IAsyncDisposable processDisposable, CancellationToken ct)
        {
            await using (processDisposable)
            {
                var processResult = await pendingResult.WaitAsync(ct).ConfigureAwait(false);

                if (processResult.ExitCode != 0)
                {
                    logger.LogError("Docker info failed with exit code {ExitCode}.", processResult.ExitCode);
                    return false;
                }

                // Optionally, parse output for health, but exit code 0 is usually sufficient.
                logger.LogInformation("Docker is running and healthy.");
                return true;
            }
        }
    }
}
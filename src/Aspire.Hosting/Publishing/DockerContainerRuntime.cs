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
        string? builderName = null;

        // Docker requires a custom buildkit instance for the image when
        // targeting the OCI format so we construct it and remove it here.
        if (options?.ImageFormat == ContainerImageFormat.Oci)
        {
            if (string.IsNullOrEmpty(options?.OutputPath))
            {
                throw new ArgumentException("OutputPath must be provided when ImageFormat is Oci.", nameof(options));
            }

            builderName = $"{imageName.Replace('/', '-').Replace(':', '-')}-builder";
            var createBuilderResult = await CreateBuildkitInstanceAsync(builderName, cancellationToken).ConfigureAwait(false);

            if (createBuilderResult != 0)
            {
                logger.LogError("Failed to create buildkit instance {BuilderName} with exit code {ExitCode}.", builderName, createBuilderResult);
                return createBuilderResult;
            }
        }

        try
        {
            var arguments = $"buildx build --file \"{dockerfilePath}\" --tag \"{imageName}\"";

            // Use the specific builder for OCI builds
            if (!string.IsNullOrEmpty(builderName))
            {
                arguments += $" --builder \"{builderName}\"";
            }

            // Add platform support if specified
            if (options?.TargetPlatform is not null)
            {
                arguments += $" --platform \"{options.TargetPlatform.Value.ToRuntimePlatformString()}\"";
            }

            // Add output format support if specified
            if (options?.ImageFormat is not null || !string.IsNullOrEmpty(options?.OutputPath))
            {
                var outputType = options?.ImageFormat switch
                {
                    ContainerImageFormat.Oci => "type=oci",
                    ContainerImageFormat.Docker => "type=docker",
                    null => "type=docker",
                    _ => throw new ArgumentOutOfRangeException(nameof(options), options.ImageFormat, "Invalid container image format")
                };

                if (!string.IsNullOrEmpty(options?.OutputPath))
                {
                    outputType += $",dest={Path.Combine(options.OutputPath, imageName)}.tar";
                }

                arguments += $" --output \"{outputType}\"";
            }

            arguments += $" \"{contextPath}\"";

            var spec = new ProcessSpec("docker")
            {
                Arguments = arguments,
                OnOutputData = output =>
                {
                    logger.LogInformation("docker buildx (stdout): {Output}", output);
                },
                OnErrorData = error =>
                {
                    logger.LogInformation("docker buildx (stderr): {Error}", error);
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
                    logger.LogError("docker buildx for {ImageName} failed with exit code {ExitCode}.", imageName, processResult.ExitCode);
                    return processResult.ExitCode;
                }

                logger.LogInformation("docker buildx for {ImageName} succeeded.", imageName);
                return processResult.ExitCode;
            }
        }
        finally
        {
            // Clean up the buildkit instance if we created one
            if (!string.IsNullOrEmpty(builderName))
            {
                await RemoveBuildkitInstanceAsync(builderName, cancellationToken).ConfigureAwait(false);
            }
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
            Arguments = "buildx version",
            OnOutputData = output =>
            {
                logger.LogInformation("docker buildx version (stdout): {Output}", output);
            },
            OnErrorData = error =>
            {
                logger.LogInformation("docker buildx version (stderr): {Error}", error);
            },
            ThrowOnNonZeroReturnCode = false,
            InheritEnv = true
        };

        logger.LogInformation("Running Docker CLI with arguments: {ArgumentList}", spec.Arguments);
        var (pendingProcessResult, processDisposable) = ProcessUtil.Run(spec);

        return CheckDockerBuildxAsync(pendingProcessResult, processDisposable, cancellationToken);

        async Task<bool> CheckDockerBuildxAsync(Task<ProcessResult> pendingResult, IAsyncDisposable processDisposable, CancellationToken ct)
        {
            await using (processDisposable)
            {
                var processResult = await pendingResult.WaitAsync(ct).ConfigureAwait(false);

                if (processResult.ExitCode != 0)
                {
                    logger.LogError("Docker buildx version failed with exit code {ExitCode}.", processResult.ExitCode);
                    return false;
                }

                logger.LogInformation("Docker buildx is available and running.");
                return true;
            }
        }
    }

    private async Task<int> CreateBuildkitInstanceAsync(string builderName, CancellationToken cancellationToken)
    {
        var arguments = $"buildx create --name \"{builderName}\" --driver docker-container";

        var spec = new ProcessSpec("docker")
        {
            Arguments = arguments,
            OnOutputData = output =>
            {
                logger.LogInformation("docker buildx create (stdout): {Output}", output);
            },
            OnErrorData = error =>
            {
                logger.LogInformation("docker buildx create (stderr): {Error}", error);
            },
            ThrowOnNonZeroReturnCode = false,
            InheritEnv = true
        };

        logger.LogInformation("Creating buildkit instance with arguments: {ArgumentList}", spec.Arguments);
        var (pendingProcessResult, processDisposable) = ProcessUtil.Run(spec);

        await using (processDisposable)
        {
            var processResult = await pendingProcessResult
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            if (processResult.ExitCode != 0)
            {
                logger.LogError("Failed to create buildkit instance {BuilderName} with exit code {ExitCode}.", builderName, processResult.ExitCode);
            }
            else
            {
                logger.LogInformation("Successfully created buildkit instance {BuilderName}.", builderName);
            }

            return processResult.ExitCode;
        }
    }

    private async Task<int> RemoveBuildkitInstanceAsync(string builderName, CancellationToken cancellationToken)
    {
        var arguments = $"buildx rm \"{builderName}\"";

        var spec = new ProcessSpec("docker")
        {
            Arguments = arguments,
            OnOutputData = output =>
            {
                logger.LogInformation("docker buildx rm (stdout): {Output}", output);
            },
            OnErrorData = error =>
            {
                logger.LogInformation("docker buildx rm (stderr): {Error}", error);
            },
            ThrowOnNonZeroReturnCode = false,
            InheritEnv = true
        };

        logger.LogInformation("Removing buildkit instance with arguments: {ArgumentList}", spec.Arguments);
        var (pendingProcessResult, processDisposable) = ProcessUtil.Run(spec);

        await using (processDisposable)
        {
            var processResult = await pendingProcessResult
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            if (processResult.ExitCode != 0)
            {
                logger.LogWarning("Failed to remove buildkit instance {BuilderName} with exit code {ExitCode}.", builderName, processResult.ExitCode);
            }
            else
            {
                logger.LogInformation("Successfully removed buildkit instance {BuilderName}.", builderName);
            }

            return processResult.ExitCode;
        }
    }
}

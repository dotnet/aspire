// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES003

using System.Text.Json;
using Aspire.Hosting.Dcp.Process;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Publishing;

internal sealed class DockerContainerRuntime : ContainerRuntimeBase<DockerContainerRuntime>
{
    public DockerContainerRuntime(ILogger<DockerContainerRuntime> logger) : base(logger)
    {
    }

    protected override string RuntimeExecutable => "docker";
    public override string Name => "Docker";
    private async Task<int> RunDockerBuildAsync(string contextPath, string dockerfilePath, string imageName, ContainerBuildOptions? options, Dictionary<string, string?> buildArguments, Dictionary<string, string?> buildSecrets, string? stage, CancellationToken cancellationToken)
    {
        string? builderName = null;
        var resourceName = imageName.Replace('/', '-').Replace(':', '-');

        // Docker requires a custom buildkit instance for the image when
        // targeting the OCI format so we construct it and remove it here.
        if (options?.ImageFormat == ContainerImageFormat.Oci)
        {
            if (string.IsNullOrEmpty(options?.OutputPath))
            {
                throw new ArgumentException("OutputPath must be provided when ImageFormat is Oci.", nameof(options));
            }

            builderName = $"{resourceName}-builder";
            var createBuilderResult = await CreateBuildkitInstanceAsync(builderName, cancellationToken).ConfigureAwait(false);

            if (createBuilderResult != 0)
            {
                Logger.LogError("Failed to create buildkit instance {BuilderName} with exit code {ExitCode}.", builderName, createBuilderResult);
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
                    outputType += $",dest={Path.Combine(options.OutputPath, resourceName)}.tar";
                }

                arguments += $" --output \"{outputType}\"";
            }

            // Add build arguments if specified
            arguments += BuildArgumentsString(buildArguments);

            // Add build secrets if specified
            arguments += BuildSecretsString(buildSecrets);

            // Add stage if specified
            arguments += BuildStageString(stage);

            arguments += $" \"{contextPath}\"";

            var spec = new ProcessSpec("docker")
            {
                Arguments = arguments,
                OnOutputData = output =>
                {
                    Logger.LogDebug("docker buildx (stdout): {Output}", output);
                },
                OnErrorData = error =>
                {
                    Logger.LogDebug("docker buildx (stderr): {Error}", error);
                },
                ThrowOnNonZeroReturnCode = false,
                InheritEnv = true,
            };

            // Add build secrets as environment variables
            foreach (var buildSecret in buildSecrets)
            {
                if (buildSecret.Value is not null)
                {
                    spec.EnvironmentVariables[buildSecret.Key.ToUpperInvariant()] = buildSecret.Value;
                }
            }

            Logger.LogDebug("Running Docker CLI with arguments: {ArgumentList}", spec.Arguments);
            var (pendingProcessResult, processDisposable) = ProcessUtil.Run(spec);

            await using (processDisposable)
            {
                var processResult = await pendingProcessResult
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (processResult.ExitCode != 0)
                {
                    Logger.LogError("docker buildx for {ImageName} failed with exit code {ExitCode}.", imageName, processResult.ExitCode);
                    return processResult.ExitCode;
                }

                Logger.LogInformation("docker buildx for {ImageName} succeeded.", imageName);
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

    public override async Task BuildImageAsync(string contextPath, string dockerfilePath, string imageName, ContainerBuildOptions? options, Dictionary<string, string?> buildArguments, Dictionary<string, string?> buildSecrets, string? stage, CancellationToken cancellationToken)
    {
        // Normalize the context path to handle trailing slashes and relative paths
        var normalizedContextPath = Path.GetFullPath(contextPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var exitCode = await RunDockerBuildAsync(
            normalizedContextPath,
            dockerfilePath,
            imageName,
            options,
            buildArguments,
            buildSecrets,
            stage,
            cancellationToken).ConfigureAwait(false);

        if (exitCode != 0)
        {
            throw new DistributedApplicationException($"Docker build failed with exit code {exitCode}.");
        }
    }

    public override async Task<bool> CheckIfRunningAsync(CancellationToken cancellationToken)
    {
        // First check if Docker daemon is running using the same check that DCP uses
        if (!await CheckDockerDaemonAsync(cancellationToken).ConfigureAwait(false))
        {
            return false;
        }

        // Then check if Docker buildx is available
        return await CheckDockerBuildxAsync(cancellationToken).ConfigureAwait(false);
    }

    public override async Task<bool> SupportsMultiArchAsync(CancellationToken cancellationToken)
    {
        try
        {
            var outputBuilder = new System.Text.StringBuilder();
            
            // Run docker info --format json to get Docker daemon information
            var spec = new ProcessSpec(RuntimeExecutable)
            {
                Arguments = "info --format json",
                OnOutputData = output => outputBuilder.AppendLine(output),
                OnErrorData = _ => { },
                ThrowOnNonZeroReturnCode = false,
                InheritEnv = true,
            };

            Logger.LogDebug("Checking Docker multi-arch support using 'docker info --format json'");
            
            var (pendingProcessResult, processDisposable) = ProcessUtil.Run(spec);

            await using (processDisposable)
            {
                var processResult = await pendingProcessResult
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (processResult.ExitCode != 0)
                {
                    Logger.LogWarning("Failed to get Docker info. Exit code: {ExitCode}", processResult.ExitCode);
                    return false;
                }

                var dockerInfo = outputBuilder.ToString();
                
                // Parse JSON to check for containerd image store
                try
                {
                    using var jsonDoc = JsonDocument.Parse(dockerInfo);
                    var root = jsonDoc.RootElement;

                    // Check if containerd is configured as the image store
                    // The containerd image store is indicated by the presence of Containerd configuration
                    if (root.TryGetProperty("DriverStatus", out var driverStatus) && driverStatus.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in driverStatus.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.Array && item.GetArrayLength() >= 2)
                            {
                                var key = item[0].GetString();
                                var value = item[1].GetString();
                                
                                // Check for "Image store" entry which indicates containerd image store
                                if (key == "Image store" && value == "containerd")
                                {
                                    Logger.LogDebug("Docker is configured with containerd image store. Multi-arch builds are supported.");
                                    return true;
                                }
                            }
                        }
                    }

                    // If we have Containerd configuration with image store support, multi-arch is supported
                    if (root.TryGetProperty("Containerd", out _))
                    {
                        // Check if we're using the containerd image store through the Driver field
                        if (root.TryGetProperty("Driver", out var driver))
                        {
                            var driverValue = driver.GetString();
                            // When containerd image store is enabled, the driver might be "containerd" or we need additional checks
                            if (driverValue == "containerd")
                            {
                                Logger.LogDebug("Docker is using containerd driver. Multi-arch builds are supported.");
                                return true;
                            }
                        }
                    }

                    // If containerd image store is not configured, log a warning
                    Logger.LogWarning(
                        "Docker does not appear to be configured with containerd image store. " +
                        "Multi-arch builds require containerd image store to be enabled. " +
                        "You can enable it in Docker Desktop settings under 'Features in development' > 'Use containerd for pulling and storing images'. " +
                        "See https://docs.docker.com/engine/storage/containerd/ for more information.");
                    
                    return false;
                }
                catch (JsonException ex)
                {
                    Logger.LogWarning(ex, "Failed to parse Docker info JSON output");
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error checking Docker multi-arch support");
            return false;
        }
    }

    private async Task<bool> CheckDockerDaemonAsync(CancellationToken cancellationToken)
    {
        try
        {
            var exitCode = await ExecuteContainerCommandWithExitCodeAsync(
                "container ls -n 1",
                "Docker daemon is not running. Exit code: {ExitCode}.",
                "Docker daemon is running.",
                cancellationToken,
                Array.Empty<object>()).ConfigureAwait(false);
            
            return exitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> CheckDockerBuildxAsync(CancellationToken cancellationToken)
    {
        try
        {
            var exitCode = await ExecuteContainerCommandWithExitCodeAsync(
                "buildx version",
                "Docker buildx version failed with exit code {ExitCode}.",
                "Docker buildx is available and running.",
                cancellationToken,
                Array.Empty<object>()).ConfigureAwait(false);
            
            return exitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private async Task<int> CreateBuildkitInstanceAsync(string builderName, CancellationToken cancellationToken)
    {
        var arguments = $"buildx create --name \"{builderName}\" --driver docker-container";

        return await ExecuteContainerCommandWithExitCodeAsync(
            arguments,
            "Failed to create buildkit instance {BuilderName} with exit code {ExitCode}.",
            "Successfully created buildkit instance {BuilderName}.",
            cancellationToken,
            new object[] { builderName }).ConfigureAwait(false);
    }

    private async Task<int> RemoveBuildkitInstanceAsync(string builderName, CancellationToken cancellationToken)
    {
        var arguments = $"buildx rm \"{builderName}\"";

        return await ExecuteContainerCommandWithExitCodeAsync(
            arguments,
            "Failed to remove buildkit instance {BuilderName} with exit code {ExitCode}.",
            "Successfully removed buildkit instance {BuilderName}.",
            cancellationToken,
            new object[] { builderName }).ConfigureAwait(false);
    }
}

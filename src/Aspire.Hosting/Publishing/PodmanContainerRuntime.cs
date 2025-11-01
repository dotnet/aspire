// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES003

using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Publishing;

internal sealed class PodmanContainerRuntime : ContainerRuntimeBase<PodmanContainerRuntime>
{
    public PodmanContainerRuntime(ILogger<PodmanContainerRuntime> logger) : base(logger)
    {
    }

    protected override string RuntimeExecutable => "podman";
    public override string Name => "Podman";
    private async Task<int> RunPodmanBuildAsync(string contextPath, string dockerfilePath, string imageName, ContainerBuildOptions? options, Dictionary<string, string?> buildArguments, Dictionary<string, string?> buildSecrets, string? stage, CancellationToken cancellationToken)
    {
        // Check if this is a multi-platform build
        var isMultiPlatform = options?.TargetPlatform is not null && 
                              HasMultiplePlatforms(options.TargetPlatform.Value);

        string? manifestName = null;
        
        if (isMultiPlatform)
        {
            // For multi-platform builds, create a manifest
            manifestName = imageName;
            var createManifestResult = await CreateManifestAsync(manifestName, cancellationToken).ConfigureAwait(false);
            
            if (createManifestResult != 0)
            {
                Logger.LogError("Failed to create manifest {ManifestName} with exit code {ExitCode}.", manifestName, createManifestResult);
                return createManifestResult;
            }
        }

        try
        {
            var arguments = $"build --file \"{dockerfilePath}\" --tag \"{imageName}\"";

            // If we have a manifest, use it
            if (manifestName is not null)
            {
                arguments += $" --manifest \"{manifestName}\"";
            }

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

            // Add build arguments if specified
            arguments += BuildArgumentsString(buildArguments);

            // Add build secrets if specified
            arguments += BuildSecretsString(buildSecrets, requireValue: true);

            // Add stage if specified
            arguments += BuildStageString(stage);

            arguments += $" \"{contextPath}\"";

            // Prepare environment variables for build secrets
            var environmentVariables = new Dictionary<string, string>();
            foreach (var buildSecret in buildSecrets)
            {
                if (buildSecret.Value is not null)
                {
                    environmentVariables[buildSecret.Key.ToUpperInvariant()] = buildSecret.Value;
                }
            }

            return await ExecuteContainerCommandWithExitCodeAsync(
                arguments,
                "Podman build for {ImageName} failed with exit code {ExitCode}.",
                "Podman build for {ImageName} succeeded.",
                cancellationToken,
                new object[] { imageName },
                environmentVariables).ConfigureAwait(false);
        }
        finally
        {
            // Clean up the manifest if we created one
            if (manifestName is not null && isMultiPlatform)
            {
                await RemoveManifestAsync(manifestName, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static bool HasMultiplePlatforms(ContainerTargetPlatform platform)
    {
        // Count how many flags are set
        var count = 0;
        if (platform.HasFlag(ContainerTargetPlatform.LinuxAmd64))
        {
            count++;
        }
        if (platform.HasFlag(ContainerTargetPlatform.LinuxArm64))
        {
            count++;
        }
        if (platform.HasFlag(ContainerTargetPlatform.LinuxArm))
        {
            count++;
        }
        if (platform.HasFlag(ContainerTargetPlatform.Linux386))
        {
            count++;
        }
        if (platform.HasFlag(ContainerTargetPlatform.WindowsAmd64))
        {
            count++;
        }
        if (platform.HasFlag(ContainerTargetPlatform.WindowsArm64))
        {
            count++;
        }
        
        return count > 1;
    }

    private async Task<int> CreateManifestAsync(string manifestName, CancellationToken cancellationToken)
    {
        var arguments = $"manifest create \"{manifestName}\"";

        return await ExecuteContainerCommandWithExitCodeAsync(
            arguments,
            "Failed to create manifest {ManifestName} with exit code {ExitCode}.",
            "Successfully created manifest {ManifestName}.",
            cancellationToken,
            new object[] { manifestName }).ConfigureAwait(false);
    }

    private async Task<int> RemoveManifestAsync(string manifestName, CancellationToken cancellationToken)
    {
        var arguments = $"manifest rm \"{manifestName}\"";

        return await ExecuteContainerCommandWithExitCodeAsync(
            arguments,
            "Failed to remove manifest {ManifestName} with exit code {ExitCode}.",
            "Successfully removed manifest {ManifestName}.",
            cancellationToken,
            new object[] { manifestName }).ConfigureAwait(false);
    }

    public override async Task BuildImageAsync(string contextPath, string dockerfilePath, string imageName, ContainerBuildOptions? options, Dictionary<string, string?> buildArguments, Dictionary<string, string?> buildSecrets, string? stage, CancellationToken cancellationToken)
    {
        var exitCode = await RunPodmanBuildAsync(
            contextPath,
            dockerfilePath,
            imageName,
            options,
            buildArguments,
            buildSecrets,
            stage,
            cancellationToken).ConfigureAwait(false);

        if (exitCode != 0)
        {
            throw new DistributedApplicationException($"Podman build failed with exit code {exitCode}.");
        }
    }

    public override async Task<bool> CheckIfRunningAsync(CancellationToken cancellationToken)
    {
        try
        {
            var exitCode = await ExecuteContainerCommandWithExitCodeAsync(
                "container ls -n 1",
                "Podman container ls failed with exit code {ExitCode}.",
                "Podman is running and healthy.",
                cancellationToken,
                Array.Empty<object>()).ConfigureAwait(false);
            
            return exitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public override async Task<bool> SupportsMultiArchAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Check if podman manifest command is available
            var exitCode = await ExecuteContainerCommandWithExitCodeAsync(
                "manifest --help",
                "Podman manifest command failed with exit code {ExitCode}.",
                "Podman manifest command is available.",
                cancellationToken,
                Array.Empty<object>()).ConfigureAwait(false);
            
            if (exitCode == 0)
            {
                Logger.LogDebug("Podman supports manifest commands. Multi-arch builds are supported.");
                return true;
            }
            else
            {
                Logger.LogWarning(
                    "Podman does not support manifest commands. " +
                    "Multi-arch builds require manifest support. " +
                    "Please ensure you are using a recent version of Podman.");
                return false;
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error checking Podman multi-arch support");
            return false;
        }
    }
}

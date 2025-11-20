// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE001
#pragma warning disable ASPIRECONTAINERRUNTIME001
#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREPIPELINES003

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp.Process;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Publishing;

/// <summary>
/// Provides a service to publishers for building containers that represent a resource.
/// </summary>
[Experimental("ASPIREPIPELINES003", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public interface IResourceContainerImageBuilder
{
    /// <summary>
    /// Builds a container that represents the specified resource.
    /// </summary>
    /// <param name="resource">The resource to build.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task BuildImageAsync(IResource resource, CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds container images for a collection of resources.
    /// </summary>
    /// <param name="resources">The resources to build images for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task BuildImagesAsync(IEnumerable<IResource> resources, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pushes a container image to a registry.
    /// </summary>
    /// <param name="imageName">The name of the image to push.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task PushImageAsync(string imageName, CancellationToken cancellationToken = default);
}

internal sealed class ResourceContainerImageBuilder(
    ILogger<ResourceContainerImageBuilder> logger,
    IContainerRuntime containerRuntime,
    IServiceProvider serviceProvider) : IResourceContainerImageBuilder
{
    // Disable concurrent builds for project resources to avoid issues with overlapping msbuild projects
    private readonly SemaphoreSlim _throttle = new(1);

    private IContainerRuntime ContainerRuntime { get; } = containerRuntime;

    public async Task BuildImagesAsync(IEnumerable<IResource> resources, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting to build container images");

        // Only check container runtime health if there are resources that need it
        if (ResourcesRequireContainerRuntime(resources))
        {
            logger.LogDebug("Checking {ContainerRuntimeName} health", ContainerRuntime.Name);

            var containerRuntimeHealthy = await ContainerRuntime.CheckIfRunningAsync(cancellationToken).ConfigureAwait(false);

            if (!containerRuntimeHealthy)
            {
                logger.LogError("Container runtime is not running or is unhealthy. Cannot build container images.");
                throw new InvalidOperationException("Container runtime is not running or is unhealthy.");
            }

            logger.LogDebug("{ContainerRuntimeName} is healthy", ContainerRuntime.Name);
        }

        foreach (var resource in resources)
        {
            // TODO: Consider parallelizing this.
            await BuildImageAsync(resource, cancellationToken).ConfigureAwait(false);
        }

        logger.LogDebug("Building container images completed");
    }

    public async Task BuildImageAsync(IResource resource, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Building container image for resource {ResourceName}", resource.Name);

        resource.TryGetLastAnnotation<ContainerImageOptionsAnnotation>(out var options);

        if (resource is ProjectResource)
        {
            // If it is a project resource we need to build the container image
            // using the .NET SDK.
            await BuildProjectContainerImageAsync(
                resource,
                options,
                cancellationToken).ConfigureAwait(false);
            return;
        }
        else if (resource.TryGetLastAnnotation<DockerfileBuildAnnotation>(out var dockerfileBuildAnnotation))
        {
            if (!resource.TryGetContainerImageName(out var imageName))
            {
                throw new InvalidOperationException("Resource image name could not be determined.");
            }

            // This is a container resource so we'll use the container runtime to build the image
            await BuildContainerImageFromDockerfileAsync(
                resource,
                dockerfileBuildAnnotation,
                imageName,
                options,
                cancellationToken).ConfigureAwait(false);
            return;
        }
        else if (resource.TryGetLastAnnotation<ContainerImageAnnotation>(out var _))
        {
            // This resource already has a container image associated with it so no build is needed.
            logger.LogDebug("Resource {ResourceName} already has a container image associated and no build annotation. Skipping build.", resource.Name);
            return;
        }
        else
        {
            throw new NotSupportedException($"The resource type '{resource.GetType().Name}' is not supported.");
        }
    }

    private async Task BuildProjectContainerImageAsync(IResource resource, ContainerImageOptionsAnnotation? options, CancellationToken cancellationToken)
    {
        await _throttle.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {

            logger.LogInformation("Building image: {ResourceName}", resource.Name);

            var success = await ExecuteDotnetPublishAsync(resource, options, cancellationToken).ConfigureAwait(false);

            if (!success)
            {
                logger.LogError("Building image for {ResourceName} failed", resource.Name);
                throw new DistributedApplicationException($"Failed to build container image.");
            }
            else
            {
                logger.LogInformation("Building image for {ResourceName} completed", resource.Name);
            }
        }
        finally
        {
            _throttle.Release();
        }
    }

    private async Task<bool> ExecuteDotnetPublishAsync(IResource resource, ContainerImageOptionsAnnotation? options, CancellationToken cancellationToken)
    {
        // This is a resource project so we'll use the .NET SDK to build the container image.
        if (!resource.TryGetLastAnnotation<IProjectMetadata>(out var projectMetadata))
        {
            throw new DistributedApplicationException($"The resource '{projectMetadata}' does not have a project metadata annotation.");
        }

        // Get the image name from the resource using TryGetContainerImageName
        string containerRepository;
        string? containerImageTag = null;

        if (resource.TryGetContainerImageName(out var fullImageName))
        {
            // Parse the full image name to extract repository and tag
            var colonIndex = fullImageName.LastIndexOf(':');
            if (colonIndex > 0)
            {
                containerRepository = fullImageName.Substring(0, colonIndex);
                containerImageTag = fullImageName.Substring(colonIndex + 1);
            }
            else
            {
                containerRepository = fullImageName;
            }
        }
        else
        {
            // Fallback to resource name if TryGetContainerImageName fails
            containerRepository = resource.Name.ToLowerInvariant();
        }

        var arguments = $"publish \"{projectMetadata.ProjectPath}\" --configuration Release /t:PublishContainer /p:ContainerRepository=\"{containerRepository}\"";

        // Add image tag if available
        if (!string.IsNullOrEmpty(containerImageTag))
        {
            arguments += $" /p:ContainerImageTag=\"{containerImageTag}\"";
        }

        // Add additional arguments based on options
        if (options is not null)
        {

            if (!string.IsNullOrEmpty(options.OutputPath))
            {
                arguments += $" /p:ContainerArchiveOutputPath=\"{options.OutputPath}\"";
            }

            if (options.ImageFormat is not null)
            {
                var format = options.ImageFormat.Value switch
                {
                    ContainerImageFormat.Docker => "Docker",
                    ContainerImageFormat.Oci => "OCI",
                    _ => throw new ArgumentOutOfRangeException(nameof(options), options.ImageFormat, "Invalid container image format")
                };
                arguments += $" /p:ContainerImageFormat=\"{format}\"";
            }

            if (options.TargetPlatform is not null)
            {
                // Use the appropriate MSBuild properties based on the number of RIDs
                var runtimeIds = options.TargetPlatform.Value.ToMSBuildRuntimeIdentifierString();
                var ridArray = runtimeIds.Split(';');

                if (ridArray.Length == 1)
                {
                    // Single platform - use RuntimeIdentifier/ContainerRuntimeIdentifier
                    arguments += $" /p:RuntimeIdentifier=\"{ridArray[0]}\"";
                    arguments += $" /p:ContainerRuntimeIdentifier=\"{ridArray[0]}\"";
                }
                else
                {
                    // Multiple platforms - use RuntimeIdentifiers/ContainerRuntimeIdentifiers
                    // MSBuild doesn't handle ';' in parameters well, need to escape the double quote. See https://github.com/dotnet/msbuild/issues/471
                    arguments += $" /p:RuntimeIdentifiers=\\\"{runtimeIds}\\\"";
                    arguments += $" /p:ContainerRuntimeIdentifiers=\\\"{runtimeIds}\\\"";
                }
            }
        }

#pragma warning disable ASPIREDOCKERFILEBUILDER001
        if (resource.TryGetLastAnnotation<DockerfileBaseImageAnnotation>(out var baseImageAnnotation) &&
            baseImageAnnotation.RuntimeImage is string baseImage)
        {
            arguments += $" /p:ContainerBaseImage=\"{baseImage}\"";
        }
#pragma warning restore ASPIREDOCKERFILEBUILDER001

        var spec = new ProcessSpec("dotnet")
        {
            Arguments = arguments,
            OnOutputData = output =>
            {
                logger.LogDebug("dotnet publish {ProjectPath} (stdout): {Output}", projectMetadata.ProjectPath, output);
            },
            OnErrorData = error =>
            {
                logger.LogDebug("dotnet publish {ProjectPath} (stderr): {Error}", projectMetadata.ProjectPath, error);
            }
        };

        logger.LogDebug(
            "Starting .NET CLI with arguments: {Arguments}",
            string.Join(" ", spec.Arguments)
            );

        var (pendingProcessResult, processDisposable) = ProcessUtil.Run(spec);

        await using (processDisposable)
        {
            var processResult = await pendingProcessResult
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            if (processResult.ExitCode != 0)
            {
                logger.LogError("dotnet publish for project {ProjectPath} failed with exit code {ExitCode}.", projectMetadata.ProjectPath, processResult.ExitCode);
                return false;
            }
            else
            {
                logger.LogDebug(
                    ".NET CLI completed with exit code: {ExitCode}",
                    processResult.ExitCode);
                return true;
            }
        }
    }

    private async Task BuildContainerImageFromDockerfileAsync(IResource resource, DockerfileBuildAnnotation dockerfileBuildAnnotation, string imageName, ContainerImageOptionsAnnotation? options, CancellationToken cancellationToken)
    {
        logger.LogInformation("Building image: {ResourceName}", resource.Name);

        // If there's a factory, generate the Dockerfile content and write it to the specified path
        if (dockerfileBuildAnnotation.DockerfileFactory is not null)
        {
            var context = new DockerfileFactoryContext
            {
                Services = serviceProvider,
                Resource = resource,
                CancellationToken = cancellationToken
            };
            var dockerfileContent = await dockerfileBuildAnnotation.DockerfileFactory(context).ConfigureAwait(false);
            await File.WriteAllTextAsync(dockerfileBuildAnnotation.DockerfilePath, dockerfileContent, cancellationToken).ConfigureAwait(false);
        }

        // Resolve build arguments
        var resolvedBuildArguments = new Dictionary<string, string?>();
        foreach (var buildArg in dockerfileBuildAnnotation.BuildArguments)
        {
            resolvedBuildArguments[buildArg.Key] = await ResolveValue(buildArg.Value, cancellationToken).ConfigureAwait(false);
        }

        // Resolve build secrets
        var resolvedBuildSecrets = new Dictionary<string, string?>();
        foreach (var buildSecret in dockerfileBuildAnnotation.BuildSecrets)
        {
            resolvedBuildSecrets[buildSecret.Key] = await ResolveValue(buildSecret.Value, cancellationToken).ConfigureAwait(false);
        }

        // ensure outputPath is created if specified since docker/podman won't create it for us
        if (options?.OutputPath is { } outputPath)
        {
            Directory.CreateDirectory(outputPath);
        }

        try
        {
            await ContainerRuntime.BuildImageAsync(
                dockerfileBuildAnnotation.ContextPath,
                dockerfileBuildAnnotation.DockerfilePath,
                imageName,
                options,
                resolvedBuildArguments,
                resolvedBuildSecrets,
                dockerfileBuildAnnotation.Stage,
                cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Building image for {ResourceName} completed", resource.Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to build container image from Dockerfile for {ResourceName}", resource.Name);
            throw;
        }
    }

    internal static async Task<string?> ResolveValue(object? value, CancellationToken cancellationToken)
    {
        try
        {
            return value switch
            {
                FileInfo filePath => filePath.FullName,
                string stringValue => stringValue,
                IValueProvider valueProvider => await valueProvider.GetValueAsync(cancellationToken).ConfigureAwait(false),
                bool boolValue => boolValue ? "true" : "false",
                null => null,
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString()
            };
        }
        catch (MissingParameterValueException)
        {
            // If a parameter value is missing, we return null to indicate that the build argument or secret cannot be resolved
            // and we should fallback to resolving it from environment variables.
            return null;
        }
    }

    public async Task PushImageAsync(string imageName, CancellationToken cancellationToken = default)
    {
        await ContainerRuntime.PushImageAsync(imageName, cancellationToken).ConfigureAwait(false);
    }

    // .NET Container builds that push OCI images to a local file path do not need a runtime
    internal static bool ResourcesRequireContainerRuntime(IEnumerable<IResource> resources)
    {
        foreach (var resource in resources)
        {
            var hasDockerfileResource = resource.TryGetLastAnnotation<ContainerImageAnnotation>(out _) &&
                                       resource.TryGetLastAnnotation<DockerfileBuildAnnotation>(out _);
            if (hasDockerfileResource)
            {
                return true;
            }

            if (resource.TryGetLastAnnotation<ContainerImageOptionsAnnotation>(out var options))
            {

                var usesDocker = options?.ImageFormat == null || options.ImageFormat == ContainerImageFormat.Docker;
                var hasNoOutputPath = options?.OutputPath == null;
                if (usesDocker || hasNoOutputPath)
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }

        return false;
    }
}

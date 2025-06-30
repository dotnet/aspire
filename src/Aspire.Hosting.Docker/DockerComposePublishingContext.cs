// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Docker.Resources;
using Aspire.Hosting.Docker.Resources.ComposeNodes;
using Aspire.Hosting.Docker.Resources.ServiceNodes;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Docker;

/// <summary>
/// Represents a context for publishing Docker Compose configurations for a distributed application.
/// </summary>
/// <remarks>
/// This context facilitates the generation of Docker Compose files using the provided application model,
/// publisher options, and execution context. It handles the allocation of ports for services and ensures
/// that the Docker Compose configuration file is created in the specified output path.
/// </remarks>
internal sealed class DockerComposePublishingContext(
    DistributedApplicationExecutionContext executionContext,
    IResourceContainerImageBuilder imageBuilder,
    string outputPath,
    ILogger logger,
    IPublishingActivityProgressReporter progressReporter,
    CancellationToken cancellationToken = default)
{
    private const UnixFileMode DefaultUmask = UnixFileMode.GroupExecute | UnixFileMode.GroupWrite | UnixFileMode.OtherExecute | UnixFileMode.OtherWrite;
    private const UnixFileMode MaxDefaultFilePermissions = UnixFileMode.UserRead | UnixFileMode.UserWrite |
        UnixFileMode.GroupRead | UnixFileMode.GroupWrite |
        UnixFileMode.OtherRead | UnixFileMode.OtherWrite;

    public readonly IResourceContainerImageBuilder ImageBuilder = imageBuilder;
    public readonly string OutputPath = outputPath;

    internal async Task WriteModelAsync(DistributedApplicationModel model, DockerComposeEnvironmentResource environment)
    {
        if (!executionContext.IsPublishMode)
        {
            logger.NotInPublishingMode();
            return;
        }

        logger.StartGeneratingDockerCompose();

        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(OutputPath);

        if (model.Resources.Count == 0)
        {
            logger.EmptyModel();
            return;
        }

        await WriteDockerComposeOutputAsync(model, environment).ConfigureAwait(false);

        logger.FinishGeneratingDockerCompose(OutputPath);
    }

    private async Task WriteDockerComposeOutputAsync(DistributedApplicationModel model, DockerComposeEnvironmentResource environment)
    {
        var defaultNetwork = new Network
        {
            Name = environment.DefaultNetworkName ?? "aspire",
            Driver = "bridge",
        };

        var composeFile = new ComposeFile();
        composeFile.AddNetwork(defaultNetwork);

        IEnumerable<IResource> resources = environment.Dashboard?.Resource is IResource r
                ? [r, .. model.Resources]
                : model.Resources;

        var containerImagesToBuild = new List<IResource>();

        foreach (var resource in resources)
        {
            if (resource.GetDeploymentTargetAnnotation(environment)?.DeploymentTarget is DockerComposeServiceResource serviceResource)
            {
                if (environment.BuildContainerImages)
                {
                    containerImagesToBuild.Add(serviceResource.TargetResource);
                }

                var composeService = serviceResource.BuildComposeService();

                HandleComposeFileVolumes(serviceResource, composeFile);

                composeService.Networks =
                [
                    defaultNetwork.Name,
                ];

                if (serviceResource.TargetResource.TryGetAnnotationsOfType<ContainerFileSystemCallbackAnnotation>(out var fsAnnotations))
                {
                    foreach (var a in fsAnnotations)
                    {
                        var files = await a.Callback(new() { Model = serviceResource.TargetResource, ServiceProvider = executionContext.ServiceProvider }, CancellationToken.None).ConfigureAwait(false);
                        foreach (var file in files)
                        {
                            HandleComposeFileConfig(composeFile, composeService, file, a.DefaultOwner, a.DefaultGroup, a.Umask ?? DefaultUmask, a.DestinationPath);
                        }
                    }
                }

                if (serviceResource.TargetResource.TryGetAnnotationsOfType<DockerComposeServiceCustomizationAnnotation>(out var annotations))
                {
                    foreach (var a in annotations)
                    {
                        a.Configure(serviceResource, composeService);
                    }
                }

                composeFile.AddService(composeService);
            }
        }

        // Build container images for the services that require it
        if (containerImagesToBuild.Count > 0)
        {
            await ImageBuilder.BuildImagesAsync(containerImagesToBuild, cancellationToken).ConfigureAwait(false);
        }

        var step = await progressReporter.CreateStepAsync(
            "Writing Docker Compose file.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var task = await step.CreateTaskAsync(
            "Writing the Docker Compose file to the output path.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        // Call the environment's ConfigureComposeFile method to allow for custom modifications
        environment.ConfigureComposeFile?.Invoke(composeFile);

        var composeOutput = composeFile.ToYaml();
        var outputFile = Path.Combine(OutputPath, "docker-compose.yaml");
        Directory.CreateDirectory(OutputPath);
        await File.WriteAllTextAsync(outputFile, composeOutput, cancellationToken).ConfigureAwait(false);

        if (environment.CapturedEnvironmentVariables.Count > 0)
        {
            // Write a .env file with the environment variable names
            // that are used in the compose file
            var envFilePath = Path.Combine(OutputPath, ".env");
            var envFile = EnvFile.Load(envFilePath);

            foreach (var entry in environment.CapturedEnvironmentVariables ?? [])
            {
                var (key, (description, defaultValue, _)) = entry;
                envFile.AddIfMissing(key, defaultValue, description);
            }

            envFile.Save(envFilePath);
        }

        await task.SucceedAsync(
            $"Docker Compose file written successfully to {outputFile}.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        await step.SucceedAsync(
            "Docker Compose file generation completed.",
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private void HandleComposeFileConfig(ComposeFile composeFile, Service composeService, ContainerFileSystemItem? item, int? uid, int? gid, UnixFileMode umask, string path)
    {
        if (item is ContainerDirectory dir)
        {
            foreach (var dirItem in dir.Entries)
            {
                HandleComposeFileConfig(composeFile, composeService, dirItem, item.Owner ?? uid, item.Group ?? gid, umask, path += "/" + item.Name);
            }

            return;
        }

        if (item is ContainerFile file)
        {
            var name = composeService.Name + "_" + path.Replace('/', '_') + "_" + file.Name;

            // If there is a source path, we should copy the file to the output path and use that instead.
            string? sourcePath = null;
            if (!string.IsNullOrEmpty(file.SourcePath))
            {
                try
                {
                    // Determine the path to copy the file to
                    sourcePath = Path.Combine(OutputPath, composeService.Name, Path.GetFileName(file.SourcePath));
                    // Files will be copied to a subdirectory named after the service
                    Directory.CreateDirectory(Path.Combine(OutputPath, composeService.Name));
                    File.Copy(file.SourcePath, sourcePath);
                    // Use a relative path for the compose file to make it portable
                    // Use unix style path separators even on Windows
                    sourcePath = Path.GetRelativePath(OutputPath, sourcePath).Replace('\\', '/');
                }
                catch
                {
                    logger.FailedToCopyFile(file.SourcePath, OutputPath);
                    throw;
                }
            }

            composeFile.AddConfig(new()
            {
                Name = name,
                File = sourcePath,
                Content = file.Contents,
            });

            composeService.AddConfig(new()
            {
                Source = name,
                Target = path + "/" + file.Name,
                Uid = (item.Owner ?? uid)?.ToString(CultureInfo.InvariantCulture),
                Gid = (item.Group ?? gid)?.ToString(CultureInfo.InvariantCulture),
                Mode = item.Mode != 0 ? item.Mode : MaxDefaultFilePermissions & ~umask,
            });

            return;
        }
    }

    private void HandleComposeFileVolumes(DockerComposeServiceResource serviceResource, ComposeFile composeFile)
    {
        foreach (var volume in serviceResource.Volumes)
        {
            if (volume.Type == "bind")
            {
                // Handle bind mounts by copying them to the output folder and updating the path
                HandleBindMount(serviceResource, volume);
            }
            else
            {
                // Handle regular volumes
                if (composeFile.Volumes.ContainsKey(volume.Name))
                {
                    continue;
                }

                var newVolume = new Volume
                {
                    Name = volume.Name,
                    Driver = volume.Driver ?? "local",
                    External = volume.External,
                };

                composeFile.AddVolume(newVolume);
            }
        }
    }

    private void HandleBindMount(DockerComposeServiceResource serviceResource, Resources.ServiceNodes.Volume volume)
    {
        if (volume.Source is null || volume.Target is null)
        {
            return;
        }

        // Special handling for Docker socket - allow it to pass through without copying
        if (IsDockerSocket(volume.Source))
        {
            logger.LogDebug("Docker socket bind mount '{Source}' will be passed through without copying", volume.Source);
            return;
        }

        // Check for named pipes and warn
        if (IsNamedPipe(volume.Source))
        {
            logger.LogWarning("Named pipe '{Source}' detected in bind mount. Named pipes may not work correctly in all Docker Compose environments", volume.Source);
            return;
        }

        try
        {
            // Determine the destination path to copy the bind mount to
            var serviceName = serviceResource.TargetResource.Name.ToLowerInvariant();
            var bindMountDirectory = Path.Combine(OutputPath, serviceName, "bindmounts");
            var destinationPath = Path.Combine(bindMountDirectory, Path.GetFileName(volume.Source));

            // Create the directory structure
            Directory.CreateDirectory(bindMountDirectory);

            // Copy the source to the destination
            if (File.Exists(volume.Source))
            {
                File.Copy(volume.Source, destinationPath, overwrite: true);
            }
            else if (Directory.Exists(volume.Source))
            {
                CopyDirectory(volume.Source, destinationPath);
            }
            else
            {
                logger.LogWarning("Bind mount source '{Source}' does not exist and will not be copied", volume.Source);
                return;
            }

            // Update the source to use a relative path for portability
            volume.Source = Path.GetRelativePath(OutputPath, destinationPath).Replace('\\', '/');
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to copy bind mount from '{Source}' to output folder", volume.Source);
            throw;
        }
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        var dir = new DirectoryInfo(sourceDir);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
        }

        // Create the destination directory
        Directory.CreateDirectory(destinationDir);

        // Copy files
        foreach (var file in dir.GetFiles())
        {
            var targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath, overwrite: true);
        }

        // Copy subdirectories recursively
        foreach (var subDir in dir.GetDirectories())
        {
            var newDestinationDir = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir.FullName, newDestinationDir);
        }
    }

    private static bool IsDockerSocket(string path)
    {
        // Docker socket paths vary by platform
        return path.Equals("/var/run/docker.sock", StringComparison.OrdinalIgnoreCase) ||
               path.Equals(@"\\.\pipe\docker_engine", StringComparison.OrdinalIgnoreCase) ||
               path.Equals("npipe:////./pipe/docker_engine", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNamedPipe(string path)
    {
        // Named pipe patterns for Windows
        return path.StartsWith(@"\\.\pipe\", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("npipe:", StringComparison.OrdinalIgnoreCase);
    }
}

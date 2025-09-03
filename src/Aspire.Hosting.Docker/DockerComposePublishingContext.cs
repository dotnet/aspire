// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
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
    IPublishingActivityReporter activityReporter,
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

                // Detect collisions before processing bind mounts and configs
                var hasCollisions = DetectSourcePathCollisions(serviceResource);

                HandleComposeFileBindMounts(serviceResource, hasCollisions);

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
                            HandleComposeFileConfig(composeFile, composeService, file, a.DefaultOwner, a.DefaultGroup, a.Umask ?? DefaultUmask, a.DestinationPath, hasCollisions);
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
            await ImageBuilder.BuildImagesAsync(containerImagesToBuild, options: null, cancellationToken).ConfigureAwait(false);
        }

        var step = await activityReporter.CreateStepAsync(
            "Writing Docker Compose file.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        await using (step.ConfigureAwait(false))
        {
            var task = await step.CreateTaskAsync(
                "Writing the Docker Compose file to the output path.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            await using (task.ConfigureAwait(false))
            {
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
                        var (key, (description, defaultValue, source)) = entry;
                        
                        // If the source is a parameter and there's no explicit default value,
                        // resolve the parameter's default value asynchronously
                        if (defaultValue is null && source is ParameterResource parameter && !parameter.Secret && parameter.Default is not null)
                        {
                            defaultValue = await parameter.GetValueAsync(cancellationToken).ConfigureAwait(false);
                        }
                        
                        envFile.AddIfMissing(key, defaultValue, description);
                    }

                    envFile.Save(envFilePath);
                }

                await task.SucceedAsync(
                    $"Docker Compose file written successfully to {outputFile}.",
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            await step.SucceedAsync(
                "Docker Compose file generation completed.",
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Detects if there are source path collisions (same filename from different sources) for a service.
    /// </summary>
    /// <param name="serviceResource">The service resource to check for collisions.</param>
    /// <returns>True if collisions are detected, false otherwise.</returns>
    private static bool DetectSourcePathCollisions(DockerComposeServiceResource serviceResource)
    {
        var fileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        // Check bind mount sources
        foreach (var volume in serviceResource.Volumes.Where(volume => volume.Type == "bind" && !string.IsNullOrEmpty(volume.Source)))
        {
            if (File.Exists(volume.Source))
            {
                var fileName = Path.GetFileName(volume.Source);
                if (!fileNames.Add(fileName))
                {
                    return true; // Collision detected
                }
            }
            else if (Directory.Exists(volume.Source))
            {
                var dirName = Path.GetFileName(volume.Source.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                if (string.IsNullOrEmpty(dirName))
                {
                    dirName = "data"; // Default name for root paths
                }
                if (!fileNames.Add(dirName))
                {
                    return true; // Collision detected
                }
            }
        }
        
        // Check container file sources (this would need to be implemented when we have access to the config files)
        // For now, we can't easily check these until HandleComposeFileConfig is called
        // So we'll be conservative and assume no collisions from this source for now
        
        return false; // No collisions detected
    }

    private void HandleComposeFileConfig(ComposeFile composeFile, Service composeService, ContainerFileSystemItem? item, int? uid, int? gid, UnixFileMode umask, string path, bool hasCollisions = false)
    {
        if (item is ContainerDirectory dir)
        {
            foreach (var dirItem in dir.Entries)
            {
                HandleComposeFileConfig(composeFile, composeService, dirItem, item.Owner ?? uid, item.Group ?? gid, umask, path += "/" + item.Name, hasCollisions);
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
                    // Use hash-based directories if collisions are detected
                    sourcePath = CopySourceToOutput(composeService.Name, file.SourcePath, useHashBasedDir: hasCollisions);
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

    private static void HandleComposeFileVolumes(DockerComposeServiceResource serviceResource, ComposeFile composeFile)
    {
        foreach (var volume in serviceResource.Volumes.Where(volume => volume.Type != "bind"))
        {
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

    private void HandleComposeFileBindMounts(DockerComposeServiceResource serviceResource, bool hasCollisions = false)
    {
        // Get all skip annotations to check against
        var skipAnnotations = serviceResource.TargetResource
            .Annotations
            .OfType<SkipBindMountCopyingAnnotation>()
            .Select(annotation => annotation.SourcePath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var volume in serviceResource.Volumes.Where(volume => volume.Type == "bind"))
        {
            if (string.IsNullOrEmpty(volume.Source))
            {
                continue;
            }

            // Skip copying if there's an annotation for this source path
            if (skipAnnotations.Contains(volume.Source))
            {
                continue;
            }

            if (File.Exists(volume.Source) || Directory.Exists(volume.Source))
            {
                try
                {
                    // Use hash-based directories if collisions are detected
                    var copiedSourceRelativePath = CopySourceToOutput(serviceResource.Name, volume.Source, useHashBasedDir: hasCollisions);
                    
                    // Update the volume source to use relative path
                    volume.Source = copiedSourceRelativePath;
                }
                catch (Exception ex)
                {
                    logger.FailedToCopyBindMountSource(ex, volume.Source);
                    throw;
                }
            }
            else
            {
                logger.BindMountSourceDoesNotExist(volume.Source);
            }
        }
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        // Create destination directory if it doesn't exist
        Directory.CreateDirectory(destDir);

        // Copy all files
        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string fileName = Path.GetFileName(file);
            string destFile = Path.Combine(destDir, fileName);
            File.Copy(file, destFile, overwrite: true);
        }

        // Copy all subdirectories recursively
        foreach (string subDir in Directory.GetDirectories(sourceDir))
        {
            string subDirName = Path.GetFileName(subDir);
            string destSubDir = Path.Combine(destDir, subDirName);
            CopyDirectory(subDir, destSubDir);
        }
    }

    /// <summary>
    /// Copies a source file or directory to the output path with collision-aware directory structure.
    /// Uses hash-based directories only when name collisions would occur.
    /// </summary>
    /// <param name="serviceName">Name of the service the source belongs to.</param>
    /// <param name="sourcePath">Path to the source file or directory to copy.</param>
    /// <param name="useHashBasedDir">Whether to always use hash-based directories to avoid collisions.</param>
    /// <returns>Relative path to the copied source that can be used in docker-compose.yaml.</returns>
    private string CopySourceToOutput(string serviceName, string sourcePath, bool useHashBasedDir = false)
    {
        var serviceDir = Path.Combine(OutputPath, serviceName);
        Directory.CreateDirectory(serviceDir);

        string destinationDir;
        if (useHashBasedDir)
        {
            // Use hash-based directory to avoid name collisions
            var sourceHash = GenerateSourceHash(sourcePath);
            destinationDir = Path.Combine(serviceDir, sourceHash);
        }
        else
        {
            // Use service directory directly for backward compatibility
            destinationDir = serviceDir;
        }

        Directory.CreateDirectory(destinationDir);

        string copiedSourcePath;
        if (File.Exists(sourcePath))
        {
            // Handle file
            var fileName = Path.GetFileName(sourcePath);
            copiedSourcePath = Path.Combine(destinationDir, fileName);
            File.Copy(sourcePath, copiedSourcePath, overwrite: true);
        }
        else if (Directory.Exists(sourcePath))
        {
            // Handle directory
            var sourceDirName = Path.GetFileName(sourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (string.IsNullOrEmpty(sourceDirName))
            {
                // If the source is a root path, use a generic folder name
                sourceDirName = "data";
            }
            
            copiedSourcePath = Path.Combine(destinationDir, sourceDirName);
            CopyDirectory(sourcePath, copiedSourcePath);
        }
        else
        {
            throw new FileNotFoundException($"Source path does not exist: {sourcePath}");
        }

        // Return relative path with unix-style separators for docker-compose compatibility
        return Path.GetRelativePath(OutputPath, copiedSourcePath).Replace('\\', '/');
    }

    /// <summary>
    /// Generates a short hash from the source path to create unique directory names.
    /// </summary>
    private static string GenerateSourceHash(string sourcePath)
    {
        var normalizedPath = Path.GetFullPath(sourcePath).ToLowerInvariant();
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedPath));
        // Use first 8 characters of the hash for a short but unique directory name
        return Convert.ToHexString(hashBytes)[..8].ToLowerInvariant();
    }
}

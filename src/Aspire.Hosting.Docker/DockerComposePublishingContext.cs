// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

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
    CancellationToken cancellationToken = default)
{
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

        foreach (var resource in model.Resources)
        {
            if (resource.GetDeploymentTargetAnnotation(environment)?.DeploymentTarget is DockerComposeServiceResource serviceResource)
            {
                if (environment.BuildContainerImages)
                {
                    await ImageBuilder.BuildImageAsync(serviceResource.TargetResource, cancellationToken).ConfigureAwait(false);
                }

                var composeService = serviceResource.ComposeService;

                HandleComposeFileVolumes(serviceResource, composeFile);

                composeService.Networks =
                [
                    defaultNetwork.Name,
                ];

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

        // Call the environment's ConfigureComposeFile method to allow for custom modifications
        environment.ConfigureComposeFile?.Invoke(composeFile);

        var composeOutput = composeFile.ToYaml();
        var outputFile = Path.Combine(OutputPath, "docker-compose.yaml");
        Directory.CreateDirectory(OutputPath);
        await File.WriteAllTextAsync(outputFile, composeOutput, cancellationToken).ConfigureAwait(false);

        if (environment.CapturedEnvironmentVariables.Count == 0)
        {
            // No environment variables to write, so we can skip creating the .env file
            return;
        }

        // Write a .env file with the environment variable names
        // that are used in the compose file
        var envFilePath = Path.Combine(OutputPath, ".env");
        var envFile = EnvFile.Load(envFilePath);

        foreach (var entry in environment.CapturedEnvironmentVariables ?? [])
        {
            var (key, (description, defaultValue)) = entry;
            envFile.AddIfMissing(key, defaultValue, description);
        }

        envFile.Save(envFilePath);
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
}

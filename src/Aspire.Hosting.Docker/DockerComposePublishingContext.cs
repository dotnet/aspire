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
    DockerComposePublisherOptions publisherOptions,
    IResourceContainerImageBuilder imageBuilder,
    ILogger logger,
    CancellationToken cancellationToken = default)
{
    public readonly IResourceContainerImageBuilder ImageBuilder = imageBuilder;
    public readonly DockerComposePublisherOptions PublisherOptions = publisherOptions;

    internal async Task WriteModelAsync(DistributedApplicationModel model)
    {
        if (!executionContext.IsPublishMode)
        {
            logger.NotInPublishingMode();
            return;
        }

        logger.StartGeneratingDockerCompose();

        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(PublisherOptions.OutputPath);

        if (model.Resources.Count == 0)
        {
            logger.EmptyModel();
            return;
        }

        await WriteDockerComposeOutputAsync(model).ConfigureAwait(false);

        logger.FinishGeneratingDockerCompose(PublisherOptions.OutputPath);
    }

    private async Task WriteDockerComposeOutputAsync(DistributedApplicationModel model)
    {
        var dockerComposeEnvironments = model.Resources.OfType<DockerComposeEnvironmentResource>().ToArray();

        if (dockerComposeEnvironments.Length > 1)
        {
            throw new NotSupportedException("Multiple Docker Compose environments are not supported.");
        }

        var environment = dockerComposeEnvironments.FirstOrDefault();

        if (environment == null)
        {
            // No Docker Compose environment found
            throw new InvalidOperationException($"No Docker Compose environment found. Ensure a Docker Compose environment is registered by calling {nameof(DockerComposeEnvironmentExtensions.AddDockerComposeEnvironment)}.");
        }

        var defaultNetwork = new Network
        {
            Name = environment.DefaultNetworkName ?? "aspire",
            Driver = "bridge",
        };

        var composeFile = new ComposeFile();
        composeFile.AddNetwork(defaultNetwork);

        foreach (var resource in model.Resources)
        {
            if (resource.GetDeploymentTargetAnnotation()?.DeploymentTarget is DockerComposeServiceResource serviceResource)
            {
                if (PublisherOptions.BuildImages)
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

        var composeOutput = composeFile.ToYaml();
        var outputFile = Path.Combine(PublisherOptions.OutputPath!, "docker-compose.yaml");
        Directory.CreateDirectory(PublisherOptions.OutputPath!);
        await File.WriteAllTextAsync(outputFile, composeOutput, cancellationToken).ConfigureAwait(false);

        if (environment.CapturedEnvironmentVariables.Count == 0)
        {
            // No environment variables to write, so we can skip creating the .env file
            return;
        }

        // Write a .env file with the environment variable names
        // that are used in the compose file
        var envFile = Path.Combine(PublisherOptions.OutputPath!, ".env");
        using var envWriter = new StreamWriter(envFile);

        foreach (var entry in environment.CapturedEnvironmentVariables ?? [])
        {
            var (key, (description, defaultValue)) = entry;

            await envWriter.WriteLineAsync($"# {description}").ConfigureAwait(false);

            if (defaultValue is not null)
            {
                await envWriter.WriteLineAsync($"{key}={defaultValue}").ConfigureAwait(false);
            }
            else
            {
                await envWriter.WriteLineAsync($"{key}=").ConfigureAwait(false);
            }

            await envWriter.WriteLineAsync().ConfigureAwait(false);
        }

        await envWriter.FlushAsync().ConfigureAwait(false);
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

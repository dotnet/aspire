// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using System.Globalization;
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
    DockerComposePublisherOptions publisherOptions,
    IResourceContainerImageBuilder imageBuilder,
    ILogger logger,
    CancellationToken cancellationToken = default)
{
    public readonly IResourceContainerImageBuilder ImageBuilder = imageBuilder;
    public readonly DockerComposePublisherOptions PublisherOptions = publisherOptions;

    private ILogger Logger => logger;

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
            return;
        }

        var defaultNetwork = new Network
        {
            Name = PublisherOptions.ExistingNetworkName ?? "aspire",
            Driver = "bridge",
        };

        var composeFile = new ComposeFile();
        composeFile.AddNetwork(defaultNetwork);

        foreach (var resource in model.Resources)
        {
            if (resource.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var deploymentTargetAnnotation) &&
                deploymentTargetAnnotation.DeploymentTarget is DockerComposeServiceResource serviceResource)
            {
                var composeServiceContext = new ComposeServiceContext(environment, serviceResource, this);
                var composeService = await composeServiceContext.BuildComposeServiceAsync(cancellationToken).ConfigureAwait(false);

                HandleComposeFileVolumes(serviceResource, composeFile);

                composeService.Networks =
                [
                    defaultNetwork.Name,
                ];

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

    private sealed class ComposeServiceContext(DockerComposeEnvironmentResource environment, DockerComposeServiceResource resource, DockerComposePublishingContext composePublishingContext)
    {
        /// <summary>
        /// Most common shell executables used as container entrypoints in Linux containers.
        /// These are used to identify when a container's entrypoint is a shell that will execute commands.
        /// </summary>
        private static readonly HashSet<string> s_shellExecutables = new(StringComparer.OrdinalIgnoreCase)
        {
            "/bin/sh",
            "/bin/bash",
            "/sh",
            "/bash",
            "sh",
            "bash",
            "/usr/bin/sh",
            "/usr/bin/bash"
        };

        public bool IsShellExec { get; private set; }

        public async Task<Service> BuildComposeServiceAsync(CancellationToken cancellationToken)
        {
            if (composePublishingContext.PublisherOptions.BuildImages)
            {
                await composePublishingContext.ImageBuilder.BuildImageAsync(resource.TargetResource, cancellationToken).ConfigureAwait(false);
            }

            if (!TryGetContainerImageName(resource.TargetResource, out var containerImageName))
            {
                composePublishingContext.Logger.FailedToGetContainerImage(resource.Name);
            }

            var composeService = new Service
            {
                Name = resource.Name.ToLowerInvariant(),
            };

            SetEntryPoint(composeService);
            AddEnvironmentVariablesAndCommandLineArgs(composeService);
            AddPorts(composeService);
            AddVolumes(composeService);
            SetContainerImage(containerImageName, composeService);
            SetDependsOn(composeService);

            return composeService;
        }

        private void SetDependsOn(Service composeService)
        {
            if (resource.TargetResource.TryGetAnnotationsOfType<WaitAnnotation>(out var waitAnnotations))
            {
                foreach (var waitAnnotation in waitAnnotations)
                {
                    // We can only wait on other compose services
                    if (waitAnnotation.Resource is ProjectResource || waitAnnotation.Resource.IsContainer())
                    {
                        // https://docs.docker.com/compose/how-tos/startup-order/#control-startup
                        composeService.DependsOn[waitAnnotation.Resource.Name.ToLowerInvariant()] = new()
                        {
                            Condition = waitAnnotation.WaitType switch
                            {
                                // REVIEW: This only works if the target service has health checks,
                                // revisit this when we have a way to add health checks to the compose service
                                // WaitType.WaitUntilHealthy => "service_healthy",
                                WaitType.WaitForCompletion => "service_completed_successfully",
                                _ => "service_started",
                            },
                        };
                    }
                }
            }
        }

        private bool TryGetContainerImageName(IResource resourceInstance, out string? containerImageName)
        {
            // If the resource has a Dockerfile build annotation, we don't have the image name
            // it will come as a parameter
            if (resourceInstance.TryGetLastAnnotation<DockerfileBuildAnnotation>(out _) || resourceInstance is ProjectResource)
            {
                var imageEnvName = $"{resourceInstance.Name.ToUpperInvariant().Replace("-", "_")}_IMAGE";

                environment.CapturedEnvironmentVariables.Add(imageEnvName, ($"Container image name for {resourceInstance.Name}", $"{resourceInstance.Name}:latest"));

                containerImageName = $"${{{imageEnvName}}}";
                return true;
            }

            return resourceInstance.TryGetContainerImageName(out containerImageName);
        }

        private void AddVolumes(Service composeService)
        {
            if (resource.Volumes.Count == 0)
            {
                return;
            }

            foreach (var volume in resource.Volumes)
            {
                composeService.AddVolume(volume);
            }
        }

        private void AddPorts(Service composeService)
        {
            if (resource.EndpointMappings.Count == 0)
            {
                return;
            }

            foreach (var (_, mapping) in resource.EndpointMappings)
            {
                var internalPort = mapping.InternalPort.ToString(CultureInfo.InvariantCulture);
                var exposedPort = mapping.ExposedPort.ToString(CultureInfo.InvariantCulture);

                composeService.Ports.Add($"{exposedPort}:{internalPort}");
            }
        }

        private static void SetContainerImage(string? containerImageName, Service composeService)
        {
            if (containerImageName is not null)
            {
                composeService.Image = containerImageName;
            }
        }

        private void SetEntryPoint(Service composeService)
        {
            if (resource.TargetResource is ContainerResource { Entrypoint: { } entrypoint })
            {
                composeService.Entrypoint.Add(entrypoint);

                if (s_shellExecutables.Contains(entrypoint))
                {
                    IsShellExec = true;
                }
            }
        }

        private void AddEnvironmentVariablesAndCommandLineArgs(Service composeService)
        {
            if (resource.EnvironmentVariables.Count > 0)
            {
                foreach (var variable in resource.EnvironmentVariables)
                {
                    composeService.AddEnvironmentalVariable(variable.Key, variable.Value);
                }
            }

            if (resource.Commands.Count > 0)
            {
                if (IsShellExec)
                {
                    var sb = new StringBuilder();
                    foreach (var command in resource.Commands)
                    {
                        // Escape any environment variables expressions in the command
                        // to prevent them from being interpreted by the docker compose CLI
                        EnvVarEscaper.EscapeUnescapedEnvVars(command, sb);
                        composeService.Command.Add(sb.ToString());
                        sb.Clear();
                    }
                }
                else
                {
                    composeService.Command.AddRange(resource.Commands);
                }
            }
        }
    }
}

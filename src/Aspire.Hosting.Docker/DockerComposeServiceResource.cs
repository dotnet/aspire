// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System.Globalization;
using System.Text;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Docker.Resources.ComposeNodes;
using Aspire.Hosting.Docker.Resources.ServiceNodes;

namespace Aspire.Hosting.Docker;

/// <summary>
/// Represents a compute resource for Docker Compose with strongly-typed properties.
/// </summary>
public class DockerComposeServiceResource(string name, IResource resource, DockerComposeEnvironmentResource composeEnvironmentResource) : Resource(name), IResourceWithParent<DockerComposeEnvironmentResource>
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

    internal bool IsShellExec { get; private set; }

    internal record struct EndpointMapping(string Scheme, string Host, int InternalPort, int ExposedPort, bool IsHttpIngress);

    /// <summary>
    /// Gets the resource that is the target of this Docker Compose service.
    /// </summary>
    internal IResource TargetResource => resource;

    /// <summary>
    /// Gets the collection of environment variables for the Docker Compose service.
    /// </summary>
    internal Dictionary<string, string?> EnvironmentVariables { get; } = [];

    /// <summary>
    /// Gets the collection of commands to be executed by the Docker Compose service.
    /// </summary>
    internal List<string> Commands { get; } = [];

    /// <summary>
    /// Gets the collection of volumes for the Docker Compose service.
    /// </summary>
    internal List<Volume> Volumes { get; } = [];

    /// <summary>
    /// Gets the mapping of endpoint names to their configurations.
    /// </summary>
    internal Dictionary<string, EndpointMapping> EndpointMappings { get; } = [];

    public Service ComposeService => GetComposeService();

    public DockerComposeEnvironmentResource Parent => composeEnvironmentResource;

    private Service GetComposeService()
    {
        var composeService = new Service
        {
            Name = resource.Name.ToLowerInvariant(),
        };

        if (TryGetContainerImageName(TargetResource, out var containerImageName))
        {
            SetContainerImage(containerImageName, composeService);
        }

        SetEntryPoint(composeService);
        AddEnvironmentVariablesAndCommandLineArgs(composeService);
        AddPorts(composeService);
        AddVolumes(composeService);
        SetDependsOn(composeService);

        if (resource.TryGetAnnotationsOfType<DockerComposeServiceCustomizationAnnotation>(out var annotations))
        {
            foreach (var a in annotations)
            {
                a.Configure(this, composeService);
            }
        }

        return composeService;
    }

    private bool TryGetContainerImageName(IResource resourceInstance, out string? containerImageName)
    {
        // If the resource has a Dockerfile build annotation, we don't have the image name
        // it will come as a parameter
        if (resourceInstance.TryGetLastAnnotation<DockerfileBuildAnnotation>(out _) || resourceInstance is ProjectResource)
        {
            var imageEnvName = $"{resourceInstance.Name.ToUpperInvariant().Replace("-", "_")}_IMAGE";

            composeEnvironmentResource.CapturedEnvironmentVariables.Add(imageEnvName, ($"Container image name for {resourceInstance.Name}", $"{resourceInstance.Name}:latest"));

            containerImageName = $"${{{imageEnvName}}}";
            return true;
        }

        return resourceInstance.TryGetContainerImageName(out containerImageName);
    }

    private void SetEntryPoint(Service composeService)
    {
        if (TargetResource is ContainerResource { Entrypoint: { } entrypoint })
        {
            composeService.Entrypoint.Add(entrypoint);

            if (s_shellExecutables.Contains(entrypoint))
            {
                IsShellExec = true;
            }
        }
    }

    private void SetDependsOn(Service composeService)
    {
        if (TargetResource.TryGetAnnotationsOfType<WaitAnnotation>(out var waitAnnotations))
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

    private static void SetContainerImage(string? containerImageName, Service composeService)
    {
        if (containerImageName is not null)
        {
            composeService.Image = containerImageName;
        }
    }

    private void AddEnvironmentVariablesAndCommandLineArgs(Service composeService)
    {
        if (EnvironmentVariables.Count > 0)
        {
            foreach (var variable in EnvironmentVariables)
            {
                composeService.AddEnvironmentalVariable(variable.Key, variable.Value);
            }
        }

        if (Commands.Count > 0)
        {
            if (IsShellExec)
            {
                var sb = new StringBuilder();
                foreach (var command in Commands)
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
                composeService.Command.AddRange(Commands);
            }
        }
    }

    private void AddPorts(Service composeService)
    {
        if (EndpointMappings.Count == 0)
        {
            return;
        }

        foreach (var (_, mapping) in EndpointMappings)
        {
            var internalPort = mapping.InternalPort.ToString(CultureInfo.InvariantCulture);
            var exposedPort = mapping.ExposedPort.ToString(CultureInfo.InvariantCulture);

            composeService.Ports.Add($"{exposedPort}:{internalPort}");
        }
    }

    private void AddVolumes(Service composeService)
    {
        if (Volumes.Count == 0)
        {
            return;
        }

        foreach (var volume in Volumes)
        {
            composeService.AddVolume(volume);
        }
    }
}

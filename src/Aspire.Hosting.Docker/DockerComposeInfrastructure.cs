// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Docker;

/// <summary>
/// Represents the infrastructure for Docker Compose within the Aspire Hosting environment.
/// Implements the <see cref="IDistributedApplicationLifecycleHook"/> interface to provide lifecycle hooks for distributed applications.
/// </summary>
internal sealed class DockerComposeInfrastructure(
    ILogger<DockerComposeInfrastructure> logger,
    DistributedApplicationExecutionContext executionContext) : IDistributedApplicationLifecycleHook
{
    public async Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        if (executionContext.IsRunMode)
        {
            return;
        }

        // Find Docker Compose environment resources
        var dockerComposeEnvironments = appModel.Resources.OfType<DockerComposeEnvironmentResource>().ToArray();

        if (dockerComposeEnvironments.Length > 1)
        {
            throw new NotSupportedException("Multiple Docker Compose environments are not supported.");
        }

        var environment = dockerComposeEnvironments.FirstOrDefault();

        if (environment == null)
        {
            return;
        }

        var dockerComposeEnvironmentContext = new DockerComposeEnvironmentContext(environment, logger);

        foreach (var r in appModel.Resources)
        {
            if (r.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out var lastAnnotation) && lastAnnotation == ManifestPublishingCallbackAnnotation.Ignore)
            {
                continue;
            }

            // Skip resources that are not containers or projects
            if (!r.IsContainer() && r is not ProjectResource)
            {
                continue;
            }

            // Create a Docker Compose compute resource for the resource
            var serviceResource = await dockerComposeEnvironmentContext.CreateDockerComposeServiceResourceAsync(r, executionContext, cancellationToken).ConfigureAwait(false);

            // Add deployment target annotation to the resource
            r.Annotations.Add(new DeploymentTargetAnnotation(serviceResource));
        }
    }

    internal sealed class DockerComposeEnvironmentContext(DockerComposeEnvironmentResource environment, ILogger logger)
    {
        private readonly Dictionary<IResource, DockerComposeServiceResource> _resourceMapping = [];
        private readonly PortAllocator _portAllocator = new();

        public async Task<DockerComposeServiceResource> CreateDockerComposeServiceResourceAsync(IResource resource, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
        {
            if (_resourceMapping.TryGetValue(resource, out var existingResource))
            {
                return existingResource;
            }

            logger.LogInformation("Creating Docker Compose resource for {ResourceName}", resource.Name);

            var serviceResource = new DockerComposeServiceResource(resource.Name, resource, environment);
            _resourceMapping[resource] = serviceResource;

            // Process endpoints
            ProcessEndpoints(serviceResource);

            // Process volumes
            ProcessVolumes(serviceResource);

            // Process environment variables
            await ProcessEnvironmentVariablesAsync(serviceResource, executionContext, cancellationToken).ConfigureAwait(false);

            // Process command line arguments
            await ProcessArgumentsAsync(serviceResource, executionContext, cancellationToken).ConfigureAwait(false);

            return serviceResource;
        }

        private void ProcessEndpoints(DockerComposeServiceResource serviceResource)
        {
            if (!serviceResource.TargetResource.TryGetEndpoints(out var endpoints))
            {
                return;
            }

            foreach (var endpoint in endpoints)
            {
                var internalPort = endpoint.TargetPort ?? _portAllocator.AllocatePort();
                _portAllocator.AddUsedPort(internalPort);

                var exposedPort = _portAllocator.AllocatePort();
                _portAllocator.AddUsedPort(exposedPort);

                serviceResource.EndpointMappings.Add(endpoint.Name, new(endpoint.UriScheme, serviceResource.TargetResource.Name, internalPort, exposedPort, false));
            }
        }

        private static void ProcessVolumes(DockerComposeServiceResource serviceResource)
        {
            if (!serviceResource.TargetResource.TryGetContainerMounts(out var mounts))
            {
                return;
            }

            foreach (var mount in mounts)
            {
                if (mount.Source is null || mount.Target is null)
                {
                    throw new InvalidOperationException("Volume source and target must be set");
                }

                serviceResource.Volumes.Add(new Resources.ServiceNodes.Volume
                {
                    Name = mount.Source,
                    Source = mount.Source,
                    Target = mount.Target,
                    Type = mount.Type == ContainerMountType.BindMount ? "bind" : "volume",
                    ReadOnly = mount.IsReadOnly
                });
            }
        }

        private async Task ProcessEnvironmentVariablesAsync(DockerComposeServiceResource serviceResource, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
        {
            if (serviceResource.TargetResource.TryGetAnnotationsOfType<EnvironmentCallbackAnnotation>(out var environmentCallbacks))
            {
                var context = new EnvironmentCallbackContext(executionContext, serviceResource.TargetResource, cancellationToken: cancellationToken);

                foreach (var callback in environmentCallbacks)
                {
                    await callback.Callback(context).ConfigureAwait(false);
                }

                // Remove HTTPS service discovery variables as Docker Compose doesn't handle certificates
                RemoveHttpsServiceDiscoveryVariables(context.EnvironmentVariables);

                foreach (var kv in context.EnvironmentVariables)
                {
                    var value = await serviceResource.ProcessValueAsync(this, executionContext, kv.Value).ConfigureAwait(false);
                    serviceResource.EnvironmentVariables.Add(kv.Key, value?.ToString() ?? string.Empty);
                }
            }
        }

        private static void RemoveHttpsServiceDiscoveryVariables(Dictionary<string, object> environmentVariables)
        {
            var keysToRemove = environmentVariables
                .Where(kvp => kvp.Value is EndpointReference epRef && epRef.Scheme == "https" && kvp.Key.StartsWith("services__"))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                environmentVariables.Remove(key);
            }
        }

        private async Task ProcessArgumentsAsync(DockerComposeServiceResource serviceResource, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
        {
            if (serviceResource.TargetResource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var commandLineArgsCallbacks))
            {
                var context = new CommandLineArgsCallbackContext([], cancellationToken: cancellationToken);

                foreach (var callback in commandLineArgsCallbacks)
                {
                    await callback.Callback(context).ConfigureAwait(false);
                }

                foreach (var arg in context.Args)
                {
                    var value = await serviceResource.ProcessValueAsync(this, executionContext, arg).ConfigureAwait(false);
                    if (value is not string str)
                    {
                        throw new NotSupportedException("Command line args must be strings");
                    }

                    serviceResource.Commands.Add(str);
                }
            }
        }

        public void AddEnv(string name, string description, string? defaultValue = null)
        {
            environment.CapturedEnvironmentVariables[name] = (description, defaultValue);
        }
    }
}

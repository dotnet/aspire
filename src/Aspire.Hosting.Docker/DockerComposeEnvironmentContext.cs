// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Docker;

internal sealed class DockerComposeEnvironmentContext(DockerComposeEnvironmentResource environment, ILogger logger)
{
    public async Task<DockerComposeServiceResource> CreateDockerComposeServiceResourceAsync(IResource resource, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
    {
        if (environment.ResourceMapping.TryGetValue(resource, out var existingResource))
        {
            return existingResource;
        }

        logger.LogInformation("Creating Docker Compose resource for {ResourceName}", resource.Name);

        var serviceResource = new DockerComposeServiceResource(resource.Name, resource, environment);
        environment.ResourceMapping[resource] = serviceResource;

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

    private static void ProcessEndpoints(DockerComposeServiceResource serviceResource)
    {
        if (!serviceResource.TargetResource.TryGetEndpoints(out var endpoints))
        {
            return;
        }

        var portAllocator = serviceResource.Parent.PortAllocator;

        string ResolveTargetPort(EndpointAnnotation endpoint)
        {
            if (endpoint.TargetPort is int port)
            {
                return port.ToString(CultureInfo.InvariantCulture);
            }

            if (serviceResource.TargetResource is ProjectResource)
            {
                return serviceResource.AsContainerPortPlaceholder();
            }

            // The container did not specify a target port, so we allocate one
            // this mimics the semantics of what happens at runtime.
            var dynamicTargetPort = portAllocator.AllocatePort();

            portAllocator.AddUsedPort(dynamicTargetPort);

            return dynamicTargetPort.ToString(CultureInfo.InvariantCulture);
        }

        foreach (var endpoint in endpoints)
        {
            var internalPort = ResolveTargetPort(endpoint);
            var exposedPort = endpoint.Port;

            serviceResource.EndpointMappings.Add(endpoint.Name,
                new(serviceResource.TargetResource,
                    endpoint.UriScheme,
                    serviceResource.TargetResource.Name,
                    internalPort,
                    exposedPort,
                    endpoint.IsExternal,
                    endpoint.Name));
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

    private static async Task ProcessEnvironmentVariablesAsync(DockerComposeServiceResource serviceResource, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
    {
        if (serviceResource.TargetResource.TryGetAnnotationsOfType<EnvironmentCallbackAnnotation>(out var environmentCallbacks))
        {
            var context = new EnvironmentCallbackContext(
                executionContext,
                serviceResource.TargetResource,
                serviceResource.EnvironmentVariables,
                cancellationToken: cancellationToken);

            foreach (var callback in environmentCallbacks)
            {
                await callback.Callback(context).ConfigureAwait(false);
            }

            // Remove HTTPS service discovery variables as Docker Compose doesn't handle certificates
            RemoveHttpsServiceDiscoveryVariables(context.EnvironmentVariables);
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

    private static async Task ProcessArgumentsAsync(DockerComposeServiceResource serviceResource, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
    {
        if (serviceResource.TargetResource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var commandLineArgsCallbacks))
        {
            var context = new CommandLineArgsCallbackContext(serviceResource.Args, cancellationToken: cancellationToken)
            {
                ExecutionContext = executionContext
            };

            foreach (var callback in commandLineArgsCallbacks)
            {
                await callback.Callback(context).ConfigureAwait(false);
            }
        }
    }
}

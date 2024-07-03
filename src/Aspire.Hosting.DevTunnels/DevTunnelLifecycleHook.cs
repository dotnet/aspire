// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;

namespace Aspire.Hosting.DevTunnels;

internal class DevTunnelLifecycleHook(DistributedApplicationExecutionContext executionContext, IDevTunnelTool devTunnelTool) : IDistributedApplicationLifecycleHook
{
    public async Task AfterEndpointsAllocatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        // Tunnels don't work in publish mode.
        if (executionContext.IsPublishMode)
        {
            return;
        }

        var devTunnelTargetResources = appModel.Resources.OfType<IResourceWithEndpoints>().Where(r => r.Annotations.OfType<DevTunnelAnnotation>().Any()).ToList();

        // Don't need to do anything if no devtunnels are requested.
        if (devTunnelTargetResources.Count == 0)
        {
            return;
        }

        var existingTunnelsResponse = await devTunnelTool.ListTunnelsAsync(cancellationToken).ConfigureAwait(false);

        var devTunnelConfigurationTasks = new List<Task>();

        foreach (var devTunnelTargetResource in devTunnelTargetResources)
        {
            foreach (var annotation in devTunnelTargetResource.Annotations.OfType<DevTunnelAnnotation>())
            {
                var devTunnelConfigurationTask = ConfigureTunnelAsync(devTunnelTargetResource, annotation, cancellationToken);
                devTunnelConfigurationTasks.Add(devTunnelConfigurationTask);
            }
        }

        await Task.WhenAll(devTunnelConfigurationTasks).ConfigureAwait(false);

        async Task ConfigureTunnelAsync(IResource devTunnelResource, DevTunnelAnnotation annotation, CancellationToken cancellationToken)
        {
            DevTunnelPort[]? devTunnelPorts;

            // TODO: StartsWith ... ugly.
            if (existingTunnelsResponse.Tunnels is not { } tunnels || !tunnels.Any(t => t.TunnelId!.StartsWith(annotation.Options.DevTunnelId + ".")))
            {
                await devTunnelTool.CreateTunnelAsync(
                    annotation.Options.DevTunnelId,
                    annotation.Options.AllowAnonymous,
                    cancellationToken).ConfigureAwait(false);

                devTunnelPorts = Array.Empty<DevTunnelPort>();
            }
            else
            {
                var listPortsResponse = await devTunnelTool.ListTunnelPortsAsync(annotation.Options.DevTunnelId, cancellationToken).ConfigureAwait(false);
                devTunnelPorts = listPortsResponse.Ports ?? Array.Empty<DevTunnelPort>();
            }

            var portDeletionTasks = new List<Task<DevTunnelDeletePortCommandResponse>>();
            foreach (var port in devTunnelPorts)
            {
                if (port.PortNumber != annotation.EndpointAnnotation.AllocatedEndpoint!.Port)
                {
                    var deletePortCommand = devTunnelTool.DeleteTunnelPortAsync(
                        annotation.Options.DevTunnelId,
                        annotation.EndpointAnnotation.AllocatedEndpoint!.Port,
                        cancellationToken);

                    portDeletionTasks.Add(deletePortCommand);
                }
            }

            await Task.WhenAll(portDeletionTasks).ConfigureAwait(false);

            if (!devTunnelPorts.Any(p => p.PortNumber == annotation.EndpointAnnotation.AllocatedEndpoint!.Port))
            {
                await devTunnelTool.CreateTunnelPortAsync(
                    annotation.Options.DevTunnelId,
                    annotation.EndpointAnnotation.AllocatedEndpoint!.Port,
                    cancellationToken).ConfigureAwait(false);
            }

            var devTunnelSidecarResourceName = $"{devTunnelResource.Name}-{annotation.EndpointAnnotation.Name}-devtunnel";
            var devTunnelExecutableresource = appModel.Resources.OfType<ExecutableResource>().Single(r => r.Name == devTunnelSidecarResourceName);
            devTunnelExecutableresource.Annotations.Add(new CommandLineArgsCallbackAnnotation((args) =>
            {
                args.Add("host");
                args.Add(annotation.Options.DevTunnelId);
            }));
        }
    }
}

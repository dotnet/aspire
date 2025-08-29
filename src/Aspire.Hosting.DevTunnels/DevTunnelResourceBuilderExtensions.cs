// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Headers;
using System.Reflection;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.DevTunnels;
using Microsoft.DevTunnels.Connections;
using Microsoft.DevTunnels.Contracts;
using Microsoft.DevTunnels.Management;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Dev tunnels resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class DevTunnelsResourceBuilderExtensions
{
    /// <summary>
    /// Adds a Dev tunnel resource.
    /// </summary>
    public static IResourceBuilder<DevTunnelResource> AddDevTunnel(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        Action<DevTunnelOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        builder.Services.TryAddSingleton<ITunnelManagementClient>(_ =>
        {
            var userAgent = TunnelUserAgent.GetUserAgent(typeof(DevTunnelResource).Assembly)
                ?? new ProductInfoHeaderValue("Aspire.Hosting.DevTunnels", Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0");
            return new TunnelManagementClient(userAgent, userTokenCallback: null, apiVersion: ManagementApiVersions.Version20230927Preview);
        });

        var options = new DevTunnelOptions();
        configure?.Invoke(options);

        var rb = builder.AddResource(new DevTunnelResource(name, options))
            .WithInitialState(new CustomResourceSnapshot
            {
                ResourceType = "DevTunnel",
                CreationTimeStamp = DateTime.UtcNow,
                State = KnownResourceStates.NotStarted,
                Properties = []
            });

        // Lifecycle
        rb.OnInitializeResource(static async (resource, init, token) =>
        {
            var tunnelResource = resource;
            var logger = init.Logger;
            var notifications = init.Notifications;

            await notifications.PublishUpdateAsync(tunnelResource, s => s with { State = KnownResourceStates.Starting }).ConfigureAwait(false);
            // TODO: Need to publish BeforeStart event for this resource here so subscribers have a chance to update the model before continuing

            // Find ports under this tunnel.
            var portResources = tunnelResource.Ports;

            if (portResources.Count == 0)
            {
                logger.LogInformation("Dev tunnel '{Tunnel}' has no ports so will not start.", tunnelResource.Name);
                await notifications.PublishUpdateAsync(tunnelResource, s => s with { State = KnownResourceStates.Finished }).ConfigureAwait(false);
                return;
            }

            var tunnelManager = init.Services.GetRequiredService<ITunnelManagementClient>();
            logger.LogInformation("Starting dev tunnel '{Tunnel}' with {Count} port(s)...", tunnelResource.Name, portResources.Count);

            try
            {
                var tunnel = new Tunnel
                {
                    TunnelId = tunnelResource.TunnelId,
                    Labels = ["aspire"],
                    Description = tunnelResource.Options.Description
                };
                tunnel = await tunnelManager.CreateOrUpdateTunnelAsync(tunnel, null, token).ConfigureAwait(false);

                // TODO: Consider lifetime of the trace listener, it's disposable, perhaps make it a singleton for the app
                // TODO: Reconsider if this should instead map the resource ILogger instead of the ILoggerFactory
                var traceListener = new LoggerFactoryTraceListener(init.Services.GetRequiredService<ILoggerFactory>());
                var traceSource = new System.Diagnostics.TraceSource(tunnelResource.Name);
                traceSource.Listeners.Add(traceListener);
                var tunnelHost = new TunnelRelayTunnelHost(tunnelManager, traceSource);
                await notifications.PublishUpdateAsync(tunnelResource, s => s with { State = KnownResourceStates.Running }).ConfigureAwait(false);

                // Subscribe to endpoint allocated events for resources being exposed by the tunnel
                foreach (var portResource in portResources)
                {
                    init.Eventing.Subscribe<ResourceEndpointsAllocatedEvent>(portResource.TargetEndpoint.Resource, async (e, ct) =>
                    {
                        if (!portResource.TargetEndpoint.IsAllocated)
                        {
                            return;
                        }

                        var port = new TunnelPort
                        {
                            PortNumber = (ushort)portResource.TargetEndpoint.Port,
                            Labels = [portResource.TargetEndpoint.Resource.Name, portResource.TargetEndpoint.EndpointName]
                        };
                        port = await tunnelManager.CreateOrUpdateTunnelPortAsync(tunnel, port, null, ct).ConfigureAwait(false);
                        logger.LogInformation("Dev tunnel '{Tunnel}' port '{Port}' is forwarding to {Source}/{Endpoint} at {Url}", tunnelResource.Name, port.PortNumber, portResource.TargetResource.Name, portResource.TargetEndpoint.EndpointName, port.PortForwardingUris?.First());

                        // TODO: Allocate endpoint to the tunnel port here
                        // TODO: Publish URLs update with port-forwarding and inspect URLs
                    });
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start dev tunnel '{Tunnel}'.", tunnelResource.Name);
                await notifications.PublishUpdateAsync(tunnelResource, s => s with { State = KnownResourceStates.FailedToStart }).ConfigureAwait(false);
            }
        });

        return rb;
    }

    /// <summary>
    /// Adds ports on the tunnel for all endpoints found on the referenced resource.
    /// </summary>
    public static IResourceBuilder<DevTunnelResource> WithReference(
        this IResourceBuilder<DevTunnelResource> tunnelBuilder,
        IResourceWithEndpoints resource,
        Action<DevTunnelPortOptions>? configurePort = null)
    {
        ArgumentNullException.ThrowIfNull(tunnelBuilder);
        ArgumentNullException.ThrowIfNull(resource);

        foreach (var endpoint in resource.GetEndpoints())
        {
            AddDevTunnelPort(tunnelBuilder, endpoint, configurePort);
        }

        return tunnelBuilder;
    }

    /// <summary>
    /// Adds ports on the tunnel for the specified endpoints on the referenced resource.
    /// </summary>
    public static IResourceBuilder<DevTunnelResource> WithReference(
        this IResourceBuilder<DevTunnelResource> tunnelBuilder,
        EndpointReference targetEndpoint,
        Action<DevTunnelPortOptions>? configurePort = null)
    {
        ArgumentNullException.ThrowIfNull(tunnelBuilder);
        ArgumentNullException.ThrowIfNull(targetEndpoint);

        AddDevTunnelPort(tunnelBuilder, targetEndpoint, configurePort);

        return tunnelBuilder;
    }

    /// <summary>
    /// Adds a dev tunnel and tunnels all HTTP endpoints on the provided resource.
    /// </summary>
    public static IResourceBuilder<TResource> WithTunneledHttpEndpoints<TResource>(
        this IResourceBuilder<TResource> resourceBuilder,
        IResourceBuilder<DevTunnelResource> tunnelBuilder,
        Action<DevTunnelPortOptions>? configurePort = null)
        where TResource : IResourceWithEndpoints
    {
        ArgumentNullException.ThrowIfNull(resourceBuilder);
        ArgumentNullException.ThrowIfNull(tunnelBuilder);

        var resource = resourceBuilder.Resource;
        tunnelBuilder.WithReference(resource, configurePort);

        return resourceBuilder;
    }

    private static void AddDevTunnelPort(
        IResourceBuilder<DevTunnelResource> tunnelBuilder,
        EndpointReference targetEndpoint,
        Action<DevTunnelPortOptions>? configurePort)
    {
        var tunnel = tunnelBuilder.Resource;
        var targetResource = targetEndpoint.Resource;
        var portOptions = new DevTunnelPortOptions
        {
            Protocol = targetEndpoint.Scheme switch
            {
                "https" => "https",
                "http" => "http",
                _ => "tcp"
            }
        };
        configurePort?.Invoke(portOptions);

        var childName = $"{tunnel.Name}-{targetResource.Name}-{targetEndpoint.EndpointName}-port";
        var portResource = new DevTunnelPortResource(
            childName,
            tunnel,
            targetEndpoint,
            portOptions);

        tunnelBuilder.ApplicationBuilder.AddResource(portResource)
            .WithParentRelationship(tunnelBuilder) // visual grouping beneath the tunnel
            .WithEndpoint(name: DevTunnelPortResource.PublicEndpointName, scheme: portOptions.Protocol) // runtime URL supplied when running
            .WithInitialState(new()
            {
                ResourceType = "DevTunnelPort",
                CreationTimeStamp = DateTime.UtcNow,
                State = KnownResourceStates.NotStarted,
                Properties =
                [
                    new(CustomResourceKnownProperties.Source, "Aspire.Hosting.DevTunnels"),
                    new("TargetResource", targetResource.Name),
                    new("TargetEndpoint", targetEndpoint.EndpointName),
                    new("Protocol", portOptions.Protocol)
                ]
            });
    }
}

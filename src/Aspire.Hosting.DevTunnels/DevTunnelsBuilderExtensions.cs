// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.DevTunnels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting;

/// <summary>
/// Fluent extensions to add and configure Dev Tunnels in an Aspire AppHost.
/// Implements lifecycle by subscribing to builder eventing from these extension methods.
/// </summary>
public static class DevTunnelsBuilderExtensions
{
    /// <summary>
    /// Adds a Dev Tunnel resource. Inert unless ports are added.
    /// Subscribes to lifecycle events via builder.Eventing to manage the tunnel at runtime.
    /// </summary>
    public static IResourceBuilder<DevTunnelResource> AddDevTunnel(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        Action<DevTunnelOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        var options = new DevTunnelOptions();
        configure?.Invoke(options);

        var tunnel = new DevTunnelResource(name, options);

        var rb = builder.AddResource(tunnel)
            .WithInitialState(new CustomResourceSnapshot
            {
                ResourceType = "DevTunnel",
                CreationTimeStamp = DateTime.UtcNow,
                State = KnownResourceStates.NotStarted,
                Properties = [new(CustomResourceKnownProperties.Source, "Aspire.Hosting.DevTunnels")]
            });

        //
        // Lifecycle: subscribe via Eventing (no IDistributedApplicationLifecycleHook).
        //
        rb.OnInitializeResource(static async (resource, init, token) =>
        {
            var tunnel = (DevTunnelResource)resource;
            var logger = init.Logger;
            var notifications = init.Notifications;

            // TODO: Need to publish BeforeStart event for this resource here
            await notifications.PublishUpdateAsync(tunnel, s => s with { State = KnownResourceStates.Starting }).ConfigureAwait(false);

            // Find ports under this tunnel by looking at the application model
            // We need to find all DevTunnelPortResource instances that have this tunnel as their parent
            var appModel = init.Services.GetRequiredService<DistributedApplicationModel>();
            var children = appModel.Resources
                .OfType<DevTunnelPortResource>()
                .Where(p => p.Tunnel == tunnel)
                .ToList();

            if (children.Count == 0)
            {
                logger.LogInformation("Dev tunnel '{Tunnel}' has no ports so will not start.", tunnel.Name);
                await notifications.PublishUpdateAsync(tunnel, s => s with { State = KnownResourceStates.Finished }).ConfigureAwait(false);
                return;
            }

            logger.LogInformation("Starting dev tunnel '{Tunnel}' with {Count} port(s)...", tunnel.Name, children.Count);

            // Start an in-process host to "expose" the ports. This is a placeholder implementation;
            // a full implementation should use Microsoft.DevTunnels.Connections to create and start a real tunnel.
            var host = new DevTunnelInProcessHost(tunnel, init.Services.GetRequiredService<ResourceLoggerService>(), init.Notifications);

            try
            {
                await host.StartAsync(children, init.Services, token).ConfigureAwait(false);
                await notifications.PublishUpdateAsync(tunnel, s => s with { State = KnownResourceStates.Running }).ConfigureAwait(false);

                // Remain active until cancellation to represent the host's lifetime.
                try { await Task.Delay(Timeout.Infinite, token).ConfigureAwait(false); }
                catch (OperationCanceledException) { }

                await notifications.PublishUpdateAsync(tunnel, s => s with { State = KnownResourceStates.Stopping }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start dev tunnel '{Tunnel}'.", tunnel.Name);
                await notifications.PublishUpdateAsync(tunnel, s => s with { State = KnownResourceStates.FailedToStart }).ConfigureAwait(false);
                throw;
            }
            finally
            {
                await notifications.PublishUpdateAsync(tunnel, s => s with { State = KnownResourceStates.Exited }).ConfigureAwait(false);
            }
        });

        return rb;
    }

    /// <summary>
    /// Adds ports on the tunnel for all HTTP/HTTPS endpoints found on the referenced resource.
    /// </summary>
    public static IResourceBuilder<DevTunnelResource> WithReference(
        this IResourceBuilder<DevTunnelResource> tunnelBuilder,
        IResource resourceWithEndpoints,
        Action<DevTunnelPortOptions>? configurePort = null)
    {
        ArgumentNullException.ThrowIfNull(tunnelBuilder);
        ArgumentNullException.ThrowIfNull(resourceWithEndpoints);

        // Discover endpoints named "http" and "https" by convention.
        var endpointNames = GetHttpEndpointNames(resourceWithEndpoints);
        foreach (var epName in endpointNames)
        {
            AddDevTunnelPort(tunnelBuilder, resourceWithEndpoints, epName, configurePort);
        }

        return tunnelBuilder;
    }

    /// <summary>
    /// Adds ports on the tunnel for the specified endpoints on the referenced resource.
    /// </summary>
    public static IResourceBuilder<DevTunnelResource> WithReferences(
        this IResourceBuilder<DevTunnelResource> tunnelBuilder,
        IResource resourceWithEndpoints,
        IEnumerable<string> endpointNames,
        Action<DevTunnelPortOptions>? configurePort = null)
    {
        ArgumentNullException.ThrowIfNull(tunnelBuilder);
        ArgumentNullException.ThrowIfNull(resourceWithEndpoints);
        ArgumentNullException.ThrowIfNull(endpointNames);

        foreach (var name in endpointNames)
        {
            AddDevTunnelPort(tunnelBuilder, resourceWithEndpoints, name, configurePort);
        }

        return tunnelBuilder;
    }

    /// <summary>
    /// Adds a dev tunnel and tunnels all HTTP endpoints on the provided resource.
    /// </summary>
    public static IResourceBuilder<IResource> WithTunneledHttpEndpoints<TResource>(
        this IResourceBuilder<TResource> resourceBuilder,
        IResourceBuilder<DevTunnelResource> tunnelBuilder,
        Action<DevTunnelPortOptions>? configurePort = null)
        where TResource : IResource
    {
        ArgumentNullException.ThrowIfNull(resourceBuilder);
        ArgumentNullException.ThrowIfNull(tunnelBuilder);

        var resource = resourceBuilder.Resource;
        tunnelBuilder.WithReference(resource, configurePort);

        return resourceBuilder.AsResourceBuilder();
    }

    private static IEnumerable<string> GetHttpEndpointNames(IResource resource)
    {
        // In a more complete implementation, we could inspect the resource's endpoint annotations
        // to discover which endpoints exist. For now, return common convention endpoints.
        _ = resource; // Mark as used
        return new[] { "http", "https" };
    }

    private static void AddDevTunnelPort(
        IResourceBuilder<DevTunnelResource> tunnelBuilder,
        IResource sourceResource,
        string sourceEndpointName,
        Action<DevTunnelPortOptions>? configurePort)
    {
        var tunnel = tunnelBuilder.Resource;
        var portOptions = new DevTunnelPortOptions
        {
            Name = sourceEndpointName,
            Protocol = sourceEndpointName.Equals("https", StringComparison.OrdinalIgnoreCase) ? "https" : "http"
        };
        configurePort?.Invoke(portOptions);

        var childName = $"{tunnel.Name}-{sourceResource.Name}-{sourceEndpointName}-port";
        var portResource = new DevTunnelPortResource(
            childName,
            tunnel,
            sourceResource,
            sourceEndpointName,
            portOptions);

        var childBuilder = tunnelBuilder.ApplicationBuilder.AddResource(portResource)
            .WithParentRelationship(tunnelBuilder) // visual grouping beneath the tunnel
            .WithEndpoint(name: DevTunnelPortResource.PublicEndpointName, scheme: portOptions.Protocol) // runtime URL supplied when running
            .WithInitialState(new CustomResourceSnapshot
            {
                ResourceType = "DevTunnelPort",
                CreationTimeStamp = DateTime.UtcNow,
                State = KnownResourceStates.NotStarted,
                Properties =
                [
                    new(CustomResourceKnownProperties.Source, "Aspire.Hosting.DevTunnels"),
                    new("SourceResource", sourceResource.Name),
                    new("SourceEndpoint", sourceEndpointName),
                    new("Protocol", portOptions.Protocol)
                ]
            });

        // Port lifecycle via eventing subscription (no lifecycle hook)
        childBuilder.OnInitializeResource(static async (port, init, token) =>
        {
            var logger = init.Logger;
            var notifications = init.Notifications;

            await notifications.PublishUpdateAsync(port, s => s with { State = KnownResourceStates.Starting }).ConfigureAwait(false);

            // The tunnel's OnInitializeResource will actually "start" the host and set URLs on the port's snapshot.
            // Here we just wait a tick and then mark Running; a complete implementation might coordinate via shared state.
            try
            {
                logger.LogInformation("Starting dev tunnel port '{Port}' for source '{Source}/{Endpoint}' (protocol {Protocol})",
                    port.Name, port.SourceResource.Name, port.SourceEndpointName, port.Options.Protocol);

                // In case the tunnel host already populated the URLs, publish a state move to Running.
                await notifications.PublishUpdateAsync(port, s => s with { State = KnownResourceStates.Running }).ConfigureAwait(false);

                // Keep alive until cancellation to represent an active forwarded port.
                try { await Task.Delay(Timeout.Infinite, token).ConfigureAwait(false); }
                catch (OperationCanceledException) { }

                await notifications.PublishUpdateAsync(port, s => s with { State = KnownResourceStates.Stopping }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start dev tunnel port '{Port}'.", port.Name);
                await notifications.PublishUpdateAsync(port, s => s with { State = KnownResourceStates.FailedToStart }).ConfigureAwait(false);
                throw;
            }
            finally
            {
                await notifications.PublishUpdateAsync(port, s => s with { State = KnownResourceStates.Exited }).ConfigureAwait(false);
            }
        });
    }

    private static IResourceBuilder<IResource> AsResourceBuilder<T>(this IResourceBuilder<T> builder) where T : IResource =>
        (IResourceBuilder<IResource>)builder;
}
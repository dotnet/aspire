// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.DevTunnels;
using Aspire.Hosting.Eventing;
using Microsoft.Extensions.DependencyInjection;
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
        string? tunnelId = null,
        Action<DevTunnelOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        var options = new DevTunnelOptions();
        configure?.Invoke(options);

        var workingDirectory = builder.AppHostDirectory;
        var appHostSha = builder.Configuration["Aspire:AppHostSha"]?[..8];
        tunnelId ??= $"{name}-{builder.Environment.ApplicationName}-{appHostSha}";

        var rb = builder.AddResource(new DevTunnelResource(name, tunnelId, "devtunnel", workingDirectory, options))
            //.WithExplicitStart()
            .WithArgs("host", tunnelId)
            .WithInitialState(new()
            {
                ResourceType = "DevTunnel",
                CreationTimeStamp = DateTime.UtcNow,
                State = KnownResourceStates.NotStarted,
                Properties = []
            });

        // Lifecycle
        rb.OnBeforeResourceStarted(static async (resource, e, ct) =>
        {
            var tunnelResource = resource;
            var logger = e.Services.GetRequiredService<ResourceLoggerService>().GetLogger(tunnelResource);
            var eventing = e.Services.GetRequiredService<DistributedApplicationEventing>();
            var notifications = e.Services.GetRequiredService<ResourceNotificationService>();
#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var interaction = e.Services.GetRequiredService<IInteractionService>();
#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            var cli = new DevTunnelCli();

            // Login to the cev tunnels service if needed
            if (await cli.UserIsLoggedInAsync(logger, ct).ConfigureAwait(false) != true)
            {
                if (interaction.IsAvailable)
                {
#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                    await interaction.PromptNotificationAsync(
                        "Dev tunnels",
                        "One or more dev tunnels resources require authentication to continue.",
                        new() { Intent = MessageIntent.Warning, PrimaryButtonText = "Login", ShowDismiss = false },
                        ct).ConfigureAwait(false);
#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                }
                var loginResult = await cli.UserLoginMicrosoftAsync(logger, cancellationToken: ct).ConfigureAwait(false);
                if (loginResult != 0)
                {
                    throw new DistributedApplicationException($"Failed to login to the dev tunnels service (exit code {loginResult}).");
                }
            }

            // Create the dev tunnel if needed
            await cli.CreateOrUpdateTunnelAsync(logger, tunnelResource.TunnelId, tunnelResource.Name, tunnelResource.Options, ct).ConfigureAwait(false);

            // Subscribe to endpoint allocated events for resources being exposed by the tunnel
            foreach (var portResource in tunnelResource.Ports)
            {
                eventing.Subscribe<ResourceEndpointsAllocatedEvent>(portResource.TargetEndpoint.Resource, async (e, ct) =>
                {
                    if (!portResource.TargetEndpoint.IsAllocated)
                    {
                        return;
                    }

                    var portLogger = e.Services.GetRequiredService<ResourceLoggerService>().GetLogger(portResource);

                    await cli.CreateOrUpdatePortAsync(portLogger, tunnelResource.TunnelId, portResource.TargetEndpoint.Port, new()
                    {
                        Labels = [portResource.TargetEndpoint.Resource.Name, portResource.TargetEndpoint.EndpointName]
                    }, ct).ConfigureAwait(false);

                    // Allocate endpoint to the tunnel port here
                    if (portResource.TryGetEndpoints(out var portEndpoints))
                    {
                        var publicEndpoint = portEndpoints.FirstOrDefault(ep => ep.Name == DevTunnelPortResource.PublicEndpointName);
                        var inspectionEndpoint = portEndpoints.FirstOrDefault(ep => ep.Name == DevTunnelPortResource.InspectionEndpointName);

                        Debug.Assert(publicEndpoint is not null);

                        publicEndpoint.AllocatedEndpoint = new(publicEndpoint, "", 1234) { };
                    }

                    // TODO: Add resource URLs with port-forwarding and inspect URLs
                    // TODO: Publish resource update
                });
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
            .WithEndpoint(name: DevTunnelPortResource.InspectionEndpointName, scheme: "https")
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

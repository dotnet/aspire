// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.DevTunnels;
using Aspire.Hosting.Eventing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Dev tunnels resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static partial class DevTunnelsResourceBuilderExtensions
{
    /// <summary>
    /// Adds a Dev tunnel resource.
    /// </summary>
    public static IResourceBuilder<DevTunnelResource> AddDevTunnel(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        string? tunnelId = null,
        DevTunnelOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        options ??= new DevTunnelOptions();

        var appHostSha = builder.Configuration["AppHost:Sha256"]?[..8];
        tunnelId ??= $"{name}-{builder.Environment.ApplicationName.Replace(".", "-")}-{appHostSha}".ToLowerInvariant();

        // Validate the TunnelId format [a-z0-9][a-z0-9-]{1,58}[a-z0-9]
        if (!TunnelIdRegex().IsMatch(tunnelId))
        {
            throw new ArgumentException($"""
                The tunnel ID '{tunnelId}' is invalid. A valid tunnel ID must:
                - start and end with a letter or number
                - consist of lowercase letters, numbers, and hyphens
                - be 1-58 characters long
                """, nameof(tunnelId));
        }

        // Add services
        builder.Services.TryAddSingleton<DevTunnelEnvironmentManager>();
        builder.Services.TryAddSingleton<IDevTunnelClient, DevTunnelCliClient>();

        var workingDirectory = builder.AppHostDirectory;
        var tunnelResource = new DevTunnelResource(name, tunnelId, DevTunnelCli.GetCliPath(builder.Configuration), workingDirectory, options);

        // Health check
        var healtCheckKey = $"{name}-check";
        builder.Services.AddHealthChecks().Add(new HealthCheckRegistration(
            healtCheckKey,
            services => new DevTunnelHealthCheck(services.GetRequiredService<IDevTunnelClient>(), tunnelResource),
            failureStatus: default,
            tags: default,
            timeout: default));

        var rb = builder.AddResource(tunnelResource)
            .WithArgs("host", tunnelId)
            .WithIconName("CloudBidirectional")
            .WithInitialState(new()
            {
                ResourceType = "DevTunnel",
                CreationTimeStamp = DateTime.UtcNow,
                State = KnownResourceStates.NotStarted,
                Properties = []
            })
            .ExcludeFromManifest() // Dev tunnels do not get deployed
            .WithHealthCheck(healtCheckKey)
            // Lifecycle
            .OnBeforeResourceStarted(static async (tunnelResource, e, ct) =>
            {
                var logger = e.Services.GetRequiredService<ResourceLoggerService>().GetLogger(tunnelResource);
                var devTunnelEnvironmentManager = e.Services.GetRequiredService<DevTunnelEnvironmentManager>();
                var devTunnelClient = e.Services.GetRequiredService<IDevTunnelClient>();

                // Login to the dev tunnels service if needed
                await devTunnelEnvironmentManager.EnsureUserLoggedInAsync(ct).ConfigureAwait(false);
                // Create the dev tunnel
                _ = await devTunnelClient.CreateOrUpdateTunnelAsync(tunnelResource.TunnelId, tunnelResource.Options, ct).ConfigureAwait(false);
            })
            .OnResourceStopped(static (tunnelResource, e, ct) =>
            {
                // Tunnel stopped, mark status as null
                tunnelResource.LastKnownStatus = null;
                return Task.CompletedTask;
            });

        // Tunnels will expire after some time not being hosted so we won't foricibly delete them when the resource or AppHost is stopped

        return rb;
    }

    /// <summary>
    /// Adds ports on the tunnel for all endpoints found on the referenced resource.
    /// </summary>
    public static IResourceBuilder<DevTunnelResource> WithReference<TResource>(
        this IResourceBuilder<DevTunnelResource> tunnelBuilder,
        IResourceBuilder<TResource> resourceBuilder,
        DevTunnelPortOptions? portOptions = null)
        where TResource : IResourceWithEndpoints
    {
        ArgumentNullException.ThrowIfNull(tunnelBuilder);
        ArgumentNullException.ThrowIfNull(resourceBuilder);

        foreach (var endpoint in resourceBuilder.Resource.GetEndpoints())
        {
            AddDevTunnelPort(tunnelBuilder, endpoint, portOptions);
        }

        return tunnelBuilder;
    }

    // NOTE: This is a separate overload to ensure it's bound over the generic service discovery extension method
    /// <summary>
    /// Adds ports on the tunnel for the specified endpoints on the referenced resource.
    /// </summary>
    public static IResourceBuilder<DevTunnelResource> WithReference(
        this IResourceBuilder<DevTunnelResource> tunnelBuilder,
        EndpointReference targetEndpoint)
        => tunnelBuilder.WithReference(targetEndpoint, portOptions: null);

    /// <summary>
    /// Adds ports on the tunnel for the specified endpoints on the referenced resource.
    /// </summary>
    public static IResourceBuilder<DevTunnelResource> WithReference(
        this IResourceBuilder<DevTunnelResource> tunnelBuilder,
        EndpointReference targetEndpoint,
        DevTunnelPortOptions? portOptions)
    {
        ArgumentNullException.ThrowIfNull(tunnelBuilder);
        ArgumentNullException.ThrowIfNull(targetEndpoint);

        AddDevTunnelPort(tunnelBuilder, targetEndpoint, portOptions);

        return tunnelBuilder;
    }

    private static void AddDevTunnelPort(
        IResourceBuilder<DevTunnelResource> tunnelBuilder,
        EndpointReference targetEndpoint,
        DevTunnelPortOptions? portOptions)
    {
        var tunnel = tunnelBuilder.Resource;
        var targetResource = targetEndpoint.Resource;

        if (targetEndpoint.Resource.Annotations
                .OfType<EndpointAnnotation>()
                .SingleOrDefault(a => StringComparers.EndpointAnnotationName.Equals(a.Name, targetEndpoint.EndpointName)) is { } targetEndpointAnnotation)
        {
            // The target endpoint already exists so let's ensure it's target is localhost
            if (!string.Equals(targetEndpointAnnotation.TargetHost, "localhost", StringComparison.OrdinalIgnoreCase)
                && !targetEndpointAnnotation.TargetHost.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
            {
                // Target endpoint is not localhost so can't be tunneled
                throw new ArgumentException($"Cannot tunnel endpoint '{targetEndpointAnnotation.Name}' with host '{targetEndpointAnnotation.TargetHost}' on resource '{targetResource.Name}' because it is not a localhost endpoint.", nameof(targetEndpoint));
            }
        }

        portOptions ??= new();
        portOptions.Protocol = targetEndpoint.Scheme switch
        {
            "https" or "http" => targetEndpoint.Scheme,
            _ => throw new ArgumentException($"Cannot tunnel endpoint '{targetEndpoint.EndpointName}' on resource '{targetResource.Name}' because it uses unsupported scheme '{targetEndpoint.Scheme}'. Only 'http' and 'https' endpoints can be tunneled."),
        };
        portOptions.Description ??= $"{targetResource.Name}/{targetEndpoint.EndpointName}";

        var portName = $"{tunnel.Name}-{targetResource.Name}-{targetEndpoint.EndpointName}";
        var portResource = new DevTunnelPortResource(
            portName,
            tunnel,
            targetEndpoint,
            portOptions);

        tunnel.Ports.Add(portResource);

        var portBuilder = tunnelBuilder.ApplicationBuilder.AddResource(portResource)
            // visual grouping beneath the tunnel
            .WithParentRelationship(tunnelBuilder)
            // indicate the target resource relationship
            .WithReferenceRelationship(targetResource)
            // public tunnel URL endpoint
            .WithHttpsEndpoint(name: DevTunnelPortResource.TunnelEndpointName, isProxied: false)
            // NOTE:
            // The endpoint target full host is set by the dev tunnels service and is not known in advance, but the suffix is always devtunnels.ms
            // We might consider updating the central logic that creates endpoint URLs to allow setting a target host like *.devtunnels.ms & if the
            // host of the allocated endpoint matches that pattern, *don't* try to add a localhost version of the URL too (because it won't work), e.g.:
            //  .WithEndpoint(DevTunnelPortResource.TunnelEndpointName, e => { e.TargetHost = "*.devtunnels.ms"; }, createIfNotExists: false)
            .WithUrls(static context =>
            {
                var urls = context.Urls;

                // Remove the port from the tunnel URL since the dev tunnels service always uses 443 for HTTPS
                if (urls.FirstOrDefault(u => string.Equals(u.Endpoint?.EndpointName, DevTunnelPortResource.TunnelEndpointName, StringComparisons.EndpointAnnotationName)
                                             && !string.Equals(new UriBuilder(u.Url).Host, "localhost")) is { } tunnelUrl)
                {
                    tunnelUrl.Url = new UriBuilder(tunnelUrl.Url).Uri.ToString();
                }

                // Remove the localhost version of the tunnel URL that's added by the central endpoint URL logic
                // HACK: See the NOTE above about potentially handling this more generically in the central endpoint URL logic
                if (urls.FirstOrDefault(u => string.Equals(u.Endpoint?.EndpointName, DevTunnelPortResource.TunnelEndpointName, StringComparisons.EndpointAnnotationName)
                                             && string.Equals(new UriBuilder(u.Url).Host, "localhost", StringComparison.OrdinalIgnoreCase)) is { } localhostTunnelUrl)
                {
                    urls.Remove(localhostTunnelUrl);
                }

                // Remove any existing inspect URL
                if (urls.FirstOrDefault(u => string.Equals(u.DisplayText, "Inspect", StringComparison.OrdinalIgnoreCase)) is { } inspectUrl)
                {
                    urls.Remove(inspectUrl);
                }

                // Add the inspect URL if available
                var portResource = (DevTunnelPortResource)context.Resource;
                if (portResource.LastKnownStatus?.PortUri is { } portUri)
                {
                    // If tunnel host is sdfdff-3456.usw.devtunnels.ms, the inspect host is sdfdff-3456-inspect.usw.devtunnels.ms
                    var hostPrefixLength = portUri.Host.IndexOf('.');
                    var hostPrefix = portUri.Host[..hostPrefixLength];
                    var hostSuffix = portUri.Host[hostPrefixLength..];
                    urls.Add(new()
                    {
                        Url = new UriBuilder(portUri) { Host = $"{hostPrefix}-inspect{hostSuffix}" }.Uri.ToString(),
                        DisplayText = "Inspect",
                        DisplayLocation = UrlDisplayLocation.DetailsOnly
                    });
                }
            })
            .WithIconName("VirtualNetwork")
            .WithInitialState(new()
            {
                ResourceType = "DevTunnelPort",
                CreationTimeStamp = DateTime.UtcNow,
                State = KnownResourceStates.NotStarted,
                Properties =
                [
                    new(CustomResourceKnownProperties.Source, $"{targetResource.Name}/{targetEndpoint.EndpointName}"),
                    new("TargetResource", targetResource.Name),
                    new("TargetEndpoint", targetEndpoint.EndpointName),
                    new("Protocol", portOptions.Protocol),
                    new("Description", portOptions.Description),
                    new("Labels", portOptions.Labels is null ? "" : $"{string.Join(", ", portOptions.Labels)}"),
                ]
            });

        // When the target endpoint is allocated, create or update the port on the dev tunnel
        var targetResourceBuilder = tunnelBuilder.ApplicationBuilder.CreateResourceBuilder(targetResource);
        targetResourceBuilder.OnResourceEndpointsAllocated(async (resource, e, ct) =>
        {
            if (!portResource.TargetEndpoint.IsAllocated)
            {
                // Target endpoint is not allocated, ignore
                return;
            }

            var portLogger = e.Services.GetRequiredService<ResourceLoggerService>().GetLogger(portResource);

            // We do this check now so that we're verifying the allocated endpoint's address
            if (!string.Equals(portResource.TargetEndpoint.Host, "localhost", StringComparison.OrdinalIgnoreCase) &&
                !portResource.TargetEndpoint.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
            {
                // Target endpoint is not localhost so can't be tunneled
                portLogger.LogError("Cannot tunnel endpoint '{Endpoint}' with host '{Host}' on resource '{Resource}' because it is not a localhost endpoint.", portResource.TargetEndpoint.EndpointName, portResource.TargetEndpoint.Host, portResource.TargetEndpoint.Resource.Name);
                return;
            }

            // Create/update the port on the tunnel
            var devTunnelClient = e.Services.GetRequiredService<IDevTunnelClient>();
            portResource.Options.Labels ??= [];
            // TODO: Validate and add the labels here
            // Labels: The field Labels must match the regular expression '[\w-=]{1,50}'.
            //portResource.Options.Labels.Add(portResource.TargetEndpoint.Resource.Name);
            //portResource.Options.Labels.Add(portResource.TargetEndpoint.EndpointName);
            _ = await devTunnelClient.CreateOrUpdatePortAsync(
                portResource.DevTunnel.TunnelId,
                portResource.TargetEndpoint.Port,
                portResource.Options,
                ct)
                .ConfigureAwait(false);

            portLogger.LogInformation("Created/updated dev tunnel port '{Port}' on tunnel '{Tunnel}' targeting endpoint '{Endpoint}' on resource '{TargetResource}'", portResource.TargetEndpoint.Port, portResource.DevTunnel.TunnelId, portResource.TargetEndpoint.EndpointName, portResource.TargetEndpoint.Resource.Name);
        });

        // Lifecycle from the tunnel
        tunnelBuilder
            .OnBeforeResourceStarted(async (tunnelResource, e, ct) =>
            {
                // Tunnel starting, clear port status
                var portLogger = e.Services.GetRequiredService<ResourceLoggerService>().GetLogger(portResource);
                portLogger.LogInformation("Tunnel starting, waiting for it to be healthy");
                var notifications = e.Services.GetRequiredService<ResourceNotificationService>();
                await notifications.PublishUpdateAsync(portResource, snapshot => snapshot with
                {
                    State = KnownResourceStates.Waiting
                }).ConfigureAwait(false);
            })
            .OnResourceReady(async (tunnelResource, e, ct) =>
            {
                // Update the port now that the tunnel is ready
                // We need to do this in this handler so that it runs every time the tunnel is started
                var tunnelStatus = portResource.DevTunnel.LastKnownStatus;
                var tunnelPortStatus = portResource.LastKnownStatus;

                // Ensure the expected state for the port still exists after the ready event was raised
                if (tunnelStatus?.HostConnections is 0 or null || tunnelPortStatus?.PortUri is null)
                {
                    // Tunnel is not ready
                    return;
                }

                var services = e.Services;
                var eventing = services.GetRequiredService<IDistributedApplicationEventing>();
                var notifications = services.GetRequiredService<ResourceNotificationService>();

                // Mark the port as starting
                await eventing.PublishAsync<BeforeResourceStartedEvent>(new(portResource, services), ct).ConfigureAwait(false);
                await notifications.PublishUpdateAsync(portResource, snapshot => snapshot with
                {
                    State = KnownResourceStates.Starting,
                    StartTimeStamp = DateTime.UtcNow
                }).ConfigureAwait(false);

                // Allocate endpoint to the tunnel port
                if (!portResource.TryGetEndpoints(out var portEndpoints)
                    || portEndpoints.FirstOrDefault(ep => ep.Name == DevTunnelPortResource.TunnelEndpointName) is not { } publicEndpoint)
                {
                    throw new DistributedApplicationException($"Could not find public tunnel endpoint for port resource '{portResource.Name}'.");
                }
                var raiseEndpointsAllocatedEvent = publicEndpoint.AllocatedEndpoint is null;
                publicEndpoint.AllocatedEndpoint = new(publicEndpoint, tunnelPortStatus.PortUri.Host, 443 /* Always 443 for public tunnel endpoint */);

                // We can only raise the endpoints allocated event once as the central URL logic assumes it's a one-time event per resource.
                // AFAIK the PortUri should not change between restarts of the same tunnel (with same tunnel ID) so we don't need to update the URLs for
                // the resource every time the tunnel starts, just the first time.
                if (raiseEndpointsAllocatedEvent)
                {
                    await eventing.PublishAsync<ResourceEndpointsAllocatedEvent>(new(portResource, services), ct).ConfigureAwait(false);
                }

                // Mark the port as running
                await notifications.PublishUpdateAsync(portResource, snapshot => snapshot with
                {
                    State = KnownResourceStates.Running,
                    Urls = [.. snapshot.Urls.Select(u => u with { IsInactive = false /* All URLs active */ })]
                }).ConfigureAwait(false);

                var portLogger = services.GetRequiredService<ResourceLoggerService>().GetLogger(portResource);
                portLogger.LogInformation("Forwarding from {PortUrl} to {TargetUrl} ({TargetResourceName}/{TargetEndpointName})", tunnelPortStatus.PortUri.ToString().TrimEnd('/'), portResource.TargetEndpoint.Url, portResource.TargetEndpoint.Resource.Name, portResource.TargetEndpoint.EndpointName);
            })
            .OnResourceStopped(async (tunnelResource, e, ct) =>
            {
                // Tunnel stopped, mark port as stopped too
                portResource.LastKnownStatus = null;

                var portLogger = e.Services.GetRequiredService<ResourceLoggerService>().GetLogger(portResource);
                var notifications = e.Services.GetRequiredService<ResourceNotificationService>();
                var eventing = e.Services.GetRequiredService<IDistributedApplicationEventing>();

                portLogger.LogInformation("Port forwarding stopped");
                CustomResourceSnapshot? stoppedSnapshot = default;
                await notifications.PublishUpdateAsync(portResource, snapshot => stoppedSnapshot = snapshot with
                {
                    State = KnownResourceStates.Finished,
                    StopTimeStamp = DateTime.UtcNow,
                    Urls = [.. snapshot.Urls.Select(u => u with { IsInactive = true /* All URLs inactive */ })]
                }).ConfigureAwait(false);
                await eventing.PublishAsync<ResourceStoppedEvent>(new(portResource, e.Services, new(portResource, portResource.Name, stoppedSnapshot!)), ct).ConfigureAwait(false);
            });
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{1,58}[a-z0-9]$")]
    private static partial Regex TunnelIdRegex();
}

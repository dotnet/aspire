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
    // TODO: Put proper doc comments here
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

        tunnelId ??= $"{name}-{builder.Environment.ApplicationName.Replace(".", "-")}-{builder.Configuration["AppHost:Sha256"]?[..8]}".ToLowerInvariant();

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
        builder.Services.TryAddSingleton<DevTunnelCliInstallationManager>();
        builder.Services.TryAddSingleton<DevTunnelLoginManager>();
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
                Properties = [
                    new("TunnelId", tunnelId)
                ]
            })
            .ExcludeFromManifest() // Dev tunnels do not get deployed
            .WithHealthCheck(healtCheckKey)
            // Lifecycle
            .OnBeforeResourceStarted(static async (tunnelResource, e, ct) =>
            {
                var logger = e.Services.GetRequiredService<ResourceLoggerService>().GetLogger(tunnelResource);
                var eventing = e.Services.GetRequiredService<IDistributedApplicationEventing>();
                var devTunnelCliInstallationManager = e.Services.GetRequiredService<DevTunnelCliInstallationManager>();
                var devTunnelEnvironmentManager = e.Services.GetRequiredService<DevTunnelLoginManager>();
                var devTunnelClient = e.Services.GetRequiredService<IDevTunnelClient>();

                // Ensure CLI is available
                await devTunnelCliInstallationManager.EnsureInstalledAsync(ct).ConfigureAwait(false);

                // Login to the dev tunnels service if needed
                logger.LogInformation("Ensuring user is logged in to dev tunnel service");
                await devTunnelEnvironmentManager.EnsureUserLoggedInAsync(ct).ConfigureAwait(false);

                // Create the dev tunnel
                logger.LogInformation("Creating or updating dev tunnel '{TunnelId}'", tunnelResource.TunnelId);
                var tunnelStatus = await devTunnelClient.CreateOrUpdateTunnelAsync(tunnelResource.TunnelId, tunnelResource.Options, ct).ConfigureAwait(false);

                // Mark the tunnel created
                tunnelResource.TunnelCreatedTcs.SetResult(tunnelStatus);
                logger.LogDebug("Dev tunnel '{TunnelId}' created/updated", tunnelResource.TunnelId);

                // Wait for the ports to be created
                logger.LogDebug("Waiting for ports to be created/updated");
                await eventing.PublishAsync<DevTunnelResourceStartedEvent>(new(tunnelResource), EventDispatchBehavior.NonBlockingConcurrent, ct).ConfigureAwait(false);
                await Task.WhenAll(tunnelResource.Ports.Select(p => p.PortCreatedTask)).ConfigureAwait(false);
            })
            .OnResourceStopped(static (tunnelResource, e, ct) =>
            {
                // Tunnel stopped, mark status as null
                tunnelResource.TunnelCreatedTcs = new();
                tunnelResource.LastKnownStatus = null;
                return Task.CompletedTask;
            });

        // Tunnels will expire after some time not being hosted so we won't foricibly delete them when the resource or AppHost is stopped

        return rb;
    }

    /// <summary>
    /// Adds ports on the dev tunnel for all endpoints found on the referenced resource.
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
    /// Exposes the specified endpoint via the dev tunnel.
    /// </summary>
    public static IResourceBuilder<DevTunnelResource> WithReference(
        this IResourceBuilder<DevTunnelResource> tunnelBuilder,
        EndpointReference targetEndpoint)
        => tunnelBuilder.WithReference(targetEndpoint, portOptions: null);

    /// <summary>
    /// Exposes the specified endpoint via the dev tunnel.
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

    /// <summary>
    /// Allows the tunnel to be publicly accessed without authentication.
    /// </summary>
    /// <remarks>
    /// Sets <see cref="DevTunnelOptions.AllowAnonymous"/> to <c>true</c> on <see cref="DevTunnelResource.Options"/> .
    /// </remarks>
    /// <param name="tunnelBuilder">The resource builder.</param>
    /// <returns>The resource builder.</returns>
    public static IResourceBuilder<DevTunnelResource> WithAnonymousAccess(this IResourceBuilder<DevTunnelResource> tunnelBuilder)
    {
        tunnelBuilder.Resource.Options.AllowAnonymous = true;
        return tunnelBuilder;
    }

    /// <summary>
    /// Gets the endpoint for the dev tunnel port that references the specified target resource and endpoint name.
    /// </summary>
    /// <param name="tunnelBuilder"></param>
    /// <param name="targetResource"></param>
    /// <param name="endpointName"></param>
    /// <returns>The endpoint.</returns>
    public static EndpointReference GetEndpoint(this IResourceBuilder<DevTunnelResource> tunnelBuilder,
        IResourceBuilder<IResourceWithEndpoints> targetResource, string endpointName)
    {
        return tunnelBuilder.Resource.Ports.FirstOrDefault(p => p.TargetEndpoint.Resource == targetResource.Resource
                                                                && string.Equals(p.TargetEndpoint.EndpointName, endpointName, StringComparisons.EndpointAnnotationName))
            ?.TunnelEndpoint
            ?? throw new ArgumentException($"Dev tunnel '{tunnelBuilder.Resource.Name}' does not have a port referencing endpoint '{endpointName}' on resource '{targetResource.Resource.Name}'.", nameof(targetResource));
    }

    private static void AddDevTunnelPort(
        IResourceBuilder<DevTunnelResource> tunnelBuilder,
        EndpointReference targetEndpoint,
        DevTunnelPortOptions? portOptions)
    {
        var tunnel = tunnelBuilder.Resource;
        var targetResource = targetEndpoint.Resource;

        if (tunnel.Ports.FirstOrDefault(p => p.TargetEndpoint == targetEndpoint) is { } existingPort)
        {
            // Port already added to the tunnel for this endpoint
            throw new ArgumentException($"Target endpoint '{targetEndpoint.EndpointName}' on resource '{targetEndpoint.Resource.Name}' has already been added to dev tunnel '{tunnel.Name}'.", nameof(targetEndpoint));
        }

        if (targetEndpoint.Resource.Annotations.OfType<EndpointAnnotation>()
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

        portResource.Annotations.Add(portResource.TunnelEndpointAnnotation);
        var portBuilder = tunnelBuilder.ApplicationBuilder.AddResource(portResource)
            // visual grouping beneath the tunnel
            .WithParentRelationship(tunnelBuilder)
            // indicate the target resource relationship
            .WithReferenceRelationship(targetResource)
            // public tunnel URL endpoint
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
            })
            .OnInitializeResource((portResource, e, ct) =>
            {
                var eventing = e.Services.GetRequiredService<IDistributedApplicationEventing>();
                _ = eventing.Subscribe<DevTunnelResourceStartedEvent>(portResource.DevTunnel, async (evt, ct) =>
                {
                    // Tunnel starting, clear port status
                    var portLogger = e.Services.GetRequiredService<ResourceLoggerService>().GetLogger(portResource);
                    portLogger.LogInformation("Tunnel starting, waiting for it to be healthy");
                    var notifications = e.Services.GetRequiredService<ResourceNotificationService>();
                    await notifications.PublishUpdateAsync(portResource, snapshot => snapshot with
                    {
                        State = KnownResourceStates.Waiting
                    }).ConfigureAwait(false);

                    var devTunnelClient = e.Services.GetRequiredService<IDevTunnelClient>();
                    try
                    {
                        // Wait here to ensure tunnel has been created & target endpoint is allocated!
                        _ = Task.WhenAll(tunnel.TunnelCreatedTask, portResource.TargetEndpointAllocatedTask).ConfigureAwait(false);

                        portResource.Options.Labels ??= [];
                        // TODO: Validate and add the labels here
                        // Labels: The field Labels must match the regular expression '[\w-=]{1,50}'.
                        //portResource.Options.Labels.Add(portResource.TargetEndpoint.Resource.Name);
                        //portResource.Options.Labels.Add(portResource.TargetEndpoint.EndpointName);

                        // Create the tunnel port
                        _ = await devTunnelClient.CreateOrUpdatePortAsync(
                            portResource.DevTunnel.TunnelId,
                            portResource.TargetEndpoint.Port,
                            portResource.Options,
                            ct)
                        .ConfigureAwait(false);

                        // Mark the port created
                        portResource.PortCreatedTcs.SetResult();

                        portLogger.LogInformation("Created/updated dev tunnel port '{Port}' on tunnel '{Tunnel}' targeting endpoint '{Endpoint}' on resource '{TargetResource}'.", portResource.TargetEndpoint.Port, portResource.DevTunnel.TunnelId, portResource.TargetEndpoint.EndpointName, portResource.TargetEndpoint.Resource.Name);
                    }
                    catch (Exception ex)
                    {
                        portLogger.LogError(ex, "Error trying to create/update dev tunnel port '{Port}' on tunnel '{Tunnel}': {Error}", portResource.TargetEndpoint.Port, portResource.DevTunnel.TunnelId, ex.Message);
                    }
                });
                return Task.CompletedTask;
            });

        // When the target endpoint is allocated, validate it and mark the TCS accordingly
        var targetResourceBuilder = tunnelBuilder.ApplicationBuilder.CreateResourceBuilder(targetResource);
        targetResourceBuilder.OnResourceEndpointsAllocated((resource, e, ct) =>
        {
            var portLogger = e.Services.GetRequiredService<ResourceLoggerService>().GetLogger(portResource);

            if (!portResource.TargetEndpoint.IsAllocated)
            {
                // Target endpoint is not allocated, ignore
                portLogger.LogWarning("Target resource endpoints allocated event was fired but target endpoint was not allocated.");
                return Task.CompletedTask;
            }

            portLogger.LogDebug("Target resource endpoints allocated.");

            // We do this check now so that we're verifying the allocated endpoint's address
            if (!string.Equals(portResource.TargetEndpoint.Host, "localhost", StringComparison.OrdinalIgnoreCase) &&
                !portResource.TargetEndpoint.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
            {
                // Target endpoint is not localhost so can't be tunneled
                portLogger.LogError("Cannot tunnel endpoint '{Endpoint}' with host '{Host}' on resource '{Resource}' because it is not a localhost endpoint.", portResource.TargetEndpoint.EndpointName, portResource.TargetEndpoint.Host, portResource.TargetEndpoint.Resource.Name);
                portResource.TargetEndpointAllocatedTcs.SetException(new DistributedApplicationException($"Cannot tunnel endpoint '{portResource.TargetEndpoint.EndpointName}' with host '{portResource.TargetEndpoint.Host}' on resource '{portResource.TargetEndpoint.Resource.Name}' because it is not a localhost endpoint."));
                return Task.CompletedTask;
            }

            // Signal the target endpoint created
            portResource.TargetEndpointAllocatedTcs.SetResult();
            return Task.CompletedTask;
        });

        // Lifecycle from the tunnel
        tunnelBuilder
            //.OnBeforeResourceStarted(async (tunnelResource, e, ct) =>
            //{
                // // Tunnel starting, clear port status
                // var portLogger = e.Services.GetRequiredService<ResourceLoggerService>().GetLogger(portResource);
                // portLogger.LogInformation("Tunnel starting, waiting for it to be healthy");
                // var notifications = e.Services.GetRequiredService<ResourceNotificationService>();
                // await notifications.PublishUpdateAsync(portResource, snapshot => snapshot with
                // {
                //     State = KnownResourceStates.Waiting
                // }).ConfigureAwait(false);

                // var devTunnelClient = e.Services.GetRequiredService<IDevTunnelClient>();
                // // Start task to wait until tunnel is created and target endpoint is allocated
                // _ = Task.Run(async () =>
                // {
                //     try
                //     {
                //         // Wait here to ensure tunnel has been created & target endpoint is allocated!
                //         _ = Task.WhenAll(tunnel.TunnelCreatedTask, portResource.TargetEndpointAllocatedTask).ConfigureAwait(false);

                //         portResource.Options.Labels ??= [];
                //         // TODO: Validate and add the labels here
                //         // Labels: The field Labels must match the regular expression '[\w-=]{1,50}'.
                //         //portResource.Options.Labels.Add(portResource.TargetEndpoint.Resource.Name);
                //         //portResource.Options.Labels.Add(portResource.TargetEndpoint.EndpointName);

                //         // Create the tunnel port
                //         _ = await devTunnelClient.CreateOrUpdatePortAsync(
                //             portResource.DevTunnel.TunnelId,
                //             portResource.TargetEndpoint.Port,
                //             portResource.Options,
                //             ct)
                //         .ConfigureAwait(false);

                //         // Mark the port created
                //         portResource.PortCreatedTcs.SetResult();

                //         portLogger.LogInformation("Created/updated dev tunnel port '{Port}' on tunnel '{Tunnel}' targeting endpoint '{Endpoint}' on resource '{TargetResource}'.", portResource.TargetEndpoint.Port, portResource.DevTunnel.TunnelId, portResource.TargetEndpoint.EndpointName, portResource.TargetEndpoint.Resource.Name);
                //     }
                //     catch (Exception ex)
                //     {
                //         portLogger.LogError(ex, "Error trying to create/update dev tunnel port '{Port}' on tunnel '{Tunnel}': {Error}", portResource.TargetEndpoint.Port, portResource.DevTunnel.TunnelId, ex.Message);
                //     }
                // }, ct);
            //})
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
                var raiseEndpointsAllocatedEvent = portResource.TunnelEndpointAnnotation.AllocatedEndpoint is null;
                portResource.TunnelEndpointAnnotation.AllocatedEndpoint = new(portResource.TunnelEndpointAnnotation, tunnelPortStatus.PortUri.Host, 443 /* Always 443 for public tunnel endpoint */);

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
                portResource.PortCreatedTcs.TrySetCanceled(ct);
                portResource.PortCreatedTcs = new();

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

    private sealed class DevTunnelResourceStartedEvent(DevTunnelResource tunnel) : IDistributedApplicationResourceEvent
    {
        public IResource Resource { get; } = tunnel;
    }
}

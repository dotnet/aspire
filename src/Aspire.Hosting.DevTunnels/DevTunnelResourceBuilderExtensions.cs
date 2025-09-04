// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.DevTunnels;
using Aspire.Hosting.Eventing;
using Microsoft.Extensions.DependencyInjection;
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

        var workingDirectory = builder.AppHostDirectory;
        var appHostSha = builder.Configuration["AppHost:Sha256"]?[..8];
        tunnelId ??= $"{name}-{builder.Environment.ApplicationName.Replace(".", "-")}-{appHostSha}".ToLowerInvariant();

        // Validate the TunnelId format [a-z0-9][a-z0-9-]{1,58}[a-z0-9]
        if (!TunnelIdRegex().IsMatch(tunnelId))
        {
            throw new ArgumentException($"""
                The tunnel ID '{tunnelId}' is invalid. A valid tunnel IDs must:
                - start and end with a letter or number
                - consist of lowercase letters, numbers, and hyphens
                - be 1-58 characters long
                """, nameof(tunnelId));
        }

        var rb = builder.AddResource(new DevTunnelResource(name, tunnelId, options.GetCliPath(), workingDirectory, options))
            //.WithExplicitStart()
            .WithArgs("host", tunnelId)
            .WithInitialState(new()
            {
                ResourceType = "DevTunnel",
                CreationTimeStamp = DateTime.UtcNow,
                State = KnownResourceStates.NotStarted,
                Properties = []
            })
            .ExcludeFromManifest(); // Dev tunnels do not get deployed

        // Health check
        var healtCheckKey = $"{name}-check";
        rb.ApplicationBuilder.Services.AddHealthChecks().Add(new HealthCheckRegistration(
            healtCheckKey,
            services => new DevTunnelHealthCheck(new DevTunnelCliClient(options.GetCliPath()), rb.Resource),
            failureStatus: default,
            tags: default,
            timeout: default));
        rb.WithHealthCheck(healtCheckKey);

        // Lifecycle
        rb.OnBeforeResourceStarted(static async (tunnelResource, e, ct) =>
        {
            var logger = e.Services.GetRequiredService<ResourceLoggerService>().GetLogger(tunnelResource);
            var eventing = e.Services.GetRequiredService<IDistributedApplicationEventing>();
            var notifications = e.Services.GetRequiredService<ResourceNotificationService>();
#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var interaction = e.Services.GetRequiredService<IInteractionService>();
#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            var devTunnelClient = new DevTunnelCliClient(tunnelResource.Options.GetCliPath());

            // Login to the dev tunnels service if needed
            var userLoginStatus = await devTunnelClient.GetUserLoginStatusAsync(ct).ConfigureAwait(false);
            if (!userLoginStatus.IsLoggedIn)
            {
                if (interaction.IsAvailable)
                {
#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                    await interaction.PromptNotificationAsync(
                        "Dev tunnels",
                        $"The dev tunnel resource '{tunnelResource.Name}' requires authentication to continue.",
                        new() { Intent = MessageIntent.Warning, PrimaryButtonText = "Login", ShowDismiss = false },
                        ct).ConfigureAwait(false);
#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                }
                // TODO: Support login for GitHub auth too
                // BUG: This doesn't pop the WAM UI on Windows likely due to interactive shell stuff
                userLoginStatus = await devTunnelClient.UserLoginAsync(LoginProvider.Microsoft, ct).ConfigureAwait(false);
                if (!userLoginStatus.IsLoggedIn)
                {
                    throw new DistributedApplicationException($"Failed to login to the dev tunnels service.");
                }
            }

            // Create the dev tunnel
            _ = await devTunnelClient.CreateOrUpdateTunnelAsync(tunnelResource.TunnelId, tunnelResource.Options, ct).ConfigureAwait(false);
        });

        rb.OnResourceReady(static async (tunnelResource, e, ct) =>
        {
            var devTunnelClient = new DevTunnelCliClient(tunnelResource.Options.GetCliPath());
            foreach (var portResource in tunnelResource.Ports)
            {
                await UpdateDevTunnelPortStatus(portResource, devTunnelClient, e.Services, ct).ConfigureAwait(false);
            }
        });

        rb.OnResourceStopped(static async (tunnelResource, e, ct) =>
        {
            var logger = e.Services.GetRequiredService<ResourceLoggerService>().GetLogger(tunnelResource);
            var notifications = e.Services.GetRequiredService<ResourceNotificationService>();
            var eventing = e.Services.GetRequiredService<IDistributedApplicationEventing>();

            // Update status of all ports to stopped
            foreach (var portResource in tunnelResource.Ports)
            {
                CustomResourceSnapshot? stoppedSnapshot = default;
                await notifications.PublishUpdateAsync(portResource, snapshot => stoppedSnapshot = snapshot with
                {
                    State = KnownResourceStates.Finished,
                    Urls = []
                }).ConfigureAwait(false);
                await eventing.PublishAsync<ResourceStoppedEvent>(new(portResource, e.Services, new(portResource, portResource.Name, stoppedSnapshot!)), ct).ConfigureAwait(false);
            }
        });

        // TODO: Should we delete tunnels when the AppHost stops?

        return rb;
    }

    /// <summary>
    /// Adds ports on the tunnel for all endpoints found on the referenced resource.
    /// </summary>
    public static IResourceBuilder<DevTunnelResource> WithReference<TResource>(
        this IResourceBuilder<DevTunnelResource> tunnelBuilder,
        IResourceBuilder<TResource> resourceBuilder,
        Action<DevTunnelPortOptions>? configurePort = null)
        where TResource : IResourceWithEndpoints
    {
        ArgumentNullException.ThrowIfNull(tunnelBuilder);
        ArgumentNullException.ThrowIfNull(resourceBuilder);

        foreach (var endpoint in resourceBuilder.Resource.GetEndpoints())
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

        tunnelBuilder.WithReference(resourceBuilder, configurePort);

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
                "https" or "http" => targetEndpoint.Scheme,
                _ => "tcp"
            }
        };
        portOptions.Description ??= $"{targetResource.Name}:{targetEndpoint.EndpointName}";
        configurePort?.Invoke(portOptions);

        var childName = $"{tunnel.Name}-{targetResource.Name}-{targetEndpoint.EndpointName}";
        var portResource = new DevTunnelPortResource(
            childName,
            tunnel,
            targetEndpoint,
            portOptions);

        tunnel.Ports.Add(portResource);

        var portBuilder = tunnelBuilder.ApplicationBuilder.AddResource(portResource)
            .WithParentRelationship(tunnelBuilder) // visual grouping beneath the tunnel
            .WithReferenceRelationship(targetResource) // indicate the target resource relationship
            .WithHttpsEndpoint(name: DevTunnelPortResource.TunnelEndpointName) // public tunnel URL endpoint
            .WithUrlForEndpoint(DevTunnelPortResource.TunnelEndpointName, static url =>
            {
                // Remove the port from the URL since the dev tunnels service always uses 443 for HTTPS
                url.Url = new UriBuilder(url.Url).ToString();
            })
            .WithInitialState(new()
            {
                ResourceType = "DevTunnelPort",
                CreationTimeStamp = DateTime.UtcNow,
                State = KnownResourceStates.NotStarted,
                Properties =
                [
                    //new(CustomResourceKnownProperties.Source, "Aspire.Hosting.DevTunnels"),
                    new("TargetResource", targetResource.Name),
                    new("TargetEndpoint", targetEndpoint.EndpointName),
                    new("Protocol", portOptions.Protocol)
                ]
            });

        tunnelBuilder.ApplicationBuilder.Eventing.Subscribe<ResourceEndpointsAllocatedEvent>(targetEndpoint.Resource, async (e, ct) =>
        {
            if (!portResource.TargetEndpoint.IsAllocated)
            {
                // Target endpoint is not allocated, ignore
                return;
            }

            var portLogger = e.Services.GetRequiredService<ResourceLoggerService>().GetLogger(portResource);

            if (!string.Equals(portResource.TargetEndpoint.Host, "localhost", StringComparison.OrdinalIgnoreCase) &&
                !portResource.TargetEndpoint.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
            {
                // Target endpoint is not localhost, ignore
                portLogger.LogError("Cannot tunnel endpoint '{Endpoint}' with host '{Host}' on resource '{Resource}' because it is not a localhost endpoint.", portResource.TargetEndpoint.EndpointName, portResource.TargetEndpoint.Host, portResource.TargetEndpoint.Resource.Name);
                return;
            }

            var devTunnelClient = new DevTunnelCliClient(portResource.DevTunnel.Options.GetCliPath());

            // Create or update the port on the dev tunnel
            portResource.Options.Labels ??= [];

            // Labels: The field Labels must match the regular expression '[\w-=]{1,50}'.
            //portResource.Options.Labels.Add(portResource.TargetEndpoint.Resource.Name);
            //portResource.Options.Labels.Add(portResource.TargetEndpoint.EndpointName);
            var portStatus = await devTunnelClient.CreateOrUpdatePortAsync(
                portResource.DevTunnel.TunnelId,
                portResource.TargetEndpoint.Port,
                portResource.Options, ct)
                .ConfigureAwait(false);

            await UpdateDevTunnelPortStatus(portResource, devTunnelClient, e.Services, ct).ConfigureAwait(false);
        });
    }

    private static async Task<bool> UpdateDevTunnelPortStatus(DevTunnelPortResource portResource, IDevTunnelClient devTunnelClient, IServiceProvider services, CancellationToken ct)
    {
        var tunnelStatus = await devTunnelClient.GetTunnelAsync(portResource.DevTunnel.TunnelId, ct).ConfigureAwait(false);
        var tunnelPortStatus = tunnelStatus.Ports?.FirstOrDefault(p => p.PortNumber == portResource.TargetEndpoint.Port);

        if (tunnelStatus.HostConnections == 0 || tunnelPortStatus?.PortUri is null)
        {
            // The tunnel is not active for this port
            return false;
        }

        // Allocate endpoint to the tunnel port
        if (!portResource.TryGetEndpoints(out var portEndpoints)
            || portEndpoints.FirstOrDefault(ep => ep.Name == DevTunnelPortResource.TunnelEndpointName) is not { } publicEndpoint)
        {
            throw new DistributedApplicationException($"Could not find public tunnel endpoint for port resource '{portResource.Name}'.");
        }
        publicEndpoint.AllocatedEndpoint = new(publicEndpoint, tunnelPortStatus.PortUri.Host, 443);

        // Tweak the endpoint URL of the port resource to remove the port number
        portResource.Annotations.Add(new ResourceUrlsCallbackAnnotation(context =>
        {
            var existing = context.Urls.FirstOrDefault(u => string.Equals(u.Endpoint?.EndpointName, DevTunnelPortResource.TunnelEndpointName, StringComparisons.EndpointAnnotationName));
            // Remove the port from the URL since the dev tunnels service always uses 443 for HTTPS
            existing?.Url = new UriBuilder(existing.Url).ToString();
        }));

        // Add inspect URL to the port resource
        if (portResource.TryGetAnnotationsOfType<ResourceUrlAnnotation>(out var urls)
            && urls.FirstOrDefault(u => u.DisplayText == "Inspect") is { } inspectUrlAnnotation)
        {
            portResource.Annotations.Remove(inspectUrlAnnotation);
        }
        // If tunnel host is sdfdff-3456.usw.devtunnels.ms, the inspect host is sdfdff-3456-inspect.usw.devtunnels.ms
        var hostParts = tunnelPortStatus.PortUri.Host.Split('.');
        var inspectionUrl = new UriBuilder(tunnelPortStatus.PortUri)
        {
            Host = $"{hostParts[0]}-inspect.{string.Join('.', hostParts[1..])}",
        }.Uri.ToString();
        portResource.Annotations.Add(new ResourceUrlAnnotation
        {
            Endpoint = new EndpointReference(portResource, publicEndpoint),
            Url = inspectionUrl,
            DisplayLocation = UrlDisplayLocation.DetailsOnly,
            DisplayText = "Inspect"
        });

        var eventing = services.GetRequiredService<IDistributedApplicationEventing>();
        await eventing.PublishAsync<ResourceEndpointsAllocatedEvent>(new(portResource, services), ct).ConfigureAwait(false);

        // Publish events and notifications
        var notifications = services.GetRequiredService<ResourceNotificationService>();
        await notifications.PublishUpdateAsync(portResource, snapshot => snapshot with
        {
            State = KnownResourceStates.Starting
        }).ConfigureAwait(false);
        await eventing.PublishAsync<BeforeResourceStartedEvent>(new(portResource, services), ct).ConfigureAwait(false);
        await notifications.PublishUpdateAsync(portResource, snapshot => snapshot with
        {
            State = KnownResourceStates.Running,
            Urls = [.. snapshot.Urls.Select(u => u with { IsInactive = false })]
        }).ConfigureAwait(false);

        return true;
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{1,58}[a-z0-9]$")]
    private static partial Regex TunnelIdRegex();
}

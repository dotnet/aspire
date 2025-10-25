// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.DevTunnels;
using Aspire.Hosting.Maui;
using Aspire.Hosting.Maui.Annotations;
using Aspire.Hosting.Maui.Otlp;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for configuring OpenTelemetry endpoints for MAUI platform resources.
/// </summary>
public static class MauiOtlpExtensions
{
    /// <summary>
    /// Configures the MAUI platform resource to send OpenTelemetry data through an automatically created dev tunnel.
    /// This is the easiest option for most scenarios, as it handles tunnel creation, configuration, and endpoint
    /// injection automatically.
    /// </summary>
    /// <typeparam name="T">The MAUI platform resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <returns>The resource builder.</returns>
    /// <remarks>
    /// <para>
    /// This method creates a dev tunnel automatically and configures the MAUI platform resource to route
    /// OTLP traffic through it. This is the recommended approach for most scenarios as it requires minimal
    /// configuration and works reliably across all mobile platforms.
    /// </para>
    /// <para>
    /// Prerequisites:
    /// <list type="bullet">
    ///   <item>Aspire.Hosting.DevTunnels package must be referenced</item>
    ///   <item>Dev tunnel CLI must be installed (automatic prompt if missing)</item>
    ///   <item>User must be logged in to dev tunnel service (automatic prompt if needed)</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// Configure a MAUI Android device to automatically use a dev tunnel for telemetry:
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// 
    /// var maui = builder.AddMauiProject("mauiapp", "../MyMauiApp/MyMauiApp.csproj");
    /// maui.AddAndroidDevice()
    ///     .WithOtlpDevTunnel(); // That's it - everything is configured automatically!
    /// 
    /// builder.Build().Run();
    /// </code>
    /// </example>
    public static IResourceBuilder<T> WithOtlpDevTunnel<T>(
        this IResourceBuilder<T> builder)
        where T : IMauiPlatformResource, IResourceWithEnvironment
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Get shared state - only create stub + tunnel once per app
        var platformResource = builder.Resource;
        var parentBuilder = builder.ApplicationBuilder.CreateResourceBuilder(platformResource.Parent);
        var configuration = builder.ApplicationBuilder.Configuration;

        // Check if we already created the stub + tunnel for this MAUI project
        if (!parentBuilder.Resource.TryGetLastAnnotation<OtlpDevTunnelConfigurationAnnotation>(out var tunnelConfig))
        {
            // First time - create stub and dev tunnel
            tunnelConfig = CreateOtlpDevTunnelInfrastructure(parentBuilder, configuration);
            parentBuilder.Resource.Annotations.Add(tunnelConfig);
        }

        // Now apply the configuration to this specific platform
        ApplyOtlpConfigurationToPlatform(builder, tunnelConfig);

        return builder;
    }

    /// <summary>
    /// Creates the OTLP dev tunnel infrastructure (stub resource + dev tunnel).
    /// This is only created once per MAUI project and shared across all platforms.
    /// </summary>
    private static OtlpDevTunnelConfigurationAnnotation CreateOtlpDevTunnelInfrastructure(
        IResourceBuilder<MauiProjectResource> parentBuilder,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        var appBuilder = parentBuilder.ApplicationBuilder;

        // Resolve OTLP scheme and port from configuration
        var (otlpScheme, otlpPort) = OtlpEndpointResolver.Resolve(configuration);

        // Create names for the tunnel infrastructure
        // The dev tunnel port resource will be named: {tunnelName}-{stubName}-{endpointName}
        // Stub must be unique across all MAUI projects, so include parent resource name
        var tunnelName = $"{parentBuilder.Resource.Name}-tunnel";
        var stubName = $"{parentBuilder.Resource.Name}-otlp";

        // Create OtlpLoopbackResource - a synthetic IResourceWithEndpoints for service discovery
        var stubResource = new OtlpLoopbackResource(stubName, otlpPort, otlpScheme);

        var stubBuilder = appBuilder.AddResource(stubResource)
            .ExcludeFromManifest();

        // Hide the stub from the dashboard UI
        stubBuilder.WithInitialState(new CustomResourceSnapshot
        {
            ResourceType = "OtlpStub",
            Properties = [],
            IsHidden = true
        });

        // Create dev tunnel with anonymous access for OTLP
        var devTunnel = appBuilder.AddDevTunnel(tunnelName)
            .WithAnonymousAccess()
            .WithReference(stubBuilder, new DevTunnelPortOptions { Protocol = "https" });

        // Manually allocate the stub endpoint so dev tunnel can start
        // Dev tunnels wait for ResourceEndpointsAllocatedEvent before starting
        appBuilder.Eventing.Subscribe<BeforeStartEvent>((evt, ct) =>
        {
            var endpoint = stubResource.Annotations.OfType<EndpointAnnotation>().FirstOrDefault();
            if (endpoint is not null && endpoint.AllocatedEndpoint is null)
            {
                endpoint.AllocatedEndpoint = new AllocatedEndpoint(endpoint, "localhost", otlpPort);
                return appBuilder.Eventing.PublishAsync(new ResourceEndpointsAllocatedEvent(stubResource, evt.Services), ct);
            }
            return Task.CompletedTask;
        });

        return new OtlpDevTunnelConfigurationAnnotation(stubResource, stubBuilder, devTunnel);
    }

    /// <summary>
    /// Applies OTLP configuration to a specific MAUI platform resource.
    /// Uses service discovery through WithReference to get the tunneled endpoint, then overrides OTEL_EXPORTER_OTLP_ENDPOINT.
    /// </summary>
    private static void ApplyOtlpConfigurationToPlatform<T>(
        IResourceBuilder<T> platformBuilder,
        OtlpDevTunnelConfigurationAnnotation tunnelConfig)
        where T : IMauiPlatformResource, IResourceWithEnvironment
    {
        // Use WithReference to inject service discovery variables for the stub through the dev tunnel
        // This adds SERVICES__<STUBNAME>__OTLP__0=https://tunnel-url which we'll use and then clean up
        platformBuilder.WithReference(tunnelConfig.OtlpStubBuilder, tunnelConfig.DevTunnel);

        // Override OTEL_EXPORTER_OTLP_ENDPOINT with the tunneled URL and clean up extra variables
        platformBuilder.WithEnvironment(context =>
        {
            // Read the service discovery variable that WithReference just added
            // Format: services__{resourcename}__otlp__0 (lowercase)
            var serviceDiscoveryKey = $"services__{tunnelConfig.OtlpStub.Name}__otlp__0";
            if (context.EnvironmentVariables.TryGetValue(serviceDiscoveryKey, out var tunnelUrl))
            {
                // Override OTEL_EXPORTER_OTLP_ENDPOINT with the tunnel URL
                context.EnvironmentVariables["OTEL_EXPORTER_OTLP_ENDPOINT"] = tunnelUrl;

                // Remove the service discovery variables since we're using direct OTLP configuration
                context.EnvironmentVariables.Remove(serviceDiscoveryKey);

                // Also remove the {RESOURCENAME}_{ENDPOINTNAME} format variable (e.g., MAUIAPP-OTLP_OTLP)
                // The resource name keeps its case/dashes, endpoint name is uppercased
                var directEndpointKey = $"{tunnelConfig.OtlpStub.Name.ToUpperInvariant()}_OTLP";
                context.EnvironmentVariables.Remove(directEndpointKey);
            }
        });
    }
}

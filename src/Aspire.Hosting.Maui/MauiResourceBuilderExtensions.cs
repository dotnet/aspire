// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.DevTunnels;
using Aspire.Hosting.Maui;
using Aspire.Hosting.Maui.DevTunnels;
using Aspire.Hosting.Maui.Platforms.Android;
using Aspire.Hosting.Maui.Platforms.iOS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for configuring .NET MAUI project resources.
/// </summary>
public static class MauiResourceBuilderExtensions
{
    /// <summary>
    /// Subscribes to lifecycle events for MAUI resource management.
    /// </summary>
    internal static IResourceBuilder<MauiProjectResource> WithMauiEventHandlers(this IResourceBuilder<MauiProjectResource> builder)
    {
        var appBuilder = builder.ApplicationBuilder;
        var configuration = GetConfiguration(builder);
        var resource = builder.Resource;

        appBuilder.Eventing.Subscribe<BeforeStartEvent>((evt, ct) =>
        {
            var loggerFactory = evt.Services.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger(typeof(MauiResourceBuilderExtensions));

            // Log warnings for explicitly configured unsupported platforms
            foreach (var platformResource in configuration.PlatformResources)
            {
                var unsupported = platformResource.Resource.Annotations.OfType<MauiUnsupportedPlatformAnnotation>().FirstOrDefault();
                if (unsupported is not null)
                {
                    logger?.LogWarning(
                        "MAUI platform '{Platform}' was explicitly configured but is not supported on this host: {Reason}",
                        platformResource.Resource.Name,
                        unsupported.Reason);
                }
            }

            // Auto-detect platforms if none explicitly configured
            var autoDetected = configuration.EnsureAutoDetection(appBuilder, moniker => builder.TryAddAutoDetectedPlatform(moniker));
            if (autoDetected.Count > 0)
            {
                logger?.LogWarning(
                    "Auto-detected .NET MAUI platform resources: {Platforms}. " +
                    "Use WithWindows(), WithAndroid(), WithiOS(), or WithMacCatalyst() to explicitly specify platforms.",
                    string.Join(", ", autoDetected));
            }
            else if (configuration.PlatformResources.Count == 0)
            {
                // Auto-detection ran but found no platforms (e.g., on Linux where no platforms are supported)
                logger?.LogWarning(
                    "No .NET MAUI platform resources were configured for '{ResourceName}'. " +
                    "Use WithWindows(), WithAndroid(), WithiOS(), or WithMacCatalyst() to add platforms.",
                    resource.Name);
            }

            return Task.CompletedTask;
        });

        return builder;
    }

    /// <summary>
    /// Adds a Windows platform resource if the MAUI project targets windows.
    /// </summary>
    public static IResourceBuilder<MauiProjectResource> WithWindows(this IResourceBuilder<MauiProjectResource> builder, string? runtimeIdentifier = null)
    {
        builder.AddPlatform("windows", runtimeIdentifier);
        return builder;
    }

    /// <summary>
    /// Adds an Android platform resource if the MAUI project targets Android.
    /// </summary>
    /// <param name="builder">The MAUI project resource builder.</param>
    /// <param name="adbTarget">Optional adb target passed as an MSBuild property (e.g. <c>-p:AdbTarget=-e</c>) to select a specific emulator or device.</param>
    /// <remarks>
    /// Android support is currently limited to adding the platform resource without additional provisioning logic.
    /// If <paramref name="adbTarget"/> is provided it is forwarded as an MSBuild property when the project is started.
    /// </remarks>
    public static IResourceBuilder<MauiProjectResource> WithAndroid(this IResourceBuilder<MauiProjectResource> builder, string? adbTarget = null)
    {
        builder.AddPlatform("android", msbuildProperty: adbTarget is null ? null : $"AdbTarget={adbTarget}");
        return builder;
    }

    /// <summary>
    /// Adds an iOS platform resource if the MAUI project targets iOS.
    /// </summary>
    /// <param name="builder">The MAUI project resource builder.</param>
    /// <param name="deviceUdid">Optional _DeviceName UDID (simulator or device) passed as -p:_DeviceName=&lt;UDID&gt;</param>
    public static IResourceBuilder<MauiProjectResource> WithiOS(this IResourceBuilder<MauiProjectResource> builder, string? deviceUdid = null)
    {
        builder.AddPlatform("ios", msbuildProperty: deviceUdid is null ? null : $"_DeviceName={deviceUdid}");
        return builder;
    }

    /// <summary>
    /// Adds a MacCatalyst platform resource if the MAUI project targets MacCatalyst.
    /// </summary>
    /// <param name="builder">The MAUI project resource builder.</param>
    /// <param name="runtimeIdentifier">Optional runtime identifier (e.g. maccatalyst-x64 or maccatalyst-arm64).</param>
    public static IResourceBuilder<MauiProjectResource> WithMacCatalyst(this IResourceBuilder<MauiProjectResource> builder, string? runtimeIdentifier = null)
    {
        builder.AddPlatform("maccatalyst", runtimeIdentifier);
        return builder;
    }

    /// <summary>
    /// Propagates a reference to another resource to all platform resources.
    /// </summary>
    /// <typeparam name="TSource">The type of the source resource.</typeparam>
    /// <param name="builder">The MAUI project resource builder.</param>
    /// <param name="source">The resource builder for the source resource.</param>
    /// <param name="connectionName">Optional connection name.</param>
    public static IResourceBuilder<MauiProjectResource> WithReference<TSource>(this IResourceBuilder<MauiProjectResource> builder, IResourceBuilder<TSource> source, string? connectionName = null)
        where TSource : IResource
    {
        ArgumentNullException.ThrowIfNull(source);

        var configuration = GetConfiguration(builder);

        // Ensure platforms are materialized early so service discovery / connection string references propagate.
        configuration.EnsureAutoDetection(builder.ApplicationBuilder, builder.TryAddAutoDetectedPlatform);

        if (source.Resource is IResourceWithEndpoints endpointsResource && !configuration.ReferencedEndpointResources.Contains(endpointsResource))
        {
            configuration.ReferencedEndpointResources.Add(endpointsResource);
        }

        foreach (var pr in configuration.PlatformResources)
        {
            if (source.Resource is IResourceWithConnectionString && pr.Resource is IResourceWithEnvironment)
            {
                pr.WithReference((IResourceBuilder<IResourceWithConnectionString>)source, connectionName);
            }
            else if (source.Resource is IResourceWithServiceDiscovery && pr.Resource is IResourceWithEnvironment)
            {
                pr.WithReference((IResourceBuilder<IResourceWithServiceDiscovery>)source);
            }
        }

        return builder;
    }

    /// <summary>
    /// Propagates service discovery variables for the specified endpoint resource through the provided dev tunnel
    /// into all MAUI platform resources (tunneled service discovery). This allows a device/emulator to reach
    /// the service via the tunnel host instead of localhost.
    /// </summary>
    /// <param name="builder">The MAUI project resource builder.</param>
    /// <param name="source">The endpoint-providing resource to expose via the tunnel.</param>
    /// <param name="tunnel">The dev tunnel resource already configured to reference <paramref name="source"/>.</param>
    /// <remarks>
    /// This keeps fluent syntax in the AppHost: <c>.WithReference(weatherApi, publicDevTunnel)</c> without requiring
    /// callers to access individual platform project resources.
    /// </remarks>
    public static IResourceBuilder<MauiProjectResource> WithReference(this IResourceBuilder<MauiProjectResource> builder, IResourceBuilder<IResourceWithEndpoints> source, IResourceBuilder<DevTunnelResource> tunnel)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(tunnel);

        var configuration = GetConfiguration(builder);
        configuration.EnsureAutoDetection(builder.ApplicationBuilder, builder.TryAddAutoDetectedPlatform);

        foreach (var pr in configuration.PlatformResources)
        {
            if (pr.Resource is IResourceWithEnvironment)
            {
                pr.WithReference(source, tunnel);
            }
        }

        return builder;
    }

    /// <summary>
    /// Creates a single Dev Tunnel exposing only the local OTLP port and rewrites the OTLP exporter endpoint
    /// to use the tunneled address for device/simulator telemetry.
    /// </summary>
    /// <param name="builder">The MAUI project resource builder.</param>
    /// <param name="tunnelName">Optional tunnel resource name; defaults to &lt;logical-name&gt;-otlp.</param>
    /// <param name="enableOtelDebug">When true, injects verbose OTEL exporter debug env vars for troubleshooting (defaults to false).</param>
    public static IResourceBuilder<MauiProjectResource> WithOtlpDevTunnel(this IResourceBuilder<MauiProjectResource> builder, string? tunnelName = null, bool enableOtelDebug = false)
    {
        var configuration = GetConfiguration(builder);
        var appBuilder = builder.ApplicationBuilder;

        if (configuration.OtlpDevTunnel is not null)
        {
            return builder; // already configured
        }

        // Use a stable dev tunnel name distinct from the OTLP concept.
        tunnelName ??= builder.Resource.Name + "-devtunnel";
        configuration.OtlpDevTunnel = appBuilder.AddDevTunnel(tunnelName).WithAnonymousAccess();
        configuration.EnableOtelDebug = enableOtelDebug;

        // Resolve OTLP endpoint (scheme & port) from configuration.
        var (otlpScheme, otlpPort) = OtlpEndpointResolver.Resolve(appBuilder.Configuration);

        // Create synthetic hidden stub resource referenced only for tunneling.
        configuration.OtlpStubName = builder.Resource.Name + "-otlpstub";
        if (appBuilder.Resources.Any(r => string.Equals(r.Name, configuration.OtlpStubName, StringComparison.OrdinalIgnoreCase)))
        {
            return builder; // defensive (should not occur normally)
        }

        configuration.OtlpStub = new OtlpLoopbackResource(configuration.OtlpStubName, otlpPort, otlpScheme);
        configuration.OtlpStubPort = otlpPort;
        var stubBuilder = appBuilder.AddResource(configuration.OtlpStub)
            .ExcludeFromManifest();

        // Hide synthetic stub (dashboard omission) while still allowing the Dev Tunnel to attach to an endpoint-providing resource.
        stubBuilder.WithInitialState(new CustomResourceSnapshot
        {
            ResourceType = nameof(OtlpLoopbackResource),
            Properties = [],
            IsHidden = true
        });

        // Prefer nesting the synthetic stub under the logical MAUI resource (rather than the dev tunnel) so that
        // it's grouped with the MAUI app conceptually. If the logical MAUI resource is not surfaced in the dashboard,
        // this may still appear near the top level, but avoids coupling its hierarchy to the tunnel itself.
        try
        {
            stubBuilder.WithParentRelationship(builder);
        }
        catch
        {
            // Fallback: if for some reason we cannot create the logical parent builder, do nothing.
        }

        // Force the dev tunnel port protocol to HTTPS regardless of the local collector's scheme so the
        // public tunnel endpoint is always an https:// URL (Dev Tunnels surface TLS endpoints).
        configuration.OtlpDevTunnel.WithReference(stubBuilder, new DevTunnelPortOptions { Protocol = "https" });
        var stubEndpointsBuilder = appBuilder.CreateResourceBuilder<IResourceWithEndpoints>(configuration.OtlpStub);

        // Ensure the stub endpoint appears allocated before the tunnel starts (the dev tunnel waits on allocation events).
        // We synthesize the allocation and raise ResourceEndpointsAllocatedEvent early.
        appBuilder.Eventing.Subscribe<BeforeStartEvent>((evt, ct) =>
        {
            if (configuration.OtlpStub is null)
            {
                return Task.CompletedTask;
            }
            var endpoint = configuration.OtlpStub.Annotations.OfType<EndpointAnnotation>().FirstOrDefault();
            if (endpoint is null)
            {
                return Task.CompletedTask;
            }
            if (endpoint.AllocatedEndpoint is null)
            {
                endpoint.AllocatedEndpoint = new AllocatedEndpoint(endpoint, "localhost", configuration.OtlpStubPort);
                return appBuilder.Eventing.PublishAsync<ResourceEndpointsAllocatedEvent>(new(configuration.OtlpStub, evt.Services), ct);
            }
            return Task.CompletedTask;
        });

        // Ensure platforms exist (auto-detect if user hasn't added explicitly yet) so we can wire env vars.
        configuration.EnsureAutoDetection(appBuilder, builder.TryAddAutoDetectedPlatform);

        foreach (var pr in configuration.PlatformResources.Where(p => p.Resource is IResourceWithEnvironment))
        {
            pr.ApplyOtlpConfigurationToPlatform(appBuilder, configuration);
        }

        return builder;
    }

    private static MauiProjectConfiguration GetConfiguration(IResourceBuilder<MauiProjectResource> builder)
    {
        return builder.Resource.Annotations.OfType<MauiProjectConfiguration>().FirstOrDefault()
            ?? throw new InvalidOperationException($"MauiProjectConfiguration not found on resource '{builder.Resource.Name}'");
    }

    private static void AddPlatform(this IResourceBuilder<MauiProjectResource> builder, string platformMoniker, string? runtimeIdentifier = null, string? msbuildProperty = null)
    {
        var configuration = GetConfiguration(builder);
        var appBuilder = builder.ApplicationBuilder;
        var mauiResource = builder.Resource;

        var platformConfig = MauiPlatformConfiguration.KnownPlatforms.GetByMoniker(platformMoniker);
        if (platformConfig is null)
        {
            return; // Unknown platform moniker
        }

        var resourceName = $"{mauiResource.Name}-{platformMoniker}";

        // Check if this platform has already been added (duplicate guard)
        if (configuration.PlatformResources.Any(pr => pr.Resource.Name.Equals(resourceName, StringComparison.OrdinalIgnoreCase)))
        {
            // Platform already added - log a warning and skip
            var loggerFactory = appBuilder.Services.BuildServiceProvider().GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger(typeof(MauiResourceBuilderExtensions));
            logger?.LogWarning("Platform '{Platform}' has already been added to MAUI project '{Project}'. Ignoring duplicate call to With{PlatformTitle}().",
                platformMoniker, mauiResource.Name, char.ToUpper(platformMoniker[0]) + platformMoniker[1..]);
            return;
        }

        // Identify TFM prefix e.g. net10.0-windows, net10.0-android
        var tfm = configuration.AvailableTfms.FirstOrDefault(t => t.Contains('-') && t.Split('-')[1].StartsWith(platformMoniker, StringComparison.OrdinalIgnoreCase));
        if (tfm is null)
        {
            // Platform was requested but project doesn't target this TFM - create a warning resource
            builder.ConfigureMissingTfmPlatform(platformMoniker, platformConfig, configuration);
            return;
        }

        // Use existing AddProject API to create the platform-specific resource.
        var platformBuilder = appBuilder.AddProject(resourceName, configuration.ProjectPath)
            .WithExplicitStart()
            .WithAnnotation(ManifestPublishingCallbackAnnotation.Ignore);

        // Configure OpenTelemetry service identification for this platform.
        platformBuilder.ConfigureOpenTelemetryEnvironment(resourceName);

        // Add platform-specific icon for dashboard visualization.
        platformBuilder.WithAnnotation(new ResourceIconAnnotation(platformConfig.IconName, IconVariant.Filled));

        // Check if the platform is supported on the current host OS.
        var supported = platformConfig.IsSupportedOnCurrentHost();

        if (!supported)
        {
            platformBuilder.ConfigureUnsupportedPlatform(appBuilder, platformConfig);
        }

        // Pass framework & device specific msbuild properties via args so launching uses correct target
        platformBuilder.WithArgs(async context =>
        {
            context.Args.Add("-f");
            context.Args.Add(tfm);
            if (!string.IsNullOrEmpty(runtimeIdentifier))
            {
                context.Args.Add("-p:RuntimeIdentifier=" + runtimeIdentifier);
            }
            if (!string.IsNullOrEmpty(msbuildProperty))
            {
                context.Args.Add("-p:" + msbuildProperty);
            }

            // Mac Catalyst requires passing -W via OpenArguments so the launched app stays running and doesn't immediately detach.
            if (platformMoniker.Equals("maccatalyst", StringComparison.OrdinalIgnoreCase))
            {
                // Avoid duplicating if user already supplied it via msbuildProperty.
                var alreadyHas = context.Args.Any(a => a is string s && s.StartsWith("-p:OpenArguments=", StringComparison.OrdinalIgnoreCase));
                if (!alreadyHas)
                {
                    context.Args.Add("-p:OpenArguments=-W");
                }
            }

            if (platformMoniker.Equals("ios", StringComparison.OrdinalIgnoreCase))
            {
                await iOSMlaunchEnvironmentTargetGenerator.AppendEnvironmentTargetsAsync(context).ConfigureAwait(false);
            }

            if (platformMoniker.Equals("android", StringComparison.OrdinalIgnoreCase))
            {
                await AndroidEnvironmentTargetGenerator.AppendEnvironmentTargetsAsync(context).ConfigureAwait(false);
            }
        });

        configuration.PlatformResources.Add(platformBuilder);

        // If OTLP tunneling already configured, wire the new platform to the stub & tunnel now.
        platformBuilder.ApplyOtlpConfigurationIfNeeded(appBuilder, configuration);

        // Conditional build hook executed right before the resource starts (per explicit start invocation).
        platformBuilder.OnBeforeResourceStarted(async (res, evt, ct) =>
        {
            var loggerService = evt.Services.GetService(typeof(ResourceLoggerService)) as ResourceLoggerService;
            var logger = loggerService?.GetLogger(res);

            // Silently prevent starting unsupported platforms - the "Unsupported" state already indicates why.
            // Don't throw an exception as that causes "Failed to start" which is misleading.
            if (res.Annotations.OfType<MauiUnsupportedPlatformAnnotation>().Any())
            {
                logger?.LogInformation("MAUI platform '{Resource}' is unsupported on this host and will not start.", res.Name);
                return;
            }

            // Skip build work during initial AppHost startup; only build on user-initiated explicit start later.
            if (!MauiStartupPhaseTracker.StartupPhaseComplete)
            {
                return;
            }

            // Defensive: ensure still an explicit-start resource.
            if (!res.Annotations.OfType<ExplicitStartupAnnotation>().Any())
            {
                return;
            }
            // Determine configuration from environment (fallback Debug)
            var config = Environment.GetEnvironmentVariable("CONFIGURATION");
            if (string.IsNullOrWhiteSpace(config))
            {
                config = "Debug";
            }

            // Compose output directory (best-effort) - MAUI layout: bin/<config>/<tfm>/
            var tfmDir = Path.Combine(Path.GetDirectoryName(configuration.ProjectPath) ?? string.Empty, "bin", config, tfm);
            bool NeedsBuild()
            {
                try
                {
                    if (!Directory.Exists(tfmDir))
                    {
                        return true;
                    }
                    if (!Directory.EnumerateFileSystemEntries(tfmDir).Any())
                    {
                        return true;
                    }
                }
                catch
                {
                    return true;
                }
                return false;
            }

            if (!NeedsBuild())
            {
                return; // artifacts present
            }

            var key = string.Join('|', configuration.ProjectPath, tfm, config);
            var lazy = MauiProjectConfiguration.Builds.GetOrAdd(key, _ => new Lazy<Task>(() => RunBuildAsync(ct)));
            try
            {
                await lazy.Value.ConfigureAwait(false);
            }
            catch
            {
                // If failed, remove so a retry start attempt can rebuild
                MauiProjectConfiguration.Builds.TryRemove(key, out _);
                throw;
            }

            async Task RunBuildAsync(CancellationToken token)
            {
                logger?.LogInformation("Artifacts missing; building {Tfm}...", tfm);

                var psi = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };

                psi.ArgumentList.Add("build");
                psi.ArgumentList.Add(configuration.ProjectPath);
                psi.ArgumentList.Add("-f");
                psi.ArgumentList.Add(tfm);

                if (!string.IsNullOrEmpty(runtimeIdentifier))
                {
                    psi.ArgumentList.Add("-p:RuntimeIdentifier=" + runtimeIdentifier);
                }

                if (!string.IsNullOrEmpty(msbuildProperty))
                {
                    psi.ArgumentList.Add("-p:" + msbuildProperty);
                }

                var sw = Stopwatch.StartNew();
                using var proc = Process.Start(psi)!;
                var stdoutTask = proc.StandardOutput.ReadToEndAsync(token);
                var stderrTask = proc.StandardError.ReadToEndAsync(token);
                await proc.WaitForExitAsync(token).ConfigureAwait(false);

                var stdout = await stdoutTask.ConfigureAwait(false);
                var stderr = await stderrTask.ConfigureAwait(false);
                sw.Stop();

                if (!string.IsNullOrWhiteSpace(stdout))
                {
                    logger?.LogDebug("{Stdout}", stdout.TrimEnd());
                }

                if (!string.IsNullOrWhiteSpace(stderr))
                {
                    logger?.LogDebug("{Stderr}", stderr.TrimEnd());
                }

                if (proc.ExitCode != 0)
                {
                    logger?.LogError("Build failed (exit {ExitCode}) for {Tfm}.", proc.ExitCode, tfm);
                    throw new InvalidOperationException($"MAUI build failed for {resourceName} ({tfm}).");
                }

                logger?.LogInformation("Build succeeded in {Seconds}s for {Tfm}.", sw.Elapsed.TotalSeconds.ToString("F1", System.Globalization.CultureInfo.InvariantCulture), tfm);
            }
        });
    }

    /// <summary>
    /// Configures OpenTelemetry environment variables for the platform resource.
    /// Overrides DCP interpolation templates with concrete values for local MAUI projects.
    /// </summary>
    private static void ConfigureOpenTelemetryEnvironment(this IResourceBuilder<ProjectResource> builder, string resourceName)
    {
        // Override OTEL_SERVICE_NAME placeholder (the generic OTLP configuration sets a DCP interpolation template
        // like {{- index .Annotations "otel-service-name" -}} which is never resolved for a local MAUI project).
        // Provide a stable concrete service name instead so the exporter doesn't emit the literal template.
        builder.WithEnvironment("OTEL_SERVICE_NAME", resourceName);

        // Override OTEL_RESOURCE_ATTRIBUTES placeholder (the generic OTLP configuration sets a DCP interpolation template
        // like {{- index .Annotations "otel-service-instance-id" -}} which is never resolved for a local MAUI project).
        // Provide a unique service instance ID for this platform resource. Each device/emulator running this app
        // will have a distinct instance ID, allowing proper telemetry tracking in the dashboard.
        builder.WithEnvironment("OTEL_RESOURCE_ATTRIBUTES", "service.instance.id=" + Guid.NewGuid().ToString());
    }

    /// <summary>
    /// Configures an unsupported platform to display a warning state in the dashboard
    /// and prevent lifecycle operations.
    /// </summary>
    private static void ConfigureUnsupportedPlatform(this IResourceBuilder<ProjectResource> builder, IDistributedApplicationBuilder appBuilder, MauiPlatformConfiguration platformConfig)
    {
        builder.WithAnnotation(new MauiUnsupportedPlatformAnnotation(platformConfig.UnsupportedReason));

        // Publish the unsupported state after resources are created.
        // The "Unsupported" state prevents the platform from being started.
        // Note: Dashboard expects "warning" (not "warn" from KnownResourceStateStyles.Warn) for the warning icon to display.
        // This is a known inconsistency in the Aspire codebase between the hosting and dashboard layers.
        var resource = builder.Resource;
        appBuilder.Eventing.Subscribe<AfterResourcesCreatedEvent>((evt, ct) =>
        {
            var notificationService = evt.Services.GetService<ResourceNotificationService>();
            if (notificationService is not null)
            {
                _ = notificationService.PublishUpdateAsync(resource, s => s with
                {
                    State = new ResourceStateSnapshot("Unsupported", "warning")
                });
            }

            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// Configures a platform that was requested but doesn't have a matching TFM in the project.
    /// Creates a warning resource in the dashboard to inform the developer.
    /// </summary>
    private static void ConfigureMissingTfmPlatform(this IResourceBuilder<MauiProjectResource> builder, string platformMoniker, MauiPlatformConfiguration platformConfig, MauiProjectConfiguration configuration)
    {
        var appBuilder = builder.ApplicationBuilder;
        var resourceName = $"{builder.Resource.Name}-{platformMoniker}";

        // Create a placeholder project resource to show in the dashboard
        var platformBuilder = appBuilder.AddProject(resourceName, configuration.ProjectPath)
            .WithExplicitStart()
            .WithAnnotation(ManifestPublishingCallbackAnnotation.Ignore);

        // Add platform-specific icon for dashboard visualization
        platformBuilder.WithAnnotation(new ResourceIconAnnotation(platformConfig.IconName, IconVariant.Filled));

        // Track that this platform is missing the required TFM
        var warningMessage = $"Project does not target {platformMoniker}. Add 'net10.0-{platformMoniker}' to TargetFrameworks in the project file.";
        platformBuilder.WithAnnotation(new MauiMissingTfmAnnotation(platformMoniker, warningMessage));

        // Publish the warning state after resources are created
        // Note: Dashboard expects "warning" (not "warn" from KnownResourceStateStyles.Warn) for the warning icon to display.
        var resource = platformBuilder.Resource;
        appBuilder.Eventing.Subscribe<AfterResourcesCreatedEvent>((evt, ct) =>
        {
            var notificationService = evt.Services.GetService<ResourceNotificationService>();
            var loggerService = evt.Services.GetService<ResourceLoggerService>();

            if (notificationService is not null)
            {
                _ = notificationService.PublishUpdateAsync(resource, s => s with
                {
                    State = new ResourceStateSnapshot("Missing TFM", "warning")
                });
            }

            // Also log a warning to help developers discover the issue
            if (loggerService is not null)
            {
                var logger = loggerService.GetLogger(resource);
                logger?.LogWarning("Platform '{Platform}' was requested but the project '{ProjectPath}' does not include 'net10.0-{Platform}' in its TargetFrameworks. Add it to the project file to enable this platform.",
                    platformMoniker, configuration.ProjectPath, platformMoniker);
            }

            return Task.CompletedTask;
        });

        // Prevent this platform from being started
        platformBuilder.OnBeforeResourceStarted((res, evt, ct) =>
        {
            var loggerService = evt.Services.GetService(typeof(ResourceLoggerService)) as ResourceLoggerService;
            var logger = loggerService?.GetLogger(res);

            logger?.LogWarning("Cannot start platform '{Platform}' because it is not included in the project's TargetFrameworks.", platformMoniker);

            return Task.CompletedTask;
        });

        configuration.PlatformResources.Add(platformBuilder);
    }

    /// <summary>
    /// Applies OTLP dev tunnel configuration to a platform if OTLP dev tunnel is enabled.
    /// </summary>
    private static void ApplyOtlpConfigurationIfNeeded(this IResourceBuilder<ProjectResource> builder, IDistributedApplicationBuilder appBuilder, MauiProjectConfiguration configuration)
    {
        if (configuration.OtlpDevTunnel is null || configuration.OtlpStub is null || builder.Resource is not IResourceWithEnvironment)
        {
            return;
        }

        builder.ApplyOtlpConfigurationToPlatform(appBuilder, configuration);
    }

    /// <summary>
    /// Applies OTLP dev tunnel configuration to a specific platform resource builder.
    /// </summary>
    private static void ApplyOtlpConfigurationToPlatform(this IResourceBuilder<ProjectResource> builder, IDistributedApplicationBuilder appBuilder, MauiProjectConfiguration configuration)
    {
        if (configuration.OtlpDevTunnel is null || configuration.OtlpStub is null)
        {
            return;
        }

        var stubEndpointsBuilder = appBuilder.CreateResourceBuilder<IResourceWithEndpoints>(configuration.OtlpStub);
        builder.WithReference(stubEndpointsBuilder, configuration.OtlpDevTunnel);

        // iOS and Android require the stub name to locate the OTLP endpoint after dev tunnel allocation.
        var platformMoniker = ExtractPlatformMonikerFromResourceName(builder.Resource.Name);
        if (configuration.OtlpStubName is not null && (platformMoniker == "ios" || platformMoniker == "android"))
        {
            builder.WithEnvironment("ASPIRE_MAUI_OTLP_STUB_NAME", configuration.OtlpStubName);
        }

        if (configuration.EnableOtelDebug)
        {
            builder.WithEnvironment("OTEL_LOG_LEVEL", "debug")
                   .WithEnvironment("OTEL_BSP_SCHEDULE_DELAY", "200")
                   .WithEnvironment("OTEL_BSP_MAX_EXPORT_BATCH_SIZE", "1");
        }
    }

    /// <summary>
    /// Extracts the platform moniker from a resource name (e.g., "myapp-ios" -> "ios").
    /// </summary>
    private static string ExtractPlatformMonikerFromResourceName(string resourceName)
    {
        var idx = resourceName.LastIndexOf('-');
        return idx >= 0 && idx < resourceName.Length - 1 ? resourceName[(idx + 1)..] : string.Empty;
    }

    /// <summary>
    /// Attempt to add platform; returns true if a new platform resource was created and annotated as auto-detected.
    /// </summary>
    private static bool TryAddAutoDetectedPlatform(this IResourceBuilder<MauiProjectResource> builder, string moniker)
    {
        var configuration = GetConfiguration(builder);
        var before = configuration.PlatformResources.Count;
        builder.AddPlatform(moniker);
        if (configuration.PlatformResources.Count > before)
        {
            configuration.PlatformResources[^1].WithAnnotation(new MauiAutoDetectedPlatformAnnotation());
            return true;
        }
        return false;
    }
}

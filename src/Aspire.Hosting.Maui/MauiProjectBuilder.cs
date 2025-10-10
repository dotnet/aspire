// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.DevTunnels;
using Aspire.Hosting.Maui.Platforms.iOS;
using Aspire.Hosting.Maui.DevTunnels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Maui;

/// <summary>
/// Builder used to configure platform specific resources for a MAUI project.
/// </summary>
public sealed class MauiProjectBuilder
{
    private readonly IDistributedApplicationBuilder _appBuilder;
    private readonly MauiProjectResource _mauiLogicalResource;
    private readonly string _projectPath;
    private readonly HashSet<string> _availableTfms;
    private readonly List<IResourceBuilder<ProjectResource>> _platformResources = [];
    private readonly HashSet<IResourceWithEndpoints> _referencedEndpointResources = [];
    private IResourceBuilder<DevTunnelResource>? _otlpDevTunnel; // Dev tunnel dedicated to OTLP traffic
    private OtlpLoopbackResource? _otlpStub; // Synthetic loopback resource representing local OTLP port
    private int _otlpStubPort; // Cached OTLP port for manual allocation
    private string? _otlpStubName; // Name of synthetic OTLP stub resource
    private bool _enableOtelDebug; // Whether to inject verbose OTEL exporter debug env vars
    private static readonly ConcurrentDictionary<string, Lazy<Task>> s_builds = new();

    internal MauiProjectBuilder(IDistributedApplicationBuilder appBuilder, MauiProjectResource logical, string projectPath)
    {
        _appBuilder = appBuilder;
        _mauiLogicalResource = logical;
        _projectPath = projectPath;
        _availableTfms = MauiPlatformDetection.LoadTargetFrameworks(projectPath);

        // We still log at BeforeStartEvent, but detection may already have happened earlier (e.g. via WithReference()).
        appBuilder.Eventing.Subscribe<BeforeStartEvent>((evt, ct) =>
        {
            var logger = evt.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Aspire.Hosting.Maui");

            // Emit warnings for any explicitly added but unsupported platform resources.
            var unsupported = _platformResources
                .Where(r => r.Resource.Annotations.OfType<MauiUnsupportedPlatformAnnotation>().Any())
                .Select(r => r.Resource.Name)
                .ToList();
            
            if (unsupported.Count > 0)
            {
                logger.LogWarning("The following .NET MAUI platform resource(s) cannot run on this host OS and will fail to start: {Platforms}", string.Join(",", unsupported));
            }

            if (_platformResources.Count == 0)
            {
                // Final chance: perform detection so the user can still launch even if they never referenced anything.
                var detected = EnsureAutoDetection();
                if (detected.Count == 0)
                {
                    logger.LogWarning("No .NET MAUI platform resources were configured for '{Name}'. Call one of the WithWindows()/WithAndroid()/WithiOS()/WithMacCatalyst() methods to enable launching a platform-specific app.", _mauiLogicalResource.Name);
                }
                else
                {
                    logger.LogWarning("Auto-detected .NET MAUI platform(s) {Platforms} for '{Name}' based on TargetFrameworks. For clarity, explicitly call WithWindows()/WithAndroid()/WithiOS()/WithMacCatalyst() in the AppHost.", string.Join(",", detected), _mauiLogicalResource.Name);
                }
            }
            else if (_platformResources.Any(p => p.Resource.Annotations.OfType<MauiAutoDetectedPlatformAnnotation>().Any()))
            {
                var auto = GetAutoDetectedPlatformNames();
                if (auto.Count > 0)
                {
                    logger.LogWarning("Auto-detected .NET MAUI platform(s) {Platforms} for '{Name}' based on TargetFrameworks. For clarity, explicitly call WithWindows()/WithAndroid()/WithiOS()/WithMacCatalyst() in the AppHost.", string.Join(",", auto), _mauiLogicalResource.Name);
                }
            }

            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// The MAUI project file path.
    /// </summary>
    public string ProjectPath => _projectPath;

    /// <summary>
    /// Adds a Windows platform resource if the MAUI project targets windows.
    /// </summary>
    public MauiProjectBuilder WithWindows(string? runtimeIdentifier = null)
    {
        AddPlatform("windows", runtimeIdentifier);
        return this;
    }

    /// <summary>
    /// Adds an Android platform resource if the MAUI project targets Android.
    /// </summary>
    /// <param name="adbTarget">Optional adb target passed as an MSBuild property (e.g. <c>-p:AdbTarget=-e</c>) to select a specific emulator or device.</param>
    /// <remarks>
    /// Android support is currently limited to adding the platform resource without additional provisioning logic.
    /// If <paramref name="adbTarget"/> is provided it is forwarded as an MSBuild property when the project is started.
    /// </remarks>
    public MauiProjectBuilder WithAndroid(string? adbTarget = null)
    {
        AddPlatform("android", msbuildProperty: adbTarget is null ? null : $"AdbTarget={adbTarget}");
        return this;
    }

    /// <summary>
    /// Adds an iOS platform resource if the MAUI project targets iOS.
    /// </summary>
    /// <param name="deviceUdid">Optional _DeviceName UDID (simulator or device) passed as -p:_DeviceName=&lt;UDID&gt;</param>
    public MauiProjectBuilder WithiOS(string? deviceUdid = null)
    {
        AddPlatform("ios", msbuildProperty: deviceUdid is null ? null : $"_DeviceName={deviceUdid}");
        return this;
    }

    /// <summary>
    /// Adds a MacCatalyst platform resource if the MAUI project targets MacCatalyst.
    /// </summary>
    /// <param name="runtimeIdentifier">Optional runtime identifier (e.g. maccatalyst-x64 or maccatalyst-arm64).</param>
    public MauiProjectBuilder WithMacCatalyst(string? runtimeIdentifier = null)
    {
        AddPlatform("maccatalyst", runtimeIdentifier);
        return this;
    }

    /// <summary>
    /// Propagates a reference to another resource to all platform resources.
    /// </summary>
    public MauiProjectBuilder WithReference<TSource>(IResourceBuilder<TSource> source, string? connectionName = null)
        where TSource : IResource
    {
        // Ensure platforms are materialized early so service discovery / connection string references propagate.
        EnsureAutoDetection();

        if (source.Resource is IResourceWithEndpoints endpointsResource && !_referencedEndpointResources.Contains(endpointsResource))
        {
            _referencedEndpointResources.Add(endpointsResource);
        }

        foreach (var pr in _platformResources)
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
        return this;
    }

    /// <summary>
    /// Propagates service discovery variables for the specified endpoint resource through the provided dev tunnel
    /// into all MAUI platform resources (tunneled service discovery). This allows a device/emulator to reach
    /// the service via the tunnel host instead of localhost.
    /// </summary>
    /// <param name="source">The endpoint-providing resource to expose via the tunnel.</param>
    /// <param name="tunnel">The dev tunnel resource already configured to reference <paramref name="source"/>.</param>
    /// <remarks>
    /// This keeps fluent syntax in the AppHost: <c>.WithReference(weatherApi, publicDevTunnel)</c> without requiring
    /// callers to access individual platform project resources.
    /// </remarks>
    public MauiProjectBuilder WithReference(IResourceBuilder<IResourceWithEndpoints> source, IResourceBuilder<DevTunnelResource> tunnel)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(tunnel);

        EnsureAutoDetection();

        foreach (var pr in _platformResources)
        {
            if (pr.Resource is IResourceWithEnvironment)
            {
                pr.WithReference(source, tunnel);
            }
        }

        return this;
    }

    /// <summary>
    /// Creates a single Dev Tunnel exposing only the local OTLP port and rewrites the OTLP exporter endpoint
    /// to use the tunneled address for device/simulator telemetry.
    /// </summary>
    /// <param name="tunnelName">Optional tunnel resource name; defaults to &lt;logical-name&gt;-otlp.</param>
    /// <param name="enableOtelDebug">When true, injects verbose OTEL exporter debug env vars for troubleshooting (defaults to false).</param>
    public MauiProjectBuilder WithOtlpDevTunnel(string? tunnelName = null, bool enableOtelDebug = false)
    {
        if (_otlpDevTunnel is not null)
        {
            return this; // already configured
        }

        // Use a stable dev tunnel name distinct from the OTLP concept.
        tunnelName ??= _mauiLogicalResource.Name + "-devtunnel";
        _otlpDevTunnel = _appBuilder.AddDevTunnel(tunnelName).WithAnonymousAccess();
        _enableOtelDebug = enableOtelDebug;

        // Resolve OTLP endpoint (scheme & port) from configuration.
        var (otlpScheme, otlpPort) = DevTunnels.OtlpEndpointResolver.Resolve(_appBuilder.Configuration);

        // Create synthetic hidden stub resource referenced only for tunneling.
        _otlpStubName = _mauiLogicalResource.Name + "-otlpstub";
        if (_appBuilder.Resources.Any(r => string.Equals(r.Name, _otlpStubName, StringComparison.OrdinalIgnoreCase)))
        {
            return this; // defensive (should not occur normally)
        }

        _otlpStub = new DevTunnels.OtlpLoopbackResource(_otlpStubName, otlpPort, otlpScheme);
        _otlpStubPort = otlpPort;
        var stubBuilder = _appBuilder.AddResource(_otlpStub)
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
            var logicalBuilder = _appBuilder.CreateResourceBuilder(_mauiLogicalResource);
            stubBuilder.WithParentRelationship(logicalBuilder);
        }
        catch
        {
            // Fallback: if for some reason we cannot create the logical parent builder, do nothing.
        }

        // Force the dev tunnel port protocol to HTTPS regardless of the local collector's scheme so the
        // public tunnel endpoint is always an https:// URL (Dev Tunnels surface TLS endpoints).
        _otlpDevTunnel.WithReference(stubBuilder, new DevTunnelPortOptions { Protocol = "https" });
        var stubEndpointsBuilder = _appBuilder.CreateResourceBuilder<IResourceWithEndpoints>(_otlpStub);

        // Ensure the stub endpoint appears allocated before the tunnel starts (the dev tunnel waits on allocation events).
        // We synthesize the allocation and raise ResourceEndpointsAllocatedEvent early.
        _appBuilder.Eventing.Subscribe<BeforeStartEvent>((evt, ct) =>
        {
            if (_otlpStub is null)
            {
                return Task.CompletedTask;
            }
            var endpoint = _otlpStub.Annotations.OfType<EndpointAnnotation>().FirstOrDefault();
            if (endpoint is null)
            {
                return Task.CompletedTask;
            }
            if (endpoint.AllocatedEndpoint is null)
            {
                endpoint.AllocatedEndpoint = new AllocatedEndpoint(endpoint, "localhost", _otlpStubPort);
                return _appBuilder.Eventing.PublishAsync<ResourceEndpointsAllocatedEvent>(new(_otlpStub, evt.Services), ct);
            }
            return Task.CompletedTask;
        });

        // Ensure platforms exist (auto-detect if user hasn't added explicitly yet) so we can wire env vars.
        EnsureAutoDetection();

        foreach (var pr in _platformResources.Where(p => p.Resource is IResourceWithEnvironment))
        {
            ApplyOtlpConfigurationToPlatform(pr);
        }

        return this;
    }

    private void AddPlatform(string platformMoniker, string? runtimeIdentifier = null, string? msbuildProperty = null)
    {
        var platformConfig = MauiPlatformConfiguration.KnownPlatforms.GetByMoniker(platformMoniker);
        if (platformConfig is null)
        {
            return; // Unknown platform moniker
        }

        var resourceName = $"{_mauiLogicalResource.Name}-{platformMoniker}";

        // Check if this platform has already been added (duplicate guard)
        if (_platformResources.Any(pr => pr.Resource.Name.Equals(resourceName, StringComparison.OrdinalIgnoreCase)))
        {
            // Platform already added - log a warning and skip
            var loggerFactory = _appBuilder.Services.BuildServiceProvider().GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger<MauiProjectBuilder>();
            logger?.LogWarning("Platform '{Platform}' has already been added to MAUI project '{Project}'. Ignoring duplicate call to With{Platform}().",
                platformMoniker, _mauiLogicalResource.Name, char.ToUpper(platformMoniker[0]) + platformMoniker[1..]);
            return;
        }

        // Identify TFM prefix e.g. net10.0-windows, net10.0-android
        var tfm = _availableTfms.FirstOrDefault(t => t.Contains('-') && t.Split('-')[1].StartsWith(platformMoniker, StringComparison.OrdinalIgnoreCase));
        if (tfm is null)
        {
            // Platform was requested but project doesn't target this TFM - create a warning resource
            ConfigureMissingTfmPlatform(platformMoniker, platformConfig);
            return;
        }

        // Use existing AddProject API to create the platform-specific resource.
        var builder = _appBuilder.AddProject(resourceName, _projectPath)
            .WithExplicitStart()
            .WithAnnotation(ManifestPublishingCallbackAnnotation.Ignore);

        // Configure OpenTelemetry service identification for this platform.
        ConfigureOpenTelemetryEnvironment(builder, resourceName);

        // Add platform-specific icon for dashboard visualization.
        builder.WithAnnotation(new ResourceIconAnnotation(platformConfig.IconName, IconVariant.Filled));

        // Check if the platform is supported on the current host OS.
        var supported = platformConfig.IsSupportedOnCurrentHost();

        if (!supported)
        {
            ConfigureUnsupportedPlatform(builder, platformConfig);
        }

        // Pass framework & device specific msbuild properties via args so launching uses correct target
        builder.WithArgs(async context =>
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
                await Platforms.Android.AndroidEnvironmentTargetGenerator.AppendEnvironmentTargetsAsync(context).ConfigureAwait(false);
            }
        });

        _platformResources.Add(builder);

        // If OTLP tunneling already configured, wire the new platform to the stub & tunnel now.
        ApplyOtlpConfigurationIfNeeded(builder);

        // Conditional build hook executed right before the resource starts (per explicit start invocation).
        builder.OnBeforeResourceStarted(async (res, evt, ct) =>
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
            var tfmDir = Path.Combine(Path.GetDirectoryName(_projectPath) ?? string.Empty, "bin", config, tfm);
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

            var key = string.Join('|', _projectPath, tfm, config);
            var lazy = s_builds.GetOrAdd(key, _ => new Lazy<Task>(() => RunBuildAsync(ct)));
            try
            {
                await lazy.Value.ConfigureAwait(false);
            }
            catch
            {
                // If failed, remove so a retry start attempt can rebuild
                s_builds.TryRemove(key, out _);
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
                psi.ArgumentList.Add(_projectPath);
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

    // Attempt to add platform; returns true if a new platform resource was created and annotated as auto-detected.
    private bool TryAddAutoDetectedPlatform(string moniker)
    {
        var before = _platformResources.Count;
        AddPlatform(moniker);
        if (_platformResources.Count > before)
        {
            _platformResources[^1].WithAnnotation(new MauiAutoDetectedPlatformAnnotation());
            return true;
        }
        return false;
    }

    private readonly object _autoDetectLock = new();
    private bool _autoDetectionAttempted;

    // Returns the set of platforms that were auto-detected during this call (empty if already done or none found).
    private List<string> EnsureAutoDetection()
    {
        if (_platformResources.Count != 0)
        {
            return [];
        }

        lock (_autoDetectLock)
        {
            if (_autoDetectionAttempted || _platformResources.Count != 0)
            {
                return [];
            }
            _autoDetectionAttempted = true;
            return MauiPlatformDetection.AutoDetect(_availableTfms, TryAddAutoDetectedPlatform);
        }
    }

    private List<string> GetAutoDetectedPlatformNames()
    {
        var list = new List<string>();
        foreach (var b in _platformResources)
        {
            if (b.Resource.Annotations.OfType<MauiAutoDetectedPlatformAnnotation>().Any())
            {
                // Resource name pattern: <logical>-<platform>
                var name = b.Resource.Name;
                var idx = name.LastIndexOf('-');
                if (idx >= 0 && idx < name.Length - 1)
                {
                    list.Add(name[(idx + 1)..]);
                }
            }
        }
        return list;
    }

    /// <summary>
    /// Configures OpenTelemetry environment variables for the platform resource.
    /// Overrides DCP interpolation templates with concrete values for local MAUI projects.
    /// </summary>
    private static void ConfigureOpenTelemetryEnvironment(IResourceBuilder<ProjectResource> builder, string resourceName)
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
    private void ConfigureUnsupportedPlatform(IResourceBuilder<ProjectResource> builder, MauiPlatformConfiguration platformConfig)
    {
        builder.WithAnnotation(new MauiUnsupportedPlatformAnnotation(platformConfig.UnsupportedReason));

        // Publish the unsupported state after resources are created.
        // The "Unsupported" state prevents the platform from being started.
        // Note: Dashboard expects "warning" (not "warn" from KnownResourceStateStyles.Warn) for the warning icon to display.
        // This is a known inconsistency in the Aspire codebase between the hosting and dashboard layers.
        var resource = builder.Resource;
        _appBuilder.Eventing.Subscribe<AfterResourcesCreatedEvent>((evt, ct) =>
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
    private void ConfigureMissingTfmPlatform(string platformMoniker, MauiPlatformConfiguration platformConfig)
    {
        var resourceName = $"{_mauiLogicalResource.Name}-{platformMoniker}";

        // Create a placeholder project resource to show in the dashboard
        var builder = _appBuilder.AddProject(resourceName, _projectPath)
            .WithExplicitStart()
            .WithAnnotation(ManifestPublishingCallbackAnnotation.Ignore);

        // Add platform-specific icon for dashboard visualization
        builder.WithAnnotation(new ResourceIconAnnotation(platformConfig.IconName, IconVariant.Filled));

        // Track that this platform is missing the required TFM
        var warningMessage = $"Project does not target {platformMoniker}. Add 'net10.0-{platformMoniker}' to TargetFrameworks in the project file.";
        builder.WithAnnotation(new MauiMissingTfmAnnotation(platformMoniker, warningMessage));

        // Publish the warning state after resources are created
        // Note: Dashboard expects "warning" (not "warn" from KnownResourceStateStyles.Warn) for the warning icon to display.
        var resource = builder.Resource;
        _appBuilder.Eventing.Subscribe<AfterResourcesCreatedEvent>((evt, ct) =>
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
                    platformMoniker, _projectPath, platformMoniker);
            }

            return Task.CompletedTask;
        });

        // Prevent this platform from being started
        builder.OnBeforeResourceStarted((res, evt, ct) =>
        {
            var loggerService = evt.Services.GetService(typeof(ResourceLoggerService)) as ResourceLoggerService;
            var logger = loggerService?.GetLogger(res);

            logger?.LogWarning("Cannot start platform '{Platform}' because it is not included in the project's TargetFrameworks.", platformMoniker);
        });

        _platformResources.Add(builder);
    }

    /// <summary>
    /// Applies OTLP dev tunnel configuration to a platform if OTLP dev tunnel is enabled.
    /// </summary>
    private void ApplyOtlpConfigurationIfNeeded(IResourceBuilder<ProjectResource> builder)
    {
        if (_otlpDevTunnel is null || _otlpStub is null || builder.Resource is not IResourceWithEnvironment)
        {
            return;
        }

        ApplyOtlpConfigurationToPlatform(builder);
    }

    /// <summary>
    /// Applies OTLP dev tunnel configuration to a specific platform resource builder.
    /// </summary>
    private void ApplyOtlpConfigurationToPlatform(IResourceBuilder<ProjectResource> builder)
    {
        if (_otlpDevTunnel is null || _otlpStub is null)
        {
            return;
        }

        var stubEndpointsBuilder = _appBuilder.CreateResourceBuilder<IResourceWithEndpoints>(_otlpStub);
        builder.WithReference(stubEndpointsBuilder, _otlpDevTunnel);

        // iOS and Android require the stub name to locate the OTLP endpoint after dev tunnel allocation.
        var platformMoniker = ExtractPlatformMonikerFromResourceName(builder.Resource.Name);
        if (_otlpStubName is not null && (platformMoniker == "ios" || platformMoniker == "android"))
        {
            builder.WithEnvironment("ASPIRE_MAUI_OTLP_STUB_NAME", _otlpStubName);
        }

        if (_enableOtelDebug)
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

}

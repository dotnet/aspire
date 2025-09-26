// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Maui.Platforms.iOS;
using Aspire.Hosting.DevTunnels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
// Android provisioning removed for now.

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
                    logger.LogWarning("No .NET MAUI platform resources were configured for '{Name}'. Call one of the WithWindows()/WithAndroid()/WithIOS()/WithMacCatalyst() methods to enable launching a platform-specific app.", _mauiLogicalResource.Name);
                }
                else
                {
                    logger.LogWarning("Auto-detected .NET MAUI platform(s) {Platforms} for '{Name}' based on TargetFrameworks. For clarity, explicitly call WithWindows()/WithAndroid()/WithIOS()/WithMacCatalyst() in the AppHost.", string.Join(",", detected), _mauiLogicalResource.Name);
                }
            }
            else if (_platformResources.Any(p => p.Resource.Annotations.OfType<MauiAutoDetectedPlatformAnnotation>().Any()))
            {
                var auto = GetAutoDetectedPlatformNames();
                if (auto.Count > 0)
                {
                    logger.LogWarning("Auto-detected .NET MAUI platform(s) {Platforms} for '{Name}' based on TargetFrameworks. For clarity, explicitly call WithWindows()/WithAndroid()/WithIOS()/WithMacCatalyst() in the AppHost.", string.Join(",", auto), _mauiLogicalResource.Name);
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
    /// <param name="deviceUdid">Optional _DeviceName UDID (simulator or device) passed as -p:_DeviceName=:v2:udid=...</param>
    public MauiProjectBuilder WithIOS(string? deviceUdid = null)
    {
        AddPlatform("ios", msbuildProperty: deviceUdid is null ? null : $"_DeviceName=:v2:udid={deviceUdid}");
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

        // Use a stable dev tunnel name distinct from the OTLP concept so nested port names can simplify to "-otlp".
        tunnelName ??= _mauiLogicalResource.Name + "-devtunnel";
        _otlpDevTunnel = _appBuilder.AddDevTunnel(tunnelName).WithAnonymousAccess();
        _enableOtelDebug = enableOtelDebug;

        // Determine OTLP port & scheme from configuration.
        // Priority: unified endpoint key -> HTTP-specific -> gRPC-specific -> fallback.
        var unifiedUrl = _appBuilder.Configuration["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"]; // launchSettings uses this key
        var httpUrl = _appBuilder.Configuration["ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL"]; // newer split key
        var grpcUrl = _appBuilder.Configuration["ASPIRE_DASHBOARD_OTLP_GRPC_ENDPOINT_URL"]; // newer split key
        var otlpPort = 18889;
        var otlpScheme = "http";
        if (Uri.TryCreate(unifiedUrl, UriKind.Absolute, out var unified))
        {
            otlpScheme = unified.Scheme;
            otlpPort = unified.IsDefaultPort ? (unified.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ? 443 : 80) : unified.Port;
        }
        else if (Uri.TryCreate(httpUrl, UriKind.Absolute, out var http))
        {
            otlpScheme = http.Scheme;
            otlpPort = http.IsDefaultPort ? (http.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ? 443 : 80) : http.Port;
        }
        else if (Uri.TryCreate(grpcUrl, UriKind.Absolute, out var grpc))
        {
            otlpScheme = grpc.Scheme; // usually http/https
            otlpPort = grpc.IsDefaultPort ? 4317 : grpc.Port;
        }

        // Revert to a valid visible name (leading underscore invalid) but we'll hide via snapshot (IsHidden=true)
        // so it does not appear in the dashboard UI.
        _otlpStubName = _mauiLogicalResource.Name + "-otlpstub";
        if (_appBuilder.Resources.Any(r => string.Equals(r.Name, _otlpStubName, StringComparison.OrdinalIgnoreCase)))
        {
            return this; // defensive (should not occur normally)
        }

         _otlpStub = new OtlpLoopbackResource(_otlpStubName, otlpPort, otlpScheme);
        _otlpStubPort = otlpPort;
        var stubBuilder = _appBuilder.AddResource(_otlpStub)
            .ExcludeFromManifest();

        // Mark the synthetic stub as hidden so the dashboard omits it while still allowing the Dev Tunnel
        // feature to attach to an endpoint-providing resource.
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
            pr.WithReference(stubEndpointsBuilder, _otlpDevTunnel);
            if (pr.Resource.Name.EndsWith("-ios", StringComparison.OrdinalIgnoreCase))
            {
                pr.WithEnvironment("ASPIRE_MAUI_OTLP_STUB_NAME", _otlpStubName);
            }
            if (_enableOtelDebug)
            {
                pr.WithEnvironment("OTEL_LOG_LEVEL", "debug")
                  .WithEnvironment("OTEL_BSP_SCHEDULE_DELAY", "200")
                  .WithEnvironment("OTEL_BSP_MAX_EXPORT_BATCH_SIZE", "1");
            }
        }

        return this;
    }

    private void AddPlatform(string platformMoniker, string? runtimeIdentifier = null, string? msbuildProperty = null)
    {
        // Identify TFM prefix e.g. net10.0-windows, net10.0-android
        var tfm = _availableTfms.FirstOrDefault(t => t.Contains('-') && t.Split('-')[1].StartsWith(platformMoniker, StringComparison.OrdinalIgnoreCase));
        if (tfm is null)
        {
            return; // platform not targeted by project
        }

        var resourceName = $"{_mauiLogicalResource.Name}-{platformMoniker}";
        // Use existing AddProject API so we don't rely on internal internals.
        var builder = _appBuilder.AddProject(resourceName, _projectPath)
            .WithExplicitStart()
            .WithAnnotation(ManifestPublishingCallbackAnnotation.Ignore);

        // // Override OTEL_SERVICE_NAME placeholder (the generic OTLP configuration sets a DCP interpolation template
        // // like {{- index .Annotations "otel-service-name" -}} which is never resolved for a local MAUI project).
        // // Provide a stable concrete service name instead so the exporter doesn't emit the literal template.
        // builder.WithEnvironment("OTEL_SERVICE_NAME", resourceName);

        // Determine if the requested platform is supported on the current host OS.
        var isWindows = OperatingSystem.IsWindows();
        var isMacOS = OperatingSystem.IsMacOS();

        // Android builds can run on both Windows and macOS (developer tooling scenario). For now we allow always.
        var supported = platformMoniker switch
        {
            "windows" => isWindows,
            "maccatalyst" or "ios" => isMacOS,
            // Future: additional host checks for android if needed.
            _ => true
        };

        if (!supported)
        {
            var reason = platformMoniker switch
            {
                "windows" => "Windows platform requires running on a Windows host.",
                "maccatalyst" => "MacCatalyst platform requires running on a macOS host.",
                "ios" => "iOS platform requires running on a macOS host with appropriate tooling.",
                _ => "Unsupported host operating system for this platform."
            };
            builder.WithAnnotation(new MauiUnsupportedPlatformAnnotation(reason));
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
        });

        _platformResources.Add(builder);

        // If OTLP tunneling already configured, wire the new platform to the stub & tunnel now.
        if (_otlpDevTunnel is not null && _otlpStub is not null && builder.Resource is IResourceWithEnvironment)
        {
            var stubEndpointsBuilder = _appBuilder.CreateResourceBuilder<IResourceWithEndpoints>(_otlpStub);
            builder.WithReference(stubEndpointsBuilder, _otlpDevTunnel);
            
            if (_otlpStubName is not null && builder.Resource.Name.EndsWith("-ios", StringComparison.OrdinalIgnoreCase))
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

        // Conditional build hook executed right before the resource starts (per explicit start invocation).
        builder.OnBeforeResourceStarted(async (res, evt, ct) =>
        {
            var loggerService = evt.Services.GetService(typeof(ResourceLoggerService)) as ResourceLoggerService;
            var logger = loggerService?.GetLogger(res);

            // Always gate unsupported platform starts (independent of initial startup phase) so tests and early attempts fail fast.
            if (res.Annotations.OfType<MauiUnsupportedPlatformAnnotation>().FirstOrDefault() is { } unsupported)
            {
                logger?.LogWarning("MAUI platform '{Resource}' cannot start on this host: {Reason}", res.Name, unsupported.Reason);
                throw new InvalidOperationException($"The .NET MAUI platform resource '{res.Name}' cannot be started on this host: {unsupported.Reason}");
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

    private sealed class OtlpLoopbackResource : Resource, IResourceWithEndpoints
    {
        public OtlpLoopbackResource(string name, int port, string scheme) : base(name)
        {
            if (port <= 0 || port > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }
            if (string.IsNullOrWhiteSpace(scheme))
            {
                scheme = "http";
            }
            // Stable endpoint name 'otlp' so service discovery key is services__{stubName}__otlp__0 regardless of scheme.
            Annotations.Add(new EndpointAnnotation(System.Net.Sockets.ProtocolType.Tcp, uriScheme: scheme, name: "otlp", port: port, isProxied: false)
            {
                TargetHost = "localhost"
            });
        }
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

    private sealed class MauiAutoDetectedPlatformAnnotation : IResourceAnnotation { }

    private sealed class MauiUnsupportedPlatformAnnotation(string reason) : IResourceAnnotation
    {
        public string Reason { get; } = reason;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;
using System.Collections.Concurrent;
using System.Diagnostics;
using Aspire.Hosting.ApplicationModel;
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
    private static readonly ConcurrentDictionary<string, Lazy<Task>> s_builds = new();

    internal MauiProjectBuilder(IDistributedApplicationBuilder appBuilder, MauiProjectResource logical, string projectPath)
    {
        _appBuilder = appBuilder;
        _mauiLogicalResource = logical;
        _projectPath = projectPath;
        _availableTfms = LoadTargetFrameworks(projectPath);

        // Warn if the user never added at least one MAUI platform for this project.
        appBuilder.Eventing.Subscribe<BeforeStartEvent>((evt, ct) =>
        {
            if (_platformResources.Count == 0)
            {
                var loggerFactory = evt.Services.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("Aspire.Hosting.Maui");
                logger.LogWarning("No .NET MAUI platform resources were configured for '{Name}'. Call one of the WithWindows()/WithAndroid()/WithIOS()/WithMacCatalyst() methods to enable launching a platform-specific app.", _mauiLogicalResource.Name);
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

    // Android support temporarily reduced to simple platform inclusion without auto provisioning.
    public MauiProjectBuilder WithAndroid(string? adbTarget = null)
    {
        AddPlatform("android", msbuildProperty: adbTarget is null ? null : ($"AdbTarget={adbTarget}"));
        return this;
    }

    /// <summary>
    /// Adds an iOS platform resource if the MAUI project targets iOS.
    /// </summary>
    /// <param name="deviceUdid">Optional _DeviceName UDID (simulator or device) passed as -p:_DeviceName=:v2:udid=...</param>
    public MauiProjectBuilder WithIOS(string? deviceUdid = null)
    {
        AddPlatform("ios", msbuildProperty: deviceUdid is null ? null : ($"_DeviceName=:v2:udid={deviceUdid}"));
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

        // Pass framework & device specific msbuild properties via args so launching uses correct target
        builder.WithArgs(context =>
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
        });

        _platformResources.Add(builder);

        // Conditional build hook executed right before the resource starts (per explicit start invocation).
        builder.OnBeforeResourceStarted(async (res, evt, ct) =>
        {
            // Skip during initial AppHost startup; only build on user-initiated explicit start later.
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

            var loggerService = evt.Services.GetService(typeof(ResourceLoggerService)) as ResourceLoggerService;
            var logger = loggerService?.GetLogger(res);

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

    private static HashSet<string> LoadTargetFrameworks(string projectPath)
    {
        var doc = XDocument.Load(projectPath);
        var ns = doc.Root?.Name.Namespace ?? XNamespace.None;
        var list = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tf in doc.Descendants(ns + "TargetFramework").Select(e => e.Value.Split(';', StringSplitOptions.RemoveEmptyEntries)))
        {
            foreach (var t in tf)
            {
                list.Add(t.Trim());
            }
        }
        foreach (var tfs in doc.Descendants(ns + "TargetFrameworks").Select(e => e.Value.Split(';', StringSplitOptions.RemoveEmptyEntries)))
        {
            foreach (var t in tfs)
            {
                list.Add(t.Trim());
            }
        }
        return list;
    }
}

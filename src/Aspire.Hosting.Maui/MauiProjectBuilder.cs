// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using System.Xml.Linq;

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

    internal MauiProjectBuilder(IDistributedApplicationBuilder appBuilder, MauiProjectResource logical, string projectPath)
    {
        _appBuilder = appBuilder;
        _mauiLogicalResource = logical;
        _projectPath = projectPath;
        _availableTfms = LoadTargetFrameworks(projectPath);
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
    /// Adds an Android platform resource if the MAUI project targets android.
    /// </summary>
    /// <param name="adbTarget">Optional AdbTarget parameter passed as -p:AdbTarget=...</param>
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
    // Use existing AddProject API so we don't rely on internal annotations.
        var builder = _appBuilder.AddProject(resourceName, _projectPath)
                 .WithExplicitStart()
                 .WithAnnotation(ManifestPublishingCallbackAnnotation.Ignore); // never in manifest

        // Pass framework & device specific msbuild properties via args so launching uses correct target
        builder.WithArgs(context =>
        {
            // Default AddProject args look like: run --project <path>
            // We augment (not duplicate) by ensuring a single -f <tfm> and appending properties.
            // Remove any existing -f option to avoid conflicting frameworks.
            for (int i = 0; i < context.Args.Count; i++)
            {
                if (context.Args[i] is string s && string.Equals(s, "-f", StringComparison.OrdinalIgnoreCase))
                {
                    // Remove -f and following value if present
                    context.Args.RemoveAt(i); // remove -f
                    if (i < context.Args.Count)
                    {
                        context.Args.RemoveAt(i); // remove value
                    }
                    i--; // adjust index
                }
            }
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
        });

        _platformResources.Add(builder);

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

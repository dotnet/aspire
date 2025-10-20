// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.DevTunnels;
using Aspire.Hosting.Maui.DevTunnels;

namespace Aspire.Hosting.Maui;

/// <summary>
/// Internal annotation that holds configuration state for a MAUI project resource.
/// </summary>
internal sealed class MauiProjectConfiguration : IResourceAnnotation
{
    private readonly object _autoDetectLock = new();
    private bool _autoDetectionAttempted;

    public MauiProjectConfiguration(string projectPath, HashSet<string> availableTfms)
    {
        ProjectPath = projectPath;
        AvailableTfms = availableTfms;
    }

    public string ProjectPath { get; }
    public HashSet<string> AvailableTfms { get; }
    public List<IResourceBuilder<ProjectResource>> PlatformResources { get; } = [];
    public HashSet<IResourceWithEndpoints> ReferencedEndpointResources { get; } = [];
    public IResourceBuilder<DevTunnelResource>? OtlpDevTunnel { get; set; }
    public OtlpLoopbackResource? OtlpStub { get; set; }
    public int OtlpStubPort { get; set; }
    public string? OtlpStubName { get; set; }
    public bool EnableOtelDebug { get; set; }
    
    public static readonly ConcurrentDictionary<string, Lazy<Task>> Builds = new();

    /// <summary>
    /// Returns the set of platforms that were auto-detected during this call (empty if already done or none found).
    /// </summary>
    internal List<string> EnsureAutoDetection(IDistributedApplicationBuilder _, Func<string, bool> tryAddPlatform)
    {
        if (PlatformResources.Count != 0)
        {
            return [];
        }

        lock (_autoDetectLock)
        {
            if (_autoDetectionAttempted || PlatformResources.Count != 0)
            {
                return [];
            }
            _autoDetectionAttempted = true;
            return MauiPlatformDetection.AutoDetect(AvailableTfms, tryAddPlatform);
        }
    }
}

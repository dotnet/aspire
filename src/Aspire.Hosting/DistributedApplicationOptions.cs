// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Aspire.Hosting;

/// <summary>
/// Options for configuring the behavior of <see cref="DistributedApplication.CreateBuilder(DistributedApplicationOptions)"/>.
/// </summary>
public sealed class DistributedApplicationOptions
{
    private readonly Lazy<Assembly?> _assembly;
    private readonly Lazy<string?> _projectDirectoryLazy;
    // This is for testing
    private string? _projectDirectory;

    /// <summary>
    /// Initializes a new instance of <see cref="DistributedApplicationOptions"/>.
    /// </summary>
    public DistributedApplicationOptions()
    {
        _assembly = new(ResolveAssembly);
        _projectDirectoryLazy = new(ResolveProjectDirectory);
    }

    /// <summary>
    /// The command line arguments.
    /// </summary>
    public string[]? Args { get; set; }

    /// <summary>
    /// The AssemblyName of the AppHost project for loading configuration attributes; if not set defaults to Assembly.GetEntryAssembly().
    /// </summary>
    public string? AssemblyName { get; set; }

    /// <summary>
    /// Determines whether the dashboard is disabled.
    /// </summary>
    public bool DisableDashboard { get; set; }

    internal Assembly? Assembly => _assembly.Value;

    internal string? ProjectDirectory
    {
        get => _projectDirectory ?? _projectDirectoryLazy.Value;
        set => _projectDirectory = value;
    }

    internal bool DashboardEnabled => !DisableDashboard;

    /// <summary>
    /// Allows the use of HTTP urls for for the AppHost resource endpoint.
    /// </summary>
    public bool AllowUnsecuredTransport { get; set; }

    private string? ResolveProjectDirectory()
    {
        var assemblyMetadata = Assembly?.GetCustomAttributes<AssemblyMetadataAttribute>();
        return GetMetadataValue(assemblyMetadata, "AppHostProjectPath");
    }

    private Assembly? ResolveAssembly()
    {
        // Calculate DCP locations from configuration options
        var appHostAssembly = Assembly.GetEntryAssembly();
        if (!string.IsNullOrEmpty(AssemblyName))
        {
            try
            {
                // Find an assembly in the current AppDomain with the given name
                appHostAssembly = Assembly.Load(AssemblyName);
                if (appHostAssembly == null)
                {
                    throw new FileNotFoundException($"No assembly with name '{AssemblyName}' exists in the current AppDomain.");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load AppHost assembly '{AssemblyName}' specified in {nameof(DistributedApplicationOptions)}.", ex);
            }
        }
        return appHostAssembly;
    }

    private static string? GetMetadataValue(IEnumerable<AssemblyMetadataAttribute>? assemblyMetadata, string key)
    {
        return assemblyMetadata?.FirstOrDefault(m => string.Equals(m.Key, key, StringComparison.OrdinalIgnoreCase))?.Value;
    }
}

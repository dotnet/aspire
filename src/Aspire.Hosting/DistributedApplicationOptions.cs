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
    private readonly Lazy<string?> _projectNameLazy;
    private readonly Lazy<string?> _configurationLazy;
    // This is for testing
    private string? _projectDirectory;
    private string? _projectName;

    /// <summary>
    /// Initializes a new instance of <see cref="DistributedApplicationOptions"/>.
    /// </summary>
    public DistributedApplicationOptions()
    {
        _assembly = new(ResolveAssembly);
        _projectDirectoryLazy = new(ResolveProjectDirectory);
        _projectNameLazy = new(ResolveProjectName);
        _configurationLazy = new(ResolveConfiguration);
    }

    /// <summary>
    /// When containers are used, use this value instead to override the container registry
    /// that is specified.
    /// </summary>
    public string? ContainerRegistryOverride { get; set; }

    /// <summary>
    /// Enables resource logging. Logs will be written to the logger category (ApplicationName.Resources.{resourceName}).
    /// </summary>
    public bool EnableResourceLogging { get; set; }

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

    internal string? Configuration => _configurationLazy.Value;

    internal string? ProjectDirectory
    {
        get => _projectDirectory ?? _projectDirectoryLazy.Value;
        set => _projectDirectory = value;
    }

    internal string? ProjectName
    {
        get => _projectName ?? _projectNameLazy.Value;
        set => _projectName = value;
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

    private string? ResolveProjectName()
    {
        var assemblyMetadata = Assembly?.GetCustomAttributes<AssemblyMetadataAttribute>();
        return GetMetadataValue(assemblyMetadata, "AppHostProjectName");
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

    private string? ResolveConfiguration()
    {
        return Assembly?.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration;
    }

    private static string? GetMetadataValue(IEnumerable<AssemblyMetadataAttribute>? assemblyMetadata, string key)
    {
        return assemblyMetadata?.FirstOrDefault(m => string.Equals(m.Key, key, StringComparison.OrdinalIgnoreCase))?.Value;
    }
}

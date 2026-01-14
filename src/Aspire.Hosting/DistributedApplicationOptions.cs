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
    private readonly Lazy<string?> _dashboardApplicationNameLazy;
    private readonly Lazy<string?> _appHostFilePathLazy;
    private readonly Lazy<string?> _configurationLazy;
    // This is for testing
    private string? _projectDirectory;
    private bool _projectDirectorySet;
    private string? _projectName;
    private string? _dashboardApplicationName;

    /// <summary>
    /// Initializes a new instance of <see cref="DistributedApplicationOptions"/>.
    /// </summary>
    public DistributedApplicationOptions()
    {
        _assembly = new(ResolveAssembly);
        _projectDirectoryLazy = new(ResolveProjectDirectory);
        _projectNameLazy = new(ResolveProjectName);
        _dashboardApplicationNameLazy = new(ResolveDashboardApplicationName);
        _appHostFilePathLazy = new(ResolveAppHostFilePath);
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

    /// <summary>
    /// The directory containing the AppHost project file. If not set, defaults to the directory resolved from assembly metadata.
    /// </summary>
    public string? ProjectDirectory
    {
        get => _projectDirectorySet ? _projectDirectory : _projectDirectoryLazy.Value;
        set
        {
            _projectDirectory = value;
            _projectDirectorySet = true;
        }
    }

    internal string? ProjectName
    {
        get => _projectName ?? _projectNameLazy.Value;
        set => _projectName = value;
    }

    /// <summary>
    /// The application name to display in the dashboard. For file-based app hosts, this defaults to the directory name.
    /// For other apps, it falls back to the environment's application name.
    /// </summary>
    public string? DashboardApplicationName
    {
        get => _dashboardApplicationName ?? _dashboardApplicationNameLazy.Value;
        set => _dashboardApplicationName = value;
    }

    /// <summary>
    /// Gets the fully qualified path to the AppHost file (either .csproj or .cs file).
    /// For .csproj-based AppHosts, this returns the path to the .csproj file.
    /// For single-file AppHosts, this returns the path to the .cs file.
    /// </summary>
    internal string? AppHostFilePath => _appHostFilePathLazy.Value;

    internal bool DashboardEnabled => !DisableDashboard;

    /// <summary>
    /// Allows the use of HTTP urls for for the AppHost resource endpoint.
    /// </summary>
    public bool AllowUnsecuredTransport { get; set; }

    /// <summary>
    /// Whether to attempt to implicitly add trust for developer certificates (currently the ASP.NET development certificate)
    /// by default at runtime. Disabling this option will also disable the automatic use of the developer certificate for server authentication.
    /// </summary>
    public bool? TrustDeveloperCertificate { get; set; }

    /// <summary>
    /// Whether to attempt to implicitly use a developer certificate (currently the ASP.NET Core development certificate) for server authentication for non-ASP.NET resources
    /// by default at runtime.
    /// </summary>
    public bool? DeveloperCertificateDefaultHttpsTerminationEnabled { get; set; }

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

    private string? ResolveDashboardApplicationName()
    {
        // For file-based app hosts (single-file programs), use the directory name as the dashboard application name
        // to provide a more meaningful identifier than the generated assembly name.
        // File-based programs set the "EntryPointFilePath" data in AppContext.
        // For example, if the apphost file is at "foo/apphost.cs", the dashboard name becomes "foo".
        var entryPointFilePath = AppContext.GetData("EntryPointFilePath") as string;
        if (!string.IsNullOrEmpty(entryPointFilePath))
        {
            // Use the directory name from ProjectDirectory if available
            var projectDirectory = ProjectDirectory;
            if (!string.IsNullOrEmpty(projectDirectory))
            {
                return Path.GetFileName(projectDirectory);
            }
        }

        // For non-file-based apps, return null to fall back to IHostEnvironment.ApplicationName
        return null;
    }

    private string? ResolveAppHostFilePath()
    {
        // For single-file app hosts, the EntryPointFilePath AppContext data contains the path to the .cs file
        var entryPointFilePath = AppContext.GetData("EntryPointFilePath") as string;
        if (!string.IsNullOrEmpty(entryPointFilePath))
        {
            return Path.GetFullPath(entryPointFilePath);
        }

        // For .csproj-based app hosts, check assembly metadata for the project path
        var assemblyMetadata = Assembly?.GetCustomAttributes<AssemblyMetadataAttribute>();
        var projectPath = GetMetadataValue(assemblyMetadata, "AppHostProjectPath");

        if (!string.IsNullOrEmpty(projectPath))
        {
            // The metadata may contain either the full path to the .csproj file or just the project directory.
            // If it's a directory, combine with the project name to form the .csproj file path.
            var fullPath = Path.GetFullPath(projectPath);
            if (Directory.Exists(fullPath))
            {
                var name = ProjectName;
                if (!string.IsNullOrEmpty(name))
                {
                    var csprojPath = Path.Combine(fullPath, $"{name}.csproj");
                    if (File.Exists(csprojPath))
                    {
                        return csprojPath;
                    }
                }
                // If we can't resolve the file, fall through to fallback logic below.
            }
            else if (File.Exists(fullPath) && fullPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                return fullPath;
            }
            // If neither, fall through to fallback logic below.
        }

        // Fallback: construct the path from directory and project name
        var projectDirectory = ProjectDirectory;
        var projectName = ProjectName;

        if (!string.IsNullOrEmpty(projectDirectory) && !string.IsNullOrEmpty(projectName))
        {
            var csprojPath = Path.Combine(projectDirectory, $"{projectName}.csproj");
            if (File.Exists(csprojPath))
            {
                return Path.GetFullPath(csprojPath);
            }
        }

        return null;
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

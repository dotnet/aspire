// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.RemoteHost;

/// <summary>
/// Service that loads assemblies on demand with proper logging.
/// </summary>
internal sealed class AssemblyLoader
{
    private readonly Lazy<IReadOnlyList<Assembly>> _assemblies;
    private readonly string _applicationBasePath;
    private readonly AssemblyLoadContext _loadContext;

    public AssemblyLoader(IConfiguration configuration, ILogger<AssemblyLoader> logger)
    {
        _applicationBasePath = AppContext.BaseDirectory;
        _loadContext = AssemblyLoadContext.GetLoadContext(typeof(AssemblyLoader).Assembly) ?? AssemblyLoadContext.Default;

        // ASPIRE_INTEGRATION_LIBS_PATH is set by the CLI when running guest (polyglot) apphosts
        // that require additional hosting integration packages. See docs/specs/bundle.md for details.
        var libsPath = configuration["ASPIRE_INTEGRATION_LIBS_PATH"];
        if (!string.IsNullOrEmpty(libsPath) && Directory.Exists(libsPath))
        {
            logger.LogDebug("Using load context {LoadContext} for integration assemblies at {Path}", _loadContext.Name ?? "<default>", libsPath);
        }

        _assemblies = new Lazy<IReadOnlyList<Assembly>>(
            () => LoadAssemblies(configuration, _loadContext, logger, libsPath, _applicationBasePath));
    }

    public IReadOnlyList<Assembly> GetAssemblies() => _assemblies.Value;

    internal static IReadOnlyList<string> GetAssemblyNamesToLoad(
        IConfiguration configuration,
        string? integrationLibsPath,
        string applicationBasePath)
    {
        var assemblyNames = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in configuration.GetSection("AtsAssemblies").Get<string[]>() ?? [])
        {
            if (!string.IsNullOrWhiteSpace(name) && seen.Add(name))
            {
                assemblyNames.Add(name);
            }
        }

        foreach (var name in DiscoverAspireHostingAssemblies([integrationLibsPath, applicationBasePath]))
        {
            if (seen.Add(name))
            {
                assemblyNames.Add(name);
            }
        }

        return assemblyNames;
    }

    internal static IReadOnlyList<string> DiscoverAspireHostingAssemblies(IEnumerable<string?> directories)
    {
        var assemblyNames = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var directory in directories)
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(directory, "Aspire*.dll", SearchOption.TopDirectoryOnly))
            {
                var assemblyName = Path.GetFileNameWithoutExtension(file);
                if (IsAutoDiscoveredAssembly(assemblyName))
                {
                    assemblyNames.Add(assemblyName);
                }
            }
        }

        return assemblyNames.ToList();
    }

    private static bool IsAutoDiscoveredAssembly(string? assemblyName)
    {
        if (string.IsNullOrWhiteSpace(assemblyName))
        {
            return false;
        }

        if (assemblyName.Equals("Aspire.Hosting.AppHost", StringComparison.OrdinalIgnoreCase) ||
            assemblyName.StartsWith("Aspire.AppHost.", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return assemblyName.Equals("Aspire.Hosting", StringComparison.OrdinalIgnoreCase) ||
            assemblyName.StartsWith("Aspire.Hosting.", StringComparison.OrdinalIgnoreCase);
    }

    private static List<Assembly> LoadAssemblies(
        IConfiguration configuration,
        AssemblyLoadContext loadContext,
        ILogger logger,
        string? integrationLibsPath,
        string applicationBasePath)
    {
        var assemblyNames = GetAssemblyNamesToLoad(configuration, integrationLibsPath, applicationBasePath);
        var assemblies = new List<Assembly>();

        foreach (var name in assemblyNames)
        {
            try
            {
                var assembly = LoadAssembly(loadContext, name, integrationLibsPath);
                assemblies.Add(assembly);
                logger.LogDebug("Loaded assembly: {AssemblyName} in {LoadContext}",
                    assembly.FullName,
                    AssemblyLoadContext.GetLoadContext(assembly)?.Name ?? "<unknown>");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to load assembly '{AssemblyName}'", name);
            }
        }

        return assemblies;
    }

    private static Assembly LoadAssembly(AssemblyLoadContext loadContext, string name, string? integrationLibsPath)
    {
        var assemblyName = new AssemblyName(name);

        if (loadContext != AssemblyLoadContext.Default)
        {
            return loadContext.LoadFromAssemblyName(assemblyName);
        }

        if (!string.IsNullOrWhiteSpace(integrationLibsPath))
        {
            var assemblyPath = Path.Combine(integrationLibsPath, $"{name}.dll");
            if (File.Exists(assemblyPath))
            {
                return LoadFromDefaultContext(assemblyName, assemblyPath);
            }
        }

        return Assembly.Load(assemblyName);
    }

    private static Assembly LoadFromDefaultContext(AssemblyName assemblyName, string assemblyPath)
    {
        var existing = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(assembly =>
                AssemblyLoadContext.GetLoadContext(assembly) == AssemblyLoadContext.Default &&
                string.Equals(assembly.GetName().Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase));

        return existing ?? AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
    }
}

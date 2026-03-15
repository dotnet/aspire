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
    private readonly string? _integrationLibsPath;
    private readonly string _applicationBasePath;

    public AssemblyLoader(IConfiguration configuration, ILogger<AssemblyLoader> logger)
    {
        _applicationBasePath = AppContext.BaseDirectory;

        // ASPIRE_INTEGRATION_LIBS_PATH is set by the CLI when running guest (polyglot) apphosts
        // that require additional hosting integration packages. See docs/specs/bundle.md for details.
        var libsPath = configuration["ASPIRE_INTEGRATION_LIBS_PATH"];
        if (!string.IsNullOrEmpty(libsPath) && Directory.Exists(libsPath))
        {
            _integrationLibsPath = libsPath;
            AssemblyLoadContext.Default.Resolving += ResolveAssemblyFromIntegrationLibs;
            logger.LogDebug("Registered assembly resolver for integration libs at {Path}", libsPath);
        }

        _assemblies = new Lazy<IReadOnlyList<Assembly>>(
            () => LoadAssemblies(configuration, logger, _integrationLibsPath, _applicationBasePath));
    }

    public IReadOnlyList<Assembly> GetAssemblies() => _assemblies.Value;

    private Assembly? ResolveAssemblyFromIntegrationLibs(AssemblyLoadContext context, AssemblyName assemblyName)
    {
        if (_integrationLibsPath is null || assemblyName.Name is null)
        {
            return null;
        }

        var assemblyPath = Path.Combine(_integrationLibsPath, $"{assemblyName.Name}.dll");
        if (File.Exists(assemblyPath))
        {
            return context.LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }

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
                var assembly = Assembly.Load(name);
                assemblies.Add(assembly);
                logger.LogDebug("Loaded assembly: {AssemblyName}", name);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to load assembly '{AssemblyName}'", name);
            }
        }

        return assemblies;
    }
}

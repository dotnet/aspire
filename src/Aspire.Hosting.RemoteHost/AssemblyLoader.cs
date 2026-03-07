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
    private readonly IntegrationLoadContext? _integrationLoadContext;

    public AssemblyLoader(IConfiguration configuration, ILogger<AssemblyLoader> logger)
    {
        // ASPIRE_INTEGRATION_LIBS_PATH is set by the CLI when running guest (polyglot) apphosts
        // that require additional hosting integration packages. See docs/specs/bundle.md for details.
        var libsPath = configuration["ASPIRE_INTEGRATION_LIBS_PATH"];
        if (!string.IsNullOrEmpty(libsPath) && Directory.Exists(libsPath))
        {
            _integrationLoadContext = new IntegrationLoadContext(libsPath);
            logger.LogDebug("Created integration load context for {Path}", libsPath);
        }

        _assemblies = new Lazy<IReadOnlyList<Assembly>>(() => LoadAssemblies(configuration, _integrationLoadContext, logger));
    }

    public IReadOnlyList<Assembly> GetAssemblies() => _assemblies.Value;

    public Assembly GetRequiredAssembly(string simpleName) =>
        _assemblies.Value.FirstOrDefault(assembly => string.Equals(assembly.GetName().Name, simpleName, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException($"Required assembly '{simpleName}' was not loaded.");

    private static List<Assembly> LoadAssemblies(
        IConfiguration configuration,
        IntegrationLoadContext? integrationLoadContext,
        ILogger logger)
    {
        var assemblyNames = configuration.GetSection("AtsAssemblies").Get<string[]>() ?? [];
        var assemblies = new List<Assembly>();

        foreach (var name in assemblyNames)
        {
            try
            {
                var assembly = integrationLoadContext is not null
                    ? integrationLoadContext.LoadFromAssemblyName(new AssemblyName(name))
                    : Assembly.Load(name);

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
}

internal sealed class IntegrationLoadContext(string integrationLibsPath) : AssemblyLoadContext("IntegrationLibs", isCollectible: true)
{
    private static readonly HashSet<string> s_sharedAssemblyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "StreamJsonRpc",
        "mscorlib",
        "netstandard",
        "System",
        "System.Diagnostics.DiagnosticSource",
        "System.Diagnostics.EventLog",
        "System.Private.CoreLib"
    };

    private readonly string _integrationLibsPath = integrationLibsPath;

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name is null)
        {
            return null;
        }

        var assemblyPath = Path.Combine(_integrationLibsPath, $"{assemblyName.Name}.dll");
        if (!ShouldShareAssembly(assemblyName.Name) && File.Exists(assemblyPath))
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return LoadFromDefaultContext(assemblyName);
    }

    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = Path.Combine(_integrationLibsPath, $"{unmanagedDllName}.dll");
        if (File.Exists(libraryPath))
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return base.LoadUnmanagedDll(unmanagedDllName);
    }

    internal static bool ShouldShareAssembly(string assemblyName)
    {
        return s_sharedAssemblyNames.Contains(assemblyName);
    }

    private static Assembly LoadFromDefaultContext(AssemblyName assemblyName)
    {
        var existing = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(assembly => string.Equals(assembly.GetName().Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase));

        return existing ?? AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
    }
}

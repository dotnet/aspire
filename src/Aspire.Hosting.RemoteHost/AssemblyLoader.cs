// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.RemoteHost;

/// <summary>
/// Service that loads assemblies on demand with proper logging.
/// </summary>
internal sealed class AssemblyLoader
{
    private readonly Lazy<IReadOnlyList<Assembly>> _assemblies;

    public AssemblyLoader(IConfiguration configuration, ILogger<AssemblyLoader> logger)
    {
        _assemblies = new Lazy<IReadOnlyList<Assembly>>(() => LoadAssemblies(configuration, logger));
    }

    public IReadOnlyList<Assembly> GetAssemblies() => _assemblies.Value;

    private static List<Assembly> LoadAssemblies(IConfiguration configuration, ILogger logger)
    {
        var assemblyNames = configuration.GetSection("AtsAssemblies").Get<string[]>() ?? [];
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

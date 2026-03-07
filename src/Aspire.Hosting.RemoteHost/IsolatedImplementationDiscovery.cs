// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.RemoteHost;

internal static class IsolatedImplementationDiscovery
{
    public static Dictionary<string, TAdapter> DiscoverByLanguage<TAdapter>(
        IServiceProvider serviceProvider,
        IReadOnlyList<Assembly> assemblies,
        Type contractType,
        ILogger logger,
        string implementationDescription,
        Func<object, Type, TAdapter> createAdapter,
        Func<TAdapter, string> getLanguage)
    {
        var implementations = new Dictionary<string, TAdapter>(StringComparer.OrdinalIgnoreCase);

        foreach (var type in GetConcreteAssignableTypes(assemblies, contractType, logger))
        {
            try
            {
                var instance = ActivatorUtilities.CreateInstance(serviceProvider, type);
                var adapter = createAdapter(instance, type);
                var language = getLanguage(adapter);

                implementations[language] = adapter;
                logger.LogDebug("Discovered {ImplementationDescription}: {TypeName} for language '{Language}'", implementationDescription, type.Name, language);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to instantiate {ImplementationDescription} '{TypeName}'", implementationDescription, type.Name);
            }
        }

        return implementations;
    }

    private static IEnumerable<Type> GetConcreteAssignableTypes(
        IReadOnlyList<Assembly> assemblies,
        Type contractType,
        ILogger logger)
    {
        foreach (var assembly in assemblies)
        {
            foreach (var type in GetLoadableTypes(assembly, logger))
            {
                if (!type.IsAbstract && !type.IsInterface && contractType.IsAssignableFrom(type))
                {
                    yield return type;
                }
            }
        }
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly, ILogger logger)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            logger.LogDebug(ex, "Some types in assembly '{AssemblyName}' could not be loaded", assembly.GetName().Name);
            return ex.Types.Where(static type => type is not null)!;
        }
    }
}

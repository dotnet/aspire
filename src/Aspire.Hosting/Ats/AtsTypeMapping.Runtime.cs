// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Reflection;

namespace Aspire.Hosting.Ats;

// Runtime reflection-based methods for AtsTypeMapping.
// These use System.Reflection.Assembly which is not AOT-compatible.
// The IAtsAssemblyInfo-based methods in AtsTypeMapping.cs are AOT-compatible.
public sealed partial class AtsTypeMapping
{
    /// <summary>
    /// Creates a type mapping by scanning the specified assemblies for <see cref="AspireExportAttribute"/> declarations.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>A type mapping containing all discovered type mappings.</returns>
    public static AtsTypeMapping FromAssemblies(IEnumerable<Assembly> assemblies)
    {
        var fullNameToTypeId = new Dictionary<string, string>(StringComparer.Ordinal);
        var typeIdToFullName = new Dictionary<string, string>(StringComparer.Ordinal);
        var exposePropertiesTypeIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var assembly in assemblies)
        {
            ScanAssemblyRuntime(assembly, fullNameToTypeId, typeIdToFullName, exposePropertiesTypeIds);
        }

        return new AtsTypeMapping(
            fullNameToTypeId.ToFrozenDictionary(StringComparer.Ordinal),
            typeIdToFullName.ToFrozenDictionary(StringComparer.Ordinal),
            exposePropertiesTypeIds.ToFrozenSet(StringComparer.Ordinal));
    }

    /// <summary>
    /// Creates a type mapping by scanning the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>A type mapping containing all discovered type mappings.</returns>
    public static AtsTypeMapping FromAssembly(Assembly assembly)
    {
        return FromAssemblies([assembly]);
    }

    private static void ScanAssemblyRuntime(
        Assembly assembly,
        Dictionary<string, string> fullNameToTypeId,
        Dictionary<string, string> typeIdToFullName,
        HashSet<string> exposePropertiesTypeIds)
    {
        var assemblyName = assembly.GetName().Name ?? "Unknown";

        // Scan assembly-level attributes
        foreach (var attr in assembly.GetCustomAttributes<AspireExportAttribute>())
        {
            if (attr.Type != null)
            {
                var fullName = attr.Type.FullName;
                if (fullName != null)
                {
                    // Derive type ID from the type's assembly and full name
                    var targetAssemblyName = attr.Type.Assembly.GetName().Name ?? assemblyName;
                    var typeId = DeriveTypeId(targetAssemblyName, fullName);

                    fullNameToTypeId[fullName] = typeId;
                    typeIdToFullName[typeId] = fullName;

                    if (attr.ExposeProperties)
                    {
                        exposePropertiesTypeIds.Add(typeId);
                    }
                }
            }
        }

        // Scan type-level attributes
        try
        {
            foreach (var type in assembly.GetTypes())
            {
                var attr = type.GetCustomAttribute<AspireExportAttribute>();
                if (attr != null)
                {
                    var fullName = type.FullName;
                    if (fullName != null)
                    {
                        // Derive type ID from assembly name and full type name
                        var typeId = DeriveTypeId(assemblyName, fullName);

                        fullNameToTypeId[fullName] = typeId;
                        typeIdToFullName[typeId] = fullName;

                        if (attr.ExposeProperties)
                        {
                            exposePropertiesTypeIds.Add(typeId);
                        }
                    }
                }
            }
        }
        catch (ReflectionTypeLoadException)
        {
            // Skip assemblies that can't be fully loaded
        }
    }
}

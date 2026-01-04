// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Reflection;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Ats;

namespace Aspire.Hosting.RemoteHost.Ats;

/// <summary>
/// Tracks the ATS type hierarchy for AppliesTo constraint validation.
/// Uses CLR type inheritance for IResource implementations and interface types.
/// </summary>
internal sealed class TypeHierarchy
{
    private readonly ConcurrentDictionary<string, Type> _atsTypeToResourceType = new();
    private readonly ConcurrentDictionary<Type, string> _resourceTypeToAtsType = new();
    private readonly ConcurrentDictionary<string, Type> _atsTypeToInterface = new();

    /// <summary>
    /// Creates a new TypeHierarchy.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan for types.</param>
    public TypeHierarchy(IEnumerable<Assembly> assemblies)
    {
        ScanAssemblies(assemblies);
    }

    /// <summary>
    /// Scans the provided assemblies for resource types and interface mappings.
    /// </summary>
    private void ScanAssemblies(IEnumerable<Assembly> assemblies)
    {
        var assemblyList = assemblies.ToList();

        // Scan assemblies for [AspireExport] type mappings and register them
        var typeMapping = AtsTypeMapping.FromAssemblies(assemblyList);
        foreach (var fullName in typeMapping.FullNames)
        {
            var typeId = typeMapping.GetTypeId(fullName);
            if (typeId != null)
            {
                // Try to find the actual CLR type
                foreach (var assembly in assemblyList)
                {
                    var type = assembly.GetType(fullName);
                    if (type != null)
                    {
                        RegisterInterfaceType(type, typeId);
                        break;
                    }
                }
            }
        }

        foreach (var assembly in assemblyList)
        {
            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    // Track resource types (IResource implementations)
                    if (typeof(IResource).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
                    {
                        var typeId = AtsIntrinsics.GetResourceTypeId(type);
                        _atsTypeToResourceType[typeId] = type;
                        _resourceTypeToAtsType[type] = typeId;
                    }
                }
            }
            catch (ReflectionTypeLoadException)
            {
                // Skip assemblies that can't be loaded
            }
        }
    }

    /// <summary>
    /// Registers an interface type for AppliesTo constraint validation.
    /// </summary>
    private void RegisterInterfaceType(Type type, string typeId)
    {
        _atsTypeToInterface[typeId] = type;
    }

    /// <summary>
    /// Checks if a type is assignable to another type in the ATS hierarchy.
    /// Uses CLR inheritance for resource types and interface types.
    /// </summary>
    /// <param name="typeId">The type to check.</param>
    /// <param name="targetTypeId">The target type.</param>
    /// <returns>True if typeId is assignable to targetTypeId.</returns>
    public bool IsAssignableTo(string typeId, string targetTypeId)
    {
        // Same type is always assignable
        if (typeId == targetTypeId)
        {
            return true;
        }

        // Check if source is a resource type
        if (_atsTypeToResourceType.TryGetValue(typeId, out var resourceType))
        {
            // Check if target is also a resource type - use CLR inheritance
            if (_atsTypeToResourceType.TryGetValue(targetTypeId, out var targetResourceType))
            {
                return targetResourceType.IsAssignableFrom(resourceType);
            }

            // Check if target is an interface type (for AppliesTo constraints)
            if (_atsTypeToInterface.TryGetValue(targetTypeId, out var targetInterface))
            {
                return targetInterface.IsAssignableFrom(resourceType);
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the CLR resource type for an ATS type ID.
    /// </summary>
    public Type? GetResourceType(string typeId)
    {
        return _atsTypeToResourceType.TryGetValue(typeId, out var type) ? type : null;
    }

    /// <summary>
    /// Gets the ATS type ID for a CLR resource type.
    /// </summary>
    public string? GetAtsTypeId(Type resourceType)
    {
        return _resourceTypeToAtsType.TryGetValue(resourceType, out var typeId) ? typeId : null;
    }

    /// <summary>
    /// Gets all registered type IDs.
    /// </summary>
    public IEnumerable<string> GetAllTypeIds()
    {
        return _atsTypeToResourceType.Keys;
    }
}

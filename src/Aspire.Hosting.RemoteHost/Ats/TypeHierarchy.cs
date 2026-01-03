// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Reflection;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.RemoteHost.Ats;

/// <summary>
/// Tracks the ATS type hierarchy for AppliesTo constraint validation.
/// For IResourceBuilder&lt;T&gt;, uses CLR type inheritance directly.
/// For [AspireHandle] types, uses explicit Extends property.
/// </summary>
internal sealed class TypeHierarchy
{
    private readonly ConcurrentDictionary<string, HashSet<string>> _handleAncestors = new();
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
    /// Scans the provided assemblies for [AspireHandle] attributes and resource types.
    /// </summary>
    private void ScanAssemblies(IEnumerable<Assembly> assemblies)
    {
        var handleTypes = new List<(Type Type, string TypeId, string? Extends)>();

        // Register well-known interface types for AppliesTo constraints
        RegisterInterfaceType(typeof(IResource), "aspire/IResource");
        RegisterInterfaceType(typeof(IResourceWithEnvironment), "aspire/IResourceWithEnvironment");
        RegisterInterfaceType(typeof(IResourceWithEndpoints), "aspire/IResourceWithEndpoints");
        RegisterInterfaceType(typeof(IResourceWithArgs), "aspire/IResourceWithArgs");
        RegisterInterfaceType(typeof(IResourceWithConnectionString), "aspire/IResourceWithConnectionString");
        RegisterInterfaceType(typeof(IResourceWithWaitSupport), "aspire/IResourceWithWaitSupport");
        RegisterInterfaceType(typeof(ContainerResource), "aspire/Container");

        foreach (var assembly in assemblies)
        {
            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    // Check for [AspireHandle]
                    var handleAttr = type.GetCustomAttribute<AspireHandleAttribute>();
                    if (handleAttr != null)
                    {
                        handleTypes.Add((type, handleAttr.HandleTypeId, handleAttr.Extends));
                        continue;
                    }

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

        // Register handle types with explicit hierarchy
        var registered = new HashSet<string>();
        var pending = new Queue<(Type Type, string TypeId, string? Extends)>(handleTypes);
        var maxIterations = handleTypes.Count * 2;
        var iterations = 0;

        while (pending.Count > 0 && iterations++ < maxIterations)
        {
            var (type, typeId, extends) = pending.Dequeue();

            if (string.IsNullOrEmpty(extends) || registered.Contains(extends))
            {
                RegisterHandle(typeId, extends);
                registered.Add(typeId);
            }
            else
            {
                pending.Enqueue((type, typeId, extends));
            }
        }
    }

    /// <summary>
    /// Registers a handle type with its explicit inheritance chain.
    /// </summary>
    private void RegisterHandle(string typeId, string? extends)
    {
        var ancestors = new HashSet<string> { typeId };

        if (!string.IsNullOrEmpty(extends))
        {
            ancestors.Add(extends);

            if (_handleAncestors.TryGetValue(extends, out var baseAncestors))
            {
                foreach (var ancestor in baseAncestors)
                {
                    ancestors.Add(ancestor);
                }
            }
        }

        _handleAncestors[typeId] = ancestors;
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
    /// For resource types, uses CLR inheritance. For handles, uses explicit Extends.
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

        // Check explicit handle hierarchy
        if (_handleAncestors.TryGetValue(typeId, out var ancestors))
        {
            return ancestors.Contains(targetTypeId);
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
        return _handleAncestors.Keys.Concat(_atsTypeToResourceType.Keys).Distinct();
    }
}

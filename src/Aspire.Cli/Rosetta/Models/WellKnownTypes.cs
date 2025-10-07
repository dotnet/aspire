// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Aspire.Cli.Rosetta.Models.Types;

namespace Aspire.Cli.Rosetta.Models;

/// <summary>
/// This class provides access to ReflectionOnly well-known types from the Aspire.Hosting assembly and other loaded assemblies.
/// </summary>

[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Types are coming from System.Reflection.Metadata which are trim/aot compatible")]
[UnconditionalSuppressMessage("Trimming", "IL2055", Justification = "Types are coming from System.Reflection.Metadata which are trim/aot compatible")]
[UnconditionalSuppressMessage("Trimming", "IL2060", Justification = "Types are coming from System.Reflection.Metadata which are trim/aot compatible")]
[UnconditionalSuppressMessage("Trimming", "IL2065", Justification = "Types are coming from System.Reflection.Metadata which are trim/aot compatible")]
[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Types are coming from System.Reflection.Metadata which are trim/aot compatible")]
[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Types are coming from System.Reflection.Metadata which are trim/aot compatible")]
[UnconditionalSuppressMessage("Trimming", "IL2104", Justification = "Types are coming from System.Reflection.Metadata which are trim/aot compatible")]
[UnconditionalSuppressMessage("Trimming", "IL3001", Justification = "Types are coming from System.Reflection.Metadata which are trim/aot compatible")]
[UnconditionalSuppressMessage("Trimming", "IL3050", Justification = "Types are coming from System.Reflection.Metadata which are trim/aot compatible")]
[UnconditionalSuppressMessage("Trimming", "IL3053", Justification = "Types are coming from System.Reflection.Metadata which are trim/aot compatible")]
internal class WellKnownTypes : IWellKnownTypes
{
    private readonly Dictionary<Type, RoType> _knownTypes = [];
    private readonly RoAssembly _aspireHostingAssembly;
    private readonly AssemblyLoaderContext _assemblyLoaderContext;

    public WellKnownTypes(AssemblyLoaderContext assemblyLoaderContext)
    {
        _assemblyLoaderContext = assemblyLoaderContext;
        _aspireHostingAssembly = assemblyLoaderContext.LoadedAssemblies["Aspire.Hosting"];
    }

    public RoType ResourceType =>
        _aspireHostingAssembly.GetType("Aspire.Hosting.ApplicationModel.Resource") ??
        throw new InvalidOperationException("Resource type not found.");

    public RoType IResourceType =>
        _aspireHostingAssembly.GetType("Aspire.Hosting.ApplicationModel.IResource") ??
        throw new InvalidOperationException("IResource type not found.");

    public RoType IResourceWithConnectionStringType =>
        _aspireHostingAssembly.GetType("Aspire.Hosting.ApplicationModel.IResourceWithConnectionString") ??
        throw new InvalidOperationException("IResourceWithConnectionString type not found.");

    public RoType IResourceBuilderType =>
        _aspireHostingAssembly.GetType("Aspire.Hosting.ApplicationModel.IResourceBuilder`1") ??
        throw new InvalidOperationException("IResourceBuilder type not found.");

    public RoType IDistributedApplicationBuilderType =>
        _aspireHostingAssembly.GetType("Aspire.Hosting.IDistributedApplicationBuilder") ??
        throw new InvalidOperationException("IDistributedApplicationBuilder type not found.");

    public bool TryGetResourceBuilderTypeArgument(RoType resourceBuilderType, [NotNullWhen(true)] out RoType? resourceType)
    {
        if (!resourceBuilderType.IsGenericType)
        {
            resourceType = null;
            return false;
        }

        if (resourceBuilderType.GenericTypeDefinition != IResourceBuilderType)
        {
            resourceType = null;
            return false;
        }

        if (resourceBuilderType.GenericTypeArguments.Count != 1)
        {
            resourceType = null;
            return false;
        }

        resourceType = resourceBuilderType.GenericTypeArguments[0];
        return true;
    }

    /// <summary>
    /// Returns the ReflectionOnly equivalent type for a given type, if it exists in the loaded assemblies.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="knownType"></param>
    /// <returns></returns>
    public bool TryGetKnownType(Type type, [NotNullWhen(true)] out RoType? knownType)
    {
        Debug.Assert(type.FullName != null);

        var typeName = type.Name;
        _ = typeName ?? throw new InvalidOperationException("Type must have a full name.");

        if (_knownTypes.TryGetValue(type, out knownType))
        {
            return knownType != null;
        }

        if (type.IsGenericType && !type.IsGenericTypeDefinition)
        {
            if (!TryGetKnownType(type.GetGenericTypeDefinition(), out var genericTypeDefinition))
            {
                return false;
            }

            var genericTypeArguments = new List<RoType>();

            foreach (var genericArgument in type.GenericTypeArguments)
            {
                if (!TryGetKnownType(genericArgument, out var knownGenericArgument))
                {
                    return false;
                }

                genericTypeArguments.Add(knownGenericArgument);
            }

            knownType = genericTypeDefinition.MakeGenericType(genericTypeArguments.ToArray());

            _knownTypes[type] = knownType;
            return true;
        }

        var typeInAssembly = _assemblyLoaderContext.GetType(type.FullName);

        if (typeInAssembly != null)
        {
            knownType = typeInAssembly;
            _knownTypes[type] = knownType;
            return true;
        }

        return knownType != null;
    }
}

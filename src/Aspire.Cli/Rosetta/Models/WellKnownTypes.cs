// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Rosetta.Models;

public class WellKnownTypes(IEnumerable<Assembly> assemblies) : IWellKnownTypes
{
    private readonly Dictionary<Type, Type> _knownTypes = [];
    private readonly IEnumerable<Assembly> _assemblies = assemblies;
    private readonly Assembly _aspireHostingAssembly = assemblies.FirstOrDefault(x => x.GetName().Name == "Aspire.Hosting") ??
            throw new InvalidOperationException("Aspire.Hosting assembly not found.");

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public Type ResourceType =>
        _aspireHostingAssembly.GetType("Aspire.Hosting.ApplicationModel.Resource") ??
        throw new InvalidOperationException("Resource type not found.");

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public Type IResourceType =>
        _aspireHostingAssembly.GetType("Aspire.Hosting.ApplicationModel.IResource") ??
        throw new InvalidOperationException("IResource type not found.");

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public Type IResourceWithConnectionStringType =>
        _aspireHostingAssembly.GetType("Aspire.Hosting.ApplicationModel.IResourceWithConnectionString") ??
        throw new InvalidOperationException("IResourceWithConnectionString type not found.");

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public Type IResourceBuilderType =>
        _aspireHostingAssembly.GetType("Aspire.Hosting.ApplicationModel.IResourceBuilder`1") ??
        throw new InvalidOperationException("IResourceBuilder type not found.");

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public Type IDistributedApplicationBuilderType =>
        _aspireHostingAssembly.GetType("Aspire.Hosting.IDistributedApplicationBuilder") ??
        throw new InvalidOperationException("IDistributedApplicationBuilder type not found.");

    public bool TryGetResourceBuilderTypeArgument(Type resourceBuilderType, [NotNullWhen(true)] out Type? resourceType)
    {
        if (!resourceBuilderType.IsGenericType)
        {
            resourceType = null;
            return false;
        }

        if (resourceBuilderType.GetGenericTypeDefinition() != IResourceBuilderType)
        {
            resourceType = null;
            return false;
        }

        if (resourceBuilderType.GenericTypeArguments.Length != 1)
        {
            resourceType = null;
            return false;
        }

        resourceType = resourceBuilderType.GenericTypeArguments[0];
        return true;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
    public bool TryGetKnownType(Type type, [NotNullWhen(true)] out Type? knownType)
    {
        Debug.Assert(type.FullName != null);

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

            var genericTypeArguments = new List<Type>();

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

        foreach (var assembly in _assemblies)
        {
            var typeInAssembly = assembly.GetType(type.FullName);
            if (typeInAssembly != null)
            {
                knownType = typeInAssembly;
                _knownTypes[type] = knownType;
                return true;
            }
        }

        return knownType != null;
    }
}

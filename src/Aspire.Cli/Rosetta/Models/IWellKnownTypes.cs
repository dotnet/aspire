// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Cli.Rosetta.Models;

public interface IWellKnownTypes
{
    bool TryGetKnownType(Type type, [NotNullWhen(true)] out Type? knownType);
    bool TryGetResourceBuilderTypeArgument(Type resourceBuilderType, [NotNullWhen(true)] out Type? resourceType);
    public Type ResourceType { get; }
    public Type IResourceType { get; }
    public Type IResourceWithConnectionStringType { get; }
    public Type IResourceBuilderType { get; }
    public Type IDistributedApplicationBuilderType { get; }
}

public static class WellKnownTypesExtensions
{
    public static bool IsNullableOfT(this IWellKnownTypes wellKnownTypes, Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == wellKnownTypes.GetKnownType(typeof(Nullable<>)))
        {
            return true;
        }

        return false;
    }

    public static Type GetKnownType<T>(this IWellKnownTypes wellKnownTypes)
    {
        return GetKnownType(wellKnownTypes, typeof(T));
    }

    public static Type GetKnownType(this IWellKnownTypes wellKnownTypes, Type type)
    {
        if (wellKnownTypes.TryGetKnownType(type, out var knownType))
        {
            return knownType;
        }

        throw new InvalidOperationException($"Unable to get known type {type.FullName}.");
    }
}

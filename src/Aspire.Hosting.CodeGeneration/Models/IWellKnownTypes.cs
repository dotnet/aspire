// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.CodeGeneration.Models.Types;

namespace Aspire.Hosting.CodeGeneration.Models;

public interface IWellKnownTypes
{
    bool TryGetKnownType(Type type, [NotNullWhen(true)] out RoType? knownType);
    bool TryGetResourceBuilderTypeArgument(RoType resourceBuilderType, [NotNullWhen(true)] out RoType? resourceType);
    public RoType ResourceType { get; }
    public RoType IResourceType { get; }
    public RoType IResourceWithConnectionStringType { get; }
    public RoType IResourceBuilderType { get; }
    public RoType IDistributedApplicationBuilderType { get; }
}

public static class WellKnownTypesExtensions
{
    public static bool IsNullableOfT(this IWellKnownTypes wellKnownTypes, RoType type)
    {
        if (type.IsGenericType && type.GenericTypeDefinition == wellKnownTypes.GetKnownType(typeof(Nullable<>)))
        {
            return true;
        }

        return false;
    }

    public static RoType GetKnownType<T>(this IWellKnownTypes wellKnownTypes)
    {
        return GetKnownType(wellKnownTypes, typeof(T));
    }

    public static RoType GetKnownType(this IWellKnownTypes wellKnownTypes, Type type)
    {
        if (wellKnownTypes.TryGetKnownType(type, out var knownType))
        {
            return knownType;
        }

        throw new InvalidOperationException($"Unable to get known type {type.FullName}.");
    }
}

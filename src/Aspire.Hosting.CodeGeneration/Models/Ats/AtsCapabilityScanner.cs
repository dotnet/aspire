// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Ats;
using Aspire.Hosting.CodeGeneration.Ats;
using Aspire.Hosting.CodeGeneration.Models.Types;
using SharedScanner = Aspire.Hosting.Ats.AtsCapabilityScanner;
using SharedCapabilityInfo = Aspire.Hosting.Ats.AtsCapabilityInfo;
using SharedParameterInfo = Aspire.Hosting.Ats.AtsParameterInfo;
using SharedTypeInfo = Aspire.Hosting.Ats.AtsTypeInfo;
using SharedTypeRef = Aspire.Hosting.Ats.AtsTypeRef;

namespace Aspire.Hosting.CodeGeneration.Models.Ats;

/// <summary>
/// Scans assemblies for [AspireExport] attributes and creates capability models.
/// Uses metadata reflection (RoMethod/RoType) for code generation.
/// </summary>
/// <remarks>
/// This class is a thin adapter over the shared <see cref="Aspire.Hosting.Ats.AtsCapabilityScanner"/>.
/// It wraps RoType/RoAssembly using the IAts* interfaces and converts results to public model types.
/// </remarks>
public static class AtsCapabilityScanner
{
    /// <summary>
    /// Result of scanning an assembly, containing both capabilities and type information.
    /// </summary>
    public sealed class ScanResult
    {
        public required List<AtsCapabilityInfo> Capabilities { get; init; }
        public required List<AtsTypeInfo> TypeInfos { get; init; }
    }

    /// <summary>
    /// Scans an assembly for [AspireExport] and [AspireContextType] attributes and returns capability models
    /// along with type information including interface implementations.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <param name="wellKnownTypes">Well-known type definitions.</param>
    /// <param name="typeMapping">The ATS type mapping for resolving type IDs.</param>
    public static ScanResult ScanAssemblyWithTypeInfo(
        RoAssembly assembly,
        IWellKnownTypes wellKnownTypes,
        AtsTypeMapping typeMapping)
    {
        // Wrap the assembly and call the shared scanner
        var wrappedAssembly = new RoAssemblyInfoWrapper(assembly);
        var typeResolver = new WellKnownTypeResolver(wellKnownTypes, typeMapping);

        var sharedResult = SharedScanner.ScanAssembly(wrappedAssembly, typeMapping, typeResolver);

        // Convert results to public types
        return new ScanResult
        {
            Capabilities = sharedResult.Capabilities.Select(ConvertCapability).ToList(),
            TypeInfos = sharedResult.TypeInfos.Select(ConvertTypeInfo).ToList()
        };
    }

    /// <summary>
    /// Scans an assembly for [AspireExport] and [AspireContextType] attributes and returns capability models.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <param name="wellKnownTypes">Well-known type definitions.</param>
    /// <param name="typeMapping">The ATS type mapping for resolving type IDs.</param>
    public static List<AtsCapabilityInfo> ScanAssembly(
        RoAssembly assembly,
        IWellKnownTypes wellKnownTypes,
        AtsTypeMapping typeMapping)
    {
        return ScanAssemblyWithTypeInfo(assembly, wellKnownTypes, typeMapping).Capabilities;
    }

    /// <summary>
    /// Derives the method name from a capability ID.
    /// </summary>
    /// <example>
    /// "Aspire.Hosting.Redis/addRedis" -> "addRedis"
    /// "Aspire.Hosting/withEnvironment" -> "withEnvironment"
    /// </example>
    public static string DeriveMethodName(string capabilityId)
    {
        return SharedScanner.DeriveMethodName(capabilityId);
    }

    /// <summary>
    /// Derives the package name from a capability ID.
    /// </summary>
    /// <example>
    /// "Aspire.Hosting.Redis/addRedis" -> "Aspire.Hosting.Redis"
    /// "Aspire.Hosting/withEnvironment" -> "Aspire.Hosting"
    /// </example>
    public static string DerivePackage(string capabilityId)
    {
        return SharedScanner.DerivePackage(capabilityId);
    }

    /// <summary>
    /// Maps a CLR type to an ATS type ID for code generation.
    /// Uses the explicit type mapping first, then falls back to inference for primitives and generics.
    /// </summary>
    public static string? MapToAtsTypeId(RoType type, IWellKnownTypes wellKnownTypes, AtsTypeMapping typeMapping)
    {
        var typeResolver = new WellKnownTypeResolver(wellKnownTypes, typeMapping);
        var wrappedType = new RoTypeInfoWrapper(type);
        return SharedScanner.MapToAtsTypeId(wrappedType, typeMapping, typeResolver);
    }

    private static AtsCapabilityInfo ConvertCapability(SharedCapabilityInfo shared)
    {
        return new AtsCapabilityInfo
        {
            CapabilityId = shared.CapabilityId,
            MethodName = shared.MethodName,
            Package = shared.Package,
            Description = shared.Description,
            Parameters = shared.Parameters.Select(ConvertParameter).ToList(),
#pragma warning disable CS0618 // Keep populating obsolete property for backwards compatibility
            ReturnTypeId = shared.ReturnTypeId,
#pragma warning restore CS0618
            ReturnType = ConvertTypeRef(shared.ReturnType),
            IsExtensionMethod = shared.IsExtensionMethod,
            TargetTypeId = shared.OriginalTargetTypeId,
            TargetType = ConvertTypeRef(shared.TargetType),
            ExpandedTargetTypeIds = shared.ExpandedTargetTypeIds.ToList(),
            ReturnsBuilder = shared.ReturnsBuilder,
            CapabilityKind = shared.CapabilityKind,
            OwningTypeName = shared.OwningTypeName
        };
    }

    private static AtsParameterInfo ConvertParameter(SharedParameterInfo shared)
    {
        return new AtsParameterInfo
        {
            Name = shared.Name,
#pragma warning disable CS0618 // Keep populating obsolete properties for backwards compatibility
            AtsTypeId = shared.AtsTypeId,
            TypeCategory = shared.TypeCategory,
#pragma warning restore CS0618
            Type = ConvertTypeRef(shared.Type),
            IsOptional = shared.IsOptional,
            IsNullable = shared.IsNullable,
            IsCallback = shared.IsCallback,
            CallbackParameters = shared.CallbackParameters?.Select(p => new AtsCallbackParameterInfo
            {
                Name = p.Name,
                Type = ConvertTypeRef(p.Type)!
            }).ToList(),
            CallbackReturnType = ConvertTypeRef(shared.CallbackReturnType),
            DefaultValue = shared.DefaultValue
        };
    }

    private static AtsTypeRef? ConvertTypeRef(SharedTypeRef? shared)
    {
        if (shared == null)
        {
            return null;
        }

        return new AtsTypeRef
        {
            TypeId = shared.TypeId,
            Category = shared.Category,
            IsInterface = shared.IsInterface,
            ElementType = ConvertTypeRef(shared.ElementType),
            KeyType = ConvertTypeRef(shared.KeyType),
            ValueType = ConvertTypeRef(shared.ValueType),
            IsReadOnly = shared.IsReadOnly
        };
    }

    private static AtsTypeInfo ConvertTypeInfo(SharedTypeInfo shared)
    {
        return new AtsTypeInfo
        {
            AtsTypeId = shared.AtsTypeId,
            ClrTypeName = shared.ClrTypeName ?? string.Empty,
            IsInterface = shared.IsInterface,
            ImplementedInterfaceTypeIds = shared.ImplementedInterfaceTypeIds.ToList()
        };
    }

    /// <summary>
    /// Adapts IWellKnownTypes to IAtsTypeResolver.
    /// </summary>
    private sealed class WellKnownTypeResolver : IAtsTypeResolver
    {
        private readonly IWellKnownTypes _wellKnownTypes;

        public WellKnownTypeResolver(IWellKnownTypes wellKnownTypes, AtsTypeMapping _)
        {
            _wellKnownTypes = wellKnownTypes;
        }

        public bool IsResourceType(IAtsTypeInfo type)
        {
            // Check if the type is assignable to IResource
            if (type is RoTypeInfoWrapper wrapper)
            {
                return _wellKnownTypes.IResourceType.IsAssignableFrom(wrapper.UnderlyingType);
            }
            // Fallback: check by type name pattern
            return type.FullName.Contains("Resource") &&
                   !type.FullName.Contains("IResourceBuilder");
        }

        public bool IsResourceBuilderType(IAtsTypeInfo type)
        {
            // Check if the type is IResourceBuilder<T>
            if (type is RoTypeInfoWrapper wrapper)
            {
                return wrapper.UnderlyingType.IsGenericType &&
                       wrapper.UnderlyingType.GenericTypeDefinition == _wellKnownTypes.IResourceBuilderType;
            }
            return type.GenericTypeDefinitionFullName == "Aspire.Hosting.ApplicationModel.IResourceBuilder`1";
        }

        public bool TryGetResourceBuilderTypeArgument(IAtsTypeInfo type, out IAtsTypeInfo? resourceType)
        {
            if (type is RoTypeInfoWrapper wrapper)
            {
                if (_wellKnownTypes.TryGetResourceBuilderTypeArgument(wrapper.UnderlyingType, out var roResourceType))
                {
                    resourceType = new RoTypeInfoWrapper(roResourceType);
                    return true;
                }
            }
            resourceType = null;
            return false;
        }
    }
}

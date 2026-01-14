// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Ats;

/// <summary>
/// Scans assemblies for [AspireExport] and [AspireContextType] attributes and creates capability models.
/// Uses System.Reflection types directly for runtime scanning.
/// </summary>
internal static class AtsCapabilityScanner
{
    /// <summary>
    /// Result of scanning an assembly.
    /// </summary>
    internal sealed class ScanResult
    {
        /// <summary>Capabilities (methods/properties with [AspireExport]) that can be invoked via RPC.</summary>
        public required List<AtsCapabilityInfo> Capabilities { get; init; }

        /// <summary>Handle types ([AspireExport] types) passed by reference using opaque handles.</summary>
        public required List<AtsTypeInfo> HandleTypes { get; init; }

        /// <summary>DTO types ([AspireDto] types) serialized as JSON objects.</summary>
        public List<AtsDtoTypeInfo> DtoTypes { get; init; } = [];

        /// <summary>Enum types found in capability signatures, serialized as strings.</summary>
        public List<AtsEnumTypeInfo> EnumTypes { get; init; } = [];

        /// <summary>Diagnostics (warnings/errors) generated during scanning.</summary>
        public List<AtsDiagnostic> Diagnostics { get; init; } = [];

        /// <summary>
        /// Runtime registry mapping capability IDs to methods.
        /// Used by CapabilityDispatcher for invocation.
        /// </summary>
        public Dictionary<string, MethodInfo> Methods { get; init; } = new();

        /// <summary>
        /// Runtime registry mapping capability IDs to properties.
        /// Used by CapabilityDispatcher for property getter/setter invocation.
        /// </summary>
        public Dictionary<string, PropertyInfo> Properties { get; init; } = new();

        /// <summary>
        /// Converts the scan result to an AtsContext for code generation.
        /// </summary>
        public AtsContext ToAtsContext()
        {
            var context = new AtsContext
            {
                Capabilities = Capabilities,
                HandleTypes = HandleTypes,
                DtoTypes = DtoTypes,
                EnumTypes = EnumTypes,
                Diagnostics = Diagnostics
            };

            // Copy runtime registries
            foreach (var (id, method) in Methods)
            {
                context.Methods[id] = method;
            }
            foreach (var (id, property) in Properties)
            {
                context.Properties[id] = property;
            }

            return context;
        }
    }

    /// <summary>
    /// Internal context for collecting enum types during scanning.
    /// </summary>
    private sealed class EnumCollector
    {
        private readonly Dictionary<string, AtsEnumTypeInfo> _enums = new(StringComparer.Ordinal);

        public void Add(Type enumType)
        {
            var fullName = enumType.FullName ?? enumType.Name;
            var typeId = AtsConstants.EnumTypeId(fullName);
            if (!_enums.ContainsKey(typeId))
            {
                var values = Enum.GetNames(enumType).ToList();
                _enums[typeId] = new AtsEnumTypeInfo
                {
                    TypeId = typeId,
                    Name = enumType.Name,
                    ClrType = enumType,
                    Values = values
                };
            }
        }

        public List<AtsEnumTypeInfo> GetEnumTypes() => [.. _enums.Values];
    }

    /// <summary>
    /// Scans multiple assemblies for capabilities and type info.
    /// Uses 2-pass scanning:
    /// 1. Collect all capabilities and types from all assemblies (no expansion)
    /// 2. Expand using the complete type info set from all assemblies
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    public static ScanResult ScanAssemblies(
        IEnumerable<Assembly> assemblies)
    {
        var allCapabilities = new List<AtsCapabilityInfo>();
        var allTypeInfos = new List<AtsTypeInfo>();
        var allDtoTypes = new List<AtsDtoTypeInfo>();
        var allEnumTypes = new List<AtsEnumTypeInfo>();
        var allDiagnostics = new List<AtsDiagnostic>();
        var allMethods = new Dictionary<string, MethodInfo>();
        var allProperties = new Dictionary<string, PropertyInfo>();
        var seenCapabilityIds = new HashSet<string>();
        var seenTypeIds = new HashSet<string>();
        var seenDtoTypeIds = new HashSet<string>();
        var seenEnumTypeIds = new HashSet<string>();

        // Pass 1: Collect capabilities and types from all assemblies (no expansion)
        foreach (var assembly in assemblies)
        {
            var result = ScanAssemblyWithoutExpansion(assembly);

            // Merge capabilities, avoiding duplicates
            foreach (var capability in result.Capabilities)
            {
                if (seenCapabilityIds.Add(capability.CapabilityId))
                {
                    allCapabilities.Add(capability);
                }
            }

            // Merge type infos, avoiding duplicates
            foreach (var typeInfo in result.HandleTypes)
            {
                if (seenTypeIds.Add(typeInfo.AtsTypeId))
                {
                    allTypeInfos.Add(typeInfo);
                }
            }

            // Merge DTO types, avoiding duplicates
            foreach (var dtoType in result.DtoTypes)
            {
                if (seenDtoTypeIds.Add(dtoType.TypeId))
                {
                    allDtoTypes.Add(dtoType);
                }
            }

            // Merge enum types, avoiding duplicates
            foreach (var enumType in result.EnumTypes)
            {
                if (seenEnumTypeIds.Add(enumType.TypeId))
                {
                    allEnumTypes.Add(enumType);
                }
            }

            // Merge runtime registries (methods and properties)
            foreach (var (id, method) in result.Methods)
            {
                allMethods.TryAdd(id, method);
            }
            foreach (var (id, property) in result.Properties)
            {
                allProperties.TryAdd(id, property);
            }

            // Merge diagnostics
            allDiagnostics.AddRange(result.Diagnostics);
        }

        // Pass 2: Build universe of valid types and resolve Unknown types
        // Valid types are ALL types with [AspireExport] - the ExposeProperties/ExposeMethods
        // flags control whether a wrapper class is generated, not whether the type is valid
        var validTypes = new HashSet<string>(allTypeInfos.Select(t => t.AtsTypeId));
        ResolveUnknownTypes(allCapabilities, validTypes);

        // Pass 3: Filter capabilities with unresolved Unknown types
        FilterInvalidCapabilities(allCapabilities, allDiagnostics);

        // Pass 4: Expand all capabilities using complete type info set
        ExpandCapabilityTargets(allCapabilities, allTypeInfos);

        // Pass 5: Filter method name collisions (overloaded methods) after expansion
        FilterMethodNameCollisions(allCapabilities, allDiagnostics);

        return new ScanResult
        {
            Capabilities = allCapabilities,
            HandleTypes = allTypeInfos,
            DtoTypes = allDtoTypes,
            EnumTypes = allEnumTypes,
            Diagnostics = allDiagnostics,
            Methods = allMethods,
            Properties = allProperties
        };
    }

    /// <summary>
    /// Scans an assembly for capabilities and type info.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    public static ScanResult ScanAssembly(
        Assembly assembly)
    {
        // Single assembly scan with expansion
        var result = ScanAssemblyWithoutExpansion(assembly);

        // Build universe and resolve Unknown types
        var validTypes = new HashSet<string>(result.HandleTypes.Select(t => t.AtsTypeId));
        ResolveUnknownTypes(result.Capabilities, validTypes);

        // Filter capabilities with unresolved Unknown types
        FilterInvalidCapabilities(result.Capabilities, result.Diagnostics);

        // Expand interface targets to concrete types
        ExpandCapabilityTargets(result.Capabilities, result.HandleTypes);

        // Filter method name collisions (overloaded methods) after expansion
        FilterMethodNameCollisions(result.Capabilities, result.Diagnostics);

        return result;
    }

    /// <summary>
    /// Internal method that scans an assembly without doing expansion.
    /// Used by both ScanAssembly and ScanAssemblies.
    /// </summary>
    private static ScanResult ScanAssemblyWithoutExpansion(
        Assembly assembly)
    {
        var assemblyName = assembly.GetName().Name ?? "";
        var capabilities = new List<AtsCapabilityInfo>();
        var typeInfos = new List<AtsTypeInfo>();
        var dtoTypes = new List<AtsDtoTypeInfo>();
        var diagnostics = new List<AtsDiagnostic>();

        // Runtime registries for CapabilityDispatcher
        var methods = new Dictionary<string, MethodInfo>();
        var properties = new Dictionary<string, PropertyInfo>();

        // Also collect resource types discovered from capability parameters
        // These are concrete types like TestRedisResource that appear in IResourceBuilder<T>
        var discoveredResourceTypes = new Dictionary<string, Type>();

        // Get all types from assembly, handling load failures gracefully
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t != null).ToArray()!;
        }

        foreach (var type in types)
        {
            // Check for [AspireDto] attribute - scan DTO types for code generation
            if (HasAspireDtoAttribute(type))
            {
                var dtoInfo = CreateDtoTypeInfo(type);
                if (dtoInfo != null)
                {
                    dtoTypes.Add(dtoInfo);
                }
            }

            // Check for [AspireExport(AtsTypeId = "...")] on types
            var typeExportAttr = GetAspireExportAttribute(type);
            if (typeExportAttr != null)
            {
                var typeInfo = CreateTypeInfo(type, typeExportAttr);
                if (typeInfo != null)
                {
                    typeInfos.Add(typeInfo);
                }
            }

            // Check for [AspireExport] at the class level (with ExposeProperties/ExposeMethods)
            // or types that have instance methods with member-level [AspireExport]
            // This allows scanning for:
            // 1. Types with ExposeProperties=true to auto-expose all properties
            // 2. Types with ExposeMethods=true to auto-expose all methods
            // 3. Types with [AspireExport] that have member-level [AspireExport] on specific instance methods
            if (HasExposePropertiesAttribute(type) || HasExposeMethodsAttribute(type) || GetAspireExportAttribute(type) != null)
            {
                // Member-level errors are captured inside CreateContextTypeCapabilities
                // and returned as diagnostics, allowing other members to be processed
                var contextResult = CreateContextTypeCapabilities(type, assemblyName);
                capabilities.AddRange(contextResult.Capabilities);
                diagnostics.AddRange(contextResult.Diagnostics);

                // Merge runtime registries from context type capabilities
                foreach (var (id, method) in contextResult.Methods)
                {
                    methods[id] = method;
                }
                foreach (var (id, property) in contextResult.Properties)
                {
                    properties[id] = property;
                }
            }

            // Scan all types for static methods with [AspireExport]
            // Note: Instance methods are scanned via CreateContextTypeCapabilities when type has [AspireExport]
            // Use BindingFlags to include internal methods (not just public) since [AspireExport] can be on internal methods
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (!method.IsStatic)
                {
                    continue;
                }

                var exportAttr = GetAspireExportAttribute(method);

                // Static methods require explicit [AspireExport] (no auto-expose)
                // Explicit [AspireExport] allows both public and internal methods
                if (!ShouldExportMember(method.IsPublic, exposeAll: false, exportAttr))
                {
                    continue;
                }

                // For static methods, exportAttr is guaranteed non-null here since we passed exposeAll: false
                // and ShouldExportMember only returns true if exportAttr != null in that case
                if (exportAttr is null)
                {
                    continue;
                }

                try
                {
                    var capability = CreateCapabilityInfo(method, exportAttr, assemblyName, out var capabilityDiagnostic);
                    if (capability != null)
                    {
                        capabilities.Add(capability);

                        // Register the method for runtime dispatch
                        methods[capability.CapabilityId] = method;

                        // Collect resource types from capability parameters and return types
                        CollectResourceTypesFromCapability(method, discoveredResourceTypes);
                    }
                    else if (capabilityDiagnostic != null)
                    {
                        // Capability was skipped with a diagnostic message
                        diagnostics.Add(capabilityDiagnostic);
                    }
                }
                catch (InvalidOperationException ex)
                {
                    // Type validation error - log as diagnostic and continue
                    diagnostics.Add(AtsDiagnostic.Error(ex.Message, $"{type.FullName}.{method.Name}"));
                }
            }
        }

        // Add discovered resource types to typeInfos for expansion
        foreach (var (typeId, resourceType) in discoveredResourceTypes)
        {
            // Skip if already in typeInfos (from [AspireExport] attribute)
            if (typeInfos.Any(t => t.AtsTypeId == typeId))
            {
                continue;
            }

            // Create synthetic type info for this resource type
            // Only collect interfaces and base types for concrete types (not interfaces)
            var isInterface = resourceType.IsInterface;
            var implementedInterfaces = !isInterface
                ? CollectAllInterfaces(resourceType)
                : [];
            var baseTypeHierarchy = !isInterface
                ? CollectBaseTypeHierarchy(resourceType)
                : [];

            typeInfos.Add(new AtsTypeInfo
            {
                AtsTypeId = typeId,
                ClrType = resourceType,
                IsInterface = isInterface,
                ImplementedInterfaces = implementedInterfaces,
                BaseTypeHierarchy = baseTypeHierarchy,
                HasExposeProperties = HasExposePropertiesAttribute(resourceType),
                HasExposeMethods = HasExposeMethodsAttribute(resourceType)
            });
        }

        // Note: Expansion and collision detection are done by the calling method
        // (ScanAssembly or ScanAssemblies) after all assemblies are processed

        // Collect enum types that are used in capabilities
        var enumTypes = CollectEnumTypes(capabilities, assembly);

        return new ScanResult
        {
            Capabilities = capabilities,
            HandleTypes = typeInfos,
            DtoTypes = dtoTypes,
            EnumTypes = enumTypes,
            Diagnostics = diagnostics,
            Methods = methods,
            Properties = properties
        };
    }

    /// <summary>
    /// Collects enum types that are used in capability parameters or return types.
    /// </summary>
    private static List<AtsEnumTypeInfo> CollectEnumTypes(
        List<AtsCapabilityInfo> capabilities,
        Assembly assembly)
    {
        // Collect all enum type IDs referenced in capabilities
        var enumTypeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var capability in capabilities)
        {
            CollectEnumTypeIds(capability.ReturnType, enumTypeIds);
            foreach (var param in capability.Parameters)
            {
                CollectEnumTypeIds(param.Type, enumTypeIds);
            }
        }

        if (enumTypeIds.Count == 0)
        {
            return [];
        }

        // Map enum full names to type IDs for lookup
        var fullNameToTypeId = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var typeId in enumTypeIds)
        {
            // Extract full name from "enum:FullTypeName"
            if (typeId.StartsWith(AtsConstants.EnumPrefix, StringComparison.Ordinal))
            {
                var fullName = typeId[AtsConstants.EnumPrefix.Length..];
                fullNameToTypeId[fullName] = typeId;
            }
        }

        // Find matching enum types in the assembly
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t != null).ToArray()!;
        }

        var result = new List<AtsEnumTypeInfo>();
        foreach (var type in types)
        {
            var fullName = type.FullName ?? type.Name;
            if (type.IsEnum && fullNameToTypeId.TryGetValue(fullName, out var typeId))
            {
                result.Add(new AtsEnumTypeInfo
                {
                    TypeId = typeId,
                    Name = type.Name,
                    ClrType = type,
                    Values = Enum.GetNames(type).ToList()
                });
            }
        }

        return result;
    }

    /// <summary>
    /// Recursively collects enum type IDs from a type reference.
    /// </summary>
    private static void CollectEnumTypeIds(AtsTypeRef? typeRef, HashSet<string> enumTypeIds)
    {
        if (typeRef == null)
        {
            return;
        }

        if (typeRef.Category == AtsTypeCategory.Enum)
        {
            enumTypeIds.Add(typeRef.TypeId);
        }

        // Check nested type refs (arrays, lists, dictionaries)
        CollectEnumTypeIds(typeRef.ElementType, enumTypeIds);
        CollectEnumTypeIds(typeRef.KeyType, enumTypeIds);
        CollectEnumTypeIds(typeRef.ValueType, enumTypeIds);
    }

    /// <summary>
    /// Resolves Unknown type references against the complete universe of valid types.
    /// Types that are found in the universe are upgraded from Unknown to Handle.
    /// </summary>
    private static void ResolveUnknownTypes(
        List<AtsCapabilityInfo> capabilities,
        HashSet<string> validTypes)
    {
        foreach (var capability in capabilities)
        {
            ResolveTypeRef(capability.ReturnType, validTypes);
            foreach (var param in capability.Parameters)
            {
                ResolveTypeRef(param.Type, validTypes);
            }
        }
    }

    /// <summary>
    /// Resolves a type reference against the valid types universe.
    /// If the type was Unknown but is now in the universe, upgrade to Handle.
    /// </summary>
    private static void ResolveTypeRef(AtsTypeRef? typeRef, HashSet<string> validTypes)
    {
        if (typeRef == null)
        {
            return;
        }

        // If Unknown but now in universe, upgrade to Handle
        if (typeRef.Category == AtsTypeCategory.Unknown && validTypes.Contains(typeRef.TypeId))
        {
            typeRef.Category = AtsTypeCategory.Handle;
        }

        // Recursively resolve nested types
        ResolveTypeRef(typeRef.ElementType, validTypes);
        ResolveTypeRef(typeRef.KeyType, validTypes);
        ResolveTypeRef(typeRef.ValueType, validTypes);

        // Resolve union member types
        if (typeRef.UnionTypes != null)
        {
            foreach (var memberType in typeRef.UnionTypes)
            {
                ResolveTypeRef(memberType, validTypes);
            }
        }
    }

    /// <summary>
    /// Filters out capabilities that still have Unknown types after resolution.
    /// These are capabilities that use types not in the ATS universe.
    /// </summary>
    private static void FilterInvalidCapabilities(
        List<AtsCapabilityInfo> capabilities,
        List<AtsDiagnostic> diagnostics)
    {
        capabilities.RemoveAll(capability =>
        {
            var invalidType = FindUnknownType(capability.ReturnType)
                           ?? FindUnknownTypeInParameters(capability.Parameters);

            if (invalidType != null)
            {
                diagnostics.Add(AtsDiagnostic.Warning(
                    $"Capability '{capability.CapabilityId}' uses non-ATS type '{invalidType}' and will be skipped.",
                    capability.CapabilityId));
                return true; // Remove
            }
            return false; // Keep
        });
    }

    /// <summary>
    /// Searches for Unknown types in a list of parameters, including callback parameter types.
    /// </summary>
    private static string? FindUnknownTypeInParameters(IReadOnlyList<AtsParameterInfo> parameters)
    {
        foreach (var param in parameters)
        {
            // Check the parameter's direct type
            var result = FindUnknownType(param.Type);
            if (result != null)
            {
                return result;
            }

            // For callbacks, also check the callback's parameter types and return type
            if (param.IsCallback)
            {
                if (param.CallbackParameters != null)
                {
                    foreach (var cbParam in param.CallbackParameters)
                    {
                        result = FindUnknownType(cbParam.Type);
                        if (result != null)
                        {
                            return result;
                        }
                    }
                }

                result = FindUnknownType(param.CallbackReturnType);
                if (result != null)
                {
                    return result;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Recursively searches for Unknown types in a type reference.
    /// Returns the type ID of the first Unknown type found, or null if none.
    /// </summary>
    private static string? FindUnknownType(AtsTypeRef? typeRef)
    {
        if (typeRef == null)
        {
            return null;
        }

        if (typeRef.Category == AtsTypeCategory.Unknown)
        {
            return typeRef.TypeId;
        }

        // Recursively check nested types
        var result = FindUnknownType(typeRef.ElementType)
            ?? FindUnknownType(typeRef.KeyType)
            ?? FindUnknownType(typeRef.ValueType);

        if (result != null)
        {
            return result;
        }

        // Check union member types
        if (typeRef.UnionTypes != null)
        {
            foreach (var memberType in typeRef.UnionTypes)
            {
                result = FindUnknownType(memberType);
                if (result != null)
                {
                    return result;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Expands capability targets from interface or base types to concrete types.
    /// For capabilities targeting an interface (e.g., "Aspire.Hosting/IResourceWithEnvironment")
    /// or a base type (e.g., "ContainerResource"), this populates ExpandedTargetTypes with
    /// all compatible concrete types (implementing the interface or inheriting from the base).
    /// </summary>
    private static void ExpandCapabilityTargets(
        List<AtsCapabilityInfo> capabilities,
        List<AtsTypeInfo> typeInfos)
    {
        // Build unified map: type -> all compatible concrete types
        // This handles BOTH interface implementations AND class inheritance
        var typeToCompatibleTypes = BuildTypeCompatibilityMap(typeInfos);

        // Expand each capability's target
        foreach (var capability in capabilities)
        {
            var originalTarget = capability.TargetTypeId;
            if (string.IsNullOrEmpty(originalTarget))
            {
                // Entry point methods have no target
                capability.ExpandedTargetTypes = [];
                continue;
            }

            // Look up compatible types (works for interfaces AND concrete base types)
            if (typeToCompatibleTypes.TryGetValue(originalTarget, out var compatibleTypes))
            {
                capability.ExpandedTargetTypes = compatibleTypes.ToList();
            }
            else
            {
                // Leaf concrete type with no derived types: expand to itself
                var targetTypeRef = capability.TargetType ?? new AtsTypeRef
                {
                    TypeId = originalTarget,
                    Category = AtsTypeCategory.Handle,
                    IsInterface = false
                };
                capability.ExpandedTargetTypes = [targetTypeRef];
            }
        }
    }

    /// <summary>
    /// Builds a unified map of type -> compatible concrete types.
    /// For each concrete type, it's registered as compatible with:
    /// 1. All interfaces it implements (for interface expansion)
    /// 2. All base types in its hierarchy (for inheritance expansion)
    /// </summary>
    private static Dictionary<string, List<AtsTypeRef>> BuildTypeCompatibilityMap(
        List<AtsTypeInfo> typeInfos)
    {
        var typeToCompatibleTypes = new Dictionary<string, List<AtsTypeRef>>();

        foreach (var typeInfo in typeInfos)
        {
            if (typeInfo.IsInterface)
            {
                continue;
            }

            // Create type ref for this concrete type
            var concreteTypeRef = new AtsTypeRef
            {
                TypeId = typeInfo.AtsTypeId,
                ClrType = typeInfo.ClrType,
                Category = AtsTypeCategory.Handle,
                IsInterface = false
            };

            // Register under each implemented interface
            foreach (var iface in typeInfo.ImplementedInterfaces)
            {
                AddToCompatibilityMap(typeToCompatibleTypes, iface.TypeId, concreteTypeRef);
            }

            // Register under each base type in hierarchy
            foreach (var baseType in typeInfo.BaseTypeHierarchy)
            {
                AddToCompatibilityMap(typeToCompatibleTypes, baseType.TypeId, concreteTypeRef);
            }
        }

        return typeToCompatibleTypes;
    }

    /// <summary>
    /// Helper to add a concrete type to the compatibility map under a given key.
    /// </summary>
    private static void AddToCompatibilityMap(
        Dictionary<string, List<AtsTypeRef>> map,
        string key,
        AtsTypeRef concreteTypeRef)
    {
        if (!map.TryGetValue(key, out var list))
        {
            list = [];
            map[key] = list;
        }
        list.Add(concreteTypeRef);
    }

    /// <summary>
    /// Detects method name collisions after capability expansion and removes overloaded methods.
    /// Since ATS doesn't support method overloading, each (TargetTypeId, MethodName) pair must be unique.
    /// Colliding capabilities are filtered out and diagnostics are added.
    /// </summary>
    private static void FilterMethodNameCollisions(List<AtsCapabilityInfo> capabilities, List<AtsDiagnostic> diagnostics)
    {
        // Group by (TargetTypeId, MethodName) to find collisions
        var collisions = capabilities
            .Where(c => c.ExpandedTargetTypes.Count > 0)
            .SelectMany(c => c.ExpandedTargetTypes.Select(t => (Target: t.TypeId, Capability: c)))
            .GroupBy(x => (x.Target, x.Capability.MethodName))
            .Where(g => g.Count() > 1)
            .ToList();

        if (collisions.Count > 0)
        {
            // Collect all colliding capability IDs to filter them out
            var collidingCapabilityIds = new HashSet<string>();

            foreach (var g in collisions)
            {
                var conflictingIds = g.Select(x => x.Capability.CapabilityId).ToList();
                var conflictingIdsStr = string.Join(", ", conflictingIds);

                diagnostics.Add(AtsDiagnostic.Warning(
                    $"Method '{g.Key.MethodName}' has multiple definitions for target '{g.Key.Target}' ({conflictingIdsStr}) and will be skipped. Use [AspireExport(MethodName = \"uniqueName\")] to disambiguate.",
                    g.Key.Target));

                foreach (var id in conflictingIds)
                {
                    collidingCapabilityIds.Add(id);
                }
            }

            // Remove all colliding capabilities
            capabilities.RemoveAll(c => collidingCapabilityIds.Contains(c.CapabilityId));
        }
    }

    /// <summary>
    /// Scans an assembly and returns only the capabilities.
    /// </summary>
    public static List<AtsCapabilityInfo> ScanCapabilities(
        Assembly assembly)
    {
        return ScanAssembly(assembly).Capabilities;
    }

    private static AtsTypeInfo? CreateTypeInfo(
        Type type,
        AspireExportAttribute exportAttr)
    {
        // Get the AtsTypeId - if not specified, derive it from the type
        var atsTypeId = exportAttr.Type != null
            ? AtsTypeMapping.DeriveTypeId(exportAttr.Type)
            : AtsTypeMapping.DeriveTypeId(type);

        // Collect ALL implemented interfaces (for concrete types only)
        // Use recursive collection to include inherited interfaces
        var implementedInterfaces = !type.IsInterface
            ? CollectAllInterfaces(type)
            : [];

        // Collect base type hierarchy (for concrete types only)
        // This enables expansion from base types to derived types
        var baseTypeHierarchy = !type.IsInterface
            ? CollectBaseTypeHierarchy(type)
            : [];

        return new AtsTypeInfo
        {
            AtsTypeId = atsTypeId,
            ClrType = type,
            IsInterface = type.IsInterface,
            ImplementedInterfaces = implementedInterfaces,
            BaseTypeHierarchy = baseTypeHierarchy,
            HasExposeProperties = exportAttr.ExposeProperties,
            HasExposeMethods = exportAttr.ExposeMethods
        };
    }

    /// <summary>
    /// Creates DTO type info for a type with [AspireDto] attribute.
    /// </summary>
    private static AtsDtoTypeInfo? CreateDtoTypeInfo(
        Type type)
    {
        var typeId = AtsTypeMapping.DeriveTypeId(type);
        var typeName = type.Name;

        // Collect public properties for the DTO interface
        var properties = new List<AtsDtoPropertyInfo>();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // Only include public readable properties (DTOs are public API)
            if (!prop.CanRead)
            {
                continue;
            }

            var propTypeRef = CreateTypeRef(prop.PropertyType);
            if (propTypeRef == null)
            {
                continue;
            }

            properties.Add(new AtsDtoPropertyInfo
            {
                Name = prop.Name,
                Type = propTypeRef,
                IsOptional = !prop.CanWrite // If no setter, it's likely init-only and required
            });
        }

        return new AtsDtoTypeInfo
        {
            TypeId = typeId,
            Name = typeName,
            ClrType = type,
            Properties = properties
        };
    }

    /// <summary>
    /// Result of creating context type capabilities, including any member-level diagnostics.
    /// </summary>
    internal sealed class ContextTypeCapabilitiesResult
    {
        public required List<AtsCapabilityInfo> Capabilities { get; init; }
        public List<AtsDiagnostic> Diagnostics { get; init; } = [];

        /// <summary>
        /// Runtime registry mapping capability IDs to methods.
        /// </summary>
        public Dictionary<string, MethodInfo> Methods { get; init; } = new();

        /// <summary>
        /// Runtime registry mapping capability IDs to properties.
        /// </summary>
        public Dictionary<string, PropertyInfo> Properties { get; init; } = new();
    }

    private static ContextTypeCapabilitiesResult CreateContextTypeCapabilities(
        Type contextType,
        string assemblyName)
    {
        var capabilities = new List<AtsCapabilityInfo>();
        var diagnostics = new List<AtsDiagnostic>();
        var methods = new Dictionary<string, MethodInfo>();
        var properties = new Dictionary<string, PropertyInfo>();

        // Derive the type ID from assembly name and full type name
        var typeName = contextType.Name;
        var fullName = contextType.FullName ?? contextType.Name;
        var contextAssemblyName = contextType.Assembly.GetName().Name ?? assemblyName;
        var typeId = AtsTypeMapping.DeriveTypeId(contextAssemblyName, fullName);

        // Extract the package (namespace) from the full type name for capability IDs
        var lastDot = fullName.LastIndexOf('.');
        var package = lastDot >= 0 ? fullName[..lastDot] : assemblyName;

        // Check for ExposeProperties and ExposeMethods flags
        var exposeAllProperties = HasExposePropertiesAttribute(contextType);
        var exposeAllMethods = HasExposeMethodsAttribute(contextType);

        // Scan properties
        foreach (var property in contextType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
        {
            // Skip static properties
            var isStatic = property.GetMethod?.IsStatic ?? property.SetMethod?.IsStatic ?? false;
            if (isStatic)
            {
                continue;
            }

            // Check for [AspireExportIgnore]
            if (HasExportIgnoreAttribute(property))
            {
                continue;
            }

            // Check if property should be exported
            // ExposeProperties=true exports public only; explicit [AspireExport] can export internal too
            var memberExportAttr = GetAspireExportAttribute(property);
            var isPublic = property.GetMethod?.IsPublic == true;
            if (!ShouldExportMember(isPublic, exposeAllProperties, memberExportAttr))
            {
                continue;
            }

            // Wrap individual property processing in try/catch to capture member-level errors
            // and continue processing other properties
            try
            {
                // Check for [AspireUnion] on property for union types (especially for Dict<string, object> value types)
                var propertyUnionAttr = GetAspireUnionAttribute(property);
                AtsTypeRef? propertyTypeRef;
                string? propertyTypeId;

                // Check if this is a Dictionary<string, object> that needs union value type
                var propType = property.PropertyType;
                var propTypeFullName = propType.FullName ?? propType.Name;
                var propGenericDef = propType.IsGenericType ? propType.GetGenericTypeDefinition().FullName : null;
                var isDictWithObjectValue =
                    (propGenericDef == "System.Collections.Generic.Dictionary`2" ||
                     propGenericDef == "System.Collections.Generic.IDictionary`2") &&
                    propType.GetGenericArguments().Skip(1).FirstOrDefault()?.FullName == "System.Object";

                if (isDictWithObjectValue)
                {
                    // Create dictionary type - use union if [AspireUnion] is present, otherwise use 'any'
                    var keyTypeRef = CreateTypeRef(propType.GetGenericArguments().First());
                    if (keyTypeRef != null)
                    {
                        var valueTypeRef = propertyUnionAttr != null
                            ? CreateUnionTypeRef(propertyUnionAttr, $"property '{property.Name}'")
                            : new AtsTypeRef { TypeId = AtsConstants.Any, Category = AtsTypeCategory.Primitive };

                        propertyTypeRef = new AtsTypeRef
                        {
                            TypeId = AtsConstants.DictTypeId(keyTypeRef.TypeId, valueTypeRef.TypeId),
                            Category = AtsTypeCategory.Dict,
                            KeyType = keyTypeRef,
                            ValueType = valueTypeRef,
                            IsReadOnly = false
                        };
                        propertyTypeId = propertyTypeRef.TypeId;
                    }
                    else
                    {
                        continue; // Skip if key type can't be mapped
                    }
                }
                else if (propTypeFullName == "System.Object")
                {
                    // Use union if [AspireUnion] is present, otherwise use 'any'
                    if (propertyUnionAttr != null)
                    {
                        propertyTypeRef = CreateUnionTypeRef(propertyUnionAttr, $"property '{property.Name}'");
                        propertyTypeId = propertyTypeRef.TypeId;
                    }
                    else
                    {
                        propertyTypeRef = new AtsTypeRef { TypeId = AtsConstants.Any, Category = AtsTypeCategory.Primitive };
                        propertyTypeId = propertyTypeRef.TypeId;
                    }
                }
                else
                {
                    propertyTypeRef = CreateTypeRef(propType);
                    propertyTypeId = MapToAtsTypeId(propType);
                }

                if (propertyTypeId is null)
                {
                    // Skip properties with unmapped types
                    continue;
                }

                // Create type ref for the context type
                var contextTypeRef = new AtsTypeRef
                {
                    TypeId = typeId,
                    ClrType = contextType,
                    Category = AtsTypeCategory.Handle,
                    IsInterface = contextType.IsInterface
                };

                // Get custom method name from attribute if specified
                var customMethodName = memberExportAttr?.Id;

                // Generate getter capability if property is readable
                // Naming: {TypeName}.{propertyName} (camelCase, no "get" prefix)
                if (property.CanRead)
                {
                    var camelCaseName = ToCamelCase(property.Name);
                    var getMethodName = customMethodName ?? $"{typeName}.{camelCaseName}";
                    var getCapabilityId = $"{package}/{getMethodName}";

                    capabilities.Add(new AtsCapabilityInfo
                    {
                        CapabilityId = getCapabilityId,
                        MethodName = camelCaseName,
                        OwningTypeName = typeName,
                        Description = $"Gets the {property.Name} property",
                        Parameters = [
                            new AtsParameterInfo
                            {
                                Name = "context",
                                Type = contextTypeRef,
                                IsOptional = false,
                                IsNullable = false,
                                IsCallback = false,
                                DefaultValue = null
                            }
                        ],
                        ReturnType = propertyTypeRef!,
                        TargetTypeId = typeId,
                        TargetType = contextTypeRef,
                        ReturnsBuilder = false,
                        CapabilityKind = AtsCapabilityKind.PropertyGetter
                    });

                    // Register property for runtime dispatch
                    properties[getCapabilityId] = property;
                }

                // Generate setter capability if property is writable
                // Naming: {TypeName}.set{PropertyName} (keep "set" prefix, PascalCase property name)
                if (property.CanWrite)
                {
                    var setMethodName = $"set{property.Name}";
                    var setCapabilityId = $"{package}/{typeName}.{setMethodName}";

                    capabilities.Add(new AtsCapabilityInfo
                    {
                        CapabilityId = setCapabilityId,
                        MethodName = setMethodName,
                        OwningTypeName = typeName,
                        Description = $"Sets the {property.Name} property",
                        Parameters = [
                            new AtsParameterInfo
                            {
                                Name = "context",
                                Type = contextTypeRef,
                                IsOptional = false,
                                IsNullable = false,
                                IsCallback = false,
                                DefaultValue = null
                            },
                            new AtsParameterInfo
                            {
                                Name = "value",
                                Type = propertyTypeRef!,
                                IsOptional = false,
                                IsNullable = false,
                                IsCallback = false,
                                DefaultValue = null
                            }
                        ],
                        ReturnType = contextTypeRef,
                        TargetTypeId = typeId,
                        TargetType = contextTypeRef,
                        ReturnsBuilder = false,
                        CapabilityKind = AtsCapabilityKind.PropertySetter
                    });

                    // Register property for runtime dispatch
                    properties[setCapabilityId] = property;
                }
            }
            catch (InvalidOperationException ex)
            {
                // Property-level error - record diagnostic and continue with other properties
                diagnostics.Add(AtsDiagnostic.Error(ex.Message, $"{fullName}.{property.Name}"));
            }
        }

        // Scan instance methods (either via ExposeMethods=true or member-level [AspireExport])
        // Create context type ref once for all methods
        var instanceContextTypeRef = new AtsTypeRef
        {
            TypeId = typeId,
            ClrType = contextType,
            Category = AtsTypeCategory.Handle,
            IsInterface = contextType.IsInterface
        };

        foreach (var method in contextType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
        {
            // Skip static methods
            if (method.IsStatic)
            {
                continue;
            }

            // Skip property accessors and special runtime methods
            // IsSpecialName catches property accessors (get_/set_), operators, etc.
            if (method.IsSpecialName ||
                method.Name == "GetType" || method.Name == "ToString" ||
                method.Name == "Equals" || method.Name == "GetHashCode")
            {
                continue;
            }

            // Skip generic method definitions (methods with type parameters like Subscribe<T>)
            // These can't be expressed in ATS since generic types are not supported
            if (method.IsGenericMethod)
            {
                continue;
            }

            // Check for [AspireExportIgnore]
            if (HasExportIgnoreAttribute(method))
            {
                continue;
            }

            // Check if method should be exported
            // ExposeMethods=true exports public only; explicit [AspireExport] can export internal too
            var memberExportAttr = GetAspireExportAttribute(method);
            if (!ShouldExportMember(method.IsPublic, exposeAllMethods, memberExportAttr))
            {
                continue;
            }

            // Wrap individual method processing in try/catch to capture member-level errors
            try
            {
                // Get custom method name from attribute if specified
                var customMethodName = memberExportAttr?.Id;

                // Generate method capability
                // If explicit [AspireExport("id")] with custom Id, use that directly (like static exports)
                // If auto-exposed via ExposeMethods=true, use TypeName.methodName pattern to avoid collisions
                string methodCapabilityName;
                string methodCapabilityId;

                if (customMethodName != null)
                {
                    // Explicit export - use the custom Id directly
                    methodCapabilityName = customMethodName;
                    methodCapabilityId = $"{package}/{customMethodName}";
                }
                else
                {
                    // Auto-exposed via ExposeMethods=true - use TypeName.methodName pattern
                    var camelCaseMethodName = ToCamelCase(method.Name);
                    methodCapabilityName = $"{typeName}.{camelCaseMethodName}";
                    methodCapabilityId = $"{package}/{methodCapabilityName}";
                }

                // Build parameters (first parameter is the context/instance)
                var paramInfos = new List<AtsParameterInfo>
                {
                    new AtsParameterInfo
                    {
                        Name = "context",
                        Type = instanceContextTypeRef,
                        IsOptional = false,
                        IsNullable = false,
                        IsCallback = false,
                        DefaultValue = null
                    }
                };

                var paramIndex = 0;
                var hasUnmappedRequiredParam = false;
                foreach (var param in method.GetParameters())
                {
                    var paramInfo = CreateParameterInfo(param, paramIndex);
                    if (paramInfo is null)
                    {
                        // Parameter type couldn't be mapped - skip if required
                        if (!param.IsOptional)
                        {
                            hasUnmappedRequiredParam = true;
                            break;
                        }
                        // Skip optional parameters with unmapped types
                        continue;
                    }
                    paramInfos.Add(paramInfo);
                    paramIndex++;
                }

                // Skip capability if a required parameter couldn't be mapped
                if (hasUnmappedRequiredParam)
                {
                    continue;
                }

                // Get return type
                var returnTypeRef = CreateTypeRef(method.ReturnType);

                // Get description from attribute if specified
                var description = memberExportAttr?.Description ?? $"Invokes the {method.Name} method";

                // Get simple method name (without type prefix)
                var simpleMethodName = customMethodName ?? ToCamelCase(method.Name);

                capabilities.Add(new AtsCapabilityInfo
                {
                    CapabilityId = methodCapabilityId,
                    MethodName = simpleMethodName,
                    OwningTypeName = typeName,
                    Description = description,
                    Parameters = paramInfos,
                    ReturnType = returnTypeRef ?? CreateVoidTypeRef(),
                    TargetTypeId = typeId,
                    TargetType = instanceContextTypeRef,
                    ReturnsBuilder = false,
                    CapabilityKind = AtsCapabilityKind.InstanceMethod
                });

                // Register method for runtime dispatch
                methods[methodCapabilityId] = method;
            }
            catch (InvalidOperationException ex)
            {
                // Method-level error - record diagnostic and continue with other methods
                diagnostics.Add(AtsDiagnostic.Error(ex.Message, $"{contextType.FullName}.{method.Name}"));
            }
        }

        return new ContextTypeCapabilitiesResult
        {
            Capabilities = capabilities,
            Diagnostics = diagnostics,
            Methods = methods,
            Properties = properties
        };
    }

    private static AtsCapabilityInfo? CreateCapabilityInfo(
        MethodInfo method,
        AspireExportAttribute exportAttr,
        string assemblyName,
        out AtsDiagnostic? diagnostic)
    {
        diagnostic = null;
        var methodLocation = method.Name;

        // Get method name from attribute
        var methodNameFromAttr = exportAttr.Id;
        if (string.IsNullOrEmpty(methodNameFromAttr))
        {
            diagnostic = AtsDiagnostic.Warning(
                $"[AspireExport] attribute on '{methodLocation}' is missing method name argument",
                methodLocation);
            return null;
        }

        // Get named arguments
        var description = exportAttr.Description;
        var methodNameOverride = exportAttr.MethodName;

        var methodName = methodNameOverride ?? methodNameFromAttr;
        // New format: {AssemblyName}/{methodName}
        var capabilityId = $"{assemblyName}/{methodNameFromAttr}";

        var parameters = method.GetParameters().ToList();

        string? extendsTypeId = null;
        AtsTypeRef? extendsTypeRef = null;
        string? targetParameterName = null;
        if (parameters.Count > 0)
        {
            var firstParam = parameters[0];
            var firstParamType = firstParam.ParameterType;

            // Check if this is IResourceBuilder<T> where T is an unresolved generic parameter
            if (IsUnresolvedGenericResourceBuilder(firstParamType))
            {
                // Skip - can't generate concrete builders for unresolved generic type parameters
                // This is expected, not a warning
                return null;
            }

            extendsTypeRef = CreateTypeRef(firstParamType);
            var firstParamTypeId = extendsTypeRef?.TypeId ?? MapToAtsTypeId(firstParamType);
            if (firstParamTypeId != null)
            {
                extendsTypeId = firstParamTypeId;
                // Capture the parameter name for code generation (e.g., "builder", "resource")
                targetParameterName = firstParam.Name;
            }
        }

        // Build parameters (skip first if it's a handle type)
        var paramInfos = new List<AtsParameterInfo>();
        var skipFirst = extendsTypeId != null;
        var paramList = skipFirst ? parameters.Skip(1) : parameters;

        var paramIndex = 0;
        foreach (var param in paramList)
        {
            var paramInfo = CreateParameterInfo(param, paramIndex);
            if (paramInfo is null)
            {
                // Parameter type couldn't be mapped - skip if required
                if (!param.IsOptional)
                {
                    // Required parameter with unmapped type - skip this capability
                    diagnostic = AtsDiagnostic.Warning(
                        $"Capability '{capabilityId}' skipped: parameter '{param.Name}' has unmapped type '{param.ParameterType.FullName}'",
                        methodLocation);
                    return null;
                }
                // Skip optional parameters with unmapped types
                continue;
            }
            paramInfos.Add(paramInfo);
            paramIndex++;
        }

        // Get return type
        var returnTypeRef = CreateTypeRef(method.ReturnType);
        var returnTypeId = MapToAtsTypeId(method.ReturnType);

        // Only set ReturnsBuilder if the return type is actually a resource builder type
        var returnsBuilder = returnTypeId != null && IsResourceBuilderType(method.ReturnType);

        return new AtsCapabilityInfo
        {
            CapabilityId = capabilityId,
            MethodName = methodName,
            Description = description,
            Parameters = paramInfos,
            ReturnType = returnTypeRef ?? CreateVoidTypeRef(),
            TargetTypeId = extendsTypeId,
            TargetType = extendsTypeRef,
            TargetParameterName = targetParameterName,
            ReturnsBuilder = returnsBuilder
        };
    }

    private static AtsParameterInfo? CreateParameterInfo(
        ParameterInfo param,
        int paramIndex)
    {
        var paramType = param.ParameterType;
        var paramName = string.IsNullOrEmpty(param.Name) ? $"arg{paramIndex}" : param.Name;

        // Check for [AspireUnion] attribute on the parameter
        var unionAttr = GetAspireUnionAttribute(param);
        if (unionAttr != null)
        {
            // Create union type from attribute
            var unionTypeRef = CreateUnionTypeRef(unionAttr, $"parameter '{paramName}'");
            return new AtsParameterInfo
            {
                Name = paramName,
                Type = unionTypeRef,
                IsOptional = param.IsOptional,
                IsNullable = false,
                IsCallback = false,
                DefaultValue = param.HasDefaultValue ? param.DefaultValue : null
            };
        }

        // Check if this is a delegate type (callbacks are inferred from delegate types)
        var isCallback = typeof(Delegate).IsAssignableFrom(paramType);

        // Create type reference
        var typeRef = CreateTypeRef(paramType);

        // Map the type - return null if unmapped (unless it's a callback)
        var atsTypeId = MapToAtsTypeId(paramType);
        if (atsTypeId is null && !isCallback)
        {
            // Can't map this parameter type - skip it
            return null;
        }

        // Extract callback signature if this is a callback parameter
        IReadOnlyList<AtsCallbackParameterInfo>? callbackParameters = null;
        AtsTypeRef? callbackReturnType = null;

        if (isCallback)
        {
            (callbackParameters, callbackReturnType) = ExtractCallbackSignature(paramType);
        }

        // Check if nullable (Nullable<T>)
        var isNullable = Nullable.GetUnderlyingType(paramType) != null;

        // For callbacks, create a callback type ref
        var finalTypeRef = isCallback
            ? new AtsTypeRef { TypeId = "callback", Category = AtsTypeCategory.Callback }
            : typeRef;

        return new AtsParameterInfo
        {
            Name = paramName,
            Type = finalTypeRef,
            IsOptional = param.IsOptional,
            IsNullable = isNullable,
            IsCallback = isCallback,
            CallbackParameters = callbackParameters,
            CallbackReturnType = callbackReturnType,
            DefaultValue = param.HasDefaultValue ? param.DefaultValue : null
        };
    }

    /// <summary>
    /// Extracts the callback signature (parameters and return type) from a delegate type.
    /// </summary>
    private static (IReadOnlyList<AtsCallbackParameterInfo>? Parameters, AtsTypeRef? ReturnType) ExtractCallbackSignature(
        Type delegateType)
    {
        // Find the Invoke method on the delegate type
        var invokeMethod = delegateType.GetMethod("Invoke");
        if (invokeMethod is null)
        {
            // Fallback for well-known delegate types when Invoke method isn't available
            // (e.g., when loading from reference assemblies without full type definitions)
            return ExtractWellKnownDelegateSignature(delegateType);
        }

        // Extract parameters
        var parameters = new List<AtsCallbackParameterInfo>();
        foreach (var param in invokeMethod.GetParameters())
        {
            var paramType = param.ParameterType;
            var paramTypeRef = CreateTypeRef(paramType);
            if (paramTypeRef != null)
            {
                parameters.Add(new AtsCallbackParameterInfo
                {
                    Name = param.Name ?? $"arg{param.Position}",
                    Type = paramTypeRef
                });
            }
        }

        // Extract return type
        var returnType = invokeMethod.ReturnType;
        AtsTypeRef? returnTypeRef;

        if (returnType == typeof(void))
        {
            returnTypeRef = new AtsTypeRef { TypeId = AtsConstants.Void, Category = AtsTypeCategory.Primitive };
        }
        else if (returnType == typeof(Task))
        {
            returnTypeRef = new AtsTypeRef { TypeId = AtsConstants.Void, Category = AtsTypeCategory.Primitive };
        }
        else if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            // Task<T> - get the inner type
            var innerType = returnType.GetGenericArguments().FirstOrDefault();
            returnTypeRef = innerType is not null
                ? CreateTypeRef(innerType)
                    ?? new AtsTypeRef { TypeId = AtsConstants.Void, Category = AtsTypeCategory.Primitive }
                : new AtsTypeRef { TypeId = AtsConstants.Void, Category = AtsTypeCategory.Primitive };
        }
        else
        {
            returnTypeRef = CreateTypeRef(returnType)
                ?? new AtsTypeRef { TypeId = AtsConstants.Void, Category = AtsTypeCategory.Primitive };
        }

        return (parameters, returnTypeRef);
    }

    /// <summary>
    /// Extracts signature from well-known delegate types based on their generic type definition.
    /// Used as fallback when the Invoke method isn't available from metadata.
    /// </summary>
    private static (IReadOnlyList<AtsCallbackParameterInfo>? Parameters, AtsTypeRef? ReturnType) ExtractWellKnownDelegateSignature(
        Type delegateType)
    {
        if (!delegateType.IsGenericType)
        {
            return (null, null);
        }

        var genericDef = delegateType.GetGenericTypeDefinition();
        var genericDefFullName = genericDef.FullName ?? "";
        var genericArgs = delegateType.GetGenericArguments().ToList();
        if (genericArgs.Count == 0)
        {
            return (null, null);
        }

        var voidTypeRef = new AtsTypeRef { TypeId = AtsConstants.Void, Category = AtsTypeCategory.Primitive };

        // Action<T>, Action<T1, T2>, etc. - all params are inputs, void return
        if (genericDefFullName.StartsWith("System.Action`"))
        {
            var parameters = new List<AtsCallbackParameterInfo>();
            for (var i = 0; i < genericArgs.Count; i++)
            {
                var paramType = genericArgs[i];
                var paramTypeRef = CreateTypeRef(paramType);
                if (paramTypeRef != null)
                {
                    parameters.Add(new AtsCallbackParameterInfo
                    {
                        Name = $"arg{i}",
                        Type = paramTypeRef
                    });
                }
            }
            return (parameters, voidTypeRef);
        }

        // Func<TResult>, Func<T, TResult>, Func<T1, T2, TResult>, etc.
        // Last generic arg is return type, rest are parameters
        if (genericDefFullName.StartsWith("System.Func`"))
        {
            var parameters = new List<AtsCallbackParameterInfo>();
            for (var i = 0; i < genericArgs.Count - 1; i++)
            {
                var paramType = genericArgs[i];
                var paramTypeRef = CreateTypeRef(paramType);
                if (paramTypeRef != null)
                {
                    parameters.Add(new AtsCallbackParameterInfo
                    {
                        Name = $"arg{i}",
                        Type = paramTypeRef
                    });
                }
            }

            var funcReturnType = genericArgs[^1];
            AtsTypeRef returnTypeRef;

            if (funcReturnType == typeof(void))
            {
                returnTypeRef = voidTypeRef;
            }
            else if (funcReturnType == typeof(Task))
            {
                returnTypeRef = voidTypeRef;
            }
            else if (funcReturnType.IsGenericType && funcReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                // Task<T> - get the inner type
                var innerType = funcReturnType.GetGenericArguments().FirstOrDefault();
                returnTypeRef = innerType is not null
                    ? CreateTypeRef(innerType) ?? voidTypeRef
                    : voidTypeRef;
            }
            else
            {
                returnTypeRef = CreateTypeRef(funcReturnType) ?? voidTypeRef;
            }

            return (parameters, returnTypeRef);
        }

        return (null, null);
    }

    /// <summary>
    /// Maps a CLR type to an ATS type ID.
    /// All type mapping logic is centralized here.
    /// </summary>
    public static string? MapToAtsTypeId(Type type)
    {
        // Handle void
        if (type == typeof(void))
        {
            return null;
        }

        // Handle Task (async void)
        if (type == typeof(Task))
        {
            return null;
        }

        // Handle Task<T> - extract T
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var genericArgs = type.GetGenericArguments();
            if (genericArgs.Length > 0)
            {
                return MapToAtsTypeId(genericArgs[0]);
            }
        }

        // Handle primitives using FrozenSet lookup
        if (AtsConstants.IsPrimitiveType(type))
        {
            return GetPrimitiveTypeId(type);
        }

        // Handle object type - maps to 'any' in TypeScript
        if (type == typeof(object))
        {
            return AtsConstants.Any;
        }

        // Handle enum types
        if (type.IsEnum)
        {
            return AtsConstants.EnumTypeId(type.FullName ?? type.Name);
        }

        // Handle Nullable<T> - unwrap
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            return MapToAtsTypeId(underlyingType);
        }

        // Handle Dictionary<K,V> - mutable dictionary
        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            var genericArgs = type.GetGenericArguments();

            if (genericDef == typeof(Dictionary<,>) || genericDef == typeof(IDictionary<,>))
            {
                if (genericArgs.Length == 2)
                {
                    var keyTypeName = genericArgs[0].Name;
                    var valueTypeName = genericArgs[1].Name;
                    return AtsConstants.DictTypeId(keyTypeName, valueTypeName);
                }
            }

            // Handle IReadOnlyDictionary<K,V> - immutable (serialized copy)
            if (genericDef == typeof(IReadOnlyDictionary<,>))
            {
                return "object"; // Serialized as JSON object copy
            }

            // Handle List<T> - mutable list
            if (genericDef == typeof(List<>) || genericDef == typeof(IList<>))
            {
                if (genericArgs.Length == 1)
                {
                    var elementTypeName = genericArgs[0].Name;
                    return AtsConstants.ListTypeId(elementTypeName);
                }
            }

            // Handle IReadOnlyList<T>, IReadOnlyCollection<T> - immutable (array)
            if (genericDef == typeof(IReadOnlyList<>) || genericDef == typeof(IReadOnlyCollection<>))
            {
                if (genericArgs.Length == 1)
                {
                    var elementTypeId = MapToAtsTypeId(genericArgs[0]);
                    return elementTypeId != null ? $"{elementTypeId}[]" : null;
                }
            }

            // Handle IResourceBuilder<T>
            if (IsResourceBuilderType(genericDef))
            {
                if (genericArgs.Length > 0)
                {
                    var resourceType = genericArgs[0];

                    // If T is a generic parameter, use its constraint type
                    if (resourceType.IsGenericParameter)
                    {
                        var constraints = resourceType.GetGenericParameterConstraints();
                        if (constraints.Length > 0)
                        {
                            return AtsTypeMapping.DeriveTypeId(constraints[0]);
                        }
                    }

                    return AtsTypeMapping.DeriveTypeId(resourceType);
                }
            }
        }

        // Handle arrays - return as typed array (serialized copy)
        if (type.IsArray)
        {
            var elementType = type.GetElementType();
            if (elementType != null)
            {
                var elementTypeId = MapToAtsTypeId(elementType);
                return elementTypeId != null ? $"{elementTypeId}[]" : null;
            }
            return null;
        }

        // Check for [AspireDto] attribute
        if (HasAspireDtoAttribute(type))
        {
            return AtsTypeMapping.DeriveTypeId(type);
        }

        // Check for [AspireExport] attribute
        if (GetAspireExportAttribute(type) != null)
        {
            return AtsTypeMapping.DeriveTypeId(type);
        }

        // No mapping found - return null to indicate unmapped type
        return null;
    }

    /// <summary>
    /// Gets the ATS type ID for a primitive CLR type.
    /// </summary>
    private static string? GetPrimitiveTypeId(Type type)
    {
        if (type == typeof(string))
        {
            return AtsConstants.String;
        }
        if (type == typeof(char))
        {
            return AtsConstants.Char;
        }
        if (type == typeof(bool))
        {
            return AtsConstants.Boolean;
        }

        // All numeric types map to "number"
        if (type == typeof(int) || type == typeof(long) || type == typeof(double) ||
            type == typeof(float) || type == typeof(short) || type == typeof(byte) ||
            type == typeof(decimal) || type == typeof(ushort) || type == typeof(uint) ||
            type == typeof(ulong) || type == typeof(sbyte))
        {
            return AtsConstants.Number;
        }

        // Date/time types
        if (type == typeof(DateTime))
        {
            return AtsConstants.DateTime;
        }
        if (type == typeof(DateTimeOffset))
        {
            return AtsConstants.DateTimeOffset;
        }
        if (type == typeof(DateOnly))
        {
            return AtsConstants.DateOnly;
        }
        if (type == typeof(TimeOnly))
        {
            return AtsConstants.TimeOnly;
        }
        if (type == typeof(TimeSpan))
        {
            return AtsConstants.TimeSpan;
        }

        // Other scalar types
        if (type == typeof(Guid))
        {
            return AtsConstants.Guid;
        }
        if (type == typeof(Uri))
        {
            return AtsConstants.Uri;
        }
        if (type == typeof(CancellationToken))
        {
            return AtsConstants.CancellationToken;
        }

        return null;
    }

    /// <summary>
    /// Checks if a type is IResourceBuilder&lt;T&gt;.
    /// </summary>
    private static bool IsResourceBuilderType(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IResourceBuilder<>);
    }

    /// <summary>
    /// Creates an AtsTypeRef from a CLR type with full type metadata.
    /// </summary>
    public static AtsTypeRef? CreateTypeRef(Type? type) =>
        CreateTypeRef(type, enumCollector: null);

    /// <summary>
    /// Creates an AtsTypeRef for void return type.
    /// </summary>
    private static AtsTypeRef CreateVoidTypeRef() => new AtsTypeRef
    {
        TypeId = AtsConstants.Void,
        Category = AtsTypeCategory.Primitive
    };

    /// <summary>
    /// Creates an AtsTypeRef from a CLR type, optionally collecting enum types.
    /// </summary>
    private static AtsTypeRef? CreateTypeRef(
        Type? type,
        EnumCollector? enumCollector)
    {
        if (type == null)
        {
            return null;
        }

        // Handle void - no type ref
        if (type == typeof(void))
        {
            return null;
        }

        // Handle Task (async void) - no type ref
        if (type == typeof(Task))
        {
            return null;
        }

        // Handle Task<T> - unwrap to inner type
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var genericArgs = type.GetGenericArguments();
            if (genericArgs.Length > 0)
            {
                return CreateTypeRef(genericArgs[0], enumCollector);
            }
            return null;
        }

        // Handle Nullable<T> - unwrap to inner type
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            return CreateTypeRef(underlyingType, enumCollector);
        }

        // Handle primitives
        var primitiveTypeId = GetPrimitiveTypeId(type);
        if (primitiveTypeId != null)
        {
            return new AtsTypeRef { TypeId = primitiveTypeId, ClrType = type, Category = AtsTypeCategory.Primitive };
        }

        // Handle object type - maps to 'any' in TypeScript
        if (type == typeof(object))
        {
            return new AtsTypeRef { TypeId = AtsConstants.Any, ClrType = type, Category = AtsTypeCategory.Primitive };
        }

        // Handle enum types
        if (type.IsEnum)
        {
            // Collect enum type info for code generation
            enumCollector?.Add(type);

            return new AtsTypeRef
            {
                TypeId = AtsConstants.EnumTypeId(type.FullName ?? type.Name),
                ClrType = type,
                Category = AtsTypeCategory.Enum
            };
        }

        // Handle generic types (Dictionary, List, IResourceBuilder, etc.)
        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            var genericArgs = type.GetGenericArguments();

            // Handle Dictionary<K,V> - mutable dictionary
            if (genericDef == typeof(Dictionary<,>) || genericDef == typeof(IDictionary<,>))
            {
                if (genericArgs.Length == 2)
                {
                    var keyTypeRef = CreateTypeRef(genericArgs[0], enumCollector);
                    var valueTypeRef = CreateTypeRef(genericArgs[1], enumCollector);
                    if (keyTypeRef != null && valueTypeRef != null)
                    {
                        return new AtsTypeRef
                        {
                            TypeId = AtsConstants.DictTypeId(keyTypeRef.TypeId, valueTypeRef.TypeId),
                            ClrType = type,
                            Category = AtsTypeCategory.Dict,
                            KeyType = keyTypeRef,
                            ValueType = valueTypeRef,
                            IsReadOnly = false
                        };
                    }
                }
                return null;
            }

            // Handle IReadOnlyDictionary<K,V> - immutable dictionary (serialized copy)
            if (genericDef == typeof(IReadOnlyDictionary<,>))
            {
                if (genericArgs.Length == 2)
                {
                    var keyTypeRef = CreateTypeRef(genericArgs[0], enumCollector);
                    var valueTypeRef = CreateTypeRef(genericArgs[1], enumCollector);
                    if (keyTypeRef != null && valueTypeRef != null)
                    {
                        return new AtsTypeRef
                        {
                            TypeId = AtsConstants.DictTypeId(keyTypeRef.TypeId, valueTypeRef.TypeId),
                            ClrType = type,
                            Category = AtsTypeCategory.Dict,
                            KeyType = keyTypeRef,
                            ValueType = valueTypeRef,
                            IsReadOnly = true
                        };
                    }
                }
                return null;
            }

            // Handle List<T> - mutable list
            if (genericDef == typeof(List<>) || genericDef == typeof(IList<>))
            {
                if (genericArgs.Length == 1)
                {
                    var elementTypeRef = CreateTypeRef(genericArgs[0], enumCollector);
                    if (elementTypeRef != null)
                    {
                        return new AtsTypeRef
                        {
                            TypeId = AtsConstants.ListTypeId(elementTypeRef.TypeId),
                            ClrType = type,
                            Category = AtsTypeCategory.List,
                            ElementType = elementTypeRef
                        };
                    }
                }
                return null;
            }

            // Handle IReadOnlyList<T>, IReadOnlyCollection<T> - immutable (serialized copy as array)
            if (genericDef == typeof(IReadOnlyList<>) || genericDef == typeof(IReadOnlyCollection<>))
            {
                if (genericArgs.Length == 1)
                {
                    var elementTypeRef = CreateTypeRef(genericArgs[0], enumCollector);
                    if (elementTypeRef != null)
                    {
                        return new AtsTypeRef
                        {
                            TypeId = AtsConstants.ArrayTypeId(elementTypeRef.TypeId),
                            ClrType = type,
                            Category = AtsTypeCategory.Array,
                            ElementType = elementTypeRef,
                            IsReadOnly = true
                        };
                    }
                }
                return null;
            }

            // Handle IResourceBuilder<T>
            if (IsResourceBuilderType(genericDef))
            {
                if (genericArgs.Length > 0)
                {
                    var resourceType = genericArgs[0];

                    // If T is a generic parameter, use the constraint type
                    if (resourceType.IsGenericParameter)
                    {
                        var constraints = resourceType.GetGenericParameterConstraints();
                        if (constraints.Length > 0)
                        {
                            var constraintType = constraints[0];
                            var constraintTypeId = AtsTypeMapping.DeriveTypeId(constraintType);
                            return new AtsTypeRef
                            {
                                TypeId = constraintTypeId,
                                ClrType = constraintType,
                                Category = AtsTypeCategory.Handle,
                                IsInterface = constraintType.IsInterface
                            };
                        }
                    }

                    var typeId = AtsTypeMapping.DeriveTypeId(resourceType);
                    return new AtsTypeRef
                    {
                        TypeId = typeId,
                        ClrType = resourceType,
                        Category = AtsTypeCategory.Handle,
                        IsInterface = resourceType.IsInterface
                    };
                }
            }
        }

        // Handle arrays - serialized copy
        if (type.IsArray)
        {
            var elementType = type.GetElementType();
            if (elementType != null)
            {
                var elementTypeRef = CreateTypeRef(elementType, enumCollector);
                if (elementTypeRef != null)
                {
                    return new AtsTypeRef
                    {
                        TypeId = AtsConstants.ArrayTypeId(elementTypeRef.TypeId),
                        ClrType = type,
                        Category = AtsTypeCategory.Array,
                        ElementType = elementTypeRef,
                        IsReadOnly = true
                    };
                }
            }
            return null;
        }

        // Check for [AspireDto] attribute - DTOs are serialized as JSON objects
        if (HasAspireDtoAttribute(type))
        {
            return new AtsTypeRef
            {
                TypeId = AtsTypeMapping.DeriveTypeId(type),
                ClrType = type,
                Category = AtsTypeCategory.Dto,
                IsInterface = type.IsInterface
            };
        }

        // Check for [AspireExport] attribute - these are handle types
        if (GetAspireExportAttribute(type) != null)
        {
            return new AtsTypeRef
            {
                TypeId = AtsTypeMapping.DeriveTypeId(type),
                ClrType = type,
                Category = AtsTypeCategory.Handle,
                IsInterface = type.IsInterface
            };
        }

        // Unknown type - mark for validation in pass 2
        return new AtsTypeRef
        {
            TypeId = AtsTypeMapping.DeriveTypeId(type),
            ClrType = type,
            Category = AtsTypeCategory.Unknown,
            IsInterface = type.IsInterface
        };
    }

    /// <summary>
    /// Derives the method name from a capability ID.
    /// Format: {Package}/{MethodName} (e.g., "Aspire.Hosting.Redis/addRedis" -> "addRedis")
    /// </summary>
    public static string DeriveMethodName(string capabilityId)
    {
        var slashIndex = capabilityId.LastIndexOf('/');
        return slashIndex >= 0 ? capabilityId[(slashIndex + 1)..] : capabilityId;
    }

    /// <summary>
    /// Derives the package name from a capability ID.
    /// Format: {Package}/{MethodName} (e.g., "Aspire.Hosting.Redis/addRedis" -> "Aspire.Hosting.Redis")
    /// </summary>
    public static string DerivePackage(string capabilityId)
    {
        var slashIndex = capabilityId.IndexOf('/');
        return slashIndex >= 0 ? capabilityId[..slashIndex] : capabilityId;
    }

    /// <summary>
    /// Checks if a type is IResourceBuilder&lt;T&gt; where T is a generic parameter
    /// with no constraints (truly unresolvable).
    /// </summary>
    private static bool IsUnresolvedGenericResourceBuilder(Type type)
    {
        // Check if this is IResourceBuilder<T>
        if (!type.IsGenericType)
        {
            return false;
        }

        var genericDef = type.GetGenericTypeDefinition();
        if (genericDef != typeof(IResourceBuilder<>))
        {
            return false;
        }

        var genericArgs = type.GetGenericArguments();
        if (genericArgs.Length == 0)
        {
            return false;
        }

        var resourceType = genericArgs[0];

        // If T is not a generic parameter, it's resolved
        if (!resourceType.IsGenericParameter)
        {
            return false;
        }

        // T is a generic parameter - check if it has any constraints
        var constraints = resourceType.GetGenericParameterConstraints();

        // If T has constraints, use them (MapToAtsTypeId will pick the first constraint)
        // Expansion will handle mapping interface constraints to concrete types
        return constraints.Length == 0;
    }

    /// <summary>
    /// Collects ALL interfaces implemented by a type, including inherited interfaces.
    /// </summary>
    private static List<AtsTypeRef> CollectAllInterfaces(Type type)
    {
        var allInterfaces = new List<AtsTypeRef>();

        // GetInterfaces() returns all interfaces including inherited ones
        foreach (var iface in type.GetInterfaces())
        {
            var ifaceTypeId = AtsTypeMapping.DeriveTypeId(iface);
            allInterfaces.Add(new AtsTypeRef
            {
                TypeId = ifaceTypeId,
                ClrType = iface,
                Category = AtsTypeCategory.Handle,
                IsInterface = true
            });
        }

        return allInterfaces;
    }

    /// <summary>
    /// Collects the base type hierarchy for a type (from immediate base up to Resource/Object).
    /// This is used for expanding capabilities targeting base types to derived types.
    /// </summary>
    private static List<AtsTypeRef> CollectBaseTypeHierarchy(Type type)
    {
        var baseTypes = new List<AtsTypeRef>();

        // Walk up the inheritance chain
        var currentBase = type.BaseType;
        while (currentBase != null)
        {
            // Stop at system types
            var baseFullName = currentBase.FullName;
            if (baseFullName == null ||
                baseFullName == "System.Object" ||
                baseFullName.StartsWith("System.", StringComparison.Ordinal) ||
                baseFullName.StartsWith("Microsoft.", StringComparison.Ordinal))
            {
                break;
            }

            var baseTypeId = AtsTypeMapping.DeriveTypeId(currentBase);
            baseTypes.Add(new AtsTypeRef
            {
                TypeId = baseTypeId,
                ClrType = currentBase,
                Category = AtsTypeCategory.Handle,
                IsInterface = false
            });

            currentBase = currentBase.BaseType;
        }

        return baseTypes;
    }

    /// <summary>
    /// Collects concrete resource types from a capability method's parameters and return type.
    /// These types are needed for expansion but may not have [AspireExport] attributes.
    /// </summary>
    private static void CollectResourceTypesFromCapability(
        MethodInfo method,
        Dictionary<string, Type> discoveredTypes)
    {
        // Check all parameters (including callback parameters)
        foreach (var param in method.GetParameters())
        {
            CollectResourceTypeFromType(param.ParameterType, discoveredTypes);
        }

        // Check return type
        CollectResourceTypeFromType(method.ReturnType, discoveredTypes);

        // Also collect constraint types from generic parameters
        // This handles cases like WithLifetime<T>(...) where T : ContainerResource
        // Without this, ContainerResource wouldn't be discovered as a type
        if (method.IsGenericMethodDefinition)
        {
            foreach (var genericParam in method.GetGenericArguments())
            {
                foreach (var constraint in genericParam.GetGenericParameterConstraints())
                {
                    // Only add if it's a class constraint (not interface or struct)
                    // and if it's a resource type (inherits from IResource)
                    if (!constraint.IsInterface && !constraint.IsValueType)
                    {
                        var typeId = AtsTypeMapping.DeriveTypeId(constraint);
                        discoveredTypes.TryAdd(typeId, constraint);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Recursively collects resource types from any type reference.
    /// Handles IResourceBuilder, Action, Func, Task, and other wrapper types.
    /// </summary>
    private static void CollectResourceTypeFromType(
        Type type,
        Dictionary<string, Type> discoveredTypes)
    {
        if (!type.IsGenericType)
        {
            return;
        }

        var genericDef = type.GetGenericTypeDefinition();
        var genericArgs = type.GetGenericArguments();

        // Handle Task<T> - unwrap and recurse
        if (genericDef == typeof(Task<>))
        {
            if (genericArgs.Length > 0)
            {
                CollectResourceTypeFromType(genericArgs[0], discoveredTypes);
            }
            return;
        }

        // Handle IResourceBuilder<T> - this is what we're looking for
        if (genericDef == typeof(IResourceBuilder<>))
        {
            if (genericArgs.Length > 0)
            {
                var resourceType = genericArgs[0];
                if (!resourceType.IsGenericParameter)
                {
                    var typeId = AtsTypeMapping.DeriveTypeId(resourceType);
                    discoveredTypes.TryAdd(typeId, resourceType);
                }
            }
            return;
        }

        // Handle Action<T>, Action<T1, T2>, etc. - recurse into generic args
        var genericDefName = genericDef.FullName;
        if (genericDefName?.StartsWith("System.Action`", StringComparison.Ordinal) == true)
        {
            foreach (var arg in genericArgs)
            {
                CollectResourceTypeFromType(arg, discoveredTypes);
            }
            return;
        }

        // Handle Func<T>, Func<T1, T2, TResult>, etc. - recurse into generic args
        if (genericDefName?.StartsWith("System.Func`", StringComparison.Ordinal) == true)
        {
            foreach (var arg in genericArgs)
            {
                CollectResourceTypeFromType(arg, discoveredTypes);
            }
            return;
        }

        // Handle Nullable<T>
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            CollectResourceTypeFromType(underlyingType, discoveredTypes);
            return;
        }

        // For other delegate types, try to get the Invoke method
        if (typeof(Delegate).IsAssignableFrom(type))
        {
            var invokeMethod = type.GetMethod("Invoke");
            if (invokeMethod != null)
            {
                foreach (var cbParam in invokeMethod.GetParameters())
                {
                    CollectResourceTypeFromType(cbParam.ParameterType, discoveredTypes);
                }
                CollectResourceTypeFromType(invokeMethod.ReturnType, discoveredTypes);
            }
        }
    }

    private static AspireExportAttribute? GetAspireExportAttribute(Type type)
    {
        return type.GetCustomAttribute<AspireExportAttribute>();
    }

    private static AspireExportAttribute? GetAspireExportAttribute(MethodInfo method)
    {
        return method.GetCustomAttribute<AspireExportAttribute>();
    }

    /// <summary>
    /// Checks if a type has [AspireExport(ExposeProperties = true)] attribute.
    /// </summary>
    private static bool HasExposePropertiesAttribute(Type type)
    {
        var attr = type.GetCustomAttribute<AspireExportAttribute>();
        return attr?.ExposeProperties == true;
    }

    /// <summary>
    /// Checks if a type has [AspireExport(ExposeMethods = true)] attribute.
    /// </summary>
    private static bool HasExposeMethodsAttribute(Type type)
    {
        var attr = type.GetCustomAttribute<AspireExportAttribute>();
        return attr?.ExposeMethods == true;
    }

    /// <summary>
    /// Checks if a property has [AspireExportIgnore] attribute.
    /// </summary>
    private static bool HasExportIgnoreAttribute(PropertyInfo property)
    {
        return property.GetCustomAttribute<AspireExportIgnoreAttribute>() != null;
    }

    /// <summary>
    /// Checks if a method has [AspireExportIgnore] attribute.
    /// </summary>
    private static bool HasExportIgnoreAttribute(MethodInfo method)
    {
        return method.GetCustomAttribute<AspireExportIgnoreAttribute>() != null;
    }

    /// <summary>
    /// Determines if a member should be exported based on visibility and attributes.
    /// Explicit [AspireExport] can export public + internal members.
    /// Auto-expose (ExposeMethods/ExposeProperties=true) only exports public members.
    /// </summary>
    private static bool ShouldExportMember(bool isPublic, bool exposeAll, AspireExportAttribute? exportAttr)
    {
        // Explicit [AspireExport] can export public + internal members
        if (exportAttr != null)
        {
            return true;
        }

        // Auto-expose only exports public members
        return exposeAll && isPublic;
    }

    /// <summary>
    /// Gets [AspireExport] attribute from a property (for member-level export).
    /// </summary>
    private static AspireExportAttribute? GetAspireExportAttribute(PropertyInfo property)
    {
        return property.GetCustomAttribute<AspireExportAttribute>();
    }

    /// <summary>
    /// Gets [AspireUnion] attribute from a parameter.
    /// </summary>
    private static AspireUnionAttribute? GetAspireUnionAttribute(ParameterInfo parameter)
    {
        return parameter.GetCustomAttribute<AspireUnionAttribute>();
    }

    /// <summary>
    /// Gets [AspireUnion] attribute from a property.
    /// </summary>
    private static AspireUnionAttribute? GetAspireUnionAttribute(PropertyInfo property)
    {
        return property.GetCustomAttribute<AspireUnionAttribute>();
    }

    /// <summary>
    /// Checks if a type has [AspireDto] attribute.
    /// </summary>
    private static bool HasAspireDtoAttribute(Type type)
    {
        return type.GetCustomAttribute<AspireDtoAttribute>() != null;
    }

    /// <summary>
    /// Creates a union type ref from an [AspireUnion] attribute.
    /// Throws if any type in the union is not a valid ATS type.
    /// </summary>
    private static AtsTypeRef CreateUnionTypeRef(
        AspireUnionAttribute unionAttr,
        string context)
    {
        if (unionAttr.Types.Length < 2)
        {
            throw new InvalidOperationException(
                $"[AspireUnion] on {context} has {unionAttr.Types.Length} type(s). Union must have at least 2 types.");
        }

        // Create type refs for each union member using the Types array directly
        var unionTypes = new List<AtsTypeRef>();
        foreach (var memberType in unionAttr.Types)
        {
            var typeRef = CreateTypeRef(memberType);
            if (typeRef == null)
            {
                var typeName = memberType.FullName ?? memberType.Name;
                throw new InvalidOperationException(
                    $"Type '{typeName}' in [AspireUnion] on {context} is not a valid ATS type. " +
                    $"Union members must be primitives, handles, DTOs, or collections thereof.");
            }
            unionTypes.Add(typeRef);
        }

        return new AtsTypeRef
        {
            TypeId = string.Join("|", unionTypes.Select(u => u.TypeId)),
            Category = AtsTypeCategory.Union,
            UnionTypes = unionTypes
        };
    }

    /// <summary>
    /// Converts a PascalCase property name to camelCase.
    /// </summary>
    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }
        return char.ToLowerInvariant(name[0]) + name[1..];
    }

}

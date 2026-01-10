// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Ats;

/// <summary>
/// Scans assemblies for [AspireExport] and [AspireContextType] attributes and creates capability models.
/// Uses the IAtsTypeInfo/IAtsMethodInfo abstraction to work with both runtime reflection and metadata reflection.
/// </summary>
internal static class AtsCapabilityScanner
{
    /// <summary>
    /// Result of scanning an assembly.
    /// </summary>
    internal sealed class ScanResult
    {
        public required List<AtsCapabilityInfo> Capabilities { get; init; }
        public required List<AtsTypeInfo> TypeInfos { get; init; }
        public List<AtsDiagnostic> Diagnostics { get; init; } = [];
    }

    /// <summary>
    /// Scans multiple assemblies for capabilities and type info.
    /// Uses 2-pass scanning:
    /// 1. Collect all capabilities and types from all assemblies (no expansion)
    /// 2. Expand using the complete type info set from all assemblies
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <param name="typeMapping">The type mapping for resolving ATS type IDs.</param>
    /// <param name="typeResolver">Optional resolver for checking type compatibility.</param>
    public static ScanResult ScanAssemblies(
        IEnumerable<IAtsAssemblyInfo> assemblies,
        AtsTypeMapping typeMapping,
        IAtsTypeResolver? typeResolver = null)
    {
        var allCapabilities = new List<AtsCapabilityInfo>();
        var allTypeInfos = new List<AtsTypeInfo>();
        var allDiagnostics = new List<AtsDiagnostic>();
        var seenCapabilityIds = new HashSet<string>();
        var seenTypeIds = new HashSet<string>();

        // Pass 1: Collect capabilities and types from all assemblies (no expansion)
        foreach (var assembly in assemblies)
        {
            var result = ScanAssemblyWithoutExpansion(assembly, typeMapping, typeResolver);

            // Merge capabilities, avoiding duplicates
            foreach (var capability in result.Capabilities)
            {
                if (seenCapabilityIds.Add(capability.CapabilityId))
                {
                    allCapabilities.Add(capability);
                }
            }

            // Merge type infos, avoiding duplicates
            foreach (var typeInfo in result.TypeInfos)
            {
                if (seenTypeIds.Add(typeInfo.AtsTypeId))
                {
                    allTypeInfos.Add(typeInfo);
                }
            }

            // Merge diagnostics
            allDiagnostics.AddRange(result.Diagnostics);
        }

        // Pass 2: Expand all capabilities using complete type info set
        ExpandCapabilityTargets(allCapabilities, allTypeInfos);

        // Detect method name collisions after expansion
        DetectMethodNameCollisions(allCapabilities);

        return new ScanResult
        {
            Capabilities = allCapabilities,
            TypeInfos = allTypeInfos,
            Diagnostics = allDiagnostics
        };
    }

    /// <summary>
    /// Scans an assembly for capabilities and type info.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <param name="typeMapping">The type mapping for resolving ATS type IDs.</param>
    /// <param name="typeResolver">Optional resolver for checking type compatibility.</param>
    public static ScanResult ScanAssembly(
        IAtsAssemblyInfo assembly,
        AtsTypeMapping typeMapping,
        IAtsTypeResolver? typeResolver = null)
    {
        // Single assembly scan with expansion
        var result = ScanAssemblyWithoutExpansion(assembly, typeMapping, typeResolver);

        // Expand interface targets to concrete types
        ExpandCapabilityTargets(result.Capabilities, result.TypeInfos);

        // Detect method name collisions after expansion
        DetectMethodNameCollisions(result.Capabilities);

        return result;
    }

    /// <summary>
    /// Internal method that scans an assembly without doing expansion.
    /// Used by both ScanAssembly and ScanAssemblies.
    /// </summary>
    private static ScanResult ScanAssemblyWithoutExpansion(
        IAtsAssemblyInfo assembly,
        AtsTypeMapping typeMapping,
        IAtsTypeResolver? typeResolver)
    {
        var capabilities = new List<AtsCapabilityInfo>();
        var typeInfos = new List<AtsTypeInfo>();
        var diagnostics = new List<AtsDiagnostic>();

        // Also collect resource types discovered from capability parameters
        // These are concrete types like TestRedisResource that appear in IResourceBuilder<T>
        var discoveredResourceTypes = new Dictionary<string, IAtsTypeInfo>();

        foreach (var type in assembly.GetTypes())
        {
            // Check for [AspireExport(AtsTypeId = "...")] on types
            var typeExportAttr = GetAspireExportAttribute(type);
            if (typeExportAttr != null)
            {
                var typeInfo = CreateTypeInfo(type, typeExportAttr, typeMapping);
                if (typeInfo != null)
                {
                    typeInfos.Add(typeInfo);
                }
            }

            // Check for [AspireExport(ExposeProperties = true)] or [AspireExport(ExposeMethods = true)]
            // Auto-generate property/method accessor capabilities
            if (HasExposePropertiesAttribute(type) || HasExposeMethodsAttribute(type))
            {
                // Member-level errors are captured inside CreateContextTypeCapabilities
                // and returned as diagnostics, allowing other members to be processed
                var result = CreateContextTypeCapabilities(type, assembly.Name, typeMapping, typeResolver);
                capabilities.AddRange(result.Capabilities);
                diagnostics.AddRange(result.Diagnostics);
            }

            // Scan static classes for [AspireExport] on methods
            if (!type.IsSealed || type.IsNested)
            {
                continue;
            }

            foreach (var method in type.GetMethods())
            {
                if (!method.IsStatic || !method.IsPublic)
                {
                    continue;
                }

                var exportAttr = GetAspireExportAttribute(method);
                if (exportAttr == null)
                {
                    continue;
                }

                try
                {
                    var capability = CreateCapabilityInfo(method, exportAttr, assembly.Name, typeMapping, typeResolver, out var capabilityDiagnostic);
                    if (capability != null)
                    {
                        capabilities.Add(capability);

                        // Collect resource types from capability parameters and return types
                        CollectResourceTypesFromCapability(method, typeMapping, discoveredResourceTypes);
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
                ? CollectAllInterfaces(resourceType, typeMapping)
                : [];
            var baseTypeHierarchy = !isInterface
                ? CollectBaseTypeHierarchy(resourceType, typeMapping)
                : [];

            typeInfos.Add(new AtsTypeInfo
            {
                AtsTypeId = typeId,
                ClrTypeName = resourceType.FullName,
                IsInterface = isInterface,
                ImplementedInterfaces = implementedInterfaces,
                BaseTypeHierarchy = baseTypeHierarchy
            });
        }

        // Note: Expansion and collision detection are done by the calling method
        // (ScanAssembly or ScanAssemblies) after all assemblies are processed

        return new ScanResult
        {
            Capabilities = capabilities,
            TypeInfos = typeInfos,
            Diagnostics = diagnostics
        };
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
            var originalTarget = capability.OriginalTargetTypeId;
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
    /// Detects method name collisions after capability expansion.
    /// Since ATS doesn't support method overloading, each (TargetTypeId, MethodName) pair must be unique.
    /// </summary>
    private static void DetectMethodNameCollisions(List<AtsCapabilityInfo> capabilities)
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
            var errors = collisions.Select(g =>
            {
                var conflictingIds = string.Join(", ", g.Select(x => x.Capability.CapabilityId));
                return $"Method '{g.Key.MethodName}' has multiple definitions for target '{g.Key.Target}': {conflictingIds}";
            });

            throw new InvalidOperationException(
                $"ATS method name collision detected. Method names must be unique per target type.\n" +
                string.Join("\n", errors) +
                "\n\nTo resolve: Use [AspireExport(MethodName = \"uniqueName\")] to disambiguate.");
        }
    }

    /// <summary>
    /// Scans an assembly and returns only the capabilities.
    /// </summary>
    public static List<AtsCapabilityInfo> ScanCapabilities(
        IAtsAssemblyInfo assembly,
        AtsTypeMapping typeMapping,
        IAtsTypeResolver? typeResolver = null)
    {
        return ScanAssembly(assembly, typeMapping, typeResolver).Capabilities;
    }

    private static AtsTypeInfo? CreateTypeInfo(
        IAtsTypeInfo type,
        IAtsAttributeInfo exportAttr,
        AtsTypeMapping typeMapping)
    {
        // Get the AtsTypeId from named arguments
        if (!exportAttr.NamedArguments.TryGetValue("AtsTypeId", out var atsTypeIdObj) ||
            atsTypeIdObj is not string atsTypeId)
        {
            return null;
        }

        // Collect ALL implemented interfaces (for concrete types only)
        // Use recursive collection to include inherited interfaces
        var implementedInterfaces = !type.IsInterface
            ? CollectAllInterfaces(type, typeMapping)
            : [];

        // Collect base type hierarchy (for concrete types only)
        // This enables expansion from base types to derived types
        var baseTypeHierarchy = !type.IsInterface
            ? CollectBaseTypeHierarchy(type, typeMapping)
            : [];

        return new AtsTypeInfo
        {
            AtsTypeId = atsTypeId,
            ClrTypeName = type.FullName,
            IsInterface = type.IsInterface,
            ImplementedInterfaces = implementedInterfaces,
            BaseTypeHierarchy = baseTypeHierarchy
        };
    }

    /// <summary>
    /// Result of creating context type capabilities, including any member-level diagnostics.
    /// </summary>
    internal sealed class ContextTypeCapabilitiesResult
    {
        public required List<AtsCapabilityInfo> Capabilities { get; init; }
        public List<AtsDiagnostic> Diagnostics { get; init; } = [];
    }

    private static ContextTypeCapabilitiesResult CreateContextTypeCapabilities(
        IAtsTypeInfo contextType,
        string assemblyName,
        AtsTypeMapping typeMapping,
        IAtsTypeResolver? typeResolver)
    {
        var capabilities = new List<AtsCapabilityInfo>();
        var diagnostics = new List<AtsDiagnostic>();

        // Derive the type ID from assembly name and full type name
        var typeName = contextType.Name;
        var typeId = AtsTypeMapping.DeriveTypeId(contextType.AssemblyName ?? assemblyName, contextType.FullName);

        // Extract the package (namespace) from the full type name for capability IDs
        var lastDot = contextType.FullName.LastIndexOf('.');
        var package = lastDot >= 0 ? contextType.FullName[..lastDot] : assemblyName;

        // Check for ExposeProperties and ExposeMethods flags
        var exposeAllProperties = HasExposePropertiesAttribute(contextType);
        var exposeAllMethods = HasExposeMethodsAttribute(contextType);

        // Scan properties
        foreach (var property in contextType.GetProperties())
        {
            if (property.IsStatic)
            {
                continue;
            }

            // Check for [AspireExportIgnore]
            if (HasExportIgnoreAttribute(property))
            {
                continue;
            }

            // Check if property should be exported (either via ExposeProperties=true or member-level [AspireExport])
            var memberExportAttr = GetAspireExportAttribute(property);
            if (!exposeAllProperties && memberExportAttr == null)
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
                var isDictWithObjectValue =
                    (propType.GenericTypeDefinitionFullName == "System.Collections.Generic.Dictionary`2" ||
                     propType.GenericTypeDefinitionFullName == "System.Collections.Generic.IDictionary`2" ||
                     propType.FullName.StartsWith("System.Collections.Generic.Dictionary`2") ||
                     propType.FullName.StartsWith("System.Collections.Generic.IDictionary`2")) &&
                    propType.GetGenericArguments().Skip(1).FirstOrDefault()?.FullName == "System.Object";

                if (isDictWithObjectValue)
                {
                    // Create dictionary type - use union if [AspireUnion] is present, otherwise use 'any'
                    var keyTypeRef = CreateTypeRef(propType.GetGenericArguments().First(), typeMapping, typeResolver);
                    if (keyTypeRef != null)
                    {
                        var valueTypeRef = propertyUnionAttr != null
                            ? CreateUnionTypeRef(propertyUnionAttr, $"property '{property.Name}'", typeMapping)
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
                else if (propType.FullName == "System.Object")
                {
                    // Use union if [AspireUnion] is present, otherwise use 'any'
                    if (propertyUnionAttr != null)
                    {
                        propertyTypeRef = CreateUnionTypeRef(propertyUnionAttr, $"property '{property.Name}'", typeMapping);
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
                    propertyTypeRef = CreateTypeRef(propType, typeMapping, typeResolver);
                    propertyTypeId = MapToAtsTypeId(propType, typeMapping, typeResolver);
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
                    Category = AtsTypeCategory.Handle,
                    IsInterface = contextType.IsInterface
                };

                // Get custom method name from attribute if specified
                var customMethodName = memberExportAttr?.NamedArguments.TryGetValue("Id", out var idObj) == true
                    ? idObj as string
                    : null;

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
                        MethodName = getMethodName,
                        Package = package,
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
                        ReturnType = propertyTypeRef,
                        IsExtensionMethod = false,
                        OriginalTargetTypeId = typeId,
                        TargetType = contextTypeRef,
                        ReturnsBuilder = false,
                        CapabilityKind = AtsCapabilityKind.PropertyGetter,
                        OwningTypeName = typeName,
                        SourceProperty = property
                    });
                }

                // Generate setter capability if property is writable
                // Naming: {TypeName}.set{PropertyName} (keep "set" prefix, PascalCase property name)
                if (property.CanWrite)
                {
                    var setMethodName = $"{typeName}.set{property.Name}";
                    var setCapabilityId = $"{package}/{setMethodName}";

                    capabilities.Add(new AtsCapabilityInfo
                    {
                        CapabilityId = setCapabilityId,
                        MethodName = setMethodName,
                        Package = package,
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
                                Type = propertyTypeRef,
                                IsOptional = false,
                                IsNullable = false,
                                IsCallback = false,
                                DefaultValue = null
                            }
                        ],
                        ReturnType = contextTypeRef,
                        IsExtensionMethod = false,
                        OriginalTargetTypeId = typeId,
                        TargetType = contextTypeRef,
                        ReturnsBuilder = false,
                        CapabilityKind = AtsCapabilityKind.PropertySetter,
                        OwningTypeName = typeName,
                        SourceProperty = property
                    });
                }
            }
            catch (InvalidOperationException ex)
            {
                // Property-level error - record diagnostic and continue with other properties
                diagnostics.Add(AtsDiagnostic.Error(ex.Message, $"{contextType.FullName}.{property.Name}"));
            }
        }

        // Scan instance methods if ExposeMethods is true
        if (exposeAllMethods)
        {
            // Create context type ref once for all methods
            var instanceContextTypeRef = new AtsTypeRef
            {
                TypeId = typeId,
                Category = AtsTypeCategory.Handle,
                IsInterface = contextType.IsInterface
            };

            foreach (var method in contextType.GetMethods())
            {
                // Skip static methods, non-public, and special methods
                if (method.IsStatic || !method.IsPublic)
                {
                    continue;
                }

                // Skip property accessors and special runtime methods
                if (method.Name.StartsWith("get_") || method.Name.StartsWith("set_") ||
                    method.Name == "GetType" || method.Name == "ToString" ||
                    method.Name == "Equals" || method.Name == "GetHashCode")
                {
                    continue;
                }

                // Check for [AspireExportIgnore]
                if (HasExportIgnoreAttribute(method))
                {
                    continue;
                }

                // Wrap individual method processing in try/catch to capture member-level errors
                try
                {
                    // Generate method capability
                    var camelCaseMethodName = ToCamelCase(method.Name);
                    var methodCapabilityName = $"{typeName}.{camelCaseMethodName}";
                    var methodCapabilityId = $"{package}/{methodCapabilityName}";

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
                        var paramInfo = CreateParameterInfo(param, paramIndex, typeMapping, typeResolver);
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
                    var returnTypeRef = CreateTypeRef(method.ReturnType, typeMapping, typeResolver);

                    capabilities.Add(new AtsCapabilityInfo
                    {
                        CapabilityId = methodCapabilityId,
                        MethodName = methodCapabilityName,
                        Package = package,
                        Description = $"Invokes the {method.Name} method",
                        Parameters = paramInfos,
                        ReturnType = returnTypeRef,
                        IsExtensionMethod = false,
                        OriginalTargetTypeId = typeId,
                        TargetType = instanceContextTypeRef,
                        ReturnsBuilder = false,
                        CapabilityKind = AtsCapabilityKind.InstanceMethod,
                        OwningTypeName = typeName,
                        SourceMethod = method
                    });
                }
                catch (InvalidOperationException ex)
                {
                    // Method-level error - record diagnostic and continue with other methods
                    diagnostics.Add(AtsDiagnostic.Error(ex.Message, $"{contextType.FullName}.{method.Name}"));
                }
            }
        }

        return new ContextTypeCapabilitiesResult
        {
            Capabilities = capabilities,
            Diagnostics = diagnostics
        };
    }

    private static AtsCapabilityInfo? CreateCapabilityInfo(
        IAtsMethodInfo method,
        IAtsAttributeInfo exportAttr,
        string assemblyName,
        AtsTypeMapping typeMapping,
        IAtsTypeResolver? typeResolver,
        out AtsDiagnostic? diagnostic)
    {
        diagnostic = null;
        var methodLocation = method.Name;

        // Get method name from first constructor argument (new format: just the method name)
        if (exportAttr.FixedArguments.Count == 0 || exportAttr.FixedArguments[0] is not string methodNameFromAttr)
        {
            diagnostic = AtsDiagnostic.Warning(
                $"[AspireExport] attribute on '{methodLocation}' is missing method name argument",
                methodLocation);
            return null;
        }

        // Get named arguments
        var description = exportAttr.NamedArguments.TryGetValue("Description", out var desc) ? desc as string : null;
        var methodNameOverride = exportAttr.NamedArguments.TryGetValue("MethodName", out var mn) ? mn as string : null;

        var methodName = methodNameOverride ?? methodNameFromAttr;
        // New format: {AssemblyName}/{methodName}
        var capabilityId = $"{assemblyName}/{methodNameFromAttr}";
        var package = assemblyName;

        // Check if extension method
        var parameters = method.GetParameters().ToList();
        var isExtensionMethod = HasExtensionAttribute(method) && parameters.Count > 0;

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

            extendsTypeRef = CreateTypeRef(firstParamType, typeMapping, typeResolver);
            var firstParamTypeId = extendsTypeRef?.TypeId ?? MapToAtsTypeId(firstParamType, typeMapping, typeResolver);
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
            var paramInfo = CreateParameterInfo(param, paramIndex, typeMapping, typeResolver);
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
        var returnTypeRef = CreateTypeRef(method.ReturnType, typeMapping, typeResolver);
        var returnTypeId = MapToAtsTypeId(method.ReturnType, typeMapping, typeResolver);

        // Only set ReturnsBuilder if the return type is actually a resource builder type
        var returnsBuilder = returnTypeId != null &&
            typeResolver != null &&
            typeResolver.IsResourceBuilderType(method.ReturnType);

        return new AtsCapabilityInfo
        {
            CapabilityId = capabilityId,
            MethodName = methodName,
            Package = package,
            Description = description,
            Parameters = paramInfos,
            ReturnType = returnTypeRef,
            IsExtensionMethod = isExtensionMethod,
            OriginalTargetTypeId = extendsTypeId,
            TargetType = extendsTypeRef,
            TargetParameterName = targetParameterName,
            ReturnsBuilder = returnsBuilder,
            SourceMethod = method // Store source method for runtime dispatch
        };
    }

    private static AtsParameterInfo? CreateParameterInfo(
        IAtsParameterInfo param,
        int paramIndex,
        AtsTypeMapping typeMapping,
        IAtsTypeResolver? typeResolver)
    {
        var paramType = param.ParameterType;
        var paramName = string.IsNullOrEmpty(param.Name) ? $"arg{paramIndex}" : param.Name;

        // Check for [AspireUnion] attribute on the parameter
        var unionAttr = GetAspireUnionAttribute(param);
        if (unionAttr != null)
        {
            // Create union type from attribute
            var unionTypeRef = CreateUnionTypeRef(unionAttr, $"parameter '{paramName}'", typeMapping);
            return new AtsParameterInfo
            {
                Name = paramName,
                Type = unionTypeRef,
                IsOptional = param.IsOptional,
                IsNullable = false,
                IsCallback = false,
                DefaultValue = param.DefaultValue
            };
        }

        // Check if this is a delegate type (callbacks are inferred from delegate types)
        var isCallback = IsDelegateType(paramType);

        // Create type reference
        var typeRef = CreateTypeRef(paramType, typeMapping, typeResolver);

        // Map the type - return null if unmapped (unless it's a callback)
        var atsTypeId = MapToAtsTypeId(paramType, typeMapping, typeResolver);
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
            (callbackParameters, callbackReturnType) = ExtractCallbackSignature(paramType, typeMapping, typeResolver);
        }

        // Check if nullable (Nullable<T>)
        var isNullable = paramType.GenericTypeDefinitionFullName == "System.Nullable`1" ||
                         param.TypeFullName.StartsWith("System.Nullable`1");

        // For callbacks, create a callback type ref
        var finalTypeRef = isCallback
            ? new AtsTypeRef { TypeId = "callback", Category = AtsTypeCategory.Callback }
            : typeRef;

        return new AtsParameterInfo
        {
            Name = string.IsNullOrEmpty(param.Name) ? $"arg{paramIndex}" : param.Name,
            Type = finalTypeRef,
            IsOptional = param.IsOptional,
            IsNullable = isNullable,
            IsCallback = isCallback,
            CallbackParameters = callbackParameters,
            CallbackReturnType = callbackReturnType,
            DefaultValue = param.DefaultValue
        };
    }

    /// <summary>
    /// Checks if a type is a delegate type.
    /// </summary>
    private static bool IsDelegateType(IAtsTypeInfo type)
    {
        // Check base type hierarchy for System.MulticastDelegate
        var baseType = type.BaseTypeFullName;
        while (baseType != null)
        {
            if (baseType == "System.MulticastDelegate")
            {
                return true;
            }
            // For abstraction, we can't walk further up the hierarchy
            // but MulticastDelegate is the direct base for all delegates
            break;
        }
        return false;
    }

    /// <summary>
    /// Extracts the callback signature (parameters and return type) from a delegate type.
    /// </summary>
    private static (IReadOnlyList<AtsCallbackParameterInfo>? Parameters, AtsTypeRef? ReturnType) ExtractCallbackSignature(
        IAtsTypeInfo delegateType,
        AtsTypeMapping typeMapping,
        IAtsTypeResolver? typeResolver)
    {
        // Find the Invoke method on the delegate type
        var invokeMethod = delegateType.GetMethods().FirstOrDefault(m => m.Name == "Invoke");
        if (invokeMethod is null)
        {
            // Fallback for well-known delegate types when Invoke method isn't available
            // (e.g., when loading from reference assemblies without full type definitions)
            return ExtractWellKnownDelegateSignature(delegateType, typeMapping, typeResolver);
        }

        // Extract parameters
        var parameters = new List<AtsCallbackParameterInfo>();
        foreach (var param in invokeMethod.GetParameters())
        {
            var paramType = param.ParameterType;
            var paramTypeRef = CreateTypeRef(paramType, typeMapping, typeResolver);
            if (paramTypeRef != null)
            {
                parameters.Add(new AtsCallbackParameterInfo
                {
                    Name = param.Name,
                    Type = paramTypeRef
                });
            }
        }

        // Extract return type
        var returnTypeFullName = invokeMethod.ReturnTypeFullName;
        AtsTypeRef? returnTypeRef;

        if (returnTypeFullName == "System.Void")
        {
            returnTypeRef = new AtsTypeRef { TypeId = AtsConstants.Void, Category = AtsTypeCategory.Primitive };
        }
        else if (returnTypeFullName == "System.Threading.Tasks.Task")
        {
            returnTypeRef = new AtsTypeRef { TypeId = AtsConstants.Void, Category = AtsTypeCategory.Primitive };
        }
        else if (returnTypeFullName.StartsWith("System.Threading.Tasks.Task`1"))
        {
            // Task<T> - get the inner type
            var innerType = invokeMethod.ReturnType.GetGenericArguments().FirstOrDefault();
            returnTypeRef = innerType is not null
                ? CreateTypeRef(innerType, typeMapping, typeResolver)
                    ?? new AtsTypeRef { TypeId = AtsConstants.Void, Category = AtsTypeCategory.Primitive }
                : new AtsTypeRef { TypeId = AtsConstants.Void, Category = AtsTypeCategory.Primitive };
        }
        else
        {
            returnTypeRef = CreateTypeRef(invokeMethod.ReturnType, typeMapping, typeResolver)
                ?? new AtsTypeRef { TypeId = AtsConstants.Void, Category = AtsTypeCategory.Primitive };
        }

        return (parameters, returnTypeRef);
    }

    /// <summary>
    /// Extracts signature from well-known delegate types based on their generic type definition.
    /// Used as fallback when the Invoke method isn't available from metadata.
    /// </summary>
    private static (IReadOnlyList<AtsCallbackParameterInfo>? Parameters, AtsTypeRef? ReturnType) ExtractWellKnownDelegateSignature(
        IAtsTypeInfo delegateType,
        AtsTypeMapping typeMapping,
        IAtsTypeResolver? typeResolver)
    {
        var genericDefFullName = delegateType.GenericTypeDefinitionFullName;
        if (string.IsNullOrEmpty(genericDefFullName))
        {
            return (null, null);
        }

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
                var paramTypeRef = CreateTypeRef(paramType, typeMapping, typeResolver);
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
                var paramTypeRef = CreateTypeRef(paramType, typeMapping, typeResolver);
                if (paramTypeRef != null)
                {
                    parameters.Add(new AtsCallbackParameterInfo
                    {
                        Name = $"arg{i}",
                        Type = paramTypeRef
                    });
                }
            }

            var returnType = genericArgs[^1];
            var returnTypeFullName = returnType.FullName;
            AtsTypeRef returnTypeRef;

            if (returnTypeFullName == "System.Void")
            {
                returnTypeRef = voidTypeRef;
            }
            else if (returnTypeFullName == "System.Threading.Tasks.Task")
            {
                returnTypeRef = voidTypeRef;
            }
            else if (returnTypeFullName.StartsWith("System.Threading.Tasks.Task`1"))
            {
                // Task<T> - get the inner type
                var innerType = returnType.GetGenericArguments().FirstOrDefault();
                returnTypeRef = innerType is not null
                    ? CreateTypeRef(innerType, typeMapping, typeResolver) ?? voidTypeRef
                    : voidTypeRef;
            }
            else
            {
                returnTypeRef = CreateTypeRef(returnType, typeMapping, typeResolver) ?? voidTypeRef;
            }

            return (parameters, returnTypeRef);
        }

        return (null, null);
    }

    /// <summary>
    /// Maps a CLR type to an ATS type ID.
    /// All type mapping logic is centralized here.
    /// </summary>
    public static string? MapToAtsTypeId(
        IAtsTypeInfo? type,
        AtsTypeMapping typeMapping,
        IAtsTypeResolver? typeResolver)
    {
        if (type == null)
        {
            return null;
        }

        var typeFullName = type.FullName;
        if (string.IsNullOrEmpty(typeFullName))
        {
            return null;
        }

        // Handle void
        if (typeFullName == "System.Void")
        {
            return null;
        }

        // Handle Task (async void)
        if (typeFullName == "System.Threading.Tasks.Task")
        {
            return null;
        }

        // Handle Task<T> - extract T using the type's generic arguments
        if (type.GenericTypeDefinitionFullName == "System.Threading.Tasks.Task`1" ||
            typeFullName.StartsWith("System.Threading.Tasks.Task`1"))
        {
            var genericArgs = type.GetGenericArguments().ToList();
            if (genericArgs.Count > 0)
            {
                return MapToAtsTypeId(genericArgs[0], typeMapping, typeResolver);
            }
        }

        // Handle primitives
        if (typeFullName == "System.String")
        {
            return AtsConstants.String;
        }
        if (typeFullName == "System.Char")
        {
            return AtsConstants.Char;
        }
        if (typeFullName == "System.Boolean")
        {
            return AtsConstants.Boolean;
        }
        if (typeFullName is "System.Int32" or "System.Int64" or "System.Double" or
            "System.Single" or "System.Int16" or "System.Byte" or "System.Decimal" or
            "System.UInt16" or "System.UInt32" or "System.UInt64" or "System.SByte")
        {
            return AtsConstants.Number;
        }

        // Handle date/time types
        if (typeFullName == "System.DateTime")
        {
            return AtsConstants.DateTime;
        }
        if (typeFullName == "System.DateTimeOffset")
        {
            return AtsConstants.DateTimeOffset;
        }
        if (typeFullName == "System.DateOnly")
        {
            return AtsConstants.DateOnly;
        }
        if (typeFullName == "System.TimeOnly")
        {
            return AtsConstants.TimeOnly;
        }
        if (typeFullName == "System.TimeSpan")
        {
            return AtsConstants.TimeSpan;
        }

        // Handle other scalar types
        if (typeFullName == "System.Guid")
        {
            return AtsConstants.Guid;
        }
        if (typeFullName == "System.Uri")
        {
            return AtsConstants.Uri;
        }

        // Handle object type - maps to 'any' in TypeScript
        if (typeFullName == "System.Object")
        {
            return AtsConstants.Any;
        }

        // Handle enum types
        if (type.IsEnum)
        {
            return AtsConstants.EnumTypeId(typeFullName);
        }

        // Handle Nullable<T>
        if (type.GenericTypeDefinitionFullName == "System.Nullable`1" ||
            typeFullName.StartsWith("System.Nullable`1"))
        {
            var genericArgs = type.GetGenericArguments().ToList();
            if (genericArgs.Count > 0)
            {
                return MapToAtsTypeId(genericArgs[0], typeMapping, typeResolver);
            }
        }

        // Handle Dictionary<K,V> - mutable dictionary, return as Dict handle
        if (type.GenericTypeDefinitionFullName == "System.Collections.Generic.Dictionary`2" ||
            type.GenericTypeDefinitionFullName == "System.Collections.Generic.IDictionary`2" ||
            typeFullName.StartsWith("System.Collections.Generic.Dictionary`2") ||
            typeFullName.StartsWith("System.Collections.Generic.IDictionary`2"))
        {
            var genericArgs = type.GetGenericArguments().ToList();
            if (genericArgs.Count == 2)
            {
                var keyTypeName = genericArgs[0].Name;
                var valueTypeName = genericArgs[1].Name;
                return AtsConstants.DictTypeId(keyTypeName, valueTypeName);
            }
        }

        // Handle IReadOnlyDictionary<K,V> - immutable, return as regular object (serialized copy)
        if (type.GenericTypeDefinitionFullName == "System.Collections.Generic.IReadOnlyDictionary`2" ||
            typeFullName.StartsWith("System.Collections.Generic.IReadOnlyDictionary`2"))
        {
            return "object"; // Serialized as JSON object copy
        }

        // Handle List<T> - mutable list, return as List handle
        if (type.GenericTypeDefinitionFullName == "System.Collections.Generic.List`1" ||
            type.GenericTypeDefinitionFullName == "System.Collections.Generic.IList`1" ||
            typeFullName.StartsWith("System.Collections.Generic.List`1") ||
            typeFullName.StartsWith("System.Collections.Generic.IList`1"))
        {
            var genericArgs = type.GetGenericArguments().ToList();
            if (genericArgs.Count == 1)
            {
                var elementTypeName = genericArgs[0].Name;
                return AtsConstants.ListTypeId(elementTypeName);
            }
        }

        // Handle IReadOnlyList<T>, IReadOnlyCollection<T> - immutable, return as array (serialized copy)
        if (type.GenericTypeDefinitionFullName == "System.Collections.Generic.IReadOnlyList`1" ||
            type.GenericTypeDefinitionFullName == "System.Collections.Generic.IReadOnlyCollection`1" ||
            typeFullName.StartsWith("System.Collections.Generic.IReadOnlyList`1") ||
            typeFullName.StartsWith("System.Collections.Generic.IReadOnlyCollection`1"))
        {
            var genericArgs = type.GetGenericArguments().ToList();
            if (genericArgs.Count == 1)
            {
                var elementTypeId = MapToAtsTypeId(genericArgs[0], typeMapping, typeResolver);
                // Only export if element type is a known ATS type
                return elementTypeId != null ? $"{elementTypeId}[]" : null;
            }
        }

        // Handle arrays - return as typed array (serialized copy)
        if (type.IsArray)
        {
            var elementType = type.GetElementType();
            if (elementType != null)
            {
                var elementTypeId = MapToAtsTypeId(elementType, typeMapping, typeResolver);
                // Only export if element type is a known ATS type
                return elementTypeId != null ? $"{elementTypeId}[]" : null;
            }
            return null;
        }

        // Handle IResourceBuilder<T> - use resolver if available for accurate type checking
        if (typeResolver != null && typeResolver.TryGetResourceBuilderTypeArgument(type, out var resourceType) && resourceType != null)
        {
            // If T is a generic parameter, use its constraint type instead
            if (resourceType.IsGenericParameter)
            {
                var constraints = resourceType.GetGenericParameterConstraintFullNames().ToList();
                if (constraints.Count > 0)
                {
                    return typeMapping.GetTypeId(constraints[0]) ?? InferResourceTypeId(constraints[0]);
                }
            }
            return typeMapping.GetTypeId(resourceType) ?? InferResourceTypeId(resourceType);
        }

        // Fallback: Check by type name for IResourceBuilder<T>
        if (type.GenericTypeDefinitionFullName == "Aspire.Hosting.ApplicationModel.IResourceBuilder`1" ||
            typeFullName.StartsWith("Aspire.Hosting.ApplicationModel.IResourceBuilder`1"))
        {
            var genericArgs = type.GetGenericArguments().ToList();
            if (genericArgs.Count > 0)
            {
                var resType = genericArgs[0];

                // If T is a generic parameter (e.g., in WithBindMount<T>(...) where T : ContainerResource),
                // use the constraint type instead of just "T"
                if (resType.IsGenericParameter)
                {
                    var constraints = resType.GetGenericParameterConstraintFullNames().ToList();
                    if (constraints.Count > 0)
                    {
                        // Use the first constraint (e.g., ContainerResource)
                        var constraintTypeId = typeMapping.GetTypeId(constraints[0]) ?? InferResourceTypeId(constraints[0]);
                        return constraintTypeId;
                    }
                }

                return typeMapping.GetTypeId(resType) ?? InferResourceTypeId(resType);
            }
        }

        // Try explicit mapping
        var typeId = typeMapping.GetTypeId(typeFullName);
        if (typeId != null)
        {
            return typeId;
        }

        // Handle arrays
        if (type.IsArray)
        {
            var elementTypeFullName = type.GetElementTypeFullName();
            if (elementTypeFullName != null)
            {
                // For arrays, we need to get element type but don't have IAtsTypeInfo
                // Fall back to string-based inference
                var elementId = typeMapping.GetTypeId(elementTypeFullName) ?? InferTypeId(elementTypeFullName);
                return $"{elementId}[]";
            }
        }

        // No mapping found - return null to indicate unmapped type
        return null;
    }

    /// <summary>
    /// Creates an AtsTypeRef from a CLR type with full type metadata.
    /// </summary>
    public static AtsTypeRef? CreateTypeRef(
        IAtsTypeInfo? type,
        AtsTypeMapping typeMapping,
        IAtsTypeResolver? typeResolver)
    {
        if (type == null)
        {
            return null;
        }

        var typeFullName = type.FullName;
        if (string.IsNullOrEmpty(typeFullName))
        {
            return null;
        }

        // Handle void - no type ref
        if (typeFullName == "System.Void")
        {
            return null;
        }

        // Handle Task (async void) - no type ref
        if (typeFullName == "System.Threading.Tasks.Task")
        {
            return null;
        }

        // Handle Task<T> - unwrap to inner type
        if (type.GenericTypeDefinitionFullName == "System.Threading.Tasks.Task`1" ||
            typeFullName.StartsWith("System.Threading.Tasks.Task`1"))
        {
            var genericArgs = type.GetGenericArguments().ToList();
            if (genericArgs.Count > 0)
            {
                return CreateTypeRef(genericArgs[0], typeMapping, typeResolver);
            }
            return null;
        }

        // Handle Nullable<T> - unwrap to inner type
        if (type.GenericTypeDefinitionFullName == "System.Nullable`1" ||
            typeFullName.StartsWith("System.Nullable`1"))
        {
            var genericArgs = type.GetGenericArguments().ToList();
            if (genericArgs.Count > 0)
            {
                return CreateTypeRef(genericArgs[0], typeMapping, typeResolver);
            }
            return null;
        }

        // Handle primitives
        if (typeFullName == "System.String")
        {
            return new AtsTypeRef { TypeId = AtsConstants.String, Category = AtsTypeCategory.Primitive };
        }
        if (typeFullName == "System.Char")
        {
            return new AtsTypeRef { TypeId = AtsConstants.Char, Category = AtsTypeCategory.Primitive };
        }
        if (typeFullName == "System.Boolean")
        {
            return new AtsTypeRef { TypeId = AtsConstants.Boolean, Category = AtsTypeCategory.Primitive };
        }
        if (typeFullName is "System.Int32" or "System.Int64" or "System.Double" or
            "System.Single" or "System.Int16" or "System.Byte" or "System.Decimal" or
            "System.UInt16" or "System.UInt32" or "System.UInt64" or "System.SByte")
        {
            return new AtsTypeRef { TypeId = AtsConstants.Number, Category = AtsTypeCategory.Primitive };
        }

        // Handle date/time types
        if (typeFullName == "System.DateTime")
        {
            return new AtsTypeRef { TypeId = AtsConstants.DateTime, Category = AtsTypeCategory.Primitive };
        }
        if (typeFullName == "System.DateTimeOffset")
        {
            return new AtsTypeRef { TypeId = AtsConstants.DateTimeOffset, Category = AtsTypeCategory.Primitive };
        }
        if (typeFullName == "System.DateOnly")
        {
            return new AtsTypeRef { TypeId = AtsConstants.DateOnly, Category = AtsTypeCategory.Primitive };
        }
        if (typeFullName == "System.TimeOnly")
        {
            return new AtsTypeRef { TypeId = AtsConstants.TimeOnly, Category = AtsTypeCategory.Primitive };
        }
        if (typeFullName == "System.TimeSpan")
        {
            return new AtsTypeRef { TypeId = AtsConstants.TimeSpan, Category = AtsTypeCategory.Primitive };
        }

        // Handle object type - maps to 'any' in TypeScript
        // This is commonly used in Dictionary<string, object> for environment variables
        if (typeFullName == "System.Object")
        {
            return new AtsTypeRef { TypeId = AtsConstants.Any, Category = AtsTypeCategory.Primitive };
        }

        // Handle other scalar types
        if (typeFullName == "System.Guid")
        {
            return new AtsTypeRef { TypeId = AtsConstants.Guid, Category = AtsTypeCategory.Primitive };
        }
        if (typeFullName == "System.Uri")
        {
            return new AtsTypeRef { TypeId = AtsConstants.Uri, Category = AtsTypeCategory.Primitive };
        }

        // Handle enum types
        if (type.IsEnum)
        {
            return new AtsTypeRef
            {
                TypeId = AtsConstants.EnumTypeId(typeFullName),
                Category = AtsTypeCategory.Primitive
            };
        }

        // Handle Dictionary<K,V> - mutable dictionary
        if (type.GenericTypeDefinitionFullName == "System.Collections.Generic.Dictionary`2" ||
            type.GenericTypeDefinitionFullName == "System.Collections.Generic.IDictionary`2" ||
            typeFullName.StartsWith("System.Collections.Generic.Dictionary`2") ||
            typeFullName.StartsWith("System.Collections.Generic.IDictionary`2"))
        {
            var genericArgs = type.GetGenericArguments().ToList();
            if (genericArgs.Count == 2)
            {
                var keyTypeRef = CreateTypeRef(genericArgs[0], typeMapping, typeResolver);
                var valueTypeRef = CreateTypeRef(genericArgs[1], typeMapping, typeResolver);
                if (keyTypeRef != null && valueTypeRef != null)
                {
                    return new AtsTypeRef
                    {
                        TypeId = AtsConstants.DictTypeId(keyTypeRef.TypeId, valueTypeRef.TypeId),
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
        if (type.GenericTypeDefinitionFullName == "System.Collections.Generic.IReadOnlyDictionary`2" ||
            typeFullName.StartsWith("System.Collections.Generic.IReadOnlyDictionary`2"))
        {
            var genericArgs = type.GetGenericArguments().ToList();
            if (genericArgs.Count == 2)
            {
                var keyTypeRef = CreateTypeRef(genericArgs[0], typeMapping, typeResolver);
                var valueTypeRef = CreateTypeRef(genericArgs[1], typeMapping, typeResolver);
                if (keyTypeRef != null && valueTypeRef != null)
                {
                    return new AtsTypeRef
                    {
                        TypeId = AtsConstants.DictTypeId(keyTypeRef.TypeId, valueTypeRef.TypeId),
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
        if (type.GenericTypeDefinitionFullName == "System.Collections.Generic.List`1" ||
            type.GenericTypeDefinitionFullName == "System.Collections.Generic.IList`1" ||
            typeFullName.StartsWith("System.Collections.Generic.List`1") ||
            typeFullName.StartsWith("System.Collections.Generic.IList`1"))
        {
            var genericArgs = type.GetGenericArguments().ToList();
            if (genericArgs.Count == 1)
            {
                var elementTypeRef = CreateTypeRef(genericArgs[0], typeMapping, typeResolver);
                if (elementTypeRef != null)
                {
                    return new AtsTypeRef
                    {
                        TypeId = AtsConstants.ListTypeId(elementTypeRef.TypeId),
                        Category = AtsTypeCategory.List,
                        ElementType = elementTypeRef
                    };
                }
            }
            return null;
        }

        // Handle IReadOnlyList<T>, IReadOnlyCollection<T> - immutable (serialized copy as array)
        if (type.GenericTypeDefinitionFullName == "System.Collections.Generic.IReadOnlyList`1" ||
            type.GenericTypeDefinitionFullName == "System.Collections.Generic.IReadOnlyCollection`1" ||
            typeFullName.StartsWith("System.Collections.Generic.IReadOnlyList`1") ||
            typeFullName.StartsWith("System.Collections.Generic.IReadOnlyCollection`1"))
        {
            var genericArgs = type.GetGenericArguments().ToList();
            if (genericArgs.Count == 1)
            {
                var elementTypeRef = CreateTypeRef(genericArgs[0], typeMapping, typeResolver);
                if (elementTypeRef != null)
                {
                    return new AtsTypeRef
                    {
                        TypeId = AtsConstants.ArrayTypeId(elementTypeRef.TypeId),
                        Category = AtsTypeCategory.Array,
                        ElementType = elementTypeRef,
                        IsReadOnly = true
                    };
                }
            }
            return null;
        }

        // Handle arrays - serialized copy
        if (type.IsArray)
        {
            var elementType = type.GetElementType();
            if (elementType != null)
            {
                var elementTypeRef = CreateTypeRef(elementType, typeMapping, typeResolver);
                if (elementTypeRef != null)
                {
                    return new AtsTypeRef
                    {
                        TypeId = AtsConstants.ArrayTypeId(elementTypeRef.TypeId),
                        Category = AtsTypeCategory.Array,
                        ElementType = elementTypeRef,
                        IsReadOnly = true
                    };
                }
            }
            return null;
        }

        // Handle IResourceBuilder<T>
        if (typeResolver != null && typeResolver.TryGetResourceBuilderTypeArgument(type, out var resourceType) && resourceType != null)
        {
            var resolvedType = resourceType;
            // If T is a generic parameter, use its constraint type instead
            if (resourceType.IsGenericParameter)
            {
                var constraintTypes = resourceType.GetGenericParameterConstraints().ToList();
                if (constraintTypes.Count > 0)
                {
                    var constraintType = constraintTypes[0];
                    var constraintTypeId = typeMapping.GetTypeId(constraintType) ?? InferResourceTypeId(constraintType);
                    return new AtsTypeRef
                    {
                        TypeId = constraintTypeId,
                        Category = AtsTypeCategory.Handle,
                        IsInterface = constraintType.IsInterface
                    };
                }
            }

            var typeId = typeMapping.GetTypeId(resolvedType) ?? InferResourceTypeId(resolvedType);
            return new AtsTypeRef
            {
                TypeId = typeId,
                Category = AtsTypeCategory.Handle,
                IsInterface = resolvedType.IsInterface
            };
        }

        // Fallback: Check by type name for IResourceBuilder<T>
        if (type.GenericTypeDefinitionFullName == "Aspire.Hosting.ApplicationModel.IResourceBuilder`1" ||
            typeFullName.StartsWith("Aspire.Hosting.ApplicationModel.IResourceBuilder`1"))
        {
            var genericArgs = type.GetGenericArguments().ToList();
            if (genericArgs.Count > 0)
            {
                var resType = genericArgs[0];

                // If T is a generic parameter, use the constraint type
                if (resType.IsGenericParameter)
                {
                    var constraintTypes = resType.GetGenericParameterConstraints().ToList();
                    if (constraintTypes.Count > 0)
                    {
                        var constraintType = constraintTypes[0];
                        var constraintTypeId = typeMapping.GetTypeId(constraintType) ?? InferResourceTypeId(constraintType);
                        return new AtsTypeRef
                        {
                            TypeId = constraintTypeId,
                            Category = AtsTypeCategory.Handle,
                            IsInterface = constraintType.IsInterface
                        };
                    }
                }

                var typeId = typeMapping.GetTypeId(resType) ?? InferResourceTypeId(resType);
                return new AtsTypeRef
                {
                    TypeId = typeId,
                    Category = AtsTypeCategory.Handle,
                    IsInterface = resType.IsInterface
                };
            }
        }

        // Try explicit mapping for other types
        var mappedTypeId = typeMapping.GetTypeId(typeFullName);
        if (mappedTypeId != null)
        {
            // Determine if it's a DTO or Handle based on type mapping metadata
            // For now, assume explicitly mapped types are Handles
            return new AtsTypeRef
            {
                TypeId = mappedTypeId,
                Category = AtsTypeCategory.Handle,
                IsInterface = type.IsInterface
            };
        }

        // No mapping found
        return null;
    }

    private static string? InferTypeId(string typeFullName)
    {
        // Handle primitives
        if (typeFullName == "System.String")
        {
            return AtsConstants.String;
        }
        if (typeFullName == "System.Char")
        {
            return AtsConstants.Char;
        }
        if (typeFullName == "System.Boolean")
        {
            return AtsConstants.Boolean;
        }
        if (typeFullName is "System.Int32" or "System.Int64" or "System.Double" or
            "System.Single" or "System.Int16" or "System.Byte" or "System.Decimal" or
            "System.UInt16" or "System.UInt32" or "System.UInt64" or "System.SByte")
        {
            return AtsConstants.Number;
        }

        // Handle date/time types
        if (typeFullName == "System.DateTime")
        {
            return AtsConstants.DateTime;
        }
        if (typeFullName == "System.DateTimeOffset")
        {
            return AtsConstants.DateTimeOffset;
        }
        if (typeFullName == "System.DateOnly")
        {
            return AtsConstants.DateOnly;
        }
        if (typeFullName == "System.TimeOnly")
        {
            return AtsConstants.TimeOnly;
        }
        if (typeFullName == "System.TimeSpan")
        {
            return AtsConstants.TimeSpan;
        }

        // Handle other scalar types
        if (typeFullName == "System.Guid")
        {
            return AtsConstants.Guid;
        }
        if (typeFullName == "System.Uri")
        {
            return AtsConstants.Uri;
        }

        // No mapping found
        return null;
    }

    private static string InferResourceTypeId(IAtsTypeInfo type)
    {
        return AtsTypeMapping.DeriveTypeId(type.AssemblyName ?? "Unknown", type.FullName);
    }

    private static string InferResourceTypeId(string? typeFullName)
    {
        if (string.IsNullOrEmpty(typeFullName))
        {
            return "Unknown/Unknown";
        }

        // Fallback: extract namespace as assembly approximation for types not available as IAtsTypeInfo
        var lastDot = typeFullName.LastIndexOf('.');
        if (lastDot > 0)
        {
            var namespacePart = typeFullName[..lastDot];
            return $"{namespacePart}/{typeFullName}";
        }
        return $"Unknown/{typeFullName}";
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
    private static bool IsUnresolvedGenericResourceBuilder(IAtsTypeInfo type)
    {
        // Check if this is IResourceBuilder<T>
        if (type.GenericTypeDefinitionFullName != "Aspire.Hosting.ApplicationModel.IResourceBuilder`1" &&
            !type.FullName.StartsWith("Aspire.Hosting.ApplicationModel.IResourceBuilder`1"))
        {
            return false;
        }

        var genericArgs = type.GetGenericArguments().ToList();
        if (genericArgs.Count == 0)
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
        var constraints = resourceType.GetGenericParameterConstraintFullNames().ToList();

        // If T has constraints, use them (MapToAtsTypeId will pick the first constraint)
        // Expansion will handle mapping interface constraints to concrete types
        return constraints.Count == 0;
    }

    /// <summary>
    /// Collects ALL interfaces implemented by a type, including inherited interfaces.
    /// </summary>
    private static List<AtsTypeRef> CollectAllInterfaces(IAtsTypeInfo type, AtsTypeMapping typeMapping)
    {
        var allInterfaces = new List<AtsTypeRef>();

        // GetInterfaces() returns all interfaces (including inherited for RoTypeInfoWrapper)
        foreach (var iface in type.GetInterfaces())
        {
            var ifaceTypeId = typeMapping.GetTypeId(iface) ?? InferResourceTypeId(iface);
            allInterfaces.Add(new AtsTypeRef
            {
                TypeId = ifaceTypeId,
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
    private static List<AtsTypeRef> CollectBaseTypeHierarchy(IAtsTypeInfo type, AtsTypeMapping typeMapping)
    {
        var baseTypes = new List<AtsTypeRef>();

        // Walk up the inheritance chain
        var currentBase = type.GetBaseType();
        while (currentBase != null)
        {
            // Stop at system types
            var baseFullName = currentBase.FullName;
            if (baseFullName == "System.Object" ||
                baseFullName.StartsWith("System.") ||
                baseFullName.StartsWith("Microsoft."))
            {
                break;
            }

            var baseTypeId = typeMapping.GetTypeId(currentBase) ?? InferResourceTypeId(currentBase);
            baseTypes.Add(new AtsTypeRef
            {
                TypeId = baseTypeId,
                Category = AtsTypeCategory.Handle,
                IsInterface = false
            });

            currentBase = currentBase.GetBaseType();
        }

        return baseTypes;
    }

    /// <summary>
    /// Collects concrete resource types from a capability method's parameters and return type.
    /// These types are needed for expansion but may not have [AspireExport] attributes.
    /// </summary>
    private static void CollectResourceTypesFromCapability(
        IAtsMethodInfo method,
        AtsTypeMapping typeMapping,
        Dictionary<string, IAtsTypeInfo> discoveredTypes)
    {
        // Check first parameter (target type for extension methods)
        var parameters = method.GetParameters().ToList();
        if (parameters.Count > 0)
        {
            CollectResourceTypeFromBuilderType(parameters[0].ParameterType, typeMapping, discoveredTypes);
        }

        // Check return type
        CollectResourceTypeFromBuilderType(method.ReturnType, typeMapping, discoveredTypes);
    }

    /// <summary>
    /// If the type is IResourceBuilder&lt;T&gt; where T is a concrete resource type,
    /// add T to the discovered types dictionary.
    /// </summary>
    private static void CollectResourceTypeFromBuilderType(
        IAtsTypeInfo type,
        AtsTypeMapping typeMapping,
        Dictionary<string, IAtsTypeInfo> discoveredTypes)
    {
        // Handle Task<T> wrapper
        if (type.GenericTypeDefinitionFullName == "System.Threading.Tasks.Task`1" ||
            type.FullName.StartsWith("System.Threading.Tasks.Task`1"))
        {
            var taskArgs = type.GetGenericArguments().ToList();
            if (taskArgs.Count > 0)
            {
                type = taskArgs[0];
            }
        }

        // Check if this is IResourceBuilder<T>
        if (type.GenericTypeDefinitionFullName != "Aspire.Hosting.ApplicationModel.IResourceBuilder`1" &&
            !type.FullName.StartsWith("Aspire.Hosting.ApplicationModel.IResourceBuilder`1"))
        {
            return;
        }

        var genericArgs = type.GetGenericArguments().ToList();
        if (genericArgs.Count == 0)
        {
            return;
        }

        var resourceType = genericArgs[0];

        // Skip generic parameters (T) - we only want concrete or interface types
        if (resourceType.IsGenericParameter)
        {
            return;
        }

        // Note: We now collect interfaces as well as concrete types.
        // Interfaces need to be in the expansion map for capabilities that target them directly
        // (e.g., withReference targeting IResourceWithEnvironment).
        // The expansion logic handles mapping interfaces to implementing concrete types.

        // Get the type ID for this resource
        var typeId = typeMapping.GetTypeId(resourceType) ?? InferResourceTypeId(resourceType);

        // Add to dictionary if not already present
        if (!discoveredTypes.ContainsKey(typeId))
        {
            discoveredTypes[typeId] = resourceType;
        }
    }

    private static bool HasExtensionAttribute(IAtsMethodInfo method)
    {
        return method.GetCustomAttributes()
            .Any(a => a.AttributeTypeFullName == "System.Runtime.CompilerServices.ExtensionAttribute");
    }

    private static IAtsAttributeInfo? GetAspireExportAttribute(IAtsTypeInfo type)
    {
        return type.GetCustomAttributes()
            .FirstOrDefault(a => a.AttributeTypeFullName == AspireExportAttributeNames.FullName);
    }

    private static IAtsAttributeInfo? GetAspireExportAttribute(IAtsMethodInfo method)
    {
        return method.GetCustomAttributes()
            .FirstOrDefault(a => a.AttributeTypeFullName == AspireExportAttributeNames.FullName);
    }

    /// <summary>
    /// Checks if a type has [AspireExport(ExposeProperties = true)] attribute.
    /// </summary>
    private static bool HasExposePropertiesAttribute(IAtsTypeInfo type)
    {
        foreach (var attr in type.GetCustomAttributes())
        {
            if (attr.AttributeTypeFullName == AspireExportAttributeNames.FullName)
            {
                if (attr.NamedArguments.TryGetValue("ExposeProperties", out var value) && value is true)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Checks if a type has [AspireExport(ExposeMethods = true)] attribute.
    /// </summary>
    private static bool HasExposeMethodsAttribute(IAtsTypeInfo type)
    {
        foreach (var attr in type.GetCustomAttributes())
        {
            if (attr.AttributeTypeFullName == AspireExportAttributeNames.FullName)
            {
                if (attr.NamedArguments.TryGetValue("ExposeMethods", out var value) && value is true)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Checks if a property has [AspireExportIgnore] attribute.
    /// </summary>
    private static bool HasExportIgnoreAttribute(IAtsPropertyInfo property)
    {
        return property.GetCustomAttributes()
            .Any(a => a.AttributeTypeFullName == AspireExportAttributeNames.IgnoreFullName);
    }

    /// <summary>
    /// Checks if a method has [AspireExportIgnore] attribute.
    /// </summary>
    private static bool HasExportIgnoreAttribute(IAtsMethodInfo method)
    {
        return method.GetCustomAttributes()
            .Any(a => a.AttributeTypeFullName == AspireExportAttributeNames.IgnoreFullName);
    }

    /// <summary>
    /// Gets [AspireExport] attribute from a property (for member-level export).
    /// </summary>
    private static IAtsAttributeInfo? GetAspireExportAttribute(IAtsPropertyInfo property)
    {
        return property.GetCustomAttributes()
            .FirstOrDefault(a => a.AttributeTypeFullName == AspireExportAttributeNames.FullName);
    }

    /// <summary>
    /// Gets [AspireUnion] attribute from a parameter.
    /// </summary>
    private static IAtsAttributeInfo? GetAspireUnionAttribute(IAtsParameterInfo parameter)
    {
        return parameter.GetCustomAttributes()
            .FirstOrDefault(a => a.AttributeTypeFullName == AspireExportAttributeNames.UnionFullName);
    }

    /// <summary>
    /// Gets [AspireUnion] attribute from a property.
    /// </summary>
    private static IAtsAttributeInfo? GetAspireUnionAttribute(IAtsPropertyInfo property)
    {
        return property.GetCustomAttributes()
            .FirstOrDefault(a => a.AttributeTypeFullName == AspireExportAttributeNames.UnionFullName);
    }

    /// <summary>
    /// Creates a union type ref from an [AspireUnion] attribute.
    /// Throws if any type in the union is not a valid ATS type.
    /// </summary>
    private static AtsTypeRef CreateUnionTypeRef(
        IAtsAttributeInfo unionAttr,
        string context,
        AtsTypeMapping typeMapping)
    {
        // The types are passed as a params Type[] which comes through as:
        // - FixedArguments[0] = Type[] (the array of types)
        if (unionAttr.FixedArguments.Count == 0)
        {
            throw new InvalidOperationException(
                $"[AspireUnion] on {context} has no types specified. Union must have at least 2 types.");
        }

        // Get the type names from the attribute
        var unionTypeNames = new List<string>();
        var firstArg = unionAttr.FixedArguments[0];

        // Handle both array and individual type arguments
        if (firstArg is object[] typeArray)
        {
            // Params array case: Type[] stored as object[]
            foreach (var typeObj in typeArray)
            {
                var extractedName = ExtractTypeName(typeObj);
                if (extractedName != null)
                {
                    unionTypeNames.Add(extractedName);
                }
            }
        }
        else if (firstArg is System.Collections.IEnumerable enumerable && firstArg is not string)
        {
            // Runtime reflection: params Type[] comes through as ReadOnlyCollection<CustomAttributeTypedArgument>
            // Need to extract the Value from each CustomAttributeTypedArgument
            foreach (var item in enumerable)
            {
                // Handle CustomAttributeTypedArgument directly (from System.Reflection)
                if (item is System.Reflection.CustomAttributeTypedArgument typedArg)
                {
                    var extractedName = ExtractTypeName(typedArg.Value);
                    if (extractedName != null)
                    {
                        unionTypeNames.Add(extractedName);
                    }
                }
                else
                {
                    // Fallback for other enumerable types
                    var extractedName = ExtractTypeName(item);
                    if (extractedName != null)
                    {
                        unionTypeNames.Add(extractedName);
                    }
                }
            }
        }
        else
        {
            // Individual arguments case or different serialization
            foreach (var arg in unionAttr.FixedArguments)
            {
                var extractedName = ExtractTypeName(arg);
                if (extractedName != null)
                {
                    unionTypeNames.Add(extractedName);
                }
            }
        }

        // Helper to extract type name from various possible representations
        static string? ExtractTypeName(object? typeObj)
        {
            if (typeObj is IAtsTypeInfo typeInfo)
            {
                return typeInfo.FullName;
            }
            if (typeObj is string typeName)
            {
                return typeName;
            }
            // Runtime reflection: typeof() arguments come through as Type objects
            if (typeObj is Type clrType && clrType.FullName != null)
            {
                return clrType.FullName;
            }
            return null;
        }

        if (unionTypeNames.Count < 2)
        {
            throw new InvalidOperationException(
                $"[AspireUnion] on {context} has {unionTypeNames.Count} type(s). Union must have at least 2 types.");
        }

        // Create type refs for each union member
        var unionTypes = new List<AtsTypeRef>();
        foreach (var typeName in unionTypeNames)
        {
            var typeRef = CreateTypeRefFromFullName(typeName, typeMapping);
            if (typeRef == null)
            {
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
    /// Creates an AtsTypeRef from a full type name string.
    /// Used for union type member resolution.
    /// </summary>
    private static AtsTypeRef? CreateTypeRefFromFullName(
        string typeFullName,
        AtsTypeMapping typeMapping)
    {
        // Strip assembly qualification if present
        // Assembly-qualified names look like: "System.String, System.Runtime, Version=..."
        var commaIndex = typeFullName.IndexOf(',');
        if (commaIndex >= 0)
        {
            typeFullName = typeFullName.Substring(0, commaIndex).Trim();
        }

        // Handle primitives
        if (typeFullName == "System.String")
        {
            return new AtsTypeRef { TypeId = AtsConstants.String, Category = AtsTypeCategory.Primitive };
        }
        if (typeFullName == "System.Char")
        {
            return new AtsTypeRef { TypeId = AtsConstants.Char, Category = AtsTypeCategory.Primitive };
        }
        if (typeFullName == "System.Boolean")
        {
            return new AtsTypeRef { TypeId = AtsConstants.Boolean, Category = AtsTypeCategory.Primitive };
        }
        if (typeFullName is "System.Int32" or "System.Int64" or "System.Double" or
            "System.Single" or "System.Int16" or "System.Byte" or "System.Decimal" or
            "System.UInt16" or "System.UInt32" or "System.UInt64" or "System.SByte")
        {
            return new AtsTypeRef { TypeId = AtsConstants.Number, Category = AtsTypeCategory.Primitive };
        }

        // Handle date/time types
        if (typeFullName == "System.DateTime")
        {
            return new AtsTypeRef { TypeId = AtsConstants.DateTime, Category = AtsTypeCategory.Primitive };
        }
        if (typeFullName == "System.DateTimeOffset")
        {
            return new AtsTypeRef { TypeId = AtsConstants.DateTimeOffset, Category = AtsTypeCategory.Primitive };
        }
        if (typeFullName == "System.DateOnly")
        {
            return new AtsTypeRef { TypeId = AtsConstants.DateOnly, Category = AtsTypeCategory.Primitive };
        }
        if (typeFullName == "System.TimeOnly")
        {
            return new AtsTypeRef { TypeId = AtsConstants.TimeOnly, Category = AtsTypeCategory.Primitive };
        }
        if (typeFullName == "System.TimeSpan")
        {
            return new AtsTypeRef { TypeId = AtsConstants.TimeSpan, Category = AtsTypeCategory.Primitive };
        }

        // Handle other scalar types
        if (typeFullName == "System.Guid")
        {
            return new AtsTypeRef { TypeId = AtsConstants.Guid, Category = AtsTypeCategory.Primitive };
        }
        if (typeFullName == "System.Uri")
        {
            return new AtsTypeRef { TypeId = AtsConstants.Uri, Category = AtsTypeCategory.Primitive };
        }

        // System.Object is NOT a valid union member - require explicit types
        if (typeFullName == "System.Object")
        {
            return null;
        }

        // Try explicit mapping for other types (handles, DTOs)
        var mappedTypeId = typeMapping.GetTypeId(typeFullName);
        if (mappedTypeId != null)
        {
            return new AtsTypeRef
            {
                TypeId = mappedTypeId,
                Category = AtsTypeCategory.Handle,
                IsInterface = false // Can't determine without full type info
            };
        }

        // No mapping found - not a valid ATS type
        return null;
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

/// <summary>
/// Provides type resolution for capability scanning.
/// </summary>
internal interface IAtsTypeResolver
{
    /// <summary>
    /// Checks if a type is assignable to IResource.
    /// </summary>
    bool IsResourceType(IAtsTypeInfo type);

    /// <summary>
    /// Checks if a type is IResourceBuilder&lt;T&gt;.
    /// </summary>
    bool IsResourceBuilderType(IAtsTypeInfo type);

    /// <summary>
    /// Tries to get the resource type argument from an IResourceBuilder&lt;T&gt; type.
    /// Returns true if the type is IResourceBuilder&lt;T&gt; and outputs the T type.
    /// </summary>
    bool TryGetResourceBuilderTypeArgument(IAtsTypeInfo type, out IAtsTypeInfo? resourceType);
}

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
        var capabilities = new List<AtsCapabilityInfo>();
        var typeInfos = new List<AtsTypeInfo>();

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
                var contextCapabilities = CreateContextTypeCapabilities(type, assembly.Name, typeMapping, typeResolver);
                capabilities.AddRange(contextCapabilities);
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

                var capability = CreateCapabilityInfo(method, exportAttr, assembly.Name, typeMapping, typeResolver);
                if (capability != null)
                {
                    capabilities.Add(capability);

                    // Collect resource types from capability parameters and return types
                    CollectResourceTypesFromCapability(method, typeMapping, discoveredResourceTypes);
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
            // Collect ALL interfaces (including inherited) for proper expansion
            var implementedInterfaces = CollectAllInterfaces(resourceType, typeMapping);

            typeInfos.Add(new AtsTypeInfo
            {
                AtsTypeId = typeId,
                ClrTypeName = resourceType.FullName,
                IsInterface = false,
                ImplementedInterfaceTypeIds = implementedInterfaces
            });
        }

        // Expand interface targets to concrete types (2-pass expansion)
        ExpandCapabilityTargets(capabilities, typeInfos);

        // Detect method name collisions after expansion
        DetectMethodNameCollisions(capabilities);

        return new ScanResult
        {
            Capabilities = capabilities,
            TypeInfos = typeInfos
        };
    }

    /// <summary>
    /// Expands capability targets from interface types to concrete types.
    /// For capabilities targeting an interface (e.g., "Aspire.Hosting/IResourceWithEnvironment"),
    /// this populates ExpandedTargetTypeIds with all concrete types implementing that interface.
    /// </summary>
    private static void ExpandCapabilityTargets(
        List<AtsCapabilityInfo> capabilities,
        List<AtsTypeInfo> typeInfos)
    {
        // Build map: interface typeId -> concrete typeIds that implement it
        // This includes both explicit interfaces (with [AspireExport]) and inferred ones (like IResource)
        var interfaceToConcreteTypes = new Dictionary<string, List<string>>();

        foreach (var typeInfo in typeInfos)
        {
            if (typeInfo.IsInterface)
            {
                continue;
            }

            // Add this concrete type to all interfaces it implements
            foreach (var interfaceTypeId in typeInfo.ImplementedInterfaceTypeIds)
            {
                if (!interfaceToConcreteTypes.TryGetValue(interfaceTypeId, out var list))
                {
                    list = [];
                    interfaceToConcreteTypes[interfaceTypeId] = list;
                }
                list.Add(typeInfo.AtsTypeId);
            }
        }

        // Expand each capability's target
        foreach (var capability in capabilities)
        {
            var originalTarget = capability.OriginalTargetTypeId;
            if (string.IsNullOrEmpty(originalTarget))
            {
                // Entry point methods have no target
                capability.ExpandedTargetTypeIds = [];
                continue;
            }

            // Check if target is an interface (either explicit or inferred from type hierarchy)
            // Use the interfaceToConcreteTypes map directly - it includes all interfaces from ImplementedInterfaceTypeIds
            if (interfaceToConcreteTypes.TryGetValue(originalTarget, out var concreteTypes))
            {
                capability.ExpandedTargetTypeIds = concreteTypes.ToList();
            }
            else
            {
                // Concrete type: expand to itself
                capability.ExpandedTargetTypeIds = [originalTarget];
            }
        }
    }

    /// <summary>
    /// Detects method name collisions after capability expansion.
    /// Since ATS doesn't support method overloading, each (TargetTypeId, MethodName) pair must be unique.
    /// </summary>
    private static void DetectMethodNameCollisions(List<AtsCapabilityInfo> capabilities)
    {
        // Group by (TargetTypeId, MethodName) to find collisions
        var collisions = capabilities
            .Where(c => c.ExpandedTargetTypeIds.Count > 0)
            .SelectMany(c => c.ExpandedTargetTypeIds.Select(t => (Target: t, Capability: c)))
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

        return new AtsTypeInfo
        {
            AtsTypeId = atsTypeId,
            ClrTypeName = type.FullName,
            IsInterface = type.IsInterface,
            ImplementedInterfaceTypeIds = implementedInterfaces
        };
    }

    private static List<AtsCapabilityInfo> CreateContextTypeCapabilities(
        IAtsTypeInfo contextType,
        string assemblyName,
        AtsTypeMapping typeMapping,
        IAtsTypeResolver? typeResolver)
    {
        var capabilities = new List<AtsCapabilityInfo>();

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

            var propertyTypeId = MapToAtsTypeId(property.PropertyType, typeMapping, typeResolver);
            if (propertyTypeId is null)
            {
                // Skip properties with unmapped types
                continue;
            }

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
                            AtsTypeId = typeId,
                            IsOptional = false,
                            IsNullable = false,
                            IsCallback = false,
                            DefaultValue = null
                        }
                    ],
                    ReturnTypeId = propertyTypeId,
                    IsExtensionMethod = false,
                    OriginalTargetTypeId = typeId,
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
                            AtsTypeId = typeId,
                            IsOptional = false,
                            IsNullable = false,
                            IsCallback = false,
                            DefaultValue = null
                        },
                        new AtsParameterInfo
                        {
                            Name = "value",
                            AtsTypeId = propertyTypeId,
                            IsOptional = false,
                            IsNullable = false,
                            IsCallback = false,
                            DefaultValue = null
                        }
                    ],
                    ReturnTypeId = typeId, // Returns the context for fluent chaining
                    IsExtensionMethod = false,
                    OriginalTargetTypeId = typeId,
                    ReturnsBuilder = false,
                    CapabilityKind = AtsCapabilityKind.PropertySetter,
                    OwningTypeName = typeName,
                    SourceProperty = property
                });
            }
        }

        // Scan instance methods if ExposeMethods is true
        if (exposeAllMethods)
        {
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
                        AtsTypeId = typeId,
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
                var returnTypeId = MapToAtsTypeId(method.ReturnType, typeMapping, typeResolver);

                capabilities.Add(new AtsCapabilityInfo
                {
                    CapabilityId = methodCapabilityId,
                    MethodName = methodCapabilityName,
                    Package = package,
                    Description = $"Invokes the {method.Name} method",
                    Parameters = paramInfos,
                    ReturnTypeId = returnTypeId,
                    IsExtensionMethod = false,
                    OriginalTargetTypeId = typeId,
                    ReturnsBuilder = false,
                    CapabilityKind = AtsCapabilityKind.InstanceMethod,
                    OwningTypeName = typeName,
                    SourceMethod = method
                });
            }
        }

        return capabilities;
    }

    private static AtsCapabilityInfo? CreateCapabilityInfo(
        IAtsMethodInfo method,
        IAtsAttributeInfo exportAttr,
        string assemblyName,
        AtsTypeMapping typeMapping,
        IAtsTypeResolver? typeResolver)
    {
        // Get method name from first constructor argument (new format: just the method name)
        if (exportAttr.FixedArguments.Count == 0 || exportAttr.FixedArguments[0] is not string methodNameFromAttr)
        {
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
        if (parameters.Count > 0)
        {
            var firstParam = parameters[0];
            var firstParamType = firstParam.ParameterType;

            // Check if this is IResourceBuilder<T> where T is an unresolved generic parameter
            if (IsUnresolvedGenericResourceBuilder(firstParamType))
            {
                // Skip - can't generate concrete builders for unresolved generic type parameters
                return null;
            }

            var firstParamTypeId = MapToAtsTypeId(firstParamType, typeMapping, typeResolver);
            if (firstParamTypeId != null)
            {
                extendsTypeId = firstParamTypeId;
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
                    return null;
                }
                // Skip optional parameters with unmapped types
                continue;
            }
            paramInfos.Add(paramInfo);
            paramIndex++;
        }

        // Get return type
        var returnTypeId = MapToAtsTypeId(method.ReturnType, typeMapping, typeResolver);
        var returnsBuilder = returnTypeId != null;

        return new AtsCapabilityInfo
        {
            CapabilityId = capabilityId,
            MethodName = methodName,
            Package = package,
            Description = description,
            Parameters = paramInfos,
            ReturnTypeId = returnTypeId,
            IsExtensionMethod = isExtensionMethod,
            OriginalTargetTypeId = extendsTypeId,
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

        // Check if this is a delegate type (callbacks are inferred from delegate types)
        var isCallback = IsDelegateType(paramType);

        // Map the type - return null if unmapped (unless it's a callback)
        var atsTypeId = MapToAtsTypeId(paramType, typeMapping, typeResolver);
        if (atsTypeId is null && !isCallback)
        {
            // Can't map this parameter type - skip it
            return null;
        }

        // Extract callback signature if this is a callback parameter
        IReadOnlyList<AtsCallbackParameterInfo>? callbackParameters = null;
        string? callbackReturnTypeId = null;

        if (isCallback)
        {
            (callbackParameters, callbackReturnTypeId) = ExtractCallbackSignature(paramType, typeMapping, typeResolver);
        }

        // Check if nullable (Nullable<T>)
        var isNullable = paramType.GenericTypeDefinitionFullName == "System.Nullable`1" ||
                         param.TypeFullName.StartsWith("System.Nullable`1");

        // Determine type kind
        var typeKind = DetermineTypeKind(atsTypeId, paramType);

        return new AtsParameterInfo
        {
            Name = string.IsNullOrEmpty(param.Name) ? $"arg{paramIndex}" : param.Name,
            AtsTypeId = isCallback ? "callback" : atsTypeId!,
            TypeCategory = AtsConstants.GetCategory(atsTypeId, isCallback),
            TypeKind = typeKind,
            IsOptional = param.IsOptional,
            IsNullable = isNullable,
            IsCallback = isCallback,
            CallbackParameters = callbackParameters,
            CallbackReturnTypeId = callbackReturnTypeId,
            DefaultValue = param.DefaultValue
        };
    }

    /// <summary>
    /// Determines the ATS type kind for a type.
    /// </summary>
    private static AtsTypeKind DetermineTypeKind(string? atsTypeId, IAtsTypeInfo type)
    {
        // Check if primitive
        if (AtsConstants.IsPrimitive(atsTypeId))
        {
            return AtsTypeKind.Primitive;
        }

        // Check if interface
        if (type.IsInterface)
        {
            return AtsTypeKind.Interface;
        }

        // Everything else is a concrete type
        return AtsTypeKind.ConcreteType;
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
    private static (IReadOnlyList<AtsCallbackParameterInfo>? Parameters, string? ReturnTypeId) ExtractCallbackSignature(
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
            // For callback parameters, if type can't be mapped, derive a handle type ID
            var paramType = param.ParameterType;
            var paramAtsTypeId = MapToAtsTypeId(paramType, typeMapping, typeResolver)
                ?? AtsTypeMapping.DeriveTypeId(paramType.AssemblyName ?? "Unknown", paramType.FullName);
            parameters.Add(new AtsCallbackParameterInfo
            {
                Name = param.Name,
                AtsTypeId = paramAtsTypeId
            });
        }

        // Extract return type
        var returnTypeFullName = invokeMethod.ReturnTypeFullName;
        string returnTypeId;

        if (returnTypeFullName == "System.Void")
        {
            returnTypeId = AtsConstants.Void;
        }
        else if (returnTypeFullName == "System.Threading.Tasks.Task")
        {
            returnTypeId = AtsConstants.Void;
        }
        else if (returnTypeFullName.StartsWith("System.Threading.Tasks.Task`1"))
        {
            // Task<T> - get the inner type
            var innerType = invokeMethod.ReturnType.GetGenericArguments().FirstOrDefault();
            returnTypeId = innerType is not null
                ? MapToAtsTypeId(innerType, typeMapping, typeResolver) ?? AtsConstants.Void
                : AtsConstants.Void;
        }
        else
        {
            // For unmapped return types, use void (callback return might not be used)
            returnTypeId = MapToAtsTypeId(invokeMethod.ReturnType, typeMapping, typeResolver) ?? AtsConstants.Void;
        }

        return (parameters, returnTypeId);
    }

    /// <summary>
    /// Extracts signature from well-known delegate types based on their generic type definition.
    /// Used as fallback when the Invoke method isn't available from metadata.
    /// </summary>
    private static (IReadOnlyList<AtsCallbackParameterInfo>? Parameters, string? ReturnTypeId) ExtractWellKnownDelegateSignature(
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

        // Action<T>, Action<T1, T2>, etc. - all params are inputs, void return
        if (genericDefFullName.StartsWith("System.Action`"))
        {
            var parameters = new List<AtsCallbackParameterInfo>();
            for (var i = 0; i < genericArgs.Count; i++)
            {
                var paramType = genericArgs[i];
                var paramAtsTypeId = MapToAtsTypeId(paramType, typeMapping, typeResolver)
                    ?? AtsTypeMapping.DeriveTypeId(paramType.AssemblyName ?? "Unknown", paramType.FullName);
                parameters.Add(new AtsCallbackParameterInfo
                {
                    Name = $"arg{i}",
                    AtsTypeId = paramAtsTypeId
                });
            }
            return (parameters, AtsConstants.Void);
        }

        // Func<TResult>, Func<T, TResult>, Func<T1, T2, TResult>, etc.
        // Last generic arg is return type, rest are parameters
        if (genericDefFullName.StartsWith("System.Func`"))
        {
            var parameters = new List<AtsCallbackParameterInfo>();
            for (var i = 0; i < genericArgs.Count - 1; i++)
            {
                var paramType = genericArgs[i];
                var paramAtsTypeId = MapToAtsTypeId(paramType, typeMapping, typeResolver)
                    ?? AtsTypeMapping.DeriveTypeId(paramType.AssemblyName ?? "Unknown", paramType.FullName);
                parameters.Add(new AtsCallbackParameterInfo
                {
                    Name = $"arg{i}",
                    AtsTypeId = paramAtsTypeId
                });
            }

            var returnType = genericArgs[^1];
            var returnTypeFullName = returnType.FullName;
            string returnTypeId;

            if (returnTypeFullName == "System.Void")
            {
                returnTypeId = AtsConstants.Void;
            }
            else if (returnTypeFullName == "System.Threading.Tasks.Task")
            {
                returnTypeId = AtsConstants.Void;
            }
            else if (returnTypeFullName.StartsWith("System.Threading.Tasks.Task`1"))
            {
                // Task<T> - get the inner type
                var innerType = returnType.GetGenericArguments().FirstOrDefault();
                returnTypeId = innerType is not null
                    ? MapToAtsTypeId(innerType, typeMapping, typeResolver) ?? AtsConstants.Void
                    : AtsConstants.Void;
            }
            else
            {
                // For unmapped return types, use void (callback return might not be used)
                returnTypeId = MapToAtsTypeId(returnType, typeMapping, typeResolver) ?? AtsConstants.Void;
            }

            return (parameters, returnTypeId);
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
    private static List<string> CollectAllInterfaces(IAtsTypeInfo type, AtsTypeMapping typeMapping)
    {
        var allInterfaces = new List<string>();

        // GetInterfaces() returns all interfaces (including inherited for RoTypeInfoWrapper)
        foreach (var iface in type.GetInterfaces())
        {
            var ifaceTypeId = typeMapping.GetTypeId(iface) ?? InferResourceTypeId(iface);
            allInterfaces.Add(ifaceTypeId);
        }

        return allInterfaces;
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

        // Skip generic parameters (T) - we only want concrete types
        if (resourceType.IsGenericParameter)
        {
            return;
        }

        // Skip interfaces - they don't need to be collected since they don't have implementations
        if (resourceType.IsInterface)
        {
            return;
        }

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

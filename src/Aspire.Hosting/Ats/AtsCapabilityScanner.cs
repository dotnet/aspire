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

            // Check for [AspireExport(ExposeProperties = true)] - auto-generate property accessor capabilities
            if (HasExposePropertiesAttribute(type))
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
    /// For capabilities targeting an interface (e.g., "aspire/IResourceWithEnvironment"),
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

        // Derive the type ID from {AssemblyName}/{TypeName}
        var typeName = contextType.Name;
        var typeId = $"{assemblyName}/{typeName}";

        // Scan properties
        foreach (var property in contextType.GetProperties())
        {
            if (property.IsStatic)
            {
                continue;
            }

            var propertyTypeId = MapToAtsTypeId(property.PropertyType, typeMapping, typeResolver);
            if (propertyTypeId is null or "any")
            {
                continue;
            }

            // Generate getter capability if property is readable
            if (property.CanRead)
            {
                var getMethodName = $"{typeName}.get{property.Name}";
                var getCapabilityId = $"{assemblyName}/{getMethodName}";

                capabilities.Add(new AtsCapabilityInfo
                {
                    CapabilityId = getCapabilityId,
                    MethodName = getMethodName,
                    Package = assemblyName,
                    Description = $"Gets the {property.Name} property",
                    Parameters = [
                        new AtsParameterInfo
                        {
                            Name = "context",
                            AtsTypeId = typeId,
                            IsOptional = false,
                            IsNullable = false,
                            IsCallback = false,
                            CallbackId = null,
                            DefaultValue = null
                        }
                    ],
                    ReturnTypeId = propertyTypeId,
                    IsExtensionMethod = false,
                    OriginalTargetTypeId = typeId,
                    ReturnsBuilder = false,
                    IsContextProperty = true,
                    IsContextPropertyGetter = true,
                    SourceProperty = property
                });
            }

            // Generate setter capability if property is writable
            if (property.CanWrite)
            {
                var setMethodName = $"{typeName}.set{property.Name}";
                var setCapabilityId = $"{assemblyName}/{setMethodName}";

                capabilities.Add(new AtsCapabilityInfo
                {
                    CapabilityId = setCapabilityId,
                    MethodName = setMethodName,
                    Package = assemblyName,
                    Description = $"Sets the {property.Name} property",
                    Parameters = [
                        new AtsParameterInfo
                        {
                            Name = "context",
                            AtsTypeId = typeId,
                            IsOptional = false,
                            IsNullable = false,
                            IsCallback = false,
                            CallbackId = null,
                            DefaultValue = null
                        },
                        new AtsParameterInfo
                        {
                            Name = "value",
                            AtsTypeId = propertyTypeId,
                            IsOptional = false,
                            IsNullable = false,
                            IsCallback = false,
                            CallbackId = null,
                            DefaultValue = null
                        }
                    ],
                    ReturnTypeId = typeId, // Returns the context for fluent chaining
                    IsExtensionMethod = false,
                    OriginalTargetTypeId = typeId,
                    ReturnsBuilder = false,
                    IsContextProperty = true,
                    IsContextPropertySetter = true,
                    SourceProperty = property
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
            paramInfos.Add(CreateParameterInfo(param, paramIndex, typeMapping, typeResolver));
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

    private static AtsParameterInfo CreateParameterInfo(
        IAtsParameterInfo param,
        int paramIndex,
        AtsTypeMapping typeMapping,
        IAtsTypeResolver? typeResolver)
    {
        var paramType = param.ParameterType;
        var atsTypeId = MapToAtsTypeId(paramType, typeMapping, typeResolver) ?? "any";

        // Check for [AspireCallback] attribute
        var callbackAttr = param.GetCustomAttributes()
            .FirstOrDefault(a => a.AttributeTypeFullName == AspireExportAttributeNames.AspireCallbackFullName);
        var isCallback = callbackAttr != null;
        var callbackId = isCallback && callbackAttr!.FixedArguments.Count > 0
            ? callbackAttr.FixedArguments[0] as string
            : null;

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

        return new AtsParameterInfo
        {
            Name = string.IsNullOrEmpty(param.Name) ? $"arg{paramIndex}" : param.Name,
            AtsTypeId = isCallback ? "callback" : atsTypeId,
            IsOptional = param.IsOptional,
            IsNullable = isNullable,
            IsCallback = isCallback,
            CallbackId = callbackId,
            CallbackParameters = callbackParameters,
            CallbackReturnTypeId = callbackReturnTypeId,
            DefaultValue = param.DefaultValue
        };
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
            return (null, null);
        }

        // Extract parameters
        var parameters = new List<AtsCallbackParameterInfo>();
        foreach (var param in invokeMethod.GetParameters())
        {
            var paramAtsTypeId = MapToAtsTypeId(param.ParameterType, typeMapping, typeResolver) ?? "any";
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
            returnTypeId = "void";
        }
        else if (returnTypeFullName == "System.Threading.Tasks.Task")
        {
            returnTypeId = "task";
        }
        else if (returnTypeFullName.StartsWith("System.Threading.Tasks.Task`1"))
        {
            // Task<T> - get the inner type
            var innerType = invokeMethod.ReturnType.GetGenericArguments().FirstOrDefault();
            returnTypeId = innerType is not null
                ? MapToAtsTypeId(innerType, typeMapping, typeResolver) ?? "any"
                : "task";
        }
        else
        {
            returnTypeId = MapToAtsTypeId(invokeMethod.ReturnType, typeMapping, typeResolver) ?? "any";
        }

        return (parameters, returnTypeId);
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
            return "string";
        }
        if (typeFullName == "System.Boolean")
        {
            return "boolean";
        }
        if (typeFullName is "System.Int32" or "System.Int64" or "System.Double" or
            "System.Single" or "System.Int16" or "System.Byte")
        {
            return "number";
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
            return typeMapping.GetTypeId(resourceType.FullName) ?? InferResourceTypeId(resourceType.FullName);
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

                return typeMapping.GetTypeId(resType.FullName) ?? InferResourceTypeId(resType.FullName);
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

        return "any";
    }

    private static string InferTypeId(string typeFullName)
    {
        // Handle primitives
        if (typeFullName == "System.String")
        {
            return "string";
        }

        if (typeFullName == "System.Boolean")
        {
            return "boolean";
        }

        if (typeFullName is "System.Int32" or "System.Int64" or "System.Double" or
            "System.Single" or "System.Int16" or "System.Byte")
        {
            return "number";
        }

        return "any";
    }

    private static string InferResourceTypeId(string? typeFullName)
    {
        if (string.IsNullOrEmpty(typeFullName))
        {
            return "Unknown/Unknown";
        }

        // Use DeriveTypeIdFromFullName for consistent type ID derivation
        return AtsTypeMapping.DeriveTypeIdFromFullName(typeFullName);
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
            var ifaceTypeId = typeMapping.GetTypeId(iface.FullName) ?? InferResourceTypeId(iface.FullName);
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
        var typeId = typeMapping.GetTypeId(resourceType.FullName) ?? InferResourceTypeId(resourceType.FullName);

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

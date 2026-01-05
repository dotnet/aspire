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

            // Check for [AspireContextType] - auto-generate property accessor capabilities
            var contextAttr = GetAspireContextTypeAttribute(type);
            if (contextAttr != null)
            {
                var contextCapabilities = CreateContextTypeCapabilities(type, contextAttr, typeMapping, typeResolver);
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

                var capability = CreateCapabilityInfo(method, exportAttr, typeMapping, typeResolver);
                if (capability != null)
                {
                    capabilities.Add(capability);
                }
            }
        }

        return new ScanResult
        {
            Capabilities = capabilities,
            TypeInfos = typeInfos
        };
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

    /// <summary>
    /// Enumerates capabilities with their source method info.
    /// Useful for runtime dispatchers that need access to the underlying method for invocation.
    /// </summary>
    public static IEnumerable<(AtsCapabilityInfo Capability, IAtsMethodInfo Method)> EnumerateCapabilitiesWithMethods(
        IAtsAssemblyInfo assembly,
        AtsTypeMapping typeMapping,
        IAtsTypeResolver? typeResolver = null)
    {
        foreach (var type in assembly.GetTypes())
        {
            // Skip nested types and non-sealed types (same as ScanAssembly)
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

                var capability = CreateCapabilityInfo(method, exportAttr, typeMapping, typeResolver);
                if (capability != null)
                {
                    yield return (capability, method);
                }
            }
        }
    }

    /// <summary>
    /// Enumerates context type capabilities with their source type and property info.
    /// Useful for runtime dispatchers that need access to the property for invocation.
    /// </summary>
    public static IEnumerable<(AtsCapabilityInfo Capability, IAtsTypeInfo ContextType, IAtsPropertyInfo Property)> EnumerateContextTypeCapabilities(
        IAtsAssemblyInfo assembly,
        AtsTypeMapping typeMapping,
        IAtsTypeResolver? typeResolver = null)
    {
        foreach (var type in assembly.GetTypes())
        {
            var contextAttr = GetAspireContextTypeAttribute(type);
            if (contextAttr == null)
            {
                continue;
            }

            // Get the type ID from first constructor argument
            if (contextAttr.FixedArguments.Count == 0 || contextAttr.FixedArguments[0] is not string typeId)
            {
                continue;
            }

            // Get version from named arguments (defaults to 1)
            var version = contextAttr.NamedArguments.TryGetValue("Version", out var v) && v is int ver ? ver : 1;

            foreach (var property in type.GetProperties())
            {
                if (!property.CanRead || property.IsStatic)
                {
                    continue;
                }

                var propertyTypeId = MapToAtsTypeId(property.PropertyType, typeMapping, typeResolver);
                if (propertyTypeId == "any")
                {
                    continue;
                }

                var propertyName = char.ToLowerInvariant(property.Name[0]) + property.Name[1..];
                var capabilityId = $"{typeId}.{propertyName}@{version}";

                var capability = new AtsCapabilityInfo
                {
                    CapabilityId = capabilityId,
                    MethodName = propertyName,
                    Package = DerivePackage(typeId),
                    ConstraintTypeId = typeId,
                    Description = null,
                    Parameters =
                    [
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
                    ExtendsTypeId = typeId,
                    ReturnsBuilder = false,
                    IsContextProperty = true
                };

                yield return (capability, type, property);
            }
        }
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

        // Collect all interfaces with ATS mappings
        var implementedInterfaces = new List<string>();
        CollectMappedInterfaces(type, typeMapping, implementedInterfaces, new HashSet<string>());

        return new AtsTypeInfo
        {
            AtsTypeId = atsTypeId,
            ClrTypeName = type.FullName,
            IsInterface = type.IsInterface,
            ImplementedInterfaceTypeIds = implementedInterfaces
        };
    }

    private static void CollectMappedInterfaces(
        IAtsTypeInfo type,
        AtsTypeMapping typeMapping,
        List<string> result,
        HashSet<string> seen)
    {
        foreach (var ifaceFullName in type.GetInterfaceFullNames())
        {
            var ifaceTypeId = typeMapping.GetTypeId(ifaceFullName);
            if (ifaceTypeId != null && ifaceTypeId.StartsWith("aspire/") && seen.Add(ifaceTypeId))
            {
                result.Add(ifaceTypeId);
            }
        }

        // Check base type
        if (type.BaseTypeFullName != null)
        {
            var baseTypeId = typeMapping.GetTypeId(type.BaseTypeFullName);
            if (baseTypeId != null && baseTypeId.StartsWith("aspire/") && seen.Add(baseTypeId))
            {
                result.Add(baseTypeId);
            }
        }
    }

    private static List<AtsCapabilityInfo> CreateContextTypeCapabilities(
        IAtsTypeInfo contextType,
        IAtsAttributeInfo contextAttr,
        AtsTypeMapping typeMapping,
        IAtsTypeResolver? typeResolver)
    {
        var capabilities = new List<AtsCapabilityInfo>();

        // Get the type ID from first constructor argument
        if (contextAttr.FixedArguments.Count == 0 || contextAttr.FixedArguments[0] is not string typeId)
        {
            return capabilities;
        }

        // Get version from named arguments (defaults to 1)
        var version = contextAttr.NamedArguments.TryGetValue("Version", out var v) && v is int ver ? ver : 1;

        // Scan properties
        foreach (var property in contextType.GetProperties())
        {
            if (!property.CanRead || property.IsStatic)
            {
                continue;
            }

            var propertyTypeId = MapToAtsTypeId(property.PropertyType, typeMapping, typeResolver);
            if (propertyTypeId == "any")
            {
                continue;
            }

            var propertyName = char.ToLowerInvariant(property.Name[0]) + property.Name[1..];
            var capabilityId = $"{typeId}.{propertyName}@{version}";

            capabilities.Add(new AtsCapabilityInfo
            {
                CapabilityId = capabilityId,
                MethodName = propertyName,
                Package = DerivePackage(typeId),
                ConstraintTypeId = typeId,
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
                ExtendsTypeId = typeId,
                ReturnsBuilder = false,
                IsContextProperty = true
            });
        }

        return capabilities;
    }

    private static AtsCapabilityInfo? CreateCapabilityInfo(
        IAtsMethodInfo method,
        IAtsAttributeInfo exportAttr,
        AtsTypeMapping typeMapping,
        IAtsTypeResolver? typeResolver)
    {
        // Get capability ID from first constructor argument
        if (exportAttr.FixedArguments.Count == 0 || exportAttr.FixedArguments[0] is not string capabilityId)
        {
            return null;
        }

        // Get named arguments
        var description = exportAttr.NamedArguments.TryGetValue("Description", out var desc) ? desc as string : null;
        var methodNameOverride = exportAttr.NamedArguments.TryGetValue("MethodName", out var mn) ? mn as string : null;

        var methodName = methodNameOverride ?? DeriveMethodName(capabilityId);
        var package = DerivePackage(capabilityId);

        // Check if extension method
        var parameters = method.GetParameters().ToList();
        var isExtensionMethod = HasExtensionAttribute(method) && parameters.Count > 0;

        string? extendsTypeId = null;
        if (parameters.Count > 0)
        {
            var firstParamTypeId = MapToAtsTypeId(parameters[0].ParameterType, typeMapping, typeResolver);
            if (firstParamTypeId != null && firstParamTypeId.StartsWith("aspire/"))
            {
                extendsTypeId = firstParamTypeId;
            }
        }

        // Extract constraint from generic parameters
        var constraintTypeId = ExtractConstraintTypeId(method, typeMapping);
        if (constraintTypeId == null && extendsTypeId != null)
        {
            constraintTypeId = extendsTypeId;
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
        var returnsBuilder = returnTypeId != null && returnTypeId.StartsWith("aspire/");

        return new AtsCapabilityInfo
        {
            CapabilityId = capabilityId,
            MethodName = methodName,
            Package = package,
            ConstraintTypeId = constraintTypeId,
            Description = description,
            Parameters = paramInfos,
            ReturnTypeId = returnTypeId,
            IsExtensionMethod = isExtensionMethod,
            ExtendsTypeId = extendsTypeId,
            ReturnsBuilder = returnsBuilder
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
            DefaultValue = param.DefaultValue
        };
    }

    private static string? ExtractConstraintTypeId(IAtsMethodInfo method, AtsTypeMapping typeMapping)
    {
        foreach (var constraints in method.GetGenericParameterConstraints())
        {
            foreach (var constraintFullName in constraints)
            {
                var typeId = typeMapping.GetTypeId(constraintFullName);
                if (typeId != null && typeId.StartsWith("aspire/"))
                {
                    return typeId;
                }
            }
        }
        return null;
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
            return "aspire/Unknown";
        }

        // Get the simple name from full name (handle both '.' and '+' for nested types)
        var lastDot = typeFullName.LastIndexOf('.');
        var typeName = lastDot >= 0 ? typeFullName[(lastDot + 1)..] : typeFullName;

        // For nested types (e.g., "OuterClass+InnerResource"), strip the outer class
        var plusIndex = typeName.LastIndexOf('+');
        if (plusIndex >= 0)
        {
            typeName = typeName[(plusIndex + 1)..];
        }

        // Strip "Resource" suffix
        if (typeName.EndsWith("Resource"))
        {
            typeName = typeName[..^8];
        }

        return $"aspire/{typeName}";
    }

    /// <summary>
    /// Derives the method name from a capability ID.
    /// </summary>
    public static string DeriveMethodName(string capabilityId)
    {
        var slashIndex = capabilityId.LastIndexOf('/');
        var methodPart = slashIndex >= 0 ? capabilityId[(slashIndex + 1)..] : capabilityId;
        var atIndex = methodPart.LastIndexOf('@');
        return atIndex >= 0 ? methodPart[..atIndex] : methodPart;
    }

    /// <summary>
    /// Derives the package name from a capability ID.
    /// </summary>
    public static string DerivePackage(string capabilityId)
    {
        var slashIndex = capabilityId.IndexOf('/');
        return slashIndex >= 0 ? capabilityId[..slashIndex] : "aspire";
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

    private static IAtsAttributeInfo? GetAspireContextTypeAttribute(IAtsTypeInfo type)
    {
        return type.GetCustomAttributes()
            .FirstOrDefault(a => a.AttributeTypeFullName == AspireExportAttributeNames.AspireContextTypeFullName);
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

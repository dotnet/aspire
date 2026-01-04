// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.CodeGeneration.Models.Types;

namespace Aspire.Hosting.CodeGeneration.Models.Ats;

/// <summary>
/// Scans assemblies for [AspireExport] attributes and creates capability models.
/// Uses metadata reflection (RoMethod/RoType) for code generation.
/// </summary>
public static class AtsCapabilityScanner
{
    private const string AspireExportAttributeName = "Aspire.Hosting.AspireExportAttribute";
    private const string AspireCallbackAttributeName = "Aspire.Hosting.AspireCallbackAttribute";
    private const string ExtensionAttributeName = "System.Runtime.CompilerServices.ExtensionAttribute";

    /// <summary>
    /// Scans an assembly for [AspireExport] attributes and returns capability models.
    /// </summary>
    public static List<AtsCapabilityInfo> ScanAssembly(RoAssembly assembly, IWellKnownTypes wellKnownTypes)
    {
        var capabilities = new List<AtsCapabilityInfo>();

        foreach (var type in assembly.GetTypeDefinitions())
        {
            // Scan static classes (sealed, non-nested) for [AspireExport] attributes
            // Include both public and internal classes since capabilities aren't tied to CLR visibility
            if (!type.IsSealed || type.IsNested)
            {
                continue;
            }

            IReadOnlyList<RoMethod> methods;
            try
            {
                methods = type.Methods;
            }
            catch (ArgumentException)
            {
                // Skip types with methods that have unresolvable parameter types
                continue;
            }

            foreach (var method in methods)
            {
                try
                {
                    // Only scan public static methods
                    if (!method.IsStatic || !method.IsPublic)
                    {
                        continue;
                    }

                    var exportAttr = GetAspireExportAttribute(method);
                    if (exportAttr == null)
                    {
                        continue;
                    }

                    var capability = CreateCapabilityInfo(method, exportAttr, wellKnownTypes);
                    if (capability != null)
                    {
                        capabilities.Add(capability);
                    }
                }
                catch (ArgumentException)
                {
                    // Skip methods with unresolvable types (e.g., generic type parameters we can't resolve)
                    // This can happen with complex generic constraints
                    continue;
                }
            }
        }

        return capabilities;
    }

    /// <summary>
    /// Gets the [AspireExport] attribute from a method, or null if not present.
    /// </summary>
    private static RoCustomAttributeData? GetAspireExportAttribute(RoMethod method)
    {
        return method.GetCustomAttributes()
            .FirstOrDefault(a => a.AttributeType.FullName == AspireExportAttributeName);
    }

    /// <summary>
    /// Creates a capability info from a method and its [AspireExport] attribute.
    /// </summary>
    private static AtsCapabilityInfo? CreateCapabilityInfo(
        RoMethod method,
        RoCustomAttributeData exportAttr,
        IWellKnownTypes wellKnownTypes)
    {
        // Get the capability ID (first fixed argument)
        if (exportAttr.FixedArguments.Count == 0 || exportAttr.FixedArguments[0] is not string capabilityId)
        {
            return null;
        }

        // Get named arguments
        var namedArgs = exportAttr.NamedArguments.ToDictionary(kv => kv.Key, kv => kv.Value);
        var appliesTo = namedArgs.TryGetValue("AppliesTo", out var at) ? at as string : null;
        var description = namedArgs.TryGetValue("Description", out var desc) ? desc as string : null;
        var methodNameOverride = namedArgs.TryGetValue("MethodName", out var mn) ? mn as string : null;

        // Derive method name
        var methodName = methodNameOverride ?? DeriveMethodName(capabilityId);
        var package = DerivePackage(capabilityId);

        // Check if this is an extension method and get the type it extends
        var isExtensionMethod = HasAttribute(method, ExtensionAttributeName) && method.Parameters.Count > 0;
        string? extendsTypeId = null;

        if (isExtensionMethod)
        {
            var firstParam = method.Parameters[0];
            extendsTypeId = MapToAtsTypeId(firstParam.ParameterType, wellKnownTypes);
        }

        // Get parameters
        // - If AppliesTo is set: skip first param (method goes on builder class, first param is "this")
        // - Otherwise for extension methods: skip first param if it will be "this" on a generated class
        var parameters = new List<AtsParameterInfo>();
        var skipFirstParam = isExtensionMethod && (
            !string.IsNullOrEmpty(appliesTo) ||  // Has AppliesTo constraint
            extendsTypeId == "aspire/Builder"    // Extends IDistributedApplicationBuilder
        );
        var paramList = skipFirstParam ? method.Parameters.Skip(1) : method.Parameters;

        var paramIndex = 0;
        foreach (var param in paramList)
        {
            var paramInfo = CreateParameterInfo(param, paramIndex, wellKnownTypes);
            parameters.Add(paramInfo);
            paramIndex++;
        }

        // Get return type info
        var returnType = method.ReturnType;
        var returnTypeId = MapToAtsTypeId(returnType, wellKnownTypes);
        var returnsBuilder = returnTypeId != null &&
            (returnTypeId.StartsWith("aspire/") || IsResourceBuilderType(returnType, wellKnownTypes));

        return new AtsCapabilityInfo
        {
            CapabilityId = capabilityId,
            MethodName = methodName,
            Package = package,
            AppliesTo = appliesTo,
            Description = description,
            Parameters = parameters,
            ReturnTypeId = returnTypeId,
            IsExtensionMethod = isExtensionMethod,
            ExtendsTypeId = extendsTypeId,
            ReturnsBuilder = returnsBuilder
        };
    }

    /// <summary>
    /// Creates parameter info from a method parameter.
    /// </summary>
    private static AtsParameterInfo CreateParameterInfo(RoParameterInfo param, int paramIndex, IWellKnownTypes wellKnownTypes)
    {
        var paramType = param.ParameterType;
        var atsTypeId = MapToAtsTypeId(paramType, wellKnownTypes) ?? "any";

        // Check for [AspireCallback] attribute
        var callbackAttr = param.GetCustomAttributes()
            .FirstOrDefault(a => a.AttributeType.FullName == AspireCallbackAttributeName);
        var isCallback = callbackAttr != null;
        var callbackId = isCallback && callbackAttr!.FixedArguments.Count > 0
            ? callbackAttr.FixedArguments[0] as string
            : null;

        // Check if nullable
        var isNullable = wellKnownTypes.IsNullableOfT(paramType);
        if (isNullable)
        {
            paramType = paramType.GetGenericArguments()[0];
            atsTypeId = MapToAtsTypeId(paramType, wellKnownTypes) ?? "any";
        }

        return new AtsParameterInfo
        {
            Name = string.IsNullOrEmpty(param.Name) ? $"arg{paramIndex}" : param.Name,
            AtsTypeId = isCallback ? "callback" : atsTypeId,
            IsOptional = param.IsOptional,
            IsNullable = isNullable,
            IsCallback = isCallback,
            CallbackId = callbackId,
            DefaultValue = param.RawDefaultValue
        };
    }

    /// <summary>
    /// Derives the method name from a capability ID.
    /// </summary>
    /// <example>
    /// "aspire.redis/addRedis@1" -> "addRedis"
    /// "aspire/withEnvironment@1" -> "withEnvironment"
    /// </example>
    public static string DeriveMethodName(string capabilityId)
    {
        // Extract after last '/': "aspire.redis/addRedis@1" -> "addRedis@1"
        var slashIndex = capabilityId.LastIndexOf('/');
        var methodPart = slashIndex >= 0 ? capabilityId[(slashIndex + 1)..] : capabilityId;

        // Strip version suffix: "addRedis@1" -> "addRedis"
        var atIndex = methodPart.LastIndexOf('@');
        return atIndex >= 0 ? methodPart[..atIndex] : methodPart;
    }

    /// <summary>
    /// Derives the package name from a capability ID.
    /// </summary>
    /// <example>
    /// "aspire.redis/addRedis@1" -> "aspire.redis"
    /// "aspire/withEnvironment@1" -> "aspire"
    /// </example>
    public static string DerivePackage(string capabilityId)
    {
        var slashIndex = capabilityId.IndexOf('/');
        return slashIndex >= 0 ? capabilityId[..slashIndex] : "aspire";
    }

    /// <summary>
    /// Maps a CLR type to an ATS type ID for code generation.
    /// </summary>
    public static string? MapToAtsTypeId(RoType type, IWellKnownTypes wellKnownTypes)
    {
        // Handle generic type parameters (e.g., T in IResourceBuilder<T>)
        // These show up as "!0", "!1", etc. or "!!0", "!!1" for method parameters
        if (type.IsGenericParameter)
        {
            // For generic type parameters, return a special marker
            // The caller should use the AppliesTo constraint instead
            return "T";
        }

        // Handle void
        if (type.FullName == "System.Void")
        {
            return null;
        }

        // Handle Task/Task<T>
        var taskType = wellKnownTypes.GetKnownType(typeof(Task));
        if (type == taskType)
        {
            return null; // void
        }

        var taskOfTType = wellKnownTypes.GetKnownType(typeof(Task<>));
        if (type.IsGenericType && type.GenericTypeDefinition == taskOfTType)
        {
            var innerType = type.GetGenericArguments()[0];
            return MapToAtsTypeId(innerType, wellKnownTypes);
        }

        // Handle primitives
        if (type == wellKnownTypes.GetKnownType(typeof(string)))
        {
            return "string";
        }
        if (type == wellKnownTypes.GetKnownType(typeof(bool)))
        {
            return "boolean";
        }
        if (type == wellKnownTypes.GetKnownType(typeof(int)) ||
            type == wellKnownTypes.GetKnownType(typeof(long)) ||
            type == wellKnownTypes.GetKnownType(typeof(double)) ||
            type == wellKnownTypes.GetKnownType(typeof(float)) ||
            type == wellKnownTypes.GetKnownType(typeof(short)) ||
            type == wellKnownTypes.GetKnownType(typeof(byte)))
        {
            return "number";
        }

        // Handle Nullable<T>
        if (wellKnownTypes.IsNullableOfT(type))
        {
            var innerType = type.GetGenericArguments()[0];
            return MapToAtsTypeId(innerType, wellKnownTypes);
        }

        // Handle IResourceBuilder<T>
        if (wellKnownTypes.TryGetResourceBuilderTypeArgument(type, out var resourceType))
        {
            return GetResourceTypeId(resourceType);
        }

        // Handle IResource types
        if (wellKnownTypes.IResourceType.IsAssignableFrom(type))
        {
            return GetResourceTypeId(type);
        }

        // Handle known intrinsic types by name
        var intrinsicId = GetIntrinsicTypeId(type);
        if (intrinsicId != null)
        {
            return intrinsicId;
        }

        // Handle arrays
        if (type.IsArray)
        {
            var elementType = type.GetElementType();
            var elementId = elementType != null ? MapToAtsTypeId(elementType, wellKnownTypes) : "any";
            return $"{elementId}[]";
        }

        // Unknown type
        return "any";
    }

    /// <summary>
    /// Gets the ATS type ID for a resource type.
    /// </summary>
    /// <example>
    /// RedisResource -> "aspire/Redis"
    /// ContainerResource -> "aspire/Container"
    /// IResource -> "aspire/IResource"
    /// IResourceWithEnvironment -> "aspire/IResourceWithEnvironment"
    /// </example>
    public static string GetResourceTypeId(RoType resourceType)
    {
        var typeName = resourceType.Name;

        // Don't strip "Resource" suffix from interface types (IResource, IResourceWithXxx)
        // They should keep their full names
        var isInterface = typeName.StartsWith('I') && typeName.Length > 1 && char.IsUpper(typeName[1]);

        // Strip "Resource" suffix if present (but not for interfaces)
        if (!isInterface && typeName.EndsWith("Resource", StringComparison.Ordinal))
        {
            typeName = typeName[..^8];
        }

        return $"aspire/{typeName}";
    }

    /// <summary>
    /// Gets the ATS type ID for known intrinsic types by name matching.
    /// </summary>
    private static string? GetIntrinsicTypeId(RoType type)
    {
        // Map by full name
        return type.FullName switch
        {
            "Aspire.Hosting.IDistributedApplicationBuilder" => "aspire/Builder",
            "Aspire.Hosting.DistributedApplication" => "aspire/Application",
            "Aspire.Hosting.DistributedApplicationExecutionContext" => "aspire/ExecutionContext",
            "Aspire.Hosting.ApplicationModel.EndpointReference" => "aspire/EndpointReference",
            "Aspire.Hosting.ApplicationModel.ReferenceExpression" => "aspire/ReferenceExpression",
            "Aspire.Hosting.ApplicationModel.EnvironmentCallbackContext" => "aspire/EnvironmentContext",
            "Microsoft.Extensions.Configuration.IConfiguration" => "aspire/Configuration",
            "Microsoft.Extensions.Hosting.IHostEnvironment" => "aspire/HostEnvironment",
            "Aspire.Hosting.Eventing.DistributedApplicationEventSubscription" => "aspire/EventSubscription",
            "System.IServiceProvider" => "aspire/ServiceProvider",
            "Aspire.Hosting.ApplicationModel.ResourceNotificationService" => "aspire/ResourceNotificationService",
            "Aspire.Hosting.ApplicationModel.ResourceLoggerService" => "aspire/ResourceLoggerService",
            "Aspire.Hosting.ApplicationModel.IResource" => "aspire/IResource",
            "Aspire.Hosting.ApplicationModel.IResourceWithEnvironment" => "aspire/IResourceWithEnvironment",
            "Aspire.Hosting.ApplicationModel.IResourceWithEndpoints" => "aspire/IResourceWithEndpoints",
            "Aspire.Hosting.ApplicationModel.IResourceWithArgs" => "aspire/IResourceWithArgs",
            "Aspire.Hosting.ApplicationModel.IResourceWithConnectionString" => "aspire/IResourceWithConnectionString",
            "Aspire.Hosting.ApplicationModel.IResourceWithWaitSupport" => "aspire/IResourceWithWaitSupport",
            "Aspire.Hosting.ApplicationModel.IResourceWithParent" => "aspire/IResourceWithParent",
            "Aspire.Hosting.ApplicationModel.ContainerResource" => "aspire/Container",
            "Aspire.Hosting.ApplicationModel.ParameterResource" => "aspire/Parameter",
            _ => null
        };
    }

    /// <summary>
    /// Checks if a method has a specific attribute by name.
    /// </summary>
    private static bool HasAttribute(RoMethod method, string attributeFullName)
    {
        return method.GetCustomAttributes().Any(a => a.AttributeType.FullName == attributeFullName);
    }

    /// <summary>
    /// Checks if a type is IResourceBuilder&lt;T&gt;.
    /// </summary>
    private static bool IsResourceBuilderType(RoType type, IWellKnownTypes wellKnownTypes)
    {
        return wellKnownTypes.TryGetResourceBuilderTypeArgument(type, out _);
    }
}

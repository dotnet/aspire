// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.CodeGeneration.Models.Types;

namespace Aspire.Hosting.CodeGeneration.Models;

/// <summary>
/// Factory for creating <see cref="DistributedApplicationBuilderModel"/> instances
/// by reflecting on IDistributedApplicationBuilder and DistributedApplication types.
/// </summary>
public static class DistributedApplicationBuilderModelFactory
{
    /// <summary>
    /// Creates a DistributedApplicationBuilderModel by reflecting on the builder and application types.
    /// </summary>
    public static DistributedApplicationBuilderModel Create(IWellKnownTypes wellKnownTypes)
    {
        var builderType = wellKnownTypes.IDistributedApplicationBuilderType;
        var appType = wellKnownTypes.DistributedApplicationType;
        var obsoleteAttributeType = wellKnownTypes.AssemblyLoaderContext.GetType("System.ObsoleteAttribute");

        // Get builder properties (non-static, readable)
        var builderProperties = builderType.Properties
            .Where(p => p.CanRead && !p.IsStatic)
            .ToList();

        // Get builder methods (just Build for now)
        var builderMethods = builderType.Methods
            .Where(m => m.Name == "Build" && !m.IsStatic)
            .Where(m => !HasAttribute(m, obsoleteAttributeType))
            .ToList();

        // Get static factory methods (CreateBuilder overloads)
        var staticMethods = appType.Methods
            .Where(m => m.IsStatic && m.IsPublic && m.Name == "CreateBuilder")
            .Where(m => !HasAttribute(m, obsoleteAttributeType))
            .ToList();

        // Get application instance methods (Run, RunAsync, StartAsync, StopAsync, etc.)
        var appMethods = appType.Methods
            .Where(m => !m.IsStatic && m.IsPublic)
            .Where(m => !HasAttribute(m, obsoleteAttributeType))
            .Where(IsExposedApplicationMethod)
            .ToList();

        // Get application properties
        var appProperties = appType.Properties
            .Where(p => !p.IsStatic && p.CanRead)
            .ToList();

        // Build proxy types for all non-primitive property types (recursively discovers nested types)
        var proxyTypes = BuildProxyTypes(builderProperties, appProperties);

        return new DistributedApplicationBuilderModel
        {
            BuilderProperties = builderProperties,
            BuilderMethods = builderMethods,
            StaticFactoryMethods = staticMethods,
            ApplicationMethods = appMethods,
            ApplicationProperties = appProperties,
            ProxyTypes = proxyTypes
        };
    }

    private static bool HasAttribute(RoMethod method, RoType? attributeType)
    {
        if (attributeType is null)
        {
            return false;
        }
        return method.GetCustomAttributes().Any(attr => attr.AttributeType == attributeType);
    }

    private static bool IsExposedApplicationMethod(RoMethod method)
    {
        // Expose key lifecycle methods
        var exposedMethods = new HashSet<string>
        {
            "Run",
            "RunAsync",
            "StartAsync",
            "StopAsync",
            "Dispose",
            "DisposeAsync"
        };

        return exposedMethods.Contains(method.Name);
    }

    private static Dictionary<RoType, ProxyTypeModel> BuildProxyTypes(
        IReadOnlyList<RoPropertyInfo> builderProperties,
        IReadOnlyList<RoPropertyInfo> appProperties)
    {
        var proxyTypes = new Dictionary<RoType, ProxyTypeModel>();

        // Use a queue to process types recursively
        var typesToProcess = new Queue<RoType>();

        // Seed with property types from builder and application
        foreach (var prop in builderProperties.Concat(appProperties))
        {
            typesToProcess.Enqueue(prop.PropertyType);
        }

        while (typesToProcess.Count > 0)
        {
            var propertyType = typesToProcess.Dequeue();

            // Skip if already processed, primitive, or generic (generics are handled by FormatJsType as arrays/maps)
            if (proxyTypes.ContainsKey(propertyType) || IsPrimitiveType(propertyType) || propertyType.IsGenericType)
            {
                continue;
            }

            // Create a proxy for this type
            var proxyName = GetProxyClassName(propertyType);
            var properties = propertyType.Properties.Where(p => p.CanRead && !p.IsStatic).ToList();

            proxyTypes[propertyType] = new ProxyTypeModel
            {
                Type = propertyType,
                ProxyClassName = proxyName,
                Properties = properties,
                Methods = propertyType.Methods.Where(m => m.IsPublic && !m.IsStatic && IsExposedProxyMethod(m)).ToList(),
                StaticMethods = propertyType.Methods.Where(m => m.IsPublic && m.IsStatic && IsExposedProxyMethod(m)).ToList(),
                HelperMethods = []
            };

            // Queue nested property types for processing
            foreach (var nestedProp in properties)
            {
                if (!proxyTypes.ContainsKey(nestedProp.PropertyType) && !IsPrimitiveType(nestedProp.PropertyType))
                {
                    typesToProcess.Enqueue(nestedProp.PropertyType);
                }
            }
        }

        return proxyTypes;
    }

    private static bool IsPrimitiveType(RoType type)
    {
        // Types that don't need proxy wrappers
        var primitiveTypes = new HashSet<string>
        {
            "String",
            "Boolean",
            "Int32",
            "Int64",
            "Double",
            "Single",
            "Decimal",
            "Byte",
            "Char",
            "Object",
            "Void",
            "Assembly" // System.Reflection.Assembly doesn't need a proxy
        };

        return primitiveTypes.Contains(type.Name) || type.IsEnum;
    }

    private static string GetProxyClassName(RoType type)
    {
        var name = type.Name;

        // Remove 'I' prefix for interfaces
        if (name.StartsWith('I') && name.Length > 1 && char.IsUpper(name[1]))
        {
            name = name[1..];
        }

        // Remove generic arity suffix (e.g., `1)
        var tickIndex = name.IndexOf('`');
        if (tickIndex > 0)
        {
            name = name[..tickIndex];
        }

        return $"{name}Proxy";
    }

    private static bool IsExposedProxyMethod(RoMethod method)
    {
        // Filter out common object methods inherited from System.Object
        var excludedMethods = new HashSet<string>
        {
            "GetType",
            "GetHashCode",
            "Equals",
            "ToString",
            "MemberwiseClone",
            "Finalize"
        };

        return !excludedMethods.Contains(method.Name);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Rosetta.Models;

public class IntegrationModel
{
    // Types that should not be handled as model types
    private static readonly HashSet<Type> s_knownTypes = [typeof(string), typeof(Uri)];

    /// <summary>
    /// Gets or sets the name of the package.
    /// </summary>
    /// <example>Aspire.Hosting.Redis</example>
    public string AssemblyName => Assembly.GetName().Name ?? string.Empty;

    /// <summary>
    /// Integration models for integration package references.
    /// </summary>
    public List<IntegrationModel> Dependencies { get; } = [];

    /// <summary>
    /// Types implementing IResource.
    /// </summary>
    public Dictionary<Type, ResourceModel> Resources { get; } = [];

    /// <summary>
    /// Extension methods for IDistributedApplicationBuilder.
    /// </summary>
    /// <example>
    /// <code>
    /// IResourceBuilder&lt;RedisResource&gt; AddRedis(this IDistributedApplicationBuilder builder, [ResourceName] string name, int? port = null)
    /// </code>
    /// </example>
    public List<MethodInfo> IDistributedApplicationBuilderExtensionMethods { get; } = [];

    public List<MethodInfo> SharedExtensionMethods { get; } = [];

    public HashSet<Type> ModelTypes { get; } = [];

    public required Assembly Assembly { get; init; }

    public required IWellKnownTypes WellKnownTypes { get; init; }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public static IntegrationModel Create(IWellKnownTypes knownTypes, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var integration = new IntegrationModel
        {
            Assembly = assembly,
            WellKnownTypes = knownTypes
        };

        // List all types implementing IResource
        var types = assembly.GetTypes()
            .Where(t => !t.IsAbstract && t.IsPublic && knownTypes.IResourceType.IsAssignableFrom(t))
            .ToList();

        // Open generic resource builder methods
        var resourceBuilderMethods = GetExtensionMethods(assembly, knownTypes, knownTypes.IResourceBuilderType)
            .Where(m => m.IsGenericMethodDefinition)
            .ToArray();

        // Shared extension methods are those that can be used on multiple resource types.
        // Each resource type will decide if they need to import these based on the constraints on the method.
        // We will remove the methods that are constrained to a specific resource type from the shared extension methods.
        var sharedExtensionMethods = resourceBuilderMethods.ToHashSet();

        foreach (var r in types)
        {
            var resourceModel = new ResourceModel
            {
                ResourceType = r
            };

            integration.Resources.Add(r, resourceModel);
        }

        var dabMethods = GetExtensionMethods(assembly, knownTypes, knownTypes.IDistributedApplicationBuilderType)
            .Where(m => !m.ReturnType.ContainsGenericParameters)
            .ToArray();

        integration.IDistributedApplicationBuilderExtensionMethods.AddRange(dabMethods);

        integration.SharedExtensionMethods.AddRange(sharedExtensionMethods);

        integration.DiscoverModelClasses(integration.IDistributedApplicationBuilderExtensionMethods, integration.ModelTypes);
        integration.DiscoverModelClasses(integration.SharedExtensionMethods, integration.ModelTypes);

        return integration;
    }

    internal IEnumerable<MethodInfo> GetExtensionMethods(IWellKnownTypes wellKnownTypes, Type extendedType)
    {
        return GetExtensionMethods(Assembly, wellKnownTypes, extendedType);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    private static IEnumerable<MethodInfo> GetExtensionMethods(Assembly assembly, IWellKnownTypes wellKnownTypes, Type extendedType)
    {
        var obsoleteAttributeType = wellKnownTypes.GetKnownType<ObsoleteAttribute>();
        var extensionAttributeType = wellKnownTypes.GetKnownType<ExtensionAttribute>();

        var isGenericTypeDefinition = extendedType.IsGenericType && extendedType.IsTypeDefinition;
#pragma warning disable IL2070 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.
        var query = from type in assembly.GetTypes()
                    where type.IsSealed && !type.IsGenericType && !type.IsNested && type.IsPublic
                    from method in type.GetMethods(BindingFlags.Static | BindingFlags.Public)
                    where HasAttribute(method, extensionAttributeType)
                    where !HasAttribute(method, obsoleteAttributeType)
                    // where !HasFuncParameters(method) // We can't handle generate Func<,> parameters for now
                    where isGenericTypeDefinition
                        ? method.GetParameters()[0].ParameterType.IsGenericType && method.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == extendedType
                        : method.GetParameters()[0].ParameterType == extendedType
                    select method;
#pragma warning restore IL2070 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.
        return query;

        bool HasAttribute(MethodInfo method, Type attributeType)
        {
            return CustomAttributeData.GetCustomAttributes(method).Any(attr => attr.AttributeType == attributeType);
        }
    }

    public void DiscoverModelClasses(List<MethodInfo> methods, HashSet<Type> modelTypes)
    {
        // Collect classes for types that are used as return types or arguments in the extension methods which 
        // are not IResourceBuilder<T>.

        void ScanMethod(MethodInfo method)
        {
            var types = method.GetParameters()
                .Select(p => p.ParameterType)
                .ToList();

            types.Add(method.ReturnType);

            foreach (var t in types)
            {
                var type = t;

                if (type.IsGenericType && type.GetGenericTypeDefinition() == WellKnownTypes.GetKnownType(typeof(Nullable<>)))
                {
                    type = type.GetGenericArguments()[0];
                }

                // Check if the type is a delegate and scan its invoke method
#pragma warning disable IL2065 // The method has a DynamicallyAccessedMembersAttribute (which applies to the implicit 'this' parameter), but the value used for the 'this' parameter can not be statically analyzed.
#pragma warning disable IL2075 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.
                if (type.IsAssignableTo(WellKnownTypes.GetKnownType<Delegate>()) && type.GetMethod("Invoke") is { } invokeMethod)
                {
                    ScanMethod(invokeMethod);
                    continue;
                }
#pragma warning restore IL2075 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.
#pragma warning restore IL2065 // The method has a DynamicallyAccessedMembersAttribute (which applies to the implicit 'this' parameter), but the value used for the 'this' parameter can not be statically analyzed.

                if (!IsCandidate(type))
                {
                    continue;
                }

                modelTypes.Add(type);
            }
        }

        foreach (var method in methods)
        {
            ScanMethod(method);
        }

        bool IsCandidate(Type type)
        {
            var objectType = WellKnownTypes.GetKnownType<object>();
            var knownTypes = s_knownTypes.Select(WellKnownTypes.GetKnownType).ToHashSet();

            // Ignore types from mscorelib as they are supposed to be converted, but include enums
            // This should probably be converted to an allow-list of types
            // e.g. UnixFileMode

            var isCandidate = !type.IsGenericParameter &&
                !type.IsByRef &&
                type.IsPublic &&
                (type.Assembly != objectType.Assembly || type.IsEnum) && 
                !knownTypes.Contains(type) &&
               !(type.IsGenericType && type.GetGenericTypeDefinition() == WellKnownTypes.IResourceBuilderType);

            return isCandidate;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Rosetta.Models;

public class ResourceModel
{
    /// <summary>
    /// The resource type.
    /// </summary>
    /// <example>RedisResource</example>
    public required Type ResourceType { get; init; }

    /// <summary>
    /// Extension methods for IResourceBuilder{T} of the current resource.
    /// </summary>
    /// <example>
    /// <code>
    /// IResourceBuilder&lt;RedisResource&gt; WithRedisCommander(this IResourceBuilder&lt;RedisResource&gt; builder, Action&lt;IResourceBuilder&lt;RedisCommanderResource&gt;&gt;? configureContainer = null, string? containerName = null)
    /// </code>
    /// </example>
    public List<MethodInfo> IResourceTypeBuilderExtensionsMethods { get; } = [];

    public HashSet<Type> ModelTypes { get; } = [];

    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
    public void DiscoverExtensionMethods(IntegrationModel integrationModel)
    {
        // Add all concrete resource builder methods from this integration.

        // Look into all shared extensions from other integrations

        // Open generic methods need to be defined on the concrete builder type (RedisResourceBuilder) so they can return
        // the same builder type, not on a base builder (ResourceBuilder).

        // Methods constrained to the resource type: e.g., IResourceBuilder<T> WithVolume<T>(this IResourceBuilder<T> builder, ...) where T : ContainerResource
        // Methods constrained to an interface: e.g., IResourceBuilder<T> WithHttpEndpoint<T>(this IResourceBuilder<T> builder, ...) where T : IResourceWithEndpoints

        var allTypeForThisResource = new HashSet<Type>();
        var allInterfacesForThisResource = new HashSet<Type>();

        void PopulateInterfaces([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type t)
        {
            foreach (var i in t.GetInterfaces())
            {
                allInterfacesForThisResource.Add(i);
            }
        }

        // Inherited types
        var parentType = ResourceType;

        while (parentType != null && parentType != typeof(object))
        {
#pragma warning disable IL2072 // Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.
            PopulateInterfaces(parentType);
#pragma warning restore IL2072 // Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.
            allTypeForThisResource.Add(parentType);
            parentType = parentType.BaseType;
        }

        foreach (var type in allTypeForThisResource)
        {
#pragma warning disable IL2055 // Either the type on which the MakeGenericType is called can't be statically determined, or the type parameters to be used for generic arguments can't be statically determined.
            var targetType = integrationModel.WellKnownTypes.IResourceBuilderType.MakeGenericType(type);
#pragma warning restore IL2055 // Either the type on which the MakeGenericType is called can't be statically determined, or the type parameters to be used for generic arguments can't be statically determined.
            var methods = integrationModel.GetExtensionMethods(integrationModel.WellKnownTypes, targetType).ToArray();
            IResourceTypeBuilderExtensionsMethods.AddRange(methods);
        }

        // Open generic methods need to be defined on the concrete builder type (RedisResourceBuilder) so they can return
        // the same builder type, not on a base builder (ResourceBuilder).
        // Methods constrained to the resource type: e.g., IResourceBuilder<T> WithVolume<T>(this IResourceBuilder<T> builder, ...) where T : ContainerResource

        var openGenericMethods = new List<MethodInfo>();

        foreach (var m in integrationModel.SharedExtensionMethods)
        {
            var genArgs = m.GetGenericArguments();
            var genArg = genArgs[0];
            
            if (genArgs.Length > 1)
            {
                // TODO: Not supported:
                // public static IResourceBuilder<T> WithEnvironment<T, TValue>(this IResourceBuilder<T> builder, string name, TValue value)
                continue;
            }

            if (genArg.GetGenericParameterConstraints().All(x => allInterfacesForThisResource.Contains(x) || allTypeForThisResource.Contains(x)))
            {
#pragma warning disable IL2060 // Call to 'System.Reflection.MethodInfo.MakeGenericMethod' can not be statically analyzed. It's not possible to guarantee the availability of requirements of the generic method.
                openGenericMethods.Add(m.MakeGenericMethod(ResourceType));
#pragma warning restore IL2060 // Call to 'System.Reflection.MethodInfo.MakeGenericMethod' can not be statically analyzed. It's not possible to guarantee the availability of requirements of the generic method.
            }
        }

        // For debugging
        // Console.WriteLine($"Found {openGenericMethods.Length} open generic methods for {ResourceType.Name} in {integrationModel.AssemblyName}.");
        // foreach (var m in openGenericMethods)
        // {
        //     Console.WriteLine($"  {m.Name}");
        // }

        IResourceTypeBuilderExtensionsMethods.AddRange(openGenericMethods);

        integrationModel.DiscoverModelClasses(IResourceTypeBuilderExtensionsMethods, ModelTypes);
    }
}

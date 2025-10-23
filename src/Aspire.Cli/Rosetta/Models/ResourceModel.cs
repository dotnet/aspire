// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Rosetta.Models.Types;

namespace Aspire.Cli.Rosetta.Models;

internal class ResourceModel
{
    /// <summary>
    /// The resource type.
    /// </summary>
    /// <example>RedisResource</example>
    public required RoType ResourceType { get; init; }

    /// <summary>
    /// Extension methods for IResourceBuilder{T} of the current resource.
    /// </summary>
    /// <example>
    /// <code>
    /// IResourceBuilder&lt;RedisResource&gt; WithRedisCommander(this IResourceBuilder&lt;RedisResource&gt; builder, Action&lt;IResourceBuilder&lt;RedisCommanderResource&gt;&gt;? configureContainer = null, string? containerName = null)
    /// </code>
    /// </example>
    public List<RoMethod> IResourceTypeBuilderExtensionsMethods { get; } = [];

    public HashSet<RoType> ModelTypes { get; } = [];

    public void DiscoverOpenGenericExtensionMethods(IntegrationModel integrationModel)
    {
        // Add all concrete resource builder methods from this integration.

        // Look into all shared extensions from other integrations

        // Open generic methods need to be defined on the concrete builder type (RedisResourceBuilder) so they can return
        // the same builder type, not on a base builder (ResourceBuilder).

        // Methods constrained to the resource type: e.g., IResourceBuilder<T> WithVolume<T>(this IResourceBuilder<T> builder, ...) where T : ContainerResource
        // Methods constrained to an interface: e.g., IResourceBuilder<T> WithHttpEndpoint<T>(this IResourceBuilder<T> builder, ...) where T : IResourceWithEndpoints

        var allTypeForThisResource = new HashSet<RoType>();
        var allInterfacesForThisResource = new HashSet<RoType>();

        void PopulateInterfaces(RoType t)
        {
            foreach (var i in t.Interfaces)
            {
                allInterfacesForThisResource.Add(i);
            }
        }

        // Inherited types
        var parentType = ResourceType;

        var objectType = integrationModel.WellKnownTypes.GetKnownType(typeof(object));

        while (parentType != null && parentType != objectType)
        {
            PopulateInterfaces(parentType);
            allTypeForThisResource.Add(parentType);
            parentType = parentType.BaseType;
        }

        // Looks from IResourceBuilder<RedisResource> and also IResourceBuilder<IResourceWithConnectionString>
        foreach (var type in allTypeForThisResource.Union(allInterfacesForThisResource))
        {
            var targetType = integrationModel.WellKnownTypes.IResourceBuilderType.MakeGenericType(type);
            var methods = integrationModel.GetExtensionMethods(integrationModel.WellKnownTypes, targetType).ToArray();
            IResourceTypeBuilderExtensionsMethods.AddRange(methods);
        }

        // Open generic methods need to be defined on the concrete builder type (RedisResourceBuilder) so they can return
        // the same builder type, not on a base builder (ResourceBuilder).
        // Methods constrained to the resource type: e.g., IResourceBuilder<T> WithVolume<T>(this IResourceBuilder<T> builder, ...) where T : ContainerResource

        var openGenericMethods = new List<RoMethod>();

        foreach (var m in integrationModel.SharedExtensionMethods)
        {
            var genArgs = m.GetGenericArguments();
            var genArg = genArgs[0];

            if (genArgs.Count > 1)
            {
                // TODO: Not supported:
                // public static IResourceBuilder<T> WithEnvironment<T, TValue>(this IResourceBuilder<T> builder, string name, TValue value)
                continue;
            }

            if (genArg.GetGenericParameterConstraints().All(x => allInterfacesForThisResource.Contains(x) || allTypeForThisResource.Contains(x)))
            {
                openGenericMethods.Add(m.MakeGenericMethod(ResourceType));
            }
        }

        IResourceTypeBuilderExtensionsMethods.AddRange(openGenericMethods);

        integrationModel.DiscoverModelClasses(IResourceTypeBuilderExtensionsMethods, ModelTypes);
    }
}

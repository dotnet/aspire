// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Aspire.Cli.Rosetta.Models.Types;

namespace Aspire.Cli.Rosetta.Models;

[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Types are coming from System.Reflection.Metadata which are trim/aot compatible")]
[UnconditionalSuppressMessage("Trimming", "IL2055", Justification = "Types are coming from System.Reflection.Metadata which are trim/aot compatible")]
[UnconditionalSuppressMessage("Trimming", "IL2060", Justification = "Types are coming from System.Reflection.Metadata which are trim/aot compatible")]
[UnconditionalSuppressMessage("Trimming", "IL2065", Justification = "Types are coming from System.Reflection.Metadata which are trim/aot compatible")]
[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Types are coming from System.Reflection.Metadata which are trim/aot compatible")]
[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Types are coming from System.Reflection.Metadata which are trim/aot compatible")]
[UnconditionalSuppressMessage("Trimming", "IL2104", Justification = "Types are coming from System.Reflection.Metadata which are trim/aot compatible")]
[UnconditionalSuppressMessage("Trimming", "IL3001", Justification = "Types are coming from System.Reflection.Metadata which are trim/aot compatible")]
[UnconditionalSuppressMessage("Trimming", "IL3050", Justification = "Types are coming from System.Reflection.Metadata which are trim/aot compatible")]
[UnconditionalSuppressMessage("Trimming", "IL3053", Justification = "Types are coming from System.Reflection.Metadata which are trim/aot compatible")]
internal class IntegrationModel
{
    // Types that should not be handled as model types
    private static readonly HashSet<Type> s_knownTypes = [typeof(string), typeof(Uri)];

    /// <summary>
    /// Gets or sets the name of the package.
    /// </summary>
    /// <example>Aspire.Hosting.Redis</example>
    public string AssemblyName => Assembly.Name ?? string.Empty;

    /// <summary>
    /// Integration models for integration package references.
    /// </summary>
    public List<IntegrationModel> Dependencies { get; } = [];

    /// <summary>
    /// Types implementing IResource.
    /// </summary>
    public Dictionary<RoType, ResourceModel> Resources { get; } = [];

    /// <summary>
    /// Extension methods for IDistributedApplicationBuilder.
    /// </summary>
    /// <example>
    /// <code>
    /// IResourceBuilder&lt;RedisResource&gt; AddRedis(this IDistributedApplicationBuilder builder, [ResourceName] string name, int? port = null)
    /// </code>
    /// </example>
    public List<RoMethod> IDistributedApplicationBuilderExtensionMethods { get; } = [];

    public List<RoMethod> SharedExtensionMethods { get; } = [];

    public HashSet<RoType> ModelTypes { get; } = [];

    public required RoAssembly Assembly { get; init; }

    public required IWellKnownTypes WellKnownTypes { get; init; }

    public static IntegrationModel Create(IWellKnownTypes knownTypes, RoAssembly assembly)
    {
        var integration = new IntegrationModel
        {
            Assembly = assembly,
            WellKnownTypes = knownTypes
        };

        // List all types implementing IResource
        var types = assembly.GetTypeDefinitions()
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

    internal IEnumerable<RoMethod> GetExtensionMethods(IWellKnownTypes wellKnownTypes, RoType extendedType)
    {
        return GetExtensionMethods(Assembly, wellKnownTypes, extendedType);
    }

    private static IEnumerable<RoMethod> GetExtensionMethods(RoAssembly assembly, IWellKnownTypes wellKnownTypes, RoType extendedType)
    {
        var obsoleteAttributeType = wellKnownTypes.GetKnownType<ObsoleteAttribute>();
        var extensionAttributeType = wellKnownTypes.GetKnownType<ExtensionAttribute>();

        var isGenericTypeDefinition = extendedType.IsGenericType && extendedType.IsTypeDefinition;
        var query = from type in assembly.GetTypeDefinitions()
                    where type.IsSealed && !type.IsGenericType && !type.IsNested && type.IsPublic
                    from method in type.Methods
                    where method.IsStatic && method.IsPublic
                    where HasAttribute(method, extensionAttributeType)
                    where !HasAttribute(method, obsoleteAttributeType)
                    // where !HasFuncParameters(method) // We can't handle generate Func<,> parameters for now
                    where isGenericTypeDefinition
                        ? method.Parameters[0].ParameterType.IsGenericType && method.Parameters[0].ParameterType.GenericTypeDefinition == extendedType
                        : method.Parameters[0].ParameterType == extendedType
                    select method;
        return query;

        bool HasAttribute(RoMethod method, RoType attributeType)
        {
            return method.GetCustomAttributes().Any(attr => attr.AttributeType == attributeType);
        }
    }

    public void DiscoverModelClasses(List<RoMethod> methods, HashSet<RoType> modelTypes)
    {
        // Collect classes for types that are used as return types or arguments in the extension methods which 
        // are not IResourceBuilder<T>.

        void ScanMethod(RoMethod method)
        {
            var types = method.Parameters
                .Select(p => p.ParameterType)
                .ToList();

            types.Add(method.ReturnType);

            foreach (var t in types)
            {
                var type = t;

                if (type.IsGenericType && type.GenericTypeDefinition == WellKnownTypes.GetKnownType(typeof(Nullable<>)))
                {
                    type = type.GetGenericArguments()[0];
                }

                // Check if the type is a delegate and scan its invoke method
                if (type.IsAssignableTo(WellKnownTypes.GetKnownType<Delegate>()) && type.GetMethod("Invoke") is { } invokeMethod)
                {
                    ScanMethod(invokeMethod);
                    continue;
                }

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

        bool IsCandidate(RoType type)
        {
            var objectType = WellKnownTypes.GetKnownType<object>();
            var knownTypes = s_knownTypes.Select(WellKnownTypes.GetKnownType).ToHashSet();

            // Ignore types from mscorelib as they are supposed to be converted, but include enums
            // This should probably be converted to an allow-list of types
            // e.g. UnixFileMode

            var isCandidate = !type.IsGenericParameter &&
                !type.IsByRef &&
                type.IsPublic &&
                (type.DeclaringAssembly != objectType.DeclaringAssembly || type.IsEnum) &&
                !knownTypes.Contains(type) &&
               !(type.IsGenericType && type.GenericTypeDefinition == WellKnownTypes.IResourceBuilderType);

            return isCandidate;
        }
    }
}

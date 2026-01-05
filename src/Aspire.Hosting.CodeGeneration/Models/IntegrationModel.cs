// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Aspire.Hosting.CodeGeneration.Ats;
using Aspire.Hosting.CodeGeneration.Models.Ats;
using Aspire.Hosting.CodeGeneration.Models.Types;
using AtsTypeMapping = Aspire.Hosting.Ats.AtsTypeMapping;

namespace Aspire.Hosting.CodeGeneration.Models;

public sealed class IntegrationModel
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

    /// <summary>
    /// ATS capabilities discovered via [AspireExport] attributes.
    /// </summary>
    /// <remarks>
    /// These capabilities are used by the ATS code generators to produce
    /// capability-based SDKs that use invokeCapability() instead of invokeStaticMethod().
    /// </remarks>
    public List<AtsCapabilityInfo> Capabilities { get; } = [];

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
        var resourceTypes = assembly.GetTypeDefinitions()
            .Where(t => !t.IsAbstract && t.IsPublic && knownTypes.IResourceType.IsAssignableFrom(t))
            .ToList();

        // Open generic resource builder methods, e.g.,
        // WithVolume<T>(this IResourceBuilder<T> builder, ...)
        var resourceBuilderMethods = GetExtensionMethods(assembly, knownTypes, knownTypes.IResourceBuilderType)
            .Where(m => m.IsGenericMethodDefinition)
            .ToArray();

        // Shared extension methods are those that can be used on multiple resource types.
        // Each resource type will decide if they need to import these based on the constraints on the method.
        // We will remove the methods that are constrained to a specific resource type from the shared extension methods.
        var sharedExtensionMethods = resourceBuilderMethods.ToHashSet();

        // List all extension methods for IDistributedApplicationBuilder
        // e.g., AddRedis(this IDistributedApplicationBuilder builder, ...)
        var dabMethods = GetExtensionMethods(assembly, knownTypes, knownTypes.IDistributedApplicationBuilderType)
            .Where(m => !m.ReturnType.ContainsGenericParameters)
            .ToArray();

        integration.IDistributedApplicationBuilderExtensionMethods.AddRange(dabMethods);

        integration.SharedExtensionMethods.AddRange(sharedExtensionMethods);

        integration.DiscoverModelClasses(integration.IDistributedApplicationBuilderExtensionMethods, integration.ModelTypes);
        integration.DiscoverModelClasses(integration.SharedExtensionMethods, integration.ModelTypes);

        // Build type mapping from both the integration assembly and Aspire.Hosting assembly
        // This ensures types like IDistributedApplicationBuilder -> "aspire/Builder" are available
        var hostingAssembly = knownTypes.IDistributedApplicationBuilderType.DeclaringAssembly;
        var typeMapping = hostingAssembly != assembly
            ? AtsTypeMapping.FromAssemblies([
                new RoAssemblyInfoWrapper(hostingAssembly),
                new RoAssemblyInfoWrapper(assembly)])
            : AtsTypeMapping.FromAssembly(new RoAssemblyInfoWrapper(assembly));

        // Scan for ATS capabilities via [AspireExport] attributes
        var capabilities = AtsCapabilityScanner.ScanAssembly(assembly, knownTypes, typeMapping);
        integration.Capabilities.AddRange(capabilities);

        foreach (var r in resourceTypes)
        {
            var resourceModel = new ResourceModel
            {
                ResourceType = r
            };

            integration.Resources.Add(r, resourceModel);
        }

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
        // PolyglotIgnoreAttribute is optional - it may not exist in older versions of Aspire.Hosting
        var polyglotAttributeType = assembly.AssemblyLoaderContext.GetType("Aspire.Hosting.Polyglot.PolyglotIgnoreAttribute");

        var isGenericTypeDefinition = extendedType.IsGenericType && extendedType.IsTypeDefinition;
        var query = from type in assembly.GetTypeDefinitions()
                    where type.IsSealed && !type.IsGenericType && !type.IsNested && type.IsPublic
                    from method in type.Methods
                    where method.IsStatic && method.IsPublic
                    where HasAttribute(method, extensionAttributeType)
                    where !HasAttribute(method, obsoleteAttributeType)
                    where polyglotAttributeType is null || !HasAttribute(method, polyglotAttributeType)
                    where method.Parameters.Count >= 1
                    // where !HasFuncParameters(method) // We can't handle generate Func<,> parameters for now
                    where isGenericTypeDefinition
                        ? method.Parameters[0].ParameterType.IsGenericType && method.Parameters[0].ParameterType.GenericTypeDefinition == extendedType
                        : method.Parameters[0].ParameterType == extendedType
                    select method;
        return query;

        bool HasAttribute(RoMethod method, RoType? attributeType)
        {
            if (attributeType is null)
            {
                return false;
            }
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

                // Check for IResourceBuilder<T> where T is an interface - need builder classes for these
                if (WellKnownTypes.TryGetResourceBuilderTypeArgument(type, out var resourceType) && resourceType.IsInterface)
                {
                    // Add interface types as resources so they get builder classes generated
                    if (!Resources.ContainsKey(resourceType))
                    {
                        Resources.Add(resourceType, new ResourceModel { ResourceType = resourceType });
                    }
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

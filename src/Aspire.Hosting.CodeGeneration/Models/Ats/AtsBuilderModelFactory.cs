// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.CodeGeneration.Models.Ats;

/// <summary>
/// Factory for creating builder models from discovered capabilities.
/// Groups capabilities by AppliesTo, establishes type hierarchy, and derives builder class names.
/// </summary>
public static class AtsBuilderModelFactory
{
    // Well-known interface type IDs in inheritance order (most specific first)
    private static readonly string[] s_interfaceTypeIds =
    [
        AtsTypeMapping.TypeIds.IResourceWithConnectionString,
        AtsTypeMapping.TypeIds.IResourceWithWaitSupport,
        AtsTypeMapping.TypeIds.IResourceWithEndpoints,
        AtsTypeMapping.TypeIds.IResourceWithArgs,
        AtsTypeMapping.TypeIds.IResourceWithEnvironment,
        AtsTypeMapping.TypeIds.IResourceWithParent,
        AtsTypeMapping.TypeIds.IResource
    ];

    /// <summary>
    /// Creates builder models from a collection of capabilities.
    /// </summary>
    /// <param name="capabilities">All discovered capabilities.</param>
    /// <returns>Builder models grouped by type, with inheritance relationships established.</returns>
    public static List<AtsBuilderInfo> CreateBuilderModels(IEnumerable<AtsCapabilityInfo> capabilities)
    {
        var capabilityList = capabilities.ToList();
        var builders = new Dictionary<string, AtsBuilderInfo>();

        // First pass: group capabilities by AppliesTo
        var capabilitiesByType = new Dictionary<string, List<AtsCapabilityInfo>>();

        foreach (var capability in capabilityList)
        {
            if (string.IsNullOrEmpty(capability.AppliesTo))
            {
                // Entry point methods (createBuilder, build, run) go on AspireClient, not builders
                continue;
            }

            if (!capabilitiesByType.TryGetValue(capability.AppliesTo, out var list))
            {
                list = [];
                capabilitiesByType[capability.AppliesTo] = list;
            }
            list.Add(capability);
        }

        // Second pass: create builder info for each type
        foreach (var (typeId, typeCapabilities) in capabilitiesByType)
        {
            var builderClassName = DeriveBuilderClassName(typeId);
            var isInterface = IsInterfaceType(typeId);

            var builder = new AtsBuilderInfo
            {
                TypeId = typeId,
                BuilderClassName = builderClassName,
                Capabilities = typeCapabilities,
                IsInterface = isInterface
            };

            // Set parent type IDs for inheritance
            if (!isInterface)
            {
                // Concrete types inherit from interface types
                foreach (var interfaceTypeId in s_interfaceTypeIds)
                {
                    if (capabilitiesByType.ContainsKey(interfaceTypeId))
                    {
                        builder.ParentTypeIds.Add(interfaceTypeId);
                    }
                }
            }
            else
            {
                // Interface types inherit from their parent interfaces
                var currentIndex = Array.IndexOf(s_interfaceTypeIds, typeId);
                if (currentIndex >= 0)
                {
                    for (var i = currentIndex + 1; i < s_interfaceTypeIds.Length; i++)
                    {
                        if (capabilitiesByType.ContainsKey(s_interfaceTypeIds[i]))
                        {
                            builder.ParentTypeIds.Add(s_interfaceTypeIds[i]);
                            break; // Only add immediate parent
                        }
                    }
                }
            }

            builders[typeId] = builder;
        }

        // Sort: interfaces first (in order), then concrete types alphabetically
        return builders.Values
            .OrderBy(b => !b.IsInterface)
            .ThenBy(b => b.IsInterface ? Array.IndexOf(s_interfaceTypeIds, b.TypeId) : 0)
            .ThenBy(b => b.BuilderClassName)
            .ToList();
    }

    /// <summary>
    /// Gets entry point capabilities (those without AppliesTo).
    /// These become methods on AspireClient rather than builder classes.
    /// </summary>
    public static List<AtsCapabilityInfo> GetEntryPointCapabilities(IEnumerable<AtsCapabilityInfo> capabilities)
    {
        return capabilities
            .Where(c => string.IsNullOrEmpty(c.AppliesTo))
            .ToList();
    }

    /// <summary>
    /// Derives the builder class name from an ATS type ID.
    /// </summary>
    /// <example>
    /// "aspire/Redis" → "RedisBuilder"
    /// "aspire/Container" → "ContainerBuilder"
    /// "aspire/IResourceWithEnvironment" → "ResourceWithEnvironmentBuilderBase"
    /// </example>
    public static string DeriveBuilderClassName(string typeId)
    {
        // Extract type name after last '/'
        var slashIndex = typeId.LastIndexOf('/');
        var typeName = slashIndex >= 0 ? typeId[(slashIndex + 1)..] : typeId;

        // Handle interface types
        if (typeName.StartsWith("IResource", StringComparison.Ordinal))
        {
            // "IResourceWithEnvironment" → "ResourceWithEnvironment"
            typeName = typeName[1..]; // Remove leading 'I'
            return $"{typeName}BuilderBase";
        }

        return $"{typeName}Builder";
    }

    /// <summary>
    /// Checks if a type ID represents an interface type.
    /// </summary>
    private static bool IsInterfaceType(string typeId)
    {
        var slashIndex = typeId.LastIndexOf('/');
        var typeName = slashIndex >= 0 ? typeId[(slashIndex + 1)..] : typeId;

        return typeName.StartsWith('I') && char.IsUpper(typeName[1]);
    }

    /// <summary>
    /// Gets the handle type alias name for a type ID.
    /// </summary>
    /// <example>
    /// "aspire/Redis" → "RedisBuilderHandle"
    /// "aspire/Builder" → "BuilderHandle"
    /// </example>
    public static string GetHandleTypeName(string typeId)
    {
        var slashIndex = typeId.LastIndexOf('/');
        var typeName = slashIndex >= 0 ? typeId[(slashIndex + 1)..] : typeId;

        // Special cases for core types
        if (typeName == "Builder")
        {
            return "BuilderHandle";
        }
        if (typeName == "Application")
        {
            return "ApplicationHandle";
        }
        if (typeName == "ExecutionContext")
        {
            return "ExecutionContextHandle";
        }

        // For interface types, keep the I prefix
        if (typeName.StartsWith('I') && typeName.Length > 1 && char.IsUpper(typeName[1]))
        {
            return $"{typeName}Handle";
        }

        return $"{typeName}BuilderHandle";
    }
}

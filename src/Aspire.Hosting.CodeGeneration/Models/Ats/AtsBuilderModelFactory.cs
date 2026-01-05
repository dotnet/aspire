// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.CodeGeneration.Models.Ats;

/// <summary>
/// Factory for creating builder models from discovered capabilities.
/// Flattens capabilities based on type hierarchy inferred from CLR metadata.
/// </summary>
public static class AtsBuilderModelFactory
{
    /// <summary>
    /// Creates builder models from capabilities and type information.
    /// Flattens capabilities so each concrete builder has all applicable methods.
    /// </summary>
    /// <param name="capabilities">All discovered capabilities.</param>
    /// <param name="typeInfos">Type information including interface implementations.</param>
    /// <returns>Builder models with flattened capabilities.</returns>
    public static List<AtsBuilderInfo> CreateBuilderModels(
        IEnumerable<AtsCapabilityInfo> capabilities,
        IEnumerable<AtsTypeInfo> typeInfos)
    {
        var capabilityList = capabilities.ToList();
        var typeInfoList = typeInfos.ToList();
        var builders = new List<AtsBuilderInfo>();

        // Group capabilities by their constraint type ID
        var capsByConstraint = capabilityList
            .Where(c => !string.IsNullOrEmpty(c.ConstraintTypeId))
            .GroupBy(c => c.ConstraintTypeId!)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Create a builder for each concrete (non-interface) type
        foreach (var typeInfo in typeInfoList.Where(t => !t.IsInterface))
        {
            var builder = new AtsBuilderInfo
            {
                TypeId = typeInfo.AtsTypeId,
                BuilderClassName = DeriveBuilderClassName(typeInfo.AtsTypeId),
                IsInterface = false,
                Capabilities = new List<AtsCapabilityInfo>()
            };

            // Collect capabilities where constraint matches this type directly
            if (capsByConstraint.TryGetValue(typeInfo.AtsTypeId, out var directCaps))
            {
                foreach (var cap in directCaps)
                {
                    if (!builder.Capabilities.Any(c => c.CapabilityId == cap.CapabilityId))
                    {
                        builder.Capabilities.Add(cap);
                    }
                }
            }

            // Flatten: collect capabilities from all implemented interfaces
            foreach (var ifaceTypeId in typeInfo.ImplementedInterfaceTypeIds)
            {
                if (capsByConstraint.TryGetValue(ifaceTypeId, out var ifaceCaps))
                {
                    foreach (var cap in ifaceCaps)
                    {
                        if (!builder.Capabilities.Any(c => c.CapabilityId == cap.CapabilityId))
                        {
                            builder.Capabilities.Add(cap);
                        }
                    }
                }
            }

            // Only add builders that have at least one capability
            if (builder.Capabilities.Count > 0)
            {
                builders.Add(builder);
            }
        }

        // Sort alphabetically by builder class name
        return builders
            .OrderBy(b => b.BuilderClassName)
            .ToList();
    }

    /// <summary>
    /// Creates builder models from a collection of capabilities (legacy overload).
    /// Uses a simple grouping without type hierarchy flattening.
    /// </summary>
    /// <param name="capabilities">All discovered capabilities.</param>
    /// <returns>Builder models grouped by constraint type.</returns>
    public static List<AtsBuilderInfo> CreateBuilderModels(IEnumerable<AtsCapabilityInfo> capabilities)
    {
        var capabilityList = capabilities.ToList();
        var builders = new Dictionary<string, AtsBuilderInfo>();

        // Group capabilities by ConstraintTypeId
        var capabilitiesByType = new Dictionary<string, List<AtsCapabilityInfo>>();

        foreach (var capability in capabilityList)
        {
            if (string.IsNullOrEmpty(capability.ConstraintTypeId))
            {
                // Entry point methods (createBuilder, build, run) go on AspireClient, not builders
                continue;
            }

            if (!capabilitiesByType.TryGetValue(capability.ConstraintTypeId, out var list))
            {
                list = [];
                capabilitiesByType[capability.ConstraintTypeId] = list;
            }
            list.Add(capability);
        }

        // Create builder info for each type
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

            builders[typeId] = builder;
        }

        // Sort: concrete types first (alphabetically), then interfaces
        return builders.Values
            .OrderBy(b => b.IsInterface)
            .ThenBy(b => b.BuilderClassName)
            .ToList();
    }

    /// <summary>
    /// Gets entry point capabilities (those without ConstraintTypeId).
    /// These become methods on AspireClient rather than builder classes.
    /// </summary>
    public static List<AtsCapabilityInfo> GetEntryPointCapabilities(IEnumerable<AtsCapabilityInfo> capabilities)
    {
        return capabilities
            .Where(c => string.IsNullOrEmpty(c.ConstraintTypeId))
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

        return typeName.StartsWith('I') && typeName.Length > 1 && char.IsUpper(typeName[1]);
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

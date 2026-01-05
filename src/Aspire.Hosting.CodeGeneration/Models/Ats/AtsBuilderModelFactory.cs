// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.CodeGeneration.Models.Ats;

/// <summary>
/// Factory for creating builder models from discovered capabilities.
/// Groups capabilities by their first parameter type (flat model).
/// </summary>
public static class AtsBuilderModelFactory
{
    // Well-known ATS type IDs
    private const string BuilderTypeId = "aspire/Builder";
    private const string ApplicationTypeId = "aspire/Application";

    /// <summary>
    /// Creates builder models from capabilities and type information.
    /// Uses a flat model where all resource capabilities go on all resource builders.
    /// </summary>
    /// <param name="capabilities">All discovered capabilities.</param>
    /// <param name="typeInfos">Type information for concrete resource types.</param>
    /// <returns>Builder models with flattened capabilities.</returns>
    public static List<AtsBuilderInfo> CreateBuilderModels(
        IEnumerable<AtsCapabilityInfo> capabilities,
        IEnumerable<AtsTypeInfo> typeInfos)
    {
        var capabilityList = capabilities.ToList();
        var typeInfoList = typeInfos.ToList();
        var builders = new List<AtsBuilderInfo>();

        // Categorize capabilities by first parameter type
        var builderCapabilities = new List<AtsCapabilityInfo>();
        var resourceCapabilities = new List<AtsCapabilityInfo>();
        var applicationCapabilities = new List<AtsCapabilityInfo>();

        foreach (var cap in capabilityList)
        {
            var extendsType = cap.ExtendsTypeId;
            if (string.IsNullOrEmpty(extendsType))
            {
                // Entry point methods (createBuilder, etc.) - skip, handled separately
                continue;
            }

            if (extendsType == BuilderTypeId)
            {
                builderCapabilities.Add(cap);
            }
            else if (extendsType == ApplicationTypeId)
            {
                applicationCapabilities.Add(cap);
            }
            else if (extendsType.StartsWith("aspire/"))
            {
                // All other aspire/* types are resource builders
                resourceCapabilities.Add(cap);
            }
        }

        // Create a builder for each concrete (non-interface) resource type
        // All resource capabilities go on ALL resource builders (flat model)
        foreach (var typeInfo in typeInfoList.Where(t => !t.IsInterface))
        {
            // Skip Builder and Application - they're not resource types
            if (typeInfo.AtsTypeId == BuilderTypeId || typeInfo.AtsTypeId == ApplicationTypeId)
            {
                continue;
            }

            var builder = new AtsBuilderInfo
            {
                TypeId = typeInfo.AtsTypeId,
                BuilderClassName = DeriveBuilderClassName(typeInfo.AtsTypeId),
                IsInterface = false,
                Capabilities = resourceCapabilities.ToList()
            };

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
    /// Uses the flat grouping model based on first parameter type.
    /// </summary>
    /// <param name="capabilities">All discovered capabilities.</param>
    /// <returns>Builder models grouped by first parameter type.</returns>
    public static List<AtsBuilderInfo> CreateBuilderModels(IEnumerable<AtsCapabilityInfo> capabilities)
    {
        var capabilityList = capabilities.ToList();
        var builders = new Dictionary<string, AtsBuilderInfo>();

        // Categorize capabilities by first parameter type
        var builderCapabilities = new List<AtsCapabilityInfo>();
        var resourceCapabilities = new List<AtsCapabilityInfo>();
        var applicationCapabilities = new List<AtsCapabilityInfo>();
        var resourceTypeIds = new HashSet<string>();

        foreach (var cap in capabilityList)
        {
            var extendsType = cap.ExtendsTypeId;
            if (string.IsNullOrEmpty(extendsType))
            {
                // Entry point methods - skip, handled by GetEntryPointCapabilities
                continue;
            }

            if (extendsType == BuilderTypeId)
            {
                builderCapabilities.Add(cap);
            }
            else if (extendsType == ApplicationTypeId)
            {
                applicationCapabilities.Add(cap);
            }
            else if (extendsType.StartsWith("aspire/"))
            {
                // All other aspire/* types are resource builders
                resourceCapabilities.Add(cap);
                resourceTypeIds.Add(extendsType);
            }
        }

        // Create a single "generic" resource builder that has all resource capabilities
        // In the flat model, all resource capabilities are available on all resource types
        foreach (var typeId in resourceTypeIds)
        {
            var builderClassName = DeriveBuilderClassName(typeId);
            var isInterface = IsInterfaceType(typeId);

            var builder = new AtsBuilderInfo
            {
                TypeId = typeId,
                BuilderClassName = builderClassName,
                Capabilities = resourceCapabilities.ToList(),
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
    /// Gets entry point capabilities (those without ExtendsTypeId).
    /// These become methods on AspireClient rather than builder classes.
    /// </summary>
    public static List<AtsCapabilityInfo> GetEntryPointCapabilities(IEnumerable<AtsCapabilityInfo> capabilities)
    {
        return capabilities
            .Where(c => string.IsNullOrEmpty(c.ExtendsTypeId))
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

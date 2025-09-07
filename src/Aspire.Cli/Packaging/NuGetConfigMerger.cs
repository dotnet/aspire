// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;
using System.Diagnostics.CodeAnalysis;

namespace Aspire.Cli.Packaging;

internal class NuGetConfigMerger
{
    private sealed record NuGetConfigContext
    {
        public required XDocument Document { get; init; }
        public required XElement Configuration { get; init; }
        public required XElement PackageSources { get; init; }
        public XElement? PackageSourceMapping { get; init; }
        public required PackageMapping[] Mappings { get; init; }
        public required string[] RequiredSources { get; init; }
        public required XElement[] ExistingAdds { get; init; }
        public required Dictionary<string, string> UrlToExistingKey { get; init; }
    }
    /// <summary>
    /// Creates or updates a NuGet.config file in the specified directory based on the provided <see cref="PackageChannel"/>.
    /// For implicit channels (no explicit mappings) this method is a no-op.
    /// </summary>
    /// <param name="targetDirectory">The directory where the NuGet.config should be created or updated.</param>
    /// <param name="channel">The package channel providing mapping information.</param>
    public static async Task CreateOrUpdateAsync(DirectoryInfo targetDirectory, PackageChannel channel)
    {
        ArgumentNullException.ThrowIfNull(targetDirectory);
        ArgumentNullException.ThrowIfNull(channel);

        // Only explicit channels (with mappings) require a NuGet.config merge/write.
        var mappings = channel.Mappings;
        if (channel.Type is not PackageChannelType.Explicit || mappings is null || mappings.Length == 0)
        {
            return;
        }

        if (!targetDirectory.Exists)
        {
            targetDirectory.Create();
        }

        if (!TryFindNuGetConfigInDirectory(targetDirectory, out var nugetConfigFile))
        {
            await CreateNewNuGetConfigAsync(targetDirectory, mappings);
        }
        else
        {
            await UpdateExistingNuGetConfigAsync(nugetConfigFile, mappings);
        }
    }

    private static async Task CreateNewNuGetConfigAsync(DirectoryInfo targetDirectory, PackageMapping[] mappings)
    {
        if (mappings.Length == 0)
        {
            return;
        }

        var targetPath = Path.Combine(targetDirectory.FullName, "NuGet.config");
        using var tmpConfig = await TemporaryNuGetConfig.CreateAsync(mappings);
        File.Copy(tmpConfig.ConfigFile.FullName, targetPath, overwrite: true);
    }

    private static async Task UpdateExistingNuGetConfigAsync(FileInfo nugetConfigFile, PackageMapping[]? mappings)
    {
        if (mappings is null || mappings.Length == 0)
        {
            return;
        }

        var configContext = await LoadAndValidateConfigAsync(nugetConfigFile, mappings);
        AddMissingPackageSources(configContext);
        
        if (configContext.PackageSourceMapping is not null)
        {
            UpdateExistingPackageSourceMapping(configContext);
        }
        else
        {
            CreateNewPackageSourceMapping(configContext);
        }
        
        await SaveConfigAsync(nugetConfigFile, configContext.Document);
    }

    private static async Task<NuGetConfigContext> LoadAndValidateConfigAsync(FileInfo nugetConfigFile, PackageMapping[] mappings)
    {
        // Get the required sources from mappings
        var requiredSources = mappings
            .Select(m => m.Source)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        // Load the existing NuGet.config
        XDocument doc;
        await using (var stream = nugetConfigFile.OpenRead())
        {
            doc = XDocument.Load(stream);
        }

        var configuration = doc.Root ?? new XElement("configuration");
        if (doc.Root is null)
        {
            doc.Add(configuration);
        }

        var packageSources = configuration.Element("packageSources");
        if (packageSources is null)
        {
            packageSources = new XElement("packageSources");
            configuration.Add(packageSources);
        }

        var existingAdds = packageSources.Elements("add").ToArray();
        var urlToExistingKey = BuildExistingSourceMappings(existingAdds);

        return new NuGetConfigContext
        {
            Document = doc,
            Configuration = configuration,
            PackageSources = packageSources,
            PackageSourceMapping = configuration.Element("packageSourceMapping"),
            Mappings = mappings,
            RequiredSources = requiredSources,
            ExistingAdds = existingAdds,
            UrlToExistingKey = urlToExistingKey
        };
    }

    private static Dictionary<string, string> BuildExistingSourceMappings(XElement[] existingAdds)
    {
        var urlToExistingKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var addElement in existingAdds)
        {
            var key = (string?)addElement.Attribute("key");
            var value = (string?)addElement.Attribute("value");
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
            {
                urlToExistingKey[value] = key;
            }
        }
        return urlToExistingKey;
    }

    private static void AddMissingPackageSources(NuGetConfigContext context)
    {
        var existingValues = new HashSet<string>(context.ExistingAdds
            .Select(e => (string?)e.Attribute("value") ?? string.Empty), StringComparer.OrdinalIgnoreCase);
        var existingKeys = new HashSet<string>(context.ExistingAdds
            .Select(e => (string?)e.Attribute("key") ?? string.Empty), StringComparer.OrdinalIgnoreCase);

        var missingSources = context.RequiredSources
            .Where(s => !existingValues.Contains(s) && !existingKeys.Contains(s))
            .ToArray();

        // Add missing sources
        foreach (var source in missingSources)
        {
            // Use the source URL as both key and value for consistency with our temporary config
            var add = new XElement("add");
            add.SetAttributeValue("key", source);
            add.SetAttributeValue("value", source);
            context.PackageSources.Add(add);
        }
    }

    private static void UpdateExistingPackageSourceMapping(NuGetConfigContext context)
    {
        var packageSourceMapping = context.PackageSourceMapping!;
        
        // Create a lookup of patterns to new sources from the mappings
        var patternToNewSource = context.Mappings.ToDictionary(m => m.PackageFilter, m => m.Source, StringComparer.OrdinalIgnoreCase);
        
        // Track sources that still have packages after remapping
        var sourcesInUse = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        var patternsToAdd = RemapExistingPatterns(packageSourceMapping, patternToNewSource, context.UrlToExistingKey, sourcesInUse);
        AddRemappedPatterns(packageSourceMapping, patternsToAdd, context.UrlToExistingKey, sourcesInUse);
        AddNewPatterns(packageSourceMapping, context, sourcesInUse);
        FixUrlBasedPackageSourceKeys(packageSourceMapping, context.UrlToExistingKey, sourcesInUse);
        HandleWildcardMappingForExistingSources(packageSourceMapping, context, sourcesInUse);
        RemoveEmptyPackageSourceElements(packageSourceMapping, context.PackageSources, context.UrlToExistingKey, sourcesInUse);
    }

    private static List<(string pattern, string newSource)> RemapExistingPatterns(
        XElement packageSourceMapping, 
        Dictionary<string, string> patternToNewSource, 
        Dictionary<string, string> urlToExistingKey,
        HashSet<string> sourcesInUse)
    {
        // First pass: Remove patterns that need to be remapped and track what needs to be added
        var patternsToAdd = new List<(string pattern, string newSource)>();
        var packageSourceElements = packageSourceMapping.Elements("packageSource").ToArray();

        foreach (var packageSourceElement in packageSourceElements)
        {
            var sourceKey = (string?)packageSourceElement.Attribute("key");
            if (string.IsNullOrEmpty(sourceKey))
            {
                continue;
            }

            var packageElements = packageSourceElement.Elements("package").ToArray();
            var elementsToRemove = new List<XElement>();

            foreach (var packageElement in packageElements)
            {
                var pattern = (string?)packageElement.Attribute("pattern");
                if (string.IsNullOrEmpty(pattern))
                {
                    continue;
                }

                // Check if this pattern needs to be remapped to a new source
                if (patternToNewSource.TryGetValue(pattern, out var newSource))
                {
                    // Determine the key that will be used for the new source
                    var expectedKey = urlToExistingKey.TryGetValue(newSource, out var existingKey) ? existingKey : newSource;
                    
                    if (!string.Equals(sourceKey, expectedKey, StringComparison.OrdinalIgnoreCase))
                    {
                        // This pattern needs to be moved to the new source
                        elementsToRemove.Add(packageElement);
                        patternsToAdd.Add((pattern, newSource));
                    }
                }
            }

            // Remove patterns that need to be moved
            foreach (var element in elementsToRemove)
            {
                element.Remove();
            }

            // If this source still has packages after removal, mark it as in use
            if (packageSourceElement.Elements("package").Any())
            {
                sourcesInUse.Add(sourceKey);
            }
        }

        return patternsToAdd;
    }

    private static void AddRemappedPatterns(
        XElement packageSourceMapping,
        List<(string pattern, string newSource)> patternsToAdd,
        Dictionary<string, string> urlToExistingKey,
        HashSet<string> sourcesInUse)
    {
        // Second pass: Group patterns by source and add them all to the same packageSource element
        var patternsBySource = patternsToAdd.GroupBy(x => x.newSource, StringComparer.OrdinalIgnoreCase);
        
        foreach (var sourceGroup in patternsBySource)
        {
            var newSource = sourceGroup.Key;
            
            // Use existing key if available, otherwise use the source URL as key
            var keyToUse = urlToExistingKey.TryGetValue(newSource, out var existingKey) ? existingKey : newSource;
            
            // Find or create the packageSource element for this source using the appropriate key
            var targetSourceElement = packageSourceMapping.Elements("packageSource")
                .FirstOrDefault(ps => string.Equals((string?)ps.Attribute("key"), keyToUse, StringComparison.OrdinalIgnoreCase));
            
            if (targetSourceElement is null)
            {
                // Create new packageSource element for this source using the appropriate key
                targetSourceElement = new XElement("packageSource");
                targetSourceElement.SetAttributeValue("key", keyToUse);
                packageSourceMapping.Add(targetSourceElement);
            }

            // Add all patterns for this source
            foreach (var (pattern, _) in sourceGroup)
            {
                // Check if this pattern already exists in the target source
                var existingPattern = targetSourceElement.Elements("package")
                    .FirstOrDefault(p => string.Equals((string?)p.Attribute("pattern"), pattern, StringComparison.OrdinalIgnoreCase));
                
                if (existingPattern is null)
                {
                    // Add the package pattern to the target source
                    var packageElement = new XElement("package");
                    packageElement.SetAttributeValue("pattern", pattern);
                    targetSourceElement.Add(packageElement);
                }
            }
            
            sourcesInUse.Add(keyToUse);
        }
    }

    private static void AddNewPatterns(
        XElement packageSourceMapping,
        NuGetConfigContext context,
        HashSet<string> sourcesInUse)
    {
        // Find patterns from mappings that don't exist anywhere in the current packageSourceMapping
        var existingPatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var packageSourceElement in packageSourceMapping.Elements("packageSource"))
        {
            foreach (var packageElement in packageSourceElement.Elements("package"))
            {
                var pattern = (string?)packageElement.Attribute("pattern");
                if (!string.IsNullOrEmpty(pattern))
                {
                    existingPatterns.Add(pattern);
                }
            }
        }

        // Group new patterns by their target source
        var newPatternsBySource = context.Mappings
            .Where(m => !existingPatterns.Contains(m.PackageFilter))
            .GroupBy(m => m.Source, StringComparer.OrdinalIgnoreCase);

        foreach (var sourceGroup in newPatternsBySource)
        {
            var targetSource = sourceGroup.Key;
            
            // Use existing key if available, otherwise use the source URL as key
            var keyToUse = context.UrlToExistingKey.TryGetValue(targetSource, out var existingKey) ? existingKey : targetSource;
            
            // Find or create the packageSource element for this source
            var targetSourceElement = packageSourceMapping.Elements("packageSource")
                .FirstOrDefault(ps => string.Equals((string?)ps.Attribute("key"), keyToUse, StringComparison.OrdinalIgnoreCase));
            
            if (targetSourceElement is null)
            {
                // Create new packageSource element for this source
                targetSourceElement = new XElement("packageSource");
                targetSourceElement.SetAttributeValue("key", keyToUse);
                packageSourceMapping.Add(targetSourceElement);
            }

            // Add all new patterns for this source
            foreach (var mapping in sourceGroup)
            {
                // Check if this pattern already exists in the target source (just in case)
                var existingPattern = targetSourceElement.Elements("package")
                    .FirstOrDefault(p => string.Equals((string?)p.Attribute("pattern"), mapping.PackageFilter, StringComparison.OrdinalIgnoreCase));
                
                if (existingPattern is null)
                {
                    // Add the package pattern to the target source
                    var packageElement = new XElement("package");
                    packageElement.SetAttributeValue("pattern", mapping.PackageFilter);
                    targetSourceElement.Add(packageElement);
                }
            }
            
            sourcesInUse.Add(keyToUse);
        }
    }

    private static void FixUrlBasedPackageSourceKeys(
        XElement packageSourceMapping,
        Dictionary<string, string> urlToExistingKey,
        HashSet<string> sourcesInUse)
    {
        // Fourth pass: Fix packageSource elements that use URLs as keys when proper keys exist
        var packageSourceElementsToFix = packageSourceMapping.Elements("packageSource")
            .Where(ps => {
                var key = (string?)ps.Attribute("key");
                return !string.IsNullOrEmpty(key) && urlToExistingKey.TryGetValue(key, out var properKey) && 
                       !string.Equals(key, properKey, StringComparison.OrdinalIgnoreCase);
            })
            .ToArray();

        foreach (var elementToFix in packageSourceElementsToFix)
        {
            var urlKey = (string?)elementToFix.Attribute("key");
            if (urlToExistingKey.TryGetValue(urlKey!, out var properKey))
            {
                // Find if there's already a packageSource with the proper key
                var existingProperElement = packageSourceMapping.Elements("packageSource")
                    .FirstOrDefault(ps => string.Equals((string?)ps.Attribute("key"), properKey, StringComparison.OrdinalIgnoreCase));
                
                if (existingProperElement is not null)
                {
                    // Move all packages from URL-based element to proper key element
                    var packagesToMove = elementToFix.Elements("package").ToArray();
                    foreach (var packageToMove in packagesToMove)
                    {
                        // Check if the pattern already exists in the target element
                        var pattern = (string?)packageToMove.Attribute("pattern");
                        var existingPattern = existingProperElement.Elements("package")
                            .FirstOrDefault(p => string.Equals((string?)p.Attribute("pattern"), pattern, StringComparison.OrdinalIgnoreCase));
                        
                        if (existingPattern is null)
                        {
                            packageToMove.Remove();
                            existingProperElement.Add(packageToMove);
                        }
                    }
                    
                    // Remove the URL-based element if it's now empty
                    if (!elementToFix.Elements("package").Any())
                    {
                        elementToFix.Remove();
                    }
                }
                else
                {
                    // Just update the key to use the proper key
                    elementToFix.SetAttributeValue("key", properKey);
                    sourcesInUse.Add(properKey);
                }
            }
        }
    }

    private static void HandleWildcardMappingForExistingSources(
        XElement packageSourceMapping,
        NuGetConfigContext context,
        HashSet<string> sourcesInUse)
    {
        // Check if we have a wildcard pattern being added - if so, add it to unmapped existing sources
        var hasWildcardMapping = context.Mappings.Any(m => m.PackageFilter == "*");
        if (hasWildcardMapping)
        {
            // Find all existing sources
            var existingSourceKeys = context.ExistingAdds
                .Select(add => (string?)add.Attribute("key"))
                .Where(key => !string.IsNullOrEmpty(key))
                .Cast<string>()
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Find sources that have any package patterns (after all the processing above)
            var sourcesWithPatterns = packageSourceMapping.Elements("packageSource")
                .Where(ps => ps.Elements("package").Any())
                .Select(ps => (string?)ps.Attribute("key"))
                .Where(key => !string.IsNullOrEmpty(key))
                .Cast<string>()
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var sourcesWithoutAnyPatterns = existingSourceKeys.Except(sourcesWithPatterns, StringComparer.OrdinalIgnoreCase).ToArray();
            
            // Add wildcard pattern only to sources that have NO patterns at all
            foreach (var sourceKey in sourcesWithoutAnyPatterns)
            {
                var sourceElement = new XElement("packageSource");
                sourceElement.SetAttributeValue("key", sourceKey);
                
                var wildcardPackage = new XElement("package");
                wildcardPackage.SetAttributeValue("pattern", "*");
                sourceElement.Add(wildcardPackage);
                
                packageSourceMapping.Add(sourceElement);
                sourcesInUse.Add(sourceKey);
            }
        }
    }

    private static void RemoveEmptyPackageSourceElements(
        XElement packageSourceMapping,
        XElement packageSources,
        Dictionary<string, string> urlToExistingKey,
        HashSet<string> sourcesInUse)
    {
        // Fifth pass: Remove empty packageSource elements and their corresponding sources from packageSources
        var emptyPackageSourceElements = packageSourceMapping.Elements("packageSource")
            .Where(ps => !ps.Elements("package").Any())
            .ToArray();

        foreach (var emptyElement in emptyPackageSourceElements)
        {
            var sourceKey = (string?)emptyElement.Attribute("key");
            emptyElement.Remove();

            // Remove the corresponding source from packageSources if it's not in use elsewhere
            if (!string.IsNullOrEmpty(sourceKey) && !sourcesInUse.Contains(sourceKey))
            {
                // Also check if any existing source key maps to this URL (for URL->key mapping scenario)
                var isUsedByExistingKey = urlToExistingKey.Any(kvp => 
                    string.Equals(kvp.Key, sourceKey, StringComparison.OrdinalIgnoreCase) && 
                    sourcesInUse.Contains(kvp.Value));
                    
                if (!isUsedByExistingKey)
                {
                    var sourceToRemove = packageSources.Elements("add")
                        .FirstOrDefault(add => string.Equals((string?)add.Attribute("key"), sourceKey, StringComparison.OrdinalIgnoreCase) ||
                                              string.Equals((string?)add.Attribute("value"), sourceKey, StringComparison.OrdinalIgnoreCase));
                    sourceToRemove?.Remove();
                }
            }
        }
    }

    private static void CreateNewPackageSourceMapping(NuGetConfigContext context)
    {
        // Create package source mapping section if it doesn't exist
        var packageSourceMapping = new XElement("packageSourceMapping");
        context.Configuration.Add(packageSourceMapping);

        // Group patterns by their target source and add them
        var patternsBySource = context.Mappings.GroupBy(m => m.Source, StringComparer.OrdinalIgnoreCase);
        
        foreach (var sourceGroup in patternsBySource)
        {
            var sourceUrl = sourceGroup.Key;
            // Use existing key if available, otherwise use the source URL as key
            var keyToUse = context.UrlToExistingKey.TryGetValue(sourceUrl, out var existingKey) ? existingKey : sourceUrl;
            
            var packageSource = new XElement("packageSource");
            packageSource.SetAttributeValue("key", keyToUse);
            
            foreach (var mapping in sourceGroup)
            {
                var packageElement = new XElement("package");
                packageElement.SetAttributeValue("pattern", mapping.PackageFilter);
                packageSource.Add(packageElement);
            }
            
            packageSourceMapping.Add(packageSource);
        }

        PreserveOriginalSourceFunctionality(packageSourceMapping, context);
    }

    private static void PreserveOriginalSourceFunctionality(XElement packageSourceMapping, NuGetConfigContext context)
    {
        // Since we're creating packageSourceMapping for the first time, we need to preserve the original behavior
        // where all existing sources could serve all packages. Any existing source that doesn't get specific
        // patterns from our mappings should get a wildcard pattern to remain functional.
        var existingSourceKeys = context.ExistingAdds
            .Select(add => (string?)add.Attribute("key"))
            .Where(key => !string.IsNullOrEmpty(key))
            .Cast<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Find sources that have mappings from our new packageSourceMapping entries
        var sourcesWithNewMappings = packageSourceMapping.Elements("packageSource")
            .Select(ps => (string?)ps.Attribute("key"))
            .Where(key => !string.IsNullOrEmpty(key))
            .Cast<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var sourcesWithoutAnyPatterns = existingSourceKeys.Except(sourcesWithNewMappings, StringComparer.OrdinalIgnoreCase).ToArray();
        
        // Add wildcard pattern to existing sources that don't have any patterns to preserve their original functionality
        foreach (var sourceKey in sourcesWithoutAnyPatterns)
        {
            var sourceElement = new XElement("packageSource");
            sourceElement.SetAttributeValue("key", sourceKey);
            
            var wildcardPackage = new XElement("package");
            wildcardPackage.SetAttributeValue("pattern", "*");
            sourceElement.Add(wildcardPackage);
            
            packageSourceMapping.Add(sourceElement);
        }
    }

    private static async Task SaveConfigAsync(FileInfo nugetConfigFile, XDocument document)
    {
        await using (var writeStream = nugetConfigFile.Open(FileMode.Create, FileAccess.Write, FileShare.None))
        {
            document.Save(writeStream);
        }
    }

    /// <summary>
    /// Checks if any sources from the mappings are missing from the existing NuGet.config
    /// or if package source mappings need to be updated.
    /// </summary>
    /// <param name="targetDirectory">The directory to check for NuGet.config.</param>
    /// <param name="channel">The package channel whose mappings are checked.</param>
    /// <returns>True if sources are missing or mappings need updates, false if all sources and mappings are correctly configured.</returns>
    public static bool HasMissingSources(DirectoryInfo targetDirectory, PackageChannel channel)
    {
        ArgumentNullException.ThrowIfNull(targetDirectory);
        ArgumentNullException.ThrowIfNull(channel);

        var mappings = channel.Mappings;
        if (channel.Type is not PackageChannelType.Explicit || mappings is null || mappings.Length == 0)
        {
            return false; // Implicit channels or empty mappings never require config changes.
        }

	if (!TryFindNuGetConfigInDirectory(targetDirectory, out var nugetConfigFile))
        {
            return true; // No config exists, so sources are "missing"
        }

        var requiredSources = mappings
            .Select(m => m.Source)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        try
        {
            using var stream = nugetConfigFile.OpenRead();
            var doc = XDocument.Load(stream);

            var packageSources = doc.Root?.Element("packageSources");
            if (packageSources is null)
            {
                return true;
            }

            var existingAdds = packageSources.Elements("add").ToArray();
            var existingValues = new HashSet<string>(existingAdds
                .Select(e => (string?)e.Attribute("value") ?? string.Empty), StringComparer.OrdinalIgnoreCase);
            var existingKeys = new HashSet<string>(existingAdds
                .Select(e => (string?)e.Attribute("key") ?? string.Empty), StringComparer.OrdinalIgnoreCase);

            // Create a mapping from source URLs to their existing keys for reuse in package source mappings
            var urlToExistingKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var addElement in existingAdds)
            {
                var key = (string?)addElement.Attribute("key");
                var value = (string?)addElement.Attribute("value");
                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                {
                    urlToExistingKey[value] = key;
                }
            }

            var missingSources = requiredSources
                .Where(s => !existingValues.Contains(s) && !existingKeys.Contains(s))
                .ToArray();

            // Check if any sources are missing
            if (missingSources.Length > 0)
            {
                return true;
            }

            // Check if package source mappings need to be updated
            var packageSourceMapping = doc.Root?.Element("packageSourceMapping");
            if (packageSourceMapping is not null)
            {
                // Create a lookup of patterns to required sources from the mappings
                var patternToRequiredSource = mappings.ToDictionary(m => m.PackageFilter, m => m.Source, StringComparer.OrdinalIgnoreCase);

                // Check if any patterns are mapped to the wrong source
                var packageSourceElements = packageSourceMapping.Elements("packageSource");
                foreach (var packageSourceElement in packageSourceElements)
                {
                    var sourceKey = (string?)packageSourceElement.Attribute("key");
                    if (string.IsNullOrEmpty(sourceKey))
                    {
                        continue;
                    }

                    var packageElements = packageSourceElement.Elements("package");
                    foreach (var packageElement in packageElements)
                    {
                        var pattern = (string?)packageElement.Attribute("pattern");
                        if (string.IsNullOrEmpty(pattern))
                        {
                            continue;
                        }

                        // Check if this pattern should be mapped to a different source
                        if (patternToRequiredSource.TryGetValue(pattern, out var requiredSourceUrl))
                        {
                            // Use existing key if available, otherwise use the source URL as key
                            var expectedKey = urlToExistingKey.TryGetValue(requiredSourceUrl, out var existingKey) ? existingKey : requiredSourceUrl;
                            
                            if (!string.Equals(sourceKey, expectedKey, StringComparison.OrdinalIgnoreCase))
                            {
                                return true; // This pattern needs to be remapped
                            }
                        }
                    }
                }
            }

            return false; // All sources and mappings are correctly configured
        }
        catch
        {
            // If we can't read the file, assume sources are missing
            return true;
        }
    }

    internal static bool TryFindNuGetConfigInDirectory(DirectoryInfo directory, [NotNullWhen(true)] out FileInfo? nugetConfigFile)
    {
        ArgumentNullException.ThrowIfNull(directory);
        // Find all files whose name matches "nuget.config" ignoring case in the top-level directory only
        var matches = directory
            .EnumerateFiles("*", SearchOption.TopDirectoryOnly)
            .Where(f => string.Equals(f.Name, "nuget.config", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (matches.Length > 1)
        {
            throw new InvalidOperationException($"Multiple NuGet.config files found in '{directory.FullName}' differing only by case.");
        }

        nugetConfigFile = matches.SingleOrDefault();
        return matches.Length == 1;
    }
}

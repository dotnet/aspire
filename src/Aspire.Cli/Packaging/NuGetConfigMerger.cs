// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;
using System.Diagnostics.CodeAnalysis;

namespace Aspire.Cli.Packaging;

internal class NuGetConfigMerger
{
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
        var existingValues = new HashSet<string>(existingAdds
            .Select(e => (string?)e.Attribute("value") ?? string.Empty), StringComparer.OrdinalIgnoreCase);
        var existingKeys = new HashSet<string>(existingAdds
            .Select(e => (string?)e.Attribute("key") ?? string.Empty), StringComparer.OrdinalIgnoreCase);

        var missingSources = requiredSources
            .Where(s => !existingValues.Contains(s) && !existingKeys.Contains(s))
            .ToArray();

        // Add missing sources
        foreach (var source in missingSources)
        {
            // Use the source URL as both key and value for consistency with our temporary config
            var add = new XElement("add");
            add.SetAttributeValue("key", source);
            add.SetAttributeValue("value", source);
            packageSources.Add(add);
        }

        // Handle package source mappings
        var packageSourceMapping = configuration.Element("packageSourceMapping");
        if (packageSourceMapping is not null)
        {
            // Create a lookup of patterns to new sources from the mappings
            var patternToNewSource = mappings.ToDictionary(m => m.PackageFilter, m => m.Source, StringComparer.OrdinalIgnoreCase);
            
            // Track sources that still have packages after remapping
            var sourcesInUse = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
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
                    if (patternToNewSource.TryGetValue(pattern, out var newSource) && 
                        !string.Equals(sourceKey, newSource, StringComparison.OrdinalIgnoreCase))
                    {
                        // This pattern needs to be moved to the new source
                        elementsToRemove.Add(packageElement);
                        patternsToAdd.Add((pattern, newSource));
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

            // Second pass: Group patterns by source and add them all to the same packageSource element
            var patternsBySource = patternsToAdd.GroupBy(x => x.newSource, StringComparer.OrdinalIgnoreCase);
            
            foreach (var sourceGroup in patternsBySource)
            {
                var newSource = sourceGroup.Key;
                
                // Find or create the packageSource element for this source
                var targetSourceElement = packageSourceMapping.Elements("packageSource")
                    .FirstOrDefault(ps => string.Equals((string?)ps.Attribute("key"), newSource, StringComparison.OrdinalIgnoreCase));
                
                if (targetSourceElement is null)
                {
                    // Create new packageSource element for this source
                    targetSourceElement = new XElement("packageSource");
                    targetSourceElement.SetAttributeValue("key", newSource);
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
                
                sourcesInUse.Add(newSource);
            }

            // Third pass: Remove empty packageSource elements and their corresponding sources from packageSources
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
                    var sourceToRemove = packageSources.Elements("add")
                        .FirstOrDefault(add => string.Equals((string?)add.Attribute("key"), sourceKey, StringComparison.OrdinalIgnoreCase) ||
                                              string.Equals((string?)add.Attribute("value"), sourceKey, StringComparison.OrdinalIgnoreCase));
                    sourceToRemove?.Remove();
                }
            }
        }
        else if (mappings.Length > 0)
        {
            // Create package source mapping section if it doesn't exist
            packageSourceMapping = new XElement("packageSourceMapping");
            configuration.Add(packageSourceMapping);

            // Group patterns by their target source and add them
            var patternsBySource = mappings.GroupBy(m => m.Source, StringComparer.OrdinalIgnoreCase);
            
            foreach (var sourceGroup in patternsBySource)
            {
                var packageSource = new XElement("packageSource");
                packageSource.SetAttributeValue("key", sourceGroup.Key);
                
                foreach (var mapping in sourceGroup)
                {
                    var packageElement = new XElement("package");
                    packageElement.SetAttributeValue("pattern", mapping.PackageFilter);
                    packageSource.Add(packageElement);
                }
                
                packageSourceMapping.Add(packageSource);
            }
        }

        // Save the updated document
        await using (var writeStream = nugetConfigFile.Open(FileMode.Create, FileAccess.Write, FileShare.None))
        {
            doc.Save(writeStream);
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
                        if (patternToRequiredSource.TryGetValue(pattern, out var requiredSource) &&
                            !string.Equals(sourceKey, requiredSource, StringComparison.OrdinalIgnoreCase))
                        {
                            return true; // This pattern needs to be remapped
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

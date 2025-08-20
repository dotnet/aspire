// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;

namespace Aspire.Cli.Packaging;

internal class NuGetConfigMerger
{
    /// <summary>
    /// Creates or updates a NuGet.config file in the specified directory based on the provided temporary NuGet config.
    /// </summary>
    /// <param name="targetDirectory">The directory where the NuGet.config should be created or updated.</param>
    /// <param name="temporaryConfig">The temporary NuGet config containing the sources to merge.</param>
    /// <param name="mappings">The package mappings to apply. If null, will be derived from the temporary config.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task CreateOrUpdateAsync(DirectoryInfo targetDirectory, TemporaryNuGetConfig? temporaryConfig, PackageMapping[]? mappings = null)
    {
        ArgumentNullException.ThrowIfNull(targetDirectory);

        // If no temporary config is provided and no mappings, there's nothing to do
        if (temporaryConfig is null && (mappings is null || mappings.Length == 0))
        {
            return;
        }

        // Ensure the target directory exists
        Directory.CreateDirectory(targetDirectory.FullName);

        // Locate an existing NuGet.config in the target directory using a case-insensitive search
        var nugetConfigFile = TryFindNuGetConfigInDirectory(targetDirectory);

        if (nugetConfigFile is null)
        {
            // Create a new NuGet.config file
            await CreateNewNuGetConfigAsync(targetDirectory, temporaryConfig, mappings);
        }
        else
        {
            // Update the existing NuGet.config file
            await UpdateExistingNuGetConfigAsync(nugetConfigFile, mappings);
        }
    }

    private static async Task CreateNewNuGetConfigAsync(DirectoryInfo targetDirectory, TemporaryNuGetConfig? temporaryConfig, PackageMapping[]? mappings)
    {
        var targetPath = Path.Combine(targetDirectory.FullName, "NuGet.config");

        if (temporaryConfig is not null)
        {
            // Use the existing temporary config file
            File.Copy(temporaryConfig.ConfigFile.FullName, targetPath, overwrite: true);
        }
        else if (mappings is not null && mappings.Length > 0)
        {
            // Generate a new NuGet.config from the mappings
            using var tmpConfig = await TemporaryNuGetConfig.CreateAsync(mappings);
            File.Copy(tmpConfig.ConfigFile.FullName, targetPath, overwrite: true);
        }
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

        if (missingSources.Length == 0)
        {
            return;
        }

        // Add missing sources
        foreach (var source in missingSources)
        {
            // Use the source URL as both key and value for consistency with our temporary config
            var add = new XElement("add");
            add.SetAttributeValue("key", source);
            add.SetAttributeValue("value", source);
            packageSources.Add(add);
        }

        // Save the updated document
        await using (var writeStream = nugetConfigFile.Open(FileMode.Create, FileAccess.Write, FileShare.None))
        {
            doc.Save(writeStream);
        }
    }

    /// <summary>
    /// Checks if any sources from the mappings are missing from the existing NuGet.config.
    /// </summary>
    /// <param name="targetDirectory">The directory to check for NuGet.config.</param>
    /// <param name="mappings">The package mappings to check against.</param>
    /// <returns>True if sources are missing, false if all sources are present or no NuGet.config exists.</returns>
    public static bool HasMissingSources(DirectoryInfo targetDirectory, PackageMapping[] mappings)
    {
        ArgumentNullException.ThrowIfNull(targetDirectory);
        ArgumentNullException.ThrowIfNull(mappings);

        if (mappings.Length == 0)
        {
            return false;
        }

        var nugetConfigFile = TryFindNuGetConfigInDirectory(targetDirectory);
        if (nugetConfigFile is null)
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

            return missingSources.Length > 0;
        }
        catch
        {
            // If we can't read the file, assume sources are missing
            return true;
        }
    }

    private static FileInfo? TryFindNuGetConfigInDirectory(DirectoryInfo directory)
    {
        ArgumentNullException.ThrowIfNull(directory);

        // Search only the specified directory for a file named "nuget.config", ignoring case
        return directory
            .EnumerateFiles("*", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(f => string.Equals(f.Name, "nuget.config", StringComparison.OrdinalIgnoreCase));
    }
}

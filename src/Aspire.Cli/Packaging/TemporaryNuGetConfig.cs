// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;

namespace Aspire.Cli.Packaging;

internal sealed class TemporaryNuGetConfig : IDisposable
{
    private readonly FileInfo _configFile;
    private bool _disposed;

    private TemporaryNuGetConfig(FileInfo configFile)
    {
        _configFile = configFile;
    }

    public FileInfo ConfigFile => _configFile;

    public static async Task<TemporaryNuGetConfig> CreateAsync(PackageMapping[] mappings)
    {
        var tempFilePath = Path.GetTempFileName();
        var configFile = new FileInfo(tempFilePath);
        await GenerateNuGetConfigAsync(mappings, configFile);
        return new TemporaryNuGetConfig(configFile);
    }

    private static async Task GenerateNuGetConfigAsync(PackageMapping[] mappings, FileInfo configFile)
    {
        var distinctSources = mappings
            .Select(m => m.Source)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        await using var fileStream = configFile.Create();
        await using var streamWriter = new StreamWriter(fileStream);
        await using var xmlWriter = XmlWriter.Create(streamWriter, new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            NewLineChars = Environment.NewLine,
            Encoding = System.Text.Encoding.UTF8,
            Async = true
        });

        await xmlWriter.WriteStartDocumentAsync();
        await xmlWriter.WriteStartElementAsync(null, "configuration", null);
        
        // Write packageSources section
        await xmlWriter.WriteStartElementAsync(null, "packageSources", null);
        foreach (var source in distinctSources)
        {
            var sourceName = GetSourceNameFromUrl(source);
            await xmlWriter.WriteStartElementAsync(null, "add", null);
            await xmlWriter.WriteAttributeStringAsync(null, "key", null, sourceName);
            await xmlWriter.WriteAttributeStringAsync(null, "value", null, source);
            await xmlWriter.WriteEndElementAsync(); // add
        }
        await xmlWriter.WriteEndElementAsync(); // packageSources

        // Add package source mappings for non-AllPackages filters
        var mappingsWithSpecificFilters = mappings
            .Where(m => m.PackageFilter != PackageMapping.AllPackages)
            .ToArray();

        if (mappingsWithSpecificFilters.Length > 0)
        {
            await xmlWriter.WriteStartElementAsync(null, "packageSourceMapping", null);

            var groupedBySource = mappingsWithSpecificFilters
                .GroupBy(m => m.Source, StringComparer.OrdinalIgnoreCase);

            foreach (var sourceGroup in groupedBySource)
            {
                var sourceName = GetSourceNameFromUrl(sourceGroup.Key);
                await xmlWriter.WriteStartElementAsync(null, "packageSource", null);
                await xmlWriter.WriteAttributeStringAsync(null, "key", null, sourceName);

                foreach (var mapping in sourceGroup)
                {
                    await xmlWriter.WriteStartElementAsync(null, "package", null);
                    await xmlWriter.WriteAttributeStringAsync(null, "pattern", null, mapping.PackageFilter);
                    await xmlWriter.WriteEndElementAsync(); // package
                }

                await xmlWriter.WriteEndElementAsync(); // packageSource
            }

            await xmlWriter.WriteEndElementAsync(); // packageSourceMapping
        }

        await xmlWriter.WriteEndElementAsync(); // configuration
        await xmlWriter.WriteEndDocumentAsync();
    }

    private static string GetSourceNameFromUrl(string url)
    {
        try
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return uri.Host;
            }
        }
        catch
        {
            // Fall back to using the URL as-is if parsing fails
        }

        // Remove special characters and use a simplified name
        return url.Replace("://", "_").Replace("/", "_").Replace(":", "_");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                if (_configFile.Exists)
                {
                    _configFile.Delete();
                }
            }
            catch
            {
                // Ignore exceptions during cleanup
            }

            _disposed = true;
        }
    }
}

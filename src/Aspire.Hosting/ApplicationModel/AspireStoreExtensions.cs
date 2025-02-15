// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Provides extension methods for <see cref="IDistributedApplicationBuilder"/> to create an <see cref="IAspireStore"/> instance.
/// </summary>
public static class AspireStoreExtensions
{
    internal const string AspireStorePathKeyName = "Aspire:Store:Path";

    /// <summary>
    /// Creates a new App Host store using the provided <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <returns>The <see cref="IAspireStore"/>.</returns>
    public static IAspireStore CreateStore(IDistributedApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var aspireDir = builder.Configuration[AspireStorePathKeyName];

        if (string.IsNullOrWhiteSpace(aspireDir))
        {
            var assemblyMetadata = builder.AppHostAssembly?.GetCustomAttributes<AssemblyMetadataAttribute>();
            aspireDir = GetMetadataValue(assemblyMetadata, "AppHostProjectBaseIntermediateOutputPath");

            if (string.IsNullOrWhiteSpace(aspireDir))
            {
                throw new InvalidOperationException($"Could not determine an appropriate location for local storage. Set the {AspireStorePathKeyName} setting to a folder where the App Host content should be stored.");
            }
        }

        return new AspireStore(Path.Combine(aspireDir, ".aspire"));
    }

    /// <summary>
    /// Gets a deterministic file path that is a copy of the <paramref name="sourceFilename"/>.
    /// The resulting file name will depend on the content of the file.
    /// </summary>
    /// <param name="aspireStore">The <see cref="IAspireStore"/> instance.</param>
    /// <param name="filenameTemplate">A file name to base the result on.</param>
    /// <param name="sourceFilename">An existing file.</param>
    /// <returns>A deterministic file path with the same content as <paramref name="sourceFilename"/>.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the source file does not exist.</exception>
    public static string GetFileNameWithContent(this IAspireStore aspireStore, string filenameTemplate, string sourceFilename)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filenameTemplate);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFilename);

        if (!File.Exists(sourceFilename))
        {
            throw new FileNotFoundException("The source file does not exist.", sourceFilename);
        }

        using var sourceStream = File.OpenRead(sourceFilename);

        return aspireStore.GetFileNameWithContent(filenameTemplate, sourceStream);
    }

    /// <summary>
    /// Gets the metadata value for the specified key from the assembly metadata.
    /// </summary>
    /// <param name="assemblyMetadata">The assembly metadata.</param>
    /// <param name="key">The key to look for.</param>
    /// <returns>The metadata value if found; otherwise, null.</returns>
    private static string? GetMetadataValue(IEnumerable<AssemblyMetadataAttribute>? assemblyMetadata, string key) =>
        assemblyMetadata?.FirstOrDefault(a => string.Equals(a.Key, key, StringComparison.OrdinalIgnoreCase))?.Value;

}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Aspire.Hosting;

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
    public static IAspireStore CreateStore(this IDistributedApplicationBuilder builder)
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
    /// Gets the metadata value for the specified key from the assembly metadata.
    /// </summary>
    /// <param name="assemblyMetadata">The assembly metadata.</param>
    /// <param name="key">The key to look for.</param>
    /// <returns>The metadata value if found; otherwise, null.</returns>
    private static string? GetMetadataValue(IEnumerable<AssemblyMetadataAttribute>? assemblyMetadata, string key) =>
        assemblyMetadata?.FirstOrDefault(a => string.Equals(a.Key, key, StringComparison.OrdinalIgnoreCase))?.Value;

}

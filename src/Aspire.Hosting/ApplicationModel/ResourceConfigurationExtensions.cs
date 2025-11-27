// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Extension methods for <see cref="IResourceConfiguration"/>.
/// </summary>
public static class ResourceConfigurationExtensions
{
    /// <summary>
    /// Tries to get metadata of the specified type from the resource configuration.
    /// </summary>
    /// <typeparam name="T">The type of metadata to retrieve.</typeparam>
    /// <param name="configuration">The resource configuration.</param>
    /// <param name="metadata">The metadata if found.</param>
    /// <returns>True if the metadata was found; otherwise, false.</returns>
    public static bool TryGetMetadata<T>(this IResourceConfiguration configuration, [NotNullWhen(true)] out T? metadata) where T : IResourceConfigurationMetadata
    {
        foreach (var item in configuration.Metadata)
        {
            if (item is T typedItem)
            {
                metadata = typedItem;
                return true;
            }
        }

        metadata = default;
        return false;
    }
}
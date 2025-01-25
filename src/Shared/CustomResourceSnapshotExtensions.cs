// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

internal static class CustomResourceSnapshotExtensions
{
    internal static ImmutableArray<ResourcePropertySnapshot> SetResourceProperty(this ImmutableArray<ResourcePropertySnapshot> properties, string name, object value)
    {
        for (var i = 0; i < properties.Length; i++)
        {
            var property = properties[i];

            if (string.Equals(property.Name, name, StringComparisons.ResourcePropertyName))
            {
                if (property.Value == value)
                {
                    // Unchanged.
                    return properties;
                }

                // Set value.
                return properties.SetItem(i, property with { Value = value });
            }
        }

        // Add property.
        return [.. properties, new ResourcePropertySnapshot(name, value)];
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

using Microsoft.FluentUI.AspNetCore.Components;

internal static class ResourceIconHelpers
{
    /// <summary>
    /// Maps a resource to an icon, checking for custom icons first, then falling back to default icons.
    /// </summary>
    public static Icon GetIconForResource(ResourceViewModel resource, IconSize desiredSize)
    {
        // Check if the resource has a custom icon specified
        if (!string.IsNullOrWhiteSpace(resource.IconName))
        {
            var customIcon = IconResolver.ResolveIconName(resource.IconName, desiredSize, resource.IconVariant ?? IconVariant.Filled);
            if (customIcon != null)
            {
                return customIcon;
            }
        }

        // Fall back to default icons based on resource type
        var icon = resource.ResourceType switch
        {
            KnownResourceTypes.Executable => IconResolver.ResolveIconName("SettingsCogMultiple", desiredSize, IconVariant.Filled),
            KnownResourceTypes.Project => IconResolver.ResolveIconName("CodeCircle", desiredSize, IconVariant.Filled),
            KnownResourceTypes.Container => IconResolver.ResolveIconName("Box", desiredSize, IconVariant.Filled),
            KnownResourceTypes.Parameter => IconResolver.ResolveIconName("Settings", desiredSize, IconVariant.Filled),
            KnownResourceTypes.ConnectionString => IconResolver.ResolveIconName("PlugConnectedSettings", desiredSize, IconVariant.Filled),
            KnownResourceTypes.ExternalService => IconResolver.ResolveIconName("CloudArrowUp", desiredSize, IconVariant.Filled),
            string t when t.Contains("database", StringComparison.OrdinalIgnoreCase) => IconResolver.ResolveIconName("Database", desiredSize, IconVariant.Filled),
            _ => IconResolver.ResolveIconName("SettingsCogMultiple", desiredSize, IconVariant.Filled),
        };

        if (icon == null)
        {
            throw new InvalidOperationException($"Couldn't resolve resource icon for {resource.Name}.");
        }

        return icon;
    }
}

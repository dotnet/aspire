// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.FluentUI.AspNetCore.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

/// <summary>
/// Represents a custom resource icon using SVG content or data URI.
/// </summary>
internal sealed class CustomResourceIcon : Icon
{
    public CustomResourceIcon(IconSize size, string iconContent) 
        : base(string.Empty, IconVariant.Regular, size, iconContent)
    {
    }
}

internal static class ResourceIconHelpers
{
    /// <summary>
    /// Maps a resource to an icon, checking for custom icons first, then falling back to default icons.
    /// </summary>
    public static Icon GetIconForResource(IconResolver iconResolver, ResourceViewModel resource, IconSize desiredSize, IconVariant desiredVariant = IconVariant.Filled)
    {
        // Check if the resource has custom icon data (takes highest precedence)
        if (!string.IsNullOrWhiteSpace(resource.CustomIconData))
        {
            return new CustomResourceIcon(desiredSize, resource.CustomIconData);
        }

        // Check if the resource has a custom icon name specified
        if (!string.IsNullOrWhiteSpace(resource.IconName))
        {
            var customIcon = iconResolver.ResolveIconName(resource.IconName, desiredSize, resource.IconVariant ?? IconVariant.Filled);
            if (customIcon != null)
            {
                return customIcon;
            }
        }

        // Fall back to default icons based on resource type
        var icon = resource.ResourceType switch
        {
            KnownResourceTypes.Executable => iconResolver.ResolveIconName("Apps", desiredSize, desiredVariant),
            KnownResourceTypes.Project => iconResolver.ResolveIconName("CodeCircle", desiredSize, desiredVariant),
            KnownResourceTypes.Container => iconResolver.ResolveIconName("Box", desiredSize, desiredVariant),
            KnownResourceTypes.Parameter => iconResolver.ResolveIconName("Key", desiredSize, desiredVariant),
            KnownResourceTypes.ConnectionString => iconResolver.ResolveIconName("PlugConnectedSettings", desiredSize, desiredVariant),
            KnownResourceTypes.ExternalService => iconResolver.ResolveIconName("GlobeArrowForward", desiredSize, desiredVariant),
            string t when t.Contains("database", StringComparison.OrdinalIgnoreCase) => iconResolver.ResolveIconName("Database", desiredSize, desiredVariant),
            _ => iconResolver.ResolveIconName("SettingsCogMultiple", desiredSize, desiredVariant),
        };

        if (icon == null)
        {
            throw new InvalidOperationException($"Couldn't resolve resource icon for {resource.Name}.");
        }

        return icon;
    }

    public static (Icon? icon, Color color) GetHealthStatusIcon(HealthStatus? healthStatus)
    {
        return healthStatus switch
        {
            HealthStatus.Healthy => (new Icons.Filled.Size16.Heart(), Color.Success),
            HealthStatus.Degraded => (new Icons.Filled.Size16.HeartBroken(), Color.Warning),
            HealthStatus.Unhealthy => (new Icons.Filled.Size16.HeartBroken(), Color.Error),
            _ => (new Icons.Regular.Size16.CircleHint(), Color.Info)
        };
    }
}

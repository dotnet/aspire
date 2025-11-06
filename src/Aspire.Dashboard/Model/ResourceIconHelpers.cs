// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.FluentUI.AspNetCore.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

internal static class ResourceIconHelpers
{
    /// <summary>
    /// Maps a resource to an icon, checking for custom icons first, then falling back to default icons.
    /// </summary>
    public static Icon GetIconForResource(IconResolver iconResolver, ResourceViewModel resource, IconSize desiredSize, IconVariant desiredVariant = IconVariant.Filled)
    {
        // Check if the resource has a custom icon specified
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
            KnownResourceTypes.Project => ResolveProjectIcon(iconResolver, resource, desiredSize, desiredVariant),
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

        static Icon? ResolveProjectIcon(IconResolver iconResolver, ResourceViewModel resource, IconSize desiredSize, IconVariant desiredVariant = IconVariant.Filled)
        {
            if (resource.TryGetProjectPath(out var projectPath) && !string.IsNullOrEmpty(projectPath))
            {
                var extension = Path.GetExtension(projectPath);
                return extension?.ToLowerInvariant() switch
                {
                    ".csproj" or ".cs" => iconResolver.ResolveIconName("CodeCsRectangle", desiredSize, desiredVariant),
                    ".fsproj" => iconResolver.ResolveIconName("CodeFsRectangle", desiredSize, desiredVariant),
                    ".vbproj" => iconResolver.ResolveIconName("CodeVbRectangle", desiredSize, desiredVariant),
                    _ => iconResolver.ResolveIconName("CodeCircle", desiredSize, desiredVariant)
                };
            }

            return iconResolver.ResolveIconName("CodeCircle", desiredSize, desiredVariant);
        }
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

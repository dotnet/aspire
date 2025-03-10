// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

using Aspire.Dashboard.Otlp.Model;
using Microsoft.FluentUI.AspNetCore.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

internal static class ResourceIconHelpers
{
    /// <summary>
    /// Maps a resource to a default icon.
    /// </summary>
    public static Icon GetIconForResource(ResourceViewModel resource)
    {
        Icon icon = resource.ResourceType switch
        {
            KnownResourceTypes.Executable => new Icons.Filled.Size24.Database(),
            KnownResourceTypes.Project => new Icons.Filled.Size24.CodeCircle(),
            KnownResourceTypes.Container => new Icons.Filled.Size24.Box(),
            string t => t.Contains("database", StringComparison.OrdinalIgnoreCase) ? new Icons.Filled.Size24.Database() : new Icons.Filled.Size24.SettingsCogMultiple(),
        };
        icon = icon.WithColor(ColorGenerator.Instance.GetColorHexByKey(resource.Name));
        return icon;
    }  
}

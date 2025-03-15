// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Xml.Linq;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Resources;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components.Extensions;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Aspire.Dashboard.Model.ResourceGraph;

public static class ResourceGraphMapper
{
    private static readonly string s_databaseIcon = GetIconPathData(new Icons.Filled.Size24.Database());
    private static readonly string s_containerIcon = GetIconPathData(new Icons.Filled.Size24.Box());
    private static readonly string s_executableIcon = GetIconPathData(new Icons.Filled.Size24.SettingsCogMultiple());
    private static readonly string s_projectIcon = GetIconPathData(new Icons.Filled.Size24.CodeCircle());

    public static ResourceDto MapResource(ResourceViewModel r, IDictionary<string, ResourceViewModel> resourcesByName, IStringLocalizer<Columns> columnsLoc)
    {
        var resolvedNames = new List<string>();

        foreach (var resourceRelationships in r.Relationships.GroupBy(r => r.ResourceName, StringComparers.ResourceName))
        {
            var matches = resourcesByName.Values
                .Where(r => string.Equals(r.DisplayName, resourceRelationships.Key, StringComparisons.ResourceName))
                .Where(r => r.KnownState != KnownResourceState.Hidden)
                .ToList();

            foreach (var match in matches)
            {
                resolvedNames.Add(match.Name);
            }
        }

        var endpoint = ResourceEndpointHelpers.GetEndpoints(r, includeInternalUrls: false).FirstOrDefault();
        var resolvedEndpointText = ResolvedEndpointText(endpoint);
        var resourceName = ResourceViewModel.GetResourceName(r, resourcesByName);
        var color = ColorGenerator.Instance.GetColorHexByKey(resourceName);

        var icon = r.ResourceType switch
        {
            KnownResourceTypes.Executable => s_executableIcon,
            KnownResourceTypes.Project => s_projectIcon,
            KnownResourceTypes.Container => s_containerIcon,
            string t => t.Contains("database", StringComparison.OrdinalIgnoreCase) ? s_databaseIcon : s_executableIcon
        };

        var stateIcon = ResourceStateViewModel.GetStateViewModel(r, columnsLoc);

        var dto = new ResourceDto
        {
            Name = r.Name,
            ResourceType = r.ResourceType,
            DisplayName = ResourceViewModel.GetResourceName(r, resourcesByName),
            Uid = r.Uid,
            ResourceIcon = new IconDto
            {
                Path = icon,
                Color = color,
                Tooltip = r.ResourceType
            },
            StateIcon = new IconDto
            {
                Path = GetIconPathData(stateIcon.Icon),
                Color = stateIcon.Color.ToAttributeValue()!,
                Tooltip = stateIcon.Text ?? r.State
            },
            ReferencedNames = resolvedNames.Distinct().OrderBy(n => n).ToImmutableArray(),
            EndpointUrl = endpoint?.Url,
            EndpointText = resolvedEndpointText
        };

        return dto;
    }

    private static string ResolvedEndpointText(DisplayedEndpoint? endpoint)
    {
        var text = endpoint?.Text ?? endpoint?.Url;
        if (string.IsNullOrEmpty(text))
        {
            return "No endpoints";
        }

        if (Uri.TryCreate(text, UriKind.Absolute, out var uri))
        {
            return $"{uri.Host}:{uri.Port}";
        }

        return text;
    }

    private static string GetIconPathData(Icon icon)
    {
        var p = icon.Content;
        var e = XElement.Parse(p);
        return e.Attribute("d")!.Value;
    }
}

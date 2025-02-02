// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Dashboard.Components.Controls;

namespace Aspire.Dashboard.Model;

internal static class ResourceEndpointHelpers
{
    /// <summary>
    /// A resource has services and endpoints. These can overlap. This method attempts to return a single list without duplicates.
    /// </summary>
    public static List<DisplayedEndpoint> GetEndpoints(ResourceViewModel resource, bool includeInternalUrls = false)
    {
        var endpoints = new List<DisplayedEndpoint>(resource.Urls.Length);

        foreach (var url in resource.Urls)
        {
            if ((includeInternalUrls && url.IsInternal) || !url.IsInternal)
            {
                endpoints.Add(new DisplayedEndpoint
                {
                    Name = url.Name,
                    Text = url.Url.OriginalString,
                    Address = url.Url.Host,
                    Port = url.Url.Port,
                    Url = url.Url.Scheme is "http" or "https" ? url.Url.OriginalString : null
                });
            }
        }

        // Make sure that endpoints have a consistent ordering.
        // Order:
        // - https
        // - other urls
        // - endpoint name
        var orderedEndpoints = endpoints
            .OrderByDescending(e => e.Url?.StartsWith("https") == true)
            .ThenByDescending(e => e.Url != null)
            .ThenBy(e => e.Name, StringComparers.EndpointAnnotationName)
            .ToList();

        return orderedEndpoints;
    }
}

[DebuggerDisplay("Name = {Name}, Text = {Text}, Address = {Address}:{Port}, Url = {Url}")]
public sealed class DisplayedEndpoint : IPropertyGridItem
{
    public required string Name { get; set; }
    public required string Text { get; set; }
    public string? Address { get; set; }
    public int? Port { get; set; }
    public string? Url { get; set; }

    /// <summary>
    /// Don't display a plain string value here. The URL will be displayed as a hyperlink
    /// in <see cref="ResourceDetails.GetContentAfterValue"/> instead.
    /// </summary>
    string? IPropertyGridItem.Value => null;

    public string? ValueToVisualize => Url ?? Text;

    public bool MatchesFilter(string filter)
        => Name.Contains(filter, StringComparison.CurrentCultureIgnoreCase) ||
           Text.Contains(filter, StringComparison.CurrentCultureIgnoreCase);
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Dashboard.Model;

internal static class ResourceEndpointHelpers
{
    /// <summary>
    /// A resource has services and endpoints. These can overlap. This method attempts to return a single list without duplicates.
    /// </summary>
    public static List<DisplayedEndpoint> GetEndpoints(ResourceViewModel resource, bool includeInteralUrls = false)
    {
        var endpoints = new List<DisplayedEndpoint>(resource.Urls.Length);

        foreach (var url in resource.Urls)
        {
            if ((includeInteralUrls && url.IsInternal) || !url.IsInternal)
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

        return endpoints;
    }
}

[DebuggerDisplay("Name = {Name}, Text = {Text}, Address = {Address}:{Port}, Url = {Url}")]
public sealed class DisplayedEndpoint
{
    public required string Name { get; set; }
    public required string Text { get; set; }
    public string? Address { get; set; }
    public int? Port { get; set; }
    public string? Url { get; set; }
}

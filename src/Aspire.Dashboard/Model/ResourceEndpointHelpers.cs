// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Dashboard.Model;

internal static class ResourceEndpointHelpers
{
    /// <summary>
    /// A resource has services and endpoints. These can overlap. This method attempts to return a single list without duplicates.
    /// </summary>
    public static List<DisplayedEndpoint> GetEndpoints(ILogger logger, ResourceViewModel resource, bool excludeServices = false, bool includeEndpointUrl = false)
    {
        var displayedEndpoints = new List<DisplayedEndpoint>();

        if (!excludeServices)
        {
            foreach (var service in resource.Services)
            {
                displayedEndpoints.Add(new DisplayedEndpoint
                {
                    Name = service.Name,
                    Text = service.AddressAndPort,
                    Address = service.AllocatedAddress,
                    Port = service.AllocatedPort
                });
            }
        }

        foreach (var endpoint in resource.Endpoints)
        {
            ProcessUrl(logger, resource, displayedEndpoints, endpoint.ProxyUrl, "ProxyUrl");
            if (includeEndpointUrl)
            {
                ProcessUrl(logger, resource, displayedEndpoints, endpoint.EndpointUrl, "EndpointUrl");
            }
        }

        // Display endpoints with a URL first, then by address and port.
        return displayedEndpoints.OrderBy(e => e.Url == null).ThenBy(e => e.Address).ThenBy(e => e.Port).ToList();
    }

    private static void ProcessUrl(ILogger logger, ResourceViewModel resource, List<DisplayedEndpoint> displayedEndpoints, string url, string name)
    {
        Uri uri;
        try
        {
            uri = new Uri(url, UriKind.Absolute);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Couldn't parse '{Url}' to a URI for resource {ResourceName}.", url, resource.Name);
            return;
        }

        // There isn't a good way to match services and endpoints other than by address and port.
        var existingMatches = displayedEndpoints.Where(e => string.Equals(e.Address, uri.Host, StringComparisons.UrlHost) && e.Port == uri.Port).ToList();

        if (existingMatches.Count > 0)
        {
            foreach (var e in existingMatches)
            {
                e.Url = uri.OriginalString;
                e.Text = uri.OriginalString;
            }
        }
        else
        {
            displayedEndpoints.Add(new DisplayedEndpoint
            {
                Name = name,
                Text = url,
                Address = uri.Host,
                Port = uri.Port,
                Url = uri.OriginalString
            });
        }
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

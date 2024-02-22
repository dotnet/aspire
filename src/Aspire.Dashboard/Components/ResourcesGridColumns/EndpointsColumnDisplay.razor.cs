// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components;

public partial class EndpointsColumnDisplay
{
    [Parameter, EditorRequired]
    public required ResourceViewModel Resource { get; set; }

    [Parameter, EditorRequired]
    public required bool HasMultipleReplicas { get; set; }

    /// <summary>
    /// A resource has services and endpoints. These can overlap. This method attempts to return a single list without duplicates.
    /// </summary>
    private static List<DisplayedEndpoint> GetEndpoints(ResourceViewModel resource, bool excludeServices = false)
    {
        var displayedEndpoints = new List<DisplayedEndpoint>();

        if (!excludeServices)
        {
            foreach (var service in resource.Services)
            {
                displayedEndpoints.Add(new DisplayedEndpoint
                {
                    Text = service.AddressAndPort,
                    Address = service.AllocatedAddress,
                    Port = service.AllocatedPort
                });
            }
        }

        foreach (var endpoint in resource.Endpoints)
        {
            Uri uri;
            try
            {
                uri = new Uri(endpoint.ProxyUrl, UriKind.Absolute);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Couldn't parse '{endpoint.ProxyUrl}' to a URI.", ex);
            }

            // There isn't a good way to match services and endpoints other than by address and port.
            var existingMatches = displayedEndpoints.Where(e => string.Equals(e.Address, uri.Host, StringComparisons.UrlHost) && e.Port == uri.Port).ToList();

            if (existingMatches.Count > 0)
            {
                foreach (var e in existingMatches)
                {
                    e.Url = uri.OriginalString;
                }
            }
            else
            {
                displayedEndpoints.Add(new DisplayedEndpoint
                {
                    Text = endpoint.ProxyUrl,
                    Address = uri.Host,
                    Port = uri.Port,
                    Url = uri.OriginalString
                });
            }
        }

        // Display endpoints with a URL first, then by address and port.
        return displayedEndpoints.OrderBy(e => e.Url != null).ThenBy(e => e.Address).ThenBy(e => e.Port).ToList();
    }

    private sealed class DisplayedEndpoint
    {
        public required string Text { get; set; }
        public string? Address { get; set; }
        public int? Port { get; set; }
        public string? Url { get; set; }
    }
}

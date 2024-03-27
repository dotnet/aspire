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

    [Inject]
    public required ILogger<EndpointsColumnDisplay> Logger { get; init; }

    private bool _popoverVisible;

    /// <summary>
    /// A resource has services and endpoints. These can overlap. This method attempts to return a single list without duplicates.
    /// </summary>
    private static List<DisplayedEndpoint> GetEndpoints(ResourceViewModel resource)
    {
        return ResourceEndpointHelpers.GetEndpoints(resource, includeInteralUrls: false);
    }
}

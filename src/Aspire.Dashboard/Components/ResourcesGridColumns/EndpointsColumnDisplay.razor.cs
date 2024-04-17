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

    [Parameter, EditorRequired]
    public required IList<DisplayedEndpoint> DisplayedEndpoints { get; set; }

    [Parameter]
    public string? AdditionalMessage { get; set; }

    [Inject]
    public required ILogger<EndpointsColumnDisplay> Logger { get; init; }

    private bool _popoverVisible;
}

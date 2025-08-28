// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Controls;

public partial class GenAIEventTitle
{
    [Parameter, EditorRequired]
    public required GenAIEventViewModel Event { get; set; }

    [Parameter, EditorRequired]
    public required string ResourceName { get; set; }

    private BadgeDetail? _categoryBadge;
    private BadgeDetail? _eventBadge;

    protected override void OnParametersSet()
    {
        _categoryBadge = Event.GetEventCategory();
        _eventBadge = Event.GetEventTitle();
    }
}

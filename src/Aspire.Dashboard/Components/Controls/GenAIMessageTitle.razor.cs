// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model.GenAI;
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Controls;

public partial class GenAIMessageTitle
{
    [Parameter, EditorRequired]
    public required GenAIMessageViewModel Message { get; set; }

    [Parameter, EditorRequired]
    public required string ResourceName { get; set; }

    private BadgeDetail? _categoryBadge;
    private BadgeDetail? _messageBadge;

    protected override void OnParametersSet()
    {
        _categoryBadge = Message.GetEventCategory();
        _messageBadge = Message.GetMessageTitle();
    }
}

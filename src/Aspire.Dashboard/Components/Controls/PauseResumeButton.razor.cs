// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Controls;

public partial class PauseResumeButton : ComponentBase
{
    [Parameter]
    public DateTime? PausedAt { get; set; }

    [Parameter]
    public EventCallback<DateTime?> PausedAtChanged { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    private async Task OnTogglePauseCoreAsync()
    {
        PausedAt = PausedAt == null ? DateTime.UtcNow : null;
        await PausedAtChanged.InvokeAsync(PausedAt);
    }
}


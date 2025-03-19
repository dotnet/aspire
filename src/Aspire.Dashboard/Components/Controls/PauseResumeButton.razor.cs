// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Controls;

public partial class PauseResumeButton : ComponentBase
{
    [Parameter]
    public bool IsPaused { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter, EditorRequired]
    public required Func<bool, Task> OnTogglePauseAsync { get; set; }

    private async Task OnTogglePauseCoreAsync()
    {
        IsPaused = !IsPaused;
        await OnTogglePauseAsync(IsPaused);
    }
}


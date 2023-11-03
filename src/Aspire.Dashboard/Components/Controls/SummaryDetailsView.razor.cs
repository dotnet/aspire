// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Controls;

public partial class SummaryDetailsView
{
    [Parameter]
    public RenderFragment? Summary { get; set; }

    [Parameter]
    public RenderFragment? Details { get; set; }

    [Parameter]
    public bool ShowDetails { get; set; }

    [Parameter, EditorRequired]
    public required string DetailsTitle { get; set; }

    [Parameter]
    public Orientation Orientation { get; set; } = Orientation.Horizontal;

    [Parameter]
    public EventCallback OnDismiss { get; set; }

    private readonly Icon _splitHorizontalIcon = new Icons.Regular.Size16.SplitHorizontal();
    private readonly Icon _splitVerticalIcon = new Icons.Regular.Size16.SplitVertical();

    private async Task HandleDismissAsync()
    {
        await OnDismiss.InvokeAsync();
    }

    private void HandleToggleOrientation()
    {
        if (Orientation == Orientation.Horizontal)
        {
            Orientation = Orientation.Vertical;
        }
        else
        {
            Orientation = Orientation.Horizontal;
        }
    }
}

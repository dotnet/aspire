// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.Fast.Components.FluentUI;

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
    public string? DetailsTitle { get; set; }

    [Parameter]
    public Orientation Orientation { get => _orientation; set => _orientation = value; }

    [Parameter]
    public EventCallback OnDismiss { get; set; }

    private Orientation _orientation = Orientation.Horizontal;

    private readonly Icon _splitHorizontalIcon = new Icons.Regular.Size16.SplitHorizontal();
    private readonly Icon _splitVerticalIcon = new Icons.Regular.Size16.SplitVertical();

    private async Task HandleDismissAsync()
    {
        await OnDismiss.InvokeAsync();
    }

    private void HandleToggleOrientation()
    {
        if (_orientation == Orientation.Horizontal)
        {
            _orientation = Orientation.Vertical;
        }
        else
        {
            _orientation = Orientation.Horizontal;
        }
    }
}

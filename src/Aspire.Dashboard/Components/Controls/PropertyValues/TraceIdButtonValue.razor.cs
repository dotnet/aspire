// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Controls.PropertyValues;

public partial class TraceIdButtonValue
{
    [Parameter, EditorRequired]
    public required string Value { get; set; }

    [Parameter, EditorRequired]
    public required string HighlightText { get; set; }

    [Parameter]
    public EventCallback OnClick { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    private async Task OnClickAsync()
    {
        if (OnClick.HasDelegate)
        {
            await OnClick.InvokeAsync();
        }
        else
        {
            NavigationManager.NavigateTo(DashboardUrls.TraceDetailUrl(Value));
        }
    }
}

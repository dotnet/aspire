// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Controls;

public partial class PropertyGrid<TItem>
{
    [Parameter, EditorRequired]
    public IQueryable<TItem>? Items { get; set; }

    [Parameter]
    public string GridTemplateColumns { get; set; } = "1fr 1fr";

    [Parameter]
    public string NameColumnTitle { get; set; } = "Name";

    [Parameter]
    public string ValueColumnTitle { get; set; } = "Value";

    [Parameter]
    public GridSort<TItem>? NameSort { get; set; }

    [Parameter]
    public GridSort<TItem>? ValueSort { get; set; }

    [Parameter]
    public bool IsNameSortable { get; set; } = true;

    [Parameter]
    public bool IsValueSortable { get; set; } = true;

    [Parameter]
    public bool EnableValueMasking { get; set; }

    [Parameter]
    public Func<TItem, string?> NameColumnValue { get; set; } = item => item?.ToString();

    [Parameter]
    public Func<TItem, string?> ValueColumnValue { get; set; } = item => item?.ToString();

    [Parameter]
    public Func<TItem, bool> GetIsItemMasked { get; set; } = item => false;

    [Parameter]
    public Action<TItem, bool> SetIsItemMasked { get; set; } = (item, newValue) => { };

    [Parameter]
    public string? HighlightText { get; set; }

    [Parameter]
    public EventCallback<TItem> IsMaskedChanged { get; set; }

    public readonly record struct PropertyGridIsMaskedChangedArgs(TItem Item, bool NewValue);

    private async Task OnIsMaskedChanged(TItem item, bool newValue)
    {
        SetIsItemMasked(item, newValue);
        await IsMaskedChanged.InvokeAsync(item);
    }
}

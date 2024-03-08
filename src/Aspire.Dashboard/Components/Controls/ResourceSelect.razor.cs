// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Controls;

public partial class ResourceSelect
{
    private const int ResourceOptionPixelHeight = 32;
    private const int MaxVisibleResourceOptions = 15;
    private const int SelectPadding = 8; // 4px top + 4px bottom

    [Parameter]
    public IEnumerable<SelectViewModel<ResourceTypeDetails>> Resources { get; set; } = default!;

    [Parameter]
    public SelectViewModel<ResourceTypeDetails> SelectedResource { get; set; } = default!;

    [Parameter]
    public EventCallback<SelectViewModel<ResourceTypeDetails>> SelectedResourceChanged { get; set; }

    [Parameter]
    public string? AriaLabel { get; set; }

    [Inject]
    public required IJSRuntime JSRuntime { get; init; }

    private FluentSelect<SelectViewModel<ResourceTypeDetails>>? _resourceSelectComponent;

    /// <summary>
    /// Workaround for issue in fluent-select web component where the display value of the
    /// selected item doesn't update automatically when the item changes.
    /// </summary>
    public ValueTask UpdateDisplayValueAsync()
    {
        if (JSRuntime is null || _resourceSelectComponent is null)
        {
            return ValueTask.CompletedTask;
        }

        return JSRuntime.InvokeVoidAsync("updateSelectDisplayValue", _resourceSelectComponent.Element);
    }

    private string? GetPopupHeight()
    {
        if (Resources?.TryGetNonEnumeratedCount(out var count) is false or null)
        {
            return null;
        }

        if (count <= MaxVisibleResourceOptions)
        {
            return null;
        }

        return $"{(ResourceOptionPixelHeight * MaxVisibleResourceOptions) + SelectPadding}px";
    }
}

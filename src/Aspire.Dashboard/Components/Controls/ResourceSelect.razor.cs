// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Microsoft.AspNetCore.Components;

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

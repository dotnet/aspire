// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components;

public partial class ResourceSelector<TResource> : ComponentBase where TResource : ResourceViewModel
{
    [Parameter, EditorRequired]
    public required ResourceSelectorViewModel<TResource> ViewModel { get; set; }

    [Parameter]
    public string? SelectResourceTitle { get; set; }

    private async Task SelectedOptionChangedAsync()
    {
        if (ViewModel.SelectedResourceChanged != null)
        {
            await ViewModel.SelectedResourceChanged(ViewModel.SelectedItem?.Resource);
        }
    }
}

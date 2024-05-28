// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Layout;

public partial class AspirePageContentLayout : ComponentBase
{
    [CascadingParameter]
    public required ViewportInformation ViewportInformation { get; init; }

    [Parameter] public required RenderFragment PageTitleSection { get; set; }

    [Parameter] public RenderFragment? MobilePageTitleToolbarSection { get; set; }

    [Parameter] public RenderFragment? ToolbarSection { get; set; }
    [Parameter] public bool AddNewlineOnDesktopToolbar { get; set; }

    [Parameter] public RenderFragment? MainSection { get; set; }

    [Parameter] public RenderFragment? FooterSection { get; set; }
    [Parameter] public bool ShouldShowFooter { get; set; } = true;
    [Parameter] public string? MobileToolbarButtonText { get; set; }

    [Parameter] public bool IsSummaryDetailsViewOpen { get; set; }

    [Inject]
    public required IDialogService DialogService { get; init; }

    private IDialogReference? _toolbarPanel;

    private string GetMobileMainStyle()
    {
        var style = "grid-area: main;";
        if (!ViewportInformation.IsUltraLowHeight)
        {
            style += "overflow: auto;";
        }

        return style;
    }

    private async Task OpenMobileToolbarAsync()
    {
        _toolbarPanel = await DialogService.ShowPanelAsync<ToolbarPanel>(
            new MobileToolbar(
                ToolbarSection!,
                MobileToolbarButtonText ?? LayoutLoc[nameof(Resources.Layout.PageLayoutViewFilters)]),
            new DialogParameters
            {
                Alignment = HorizontalAlignment.Center,
                Title = ControlsStringsLoc[nameof(ControlsStrings.ChartContainerFiltersHeader)],
                Width = "100%",
                Height = "90%",
                Modal = false,
                PrimaryAction = null,
                SecondaryAction = null
            });
    }

    public async Task CloseMobileToolbarAsync()
    {
        if (_toolbarPanel is not null)
        {
            await _toolbarPanel.CloseAsync();
        }
    }

    public record MobileToolbar(RenderFragment ToolbarSection, string MobileToolbarButtonText);
}


using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Layout;

public partial class AspirePageContentLayout : ComponentBase
{
    [CascadingParameter]
    public required ViewportInformation ViewportInformation { get; init; }

    [Parameter] public required RenderFragment PageTitle { get; set; }

    [Parameter] public RenderFragment? MobilePageTitleToolbarContent { get; set; }

    [Parameter] public RenderFragment? ToolbarContent { get; set; }
    [Parameter] public bool AddNewlineOnDesktopToolbar { get; set; }

    [Parameter] public RenderFragment? MainContent { get; set; }

    [Parameter] public RenderFragment? FooterContent { get; set; }

    [Inject]
    public required IDialogService DialogService { get; init; }

    private async Task OpenMobileToolbarAsync()
    {
        await DialogService.ShowPanelAsync<ToolbarPanel>(ToolbarContent!, new DialogParameters
        {
            Alignment = HorizontalAlignment.Center,
            Title = "Filters",
            Width = "100%",
            Height = "90%",
            Modal = false,
            PrimaryAction = null,
            SecondaryAction = null
        });
    }
}


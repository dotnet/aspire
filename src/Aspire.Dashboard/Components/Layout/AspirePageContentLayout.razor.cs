using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Layout;

public partial class AspirePageContentLayout : ComponentBase
{
    [CascadingParameter]
    public required ViewportInformation ViewportInformation { get; init; }

    [Parameter] public required RenderFragment PageTitle { get; set; }
    [Parameter] public RenderFragment? ToolbarContent { get; set; }

    [Parameter] public RenderFragment? MainContent { get; set; }

    [Parameter] public RenderFragment? FooterContent { get; set; }

    [Inject]
    public required IDialogService DialogService { get; init; }

    private bool _isMobileToolbarOpen;

    private async Task OpenMobileToolbarAsync()
    {
        _isMobileToolbarOpen = true;

        await DialogService.ShowPanelAsync<ToolbarPanel>(ToolbarContent!, new DialogParameters
        {
            Alignment = HorizontalAlignment.Center,
            Title = "Filters",
            Width = "100%",
            Height = "90%",
            Modal = false,
            PrimaryAction = null,
            SecondaryAction = null,
            OnDialogResult = DialogService.CreateDialogCallback(this, HandlePanelAsync)
        });
    }

    private Task HandlePanelAsync(DialogResult result)
    {
        _isMobileToolbarOpen = false;
        return Task.CompletedTask;
    }

}


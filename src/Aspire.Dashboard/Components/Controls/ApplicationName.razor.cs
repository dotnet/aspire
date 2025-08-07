// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Components.Dialogs;
using Grpc.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components;

public sealed partial class ApplicationName : ComponentBase, IDisposable
{
    private CancellationTokenSource? _disposalCts;

    [Parameter]
    public string? AdditionalText { get; set; }

    [Parameter]
    public string? ResourceName { get; set; }

    [Parameter]
    public IStringLocalizer? Loc { get; set; }

    [Inject]
    public required IDashboardClient DashboardClient { get; init; }

    [Inject]
    public required IDialogService DialogService { get; init; }

    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    [Inject]
    public required IStringLocalizer<Resources.Dialogs> DialogsLoc { get; init; }

    private string? _pageTitle;

    protected override async Task OnInitializedAsync()
    {
        // We won't have an application name until the client has connected to the server.
        if (DashboardClient.IsEnabled && !DashboardClient.WhenConnected.IsCompletedSuccessfully)
        {
            _disposalCts = new CancellationTokenSource();
            try
            {
                await DashboardClient.WhenConnected.WaitAsync(_disposalCts.Token);
            }
            catch (RpcException)
            {
                // Connection to app host failed, show connection error dialog
                await ShowConnectionErrorDialogAsync();
            }
            catch (OperationCanceledException)
            {
                // Component was disposed while waiting for connection
                return;
            }
            catch (Exception)
            {
                // Other connection-related errors, show connection error dialog
                await ShowConnectionErrorDialogAsync();
            }
        }
    }

    private async Task ShowConnectionErrorDialogAsync()
    {
        var parameters = new DialogParameters
        {
            Title = DialogsLoc[nameof(Resources.Dialogs.ConnectionErrorDialogTitle)],
            PrimaryAction = null,
            SecondaryAction = null,
            TrapFocus = true,
            Modal = true,
            Alignment = HorizontalAlignment.Center,
            Width = "400px",
            Height = "auto"
        };

        var dialogReference = await DialogService.ShowDialogAsync<ConnectionErrorDialog>(parameters);
        var result = await dialogReference.Result;

        if (result?.Data is ConnectionErrorDialog.ConnectionErrorDialogResult.Retry)
        {
            // Refresh the dashboard by navigating to root
            NavigationManager.NavigateTo("/", forceLoad: true);
        }
    }

    protected override void OnParametersSet()
    {
        string applicationName;

        if (ResourceName is not null && Loc is not null)
        {
            applicationName = string.Format(CultureInfo.InvariantCulture, Loc[ResourceName], DashboardClient.ApplicationName);
        }
        else
        {
            applicationName = DashboardClient.ApplicationName;
        }

        _pageTitle = string.IsNullOrEmpty(AdditionalText)
            ? applicationName
            : $"{applicationName} ({AdditionalText})";
    }

    public void Dispose()
    {
        _disposalCts?.Cancel();
        _disposalCts?.Dispose();
    }
}

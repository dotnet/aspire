// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Model;

public sealed class DashboardCommandExecutor(
    IDashboardClient dashboardClient,
    IDialogService dialogService,
    IToastService toastService,
    IStringLocalizer<Dashboard.Resources.Resources> loc,
    NavigationManager navigationManager)
{
    public async Task ExecuteAsync(ResourceViewModel resource, CommandViewModel command, Func<ResourceViewModel, string> getResourceName)
    {
        if (!string.IsNullOrWhiteSpace(command.ConfirmationMessage))
        {
            var dialogReference = await dialogService.ShowConfirmationAsync(command.ConfirmationMessage).ConfigureAwait(false);
            var result = await dialogReference.Result.ConfigureAwait(false);
            if (result.Cancelled)
            {
                return;
            }
        }

        var messageResourceName = getResourceName(resource);

        var toastParameters = new ToastParameters<CommunicationToastContent>()
        {
            Id = Guid.NewGuid().ToString(),
            Intent = ToastIntent.Progress,
            Title = string.Format(CultureInfo.InvariantCulture, loc[nameof(Dashboard.Resources.Resources.ResourceCommandStarting)], messageResourceName, command.DisplayName),
            Content = new CommunicationToastContent()
        };

        // Show a toast immediately to indicate the command is starting.
        toastService.ShowCommunicationToast(toastParameters);

        var response = await dashboardClient.ExecuteResourceCommandAsync(resource.Name, resource.ResourceType, command, CancellationToken.None).ConfigureAwait(false);

        // Update toast with the result;
        if (response.Kind == ResourceCommandResponseKind.Succeeded)
        {
            toastParameters.Title = string.Format(CultureInfo.InvariantCulture, loc[nameof(Dashboard.Resources.Resources.ResourceCommandSuccess)], messageResourceName, command.DisplayName);
            toastParameters.Intent = ToastIntent.Success;
            toastParameters.Icon = GetIntentIcon(ToastIntent.Success);
        }
        else
        {
            toastParameters.Title = string.Format(CultureInfo.InvariantCulture, loc[nameof(Dashboard.Resources.Resources.ResourceCommandFailed)], messageResourceName, command.DisplayName);
            toastParameters.Intent = ToastIntent.Error;
            toastParameters.Icon = GetIntentIcon(ToastIntent.Error);
            toastParameters.Content.Details = response.ErrorMessage;
            toastParameters.PrimaryAction = loc[nameof(Dashboard.Resources.Resources.ResourceCommandToastViewLogs)];
            toastParameters.OnPrimaryAction = EventCallback.Factory.Create<ToastResult>(this, () => navigationManager.NavigateTo(DashboardUrls.ConsoleLogsUrl(resource: resource.Name)));
        }

        toastService.UpdateToast(toastParameters.Id, toastParameters);
    }

    // Copied from FluentUI.
    private static (Icon Icon, Color Color)? GetIntentIcon(ToastIntent intent)
    {
        return intent switch
        {
            ToastIntent.Success => (new Icons.Filled.Size24.CheckmarkCircle(), Color.Success),
            ToastIntent.Warning => (new Icons.Filled.Size24.Warning(), Color.Warning),
            ToastIntent.Error => (new Icons.Filled.Size24.DismissCircle(), Color.Error),
            ToastIntent.Info => (new Icons.Filled.Size24.Info(), Color.Info),
            ToastIntent.Progress => (new Icons.Regular.Size24.Flash(), Color.Neutral),
            ToastIntent.Upload => (new Icons.Regular.Size24.ArrowUpload(), Color.Neutral),
            ToastIntent.Download => (new Icons.Regular.Size24.ArrowDownload(), Color.Neutral),
            ToastIntent.Event => (new Icons.Regular.Size24.CalendarLtr(), Color.Neutral),
            ToastIntent.Mention => (new Icons.Regular.Size24.Person(), Color.Neutral),
            ToastIntent.Custom => null,
            _ => throw new InvalidOperationException()
        };
    }
}

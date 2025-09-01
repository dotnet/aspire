// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Controls;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.GenAI;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Dialogs;

public partial class GenAIVisualizerDialog : ComponentBase
{
    private TreeGenAISelector? _treeGenAISelector;

    [Parameter, EditorRequired]
    public required GenAIVisualizerDialogViewModel Content { get; set; }

    [Inject]
    public required BrowserTimeProvider TimeProvider { get; set; }

    private Task HandleSelectedTreeItemChangedAsync()
    {
        Content.SelectedMessage = Content.SelectedTreeItem?.Data as GenAIMessageViewModel;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private void OnOverviewTabChange(FluentTab newTab)
    {
        var id = newTab.Id?.Substring("tab-overview-".Length);

        if (id is null
            || !Enum.TryParse(typeof(OverviewViewKind), id, out var o)
            || o is not OverviewViewKind viewKind)
        {
            return;
        }

        Content.OverviewActiveView = viewKind;
    }

    private void OnEventTabChange(FluentTab newTab)
    {
        var id = newTab.Id?.Substring("tab-event-".Length);

        if (id is null
            || !Enum.TryParse(typeof(EventViewKind), id, out var o)
            || o is not EventViewKind viewKind)
        {
            return;
        }

        Content.EventActiveView = viewKind;
    }

    private static string GetEventTitle(GenAIMessageViewModel e)
    {
        return e.Type switch
        {
            GenAIEventType.SystemMessage => "System message",
            GenAIEventType.UserMessage => "User",
            GenAIEventType.AssistantMessage or GenAIEventType.Choice => "Assistant",
            GenAIEventType.ToolMessage => "Tool",
            _ => string.Empty
        };
    }

    public static async Task OpenDialogAsync(ViewportInformation viewportInformation, IDialogService dialogService,
        IStringLocalizer<Resources.Dialogs> dialogsLoc, OtlpSpan span, List<OtlpLogEntry> logEntries, long? selectedLogEntryId,
        TelemetryRepository telemetryRepository, List<OtlpResource> resources)
    {
        var title = span.Name;
        var width = viewportInformation.IsDesktop ? "75vw" : "100vw";
        var parameters = new DialogParameters
        {
            Title = title,
            DismissTitle = dialogsLoc[nameof(Resources.Dialogs.DialogCloseButtonText)],
            Width = $"min(1000px, {width})",
            TrapFocus = true,
            Modal = true,
            PreventScroll = true,
        };

        var spanDetailsViewModel = SpanDetailsViewModel.Create(span, telemetryRepository, resources);

        var dialogViewModel = GenAIVisualizerDialogViewModel.Create(logEntries, spanDetailsViewModel, selectedLogEntryId, telemetryRepository);

        await dialogService.ShowDialogAsync<GenAIVisualizerDialog>(dialogViewModel, parameters);
    }
}


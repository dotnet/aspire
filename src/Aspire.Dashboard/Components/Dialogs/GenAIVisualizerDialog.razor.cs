// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.GenAI;
using Aspire.Dashboard.Model.Markdown;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Resources;
using Aspire.Dashboard.Telemetry;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Dialogs;

public partial class GenAIVisualizerDialog : ComponentBase, IDisposable
{
    private readonly string _copyButtonId = $"copy-{Guid.NewGuid():N}";

    private MarkdownProcessor _markdownProcess = default!;
    private Subscription? _resourcesSubscription;
    private Subscription? _tracesSubscription;
    private Subscription? _logsSubscription;

    private List<OtlpSpan> _contextSpans = default!;
    private int _currentSpanContextIndex;
    private GenAIVisualizerDialogViewModel? _content;

    private GenAIItemViewModel? SelectedItem { get; set; }

    private OverviewViewKind OverviewActiveView { get; set; }
    private ItemViewKind MessageActiveView { get; set; }

    [Parameter, EditorRequired]
    public required GenAIVisualizerDialogViewModel Content { get; set; }

    [Inject]
    public required BrowserTimeProvider TimeProvider { get; init; }

    [Inject]
    public required TelemetryRepository TelemetryRepository { get; init; }

    [Inject]
    public required IStringLocalizer<Resources.Dialogs> Loc { get; init; }

    [Inject]
    public required IStringLocalizer<ControlsStrings> ControlsStringsLoc { get; init; }

    [Inject]
    public required ILogger<GenAIVisualizerDialog> Logger { get; init; }

    [Inject]
    public required ITelemetryErrorRecorder ErrorRecorder { get; init; }

    protected override void OnInitialized()
    {
        _markdownProcess = GenAIMarkdownHelper.CreateProcessor(ControlsStringsLoc);
        _resourcesSubscription = TelemetryRepository.OnNewResources(UpdateDialogData);
        _tracesSubscription = TelemetryRepository.OnNewTraces(Content.Span.Source.ResourceKey, SubscriptionType.Read, UpdateDialogData);
        _logsSubscription = TelemetryRepository.OnNewLogs(Content.Span.Source.ResourceKey, SubscriptionType.Read, UpdateDialogData);
    }

    protected override void OnParametersSet()
    {
        if (_content != Content)
        {
            _contextSpans = Content.GetContextGenAISpans();
            _currentSpanContextIndex = _contextSpans.FindIndex(s => s.SpanId == Content.Span.SpanId);
            _content = Content;

            if (Content.SelectedLogEntryId != null)
            {
                SelectedItem = Content.Items.SingleOrDefault(e => e.InternalId == Content.SelectedLogEntryId);
            }
        }
    }

    private async Task UpdateDialogData()
    {
        await InvokeAsync(() =>
        {
            var hasUpdatedTrace = TelemetryRepository.HasUpdatedTrace(Content.Span.Trace);
            var newContextSpans = Content.GetContextGenAISpans();

            // Only update dialog data if the current trace has been updated,
            // or if there are new context spans (for the next/previous buttons).
            var newData = (hasUpdatedTrace || newContextSpans.Count > _contextSpans.Count);
            if (newData)
            {
                var span = newContextSpans.Find(s => s.SpanId == Content.Span.SpanId)!;

                _contextSpans = Content.GetContextGenAISpans();
                _currentSpanContextIndex = _contextSpans.IndexOf(span);

                TryUpdateViewedGenAISpan(span);
                StateHasChanged();
            }
        });
    }

    private void OnViewItem(GenAIItemViewModel viewModel)
    {
        SelectedItem = viewModel;
    }

    private Task HandleSelectedTreeItemChangedAsync()
    {
        var selectedIndex = Content.SelectedTreeItem?.Data as int?;
        SelectedItem = Content.Items.FirstOrDefault(m => m.Index == selectedIndex);
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

        OverviewActiveView = viewKind;
    }

    private void OnMessageTabChange(FluentTab newTab)
    {
        var id = newTab.Id?.Substring("tab-message-".Length);

        if (id is null
            || !Enum.TryParse(typeof(ItemViewKind), id, out var o)
            || o is not ItemViewKind viewKind)
        {
            return;
        }

        MessageActiveView = viewKind;
    }

    private void OnPreviousGenAISpan()
    {
        if (TryGetContextSpanByIndex(_currentSpanContextIndex - 1, out var span))
        {
            TryUpdateViewedGenAISpan(span);
        }
    }

    private void OnNextGenAISpan()
    {
        if (TryGetContextSpanByIndex(_currentSpanContextIndex + 1, out var span))
        {
            TryUpdateViewedGenAISpan(span);
        }
    }

    private bool TryGetContextSpanByIndex(int index, [NotNullWhen(true)] out OtlpSpan? span)
    {
        span = _contextSpans.ElementAtOrDefault(index);
        return span != null;
    }

    private bool TryUpdateViewedGenAISpan(OtlpSpan newSpan)
    {
        var selectedIndex = SelectedItem?.Index;

        var spanDetailsViewModel = SpanDetailsViewModel.Create(newSpan, TelemetryRepository, TelemetryRepository.GetResources());
        var dialogViewModel = GenAIVisualizerDialogViewModel.Create(spanDetailsViewModel, selectedLogEntryId: null, ErrorRecorder, TelemetryRepository, Content.GetContextGenAISpans);

        if (selectedIndex != null)
        {
            SelectedItem = dialogViewModel.Items.SingleOrDefault(e => e.Index == selectedIndex);
        }

        Content = dialogViewModel;
        _currentSpanContextIndex = _contextSpans.IndexOf(newSpan);

        return true;
    }

    private string GetItemTitle(GenAIItemViewModel e)
    {
        return e.Type switch
        {
            GenAIItemType.SystemMessage => Loc[nameof(Resources.Dialogs.GenAIMessageTitleSystem)],
            GenAIItemType.UserMessage => Loc[nameof(Resources.Dialogs.GenAIMessageTitleUser)],
            GenAIItemType.AssistantMessage or GenAIItemType.OutputMessage => Loc[nameof(Resources.Dialogs.GenAIMessageTitleAssistant)],
            GenAIItemType.ToolMessage => Loc[nameof(Resources.Dialogs.GenAIMessageTitleTool)],
            GenAIItemType.Error => "Error",
            _ => string.Empty
        };
    }

    private record DataInfo(string Url, string MimeType, string FileName);

    private static bool TryGetDataPart(GenAIItemPartViewModel itemPart, HashSet<string>? matchingMimeTypes, [NotNullWhen(true)] out DataInfo? dataInfo)
    {
        switch (itemPart.MessagePart?.Type)
        {
            case "blob":
                {
                    if (MatchMimeType(itemPart, matchingMimeTypes, out var mimeType))
                    {
                        if (itemPart.TryGetPropertyValue("content", out var content))
                        {
                            dataInfo = new DataInfo(
                                Url: $"data:{mimeType};base64,{content}",
                                MimeType: mimeType,
                                FileName: CalculateFileName(currentFileName: null, mimeType));
                            return true;
                        }
                    }
                    break;
                }
            case "uri":
                {
                    if (MatchMimeType(itemPart, matchingMimeTypes, out var mimeType))
                    {
                        if (itemPart.TryGetPropertyValue("uri", out var uri))
                        {
                            // Only attempt to display image if it is an http/https address.
                            if (Uri.TryCreate(uri, UriKind.Absolute, out var result) && result.Scheme.ToLowerInvariant() is "http" or "https")
                            {
                                dataInfo = new DataInfo(
                                    Url: uri,
                                    MimeType: mimeType,
                                    FileName: CalculateFileName(Path.GetFileName(result.LocalPath), mimeType));
                                return true;
                            }
                        }
                    }
                    break;
                }
        }

        dataInfo = null;
        return false;

        static bool MatchMimeType(GenAIItemPartViewModel viewModel, HashSet<string>? matchingMimeTypes, [NotNullWhen(true)] out string? mimeType)
        {
            if (viewModel.TryGetPropertyValue("mime_type", out mimeType))
            {
                return matchingMimeTypes == null || matchingMimeTypes.Contains(mimeType);
            }

            return false;
        }

        static string CalculateFileName(string? currentFileName, string mimeType)
        {
            if (!string.IsNullOrEmpty(currentFileName))
            {
                return currentFileName;
            }

            if (MimeTypeHelpers.MimeToExtension.TryGetValue(mimeType, out var extension))
            {
                return $"download{extension}";
            }
            else
            {
                // The part didn't include a name (probably a blob) and we don't know the mime type.
                // We have to give a download file name without an extension.
                return "download";
            }
        }
    }

    public void Dispose()
    {
        _resourcesSubscription?.Dispose();
        _tracesSubscription?.Dispose();
        _logsSubscription?.Dispose();
    }

    public static async Task OpenDialogAsync(ViewportInformation viewportInformation, IDialogService dialogService,
        IStringLocalizer<Resources.Dialogs> dialogsLoc, OtlpSpan span, long? selectedLogEntryId,
        TelemetryRepository telemetryRepository, ITelemetryErrorRecorder errorRecorder, List<OtlpResource> resources, Func<List<OtlpSpan>> getContextGenAISpans)
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

        var dialogViewModel = GenAIVisualizerDialogViewModel.Create(spanDetailsViewModel, selectedLogEntryId, errorRecorder, telemetryRepository, getContextGenAISpans);

        await dialogService.ShowDialogAsync<GenAIVisualizerDialog>(dialogViewModel, parameters);
    }
}


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.GenAI;
using Aspire.Dashboard.Model.Markdown;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Resources;
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
            _contextSpans = Content.GetContextGenAISpans();
            var span = _contextSpans.Find(s => s.SpanId == Content.Span.SpanId)!;
            _currentSpanContextIndex = _contextSpans.IndexOf(span);

            TryUpdateViewedGenAISpan(span);
            StateHasChanged();
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
        var dialogViewModel = GenAIVisualizerDialogViewModel.Create(spanDetailsViewModel, selectedLogEntryId: null, TelemetryRepository, Content.GetContextGenAISpans);

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

    private static bool IsImagePart(GenAIItemPartViewModel itemPart, [NotNullWhen(true)] out string? imageContent)
    {
        // Image part is a generic part with type "image" and content in additional properties.
        // An image part isn't in the GenAI semantic conventions. This code follows what MEAI does and will need to change to support a future standard.
        // See https://github.com/dotnet/extensions/pull/6809.
        if (itemPart.MessagePart?.Type == "image")
        {
            var contentType = itemPart.AdditionalProperties?.SingleOrDefault(p => p.Name == "content");
            imageContent = contentType?.Value;
            return !string.IsNullOrEmpty(imageContent);
        }

        imageContent = null;
        return false;
    }

    private static bool IsSupportedImageScheme(string imageContent)
    {
        if (Uri.TryCreate(imageContent, UriKind.Absolute, out var result))
        {
            // Only attempt to display image if it is an http/https address, or an inline data image.
            return result.Scheme.ToLowerInvariant() is "http" or "https" or "data";
        }

        return false;
    }

    public void Dispose()
    {
        _resourcesSubscription?.Dispose();
        _tracesSubscription?.Dispose();
        _logsSubscription?.Dispose();
    }

    public static async Task OpenDialogAsync(ViewportInformation viewportInformation, IDialogService dialogService,
        IStringLocalizer<Resources.Dialogs> dialogsLoc, OtlpSpan span, long? selectedLogEntryId,
        TelemetryRepository telemetryRepository, List<OtlpResource> resources, Func<List<OtlpSpan>> getContextGenAISpans)
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

        var dialogViewModel = GenAIVisualizerDialogViewModel.Create(spanDetailsViewModel, selectedLogEntryId, telemetryRepository, getContextGenAISpans);

        await dialogService.ShowDialogAsync<GenAIVisualizerDialog>(dialogViewModel, parameters);
    }
}


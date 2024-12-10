// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Aspire.Dashboard.Extensions;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Pages;

public partial class TraceDetail : ComponentBase, IDisposable
{
    private const string NameColumn = nameof(NameColumn);
    private const string TicksColumn = nameof(TicksColumn);
    private const string ActionsColumn = nameof(ActionsColumn);

    private readonly List<IDisposable> _peerChangesSubscriptions = new();
    private OtlpTrace? _trace;
    private Subscription? _tracesSubscription;
    private List<SpanWaterfallViewModel>? _spanWaterfallViewModels;
    private int _maxDepth;
    private List<OtlpApplication> _applications = default!;
    private readonly List<string> _collapsedSpanIds = [];
    private string? _elementIdBeforeDetailsViewOpened;
    private FluentDataGrid<SpanWaterfallViewModel> _dataGrid = null!;
    private GridColumnManager _manager = null!;
    private IList<GridColumn> _gridColumns = null!;

    [Parameter]
    public required string TraceId { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public string? SpanId { get; set; }

    [Inject]
    public required TelemetryRepository TelemetryRepository { get; init; }

    [Inject]
    public required IEnumerable<IOutgoingPeerResolver> OutgoingPeerResolvers { get; init; }

    [Inject]
    public required BrowserTimeProvider TimeProvider { get; init; }

    [Inject]
    public required IJSRuntime JS { get; init; }

    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    protected override void OnInitialized()
    {
        _gridColumns = [
            new GridColumn(Name: NameColumn, DesktopWidth: "4fr", MobileWidth: "4fr"),
            new GridColumn(Name: TicksColumn, DesktopWidth: "12fr", MobileWidth: "12fr"),
            new GridColumn(Name: ActionsColumn, DesktopWidth: "90px", MobileWidth: null)
        ];

        foreach (var resolver in OutgoingPeerResolvers)
        {
            _peerChangesSubscriptions.Add(resolver.OnPeerChanges(async () =>
            {
                UpdateDetailViewData();
                await InvokeAsync(StateHasChanged);
                await InvokeAsync(_dataGrid.SafeRefreshDataAsync);
            }));
        }
    }

    private ValueTask<GridItemsProviderResult<SpanWaterfallViewModel>> GetData(GridItemsProviderRequest<SpanWaterfallViewModel> request)
    {
        Debug.Assert(_spanWaterfallViewModels != null);

        var visibleSpanWaterfallViewModels = _spanWaterfallViewModels.Where(viewModel => !viewModel.IsHidden).ToList();

        var page = visibleSpanWaterfallViewModels.AsEnumerable();
        if (request.StartIndex > 0)
        {
            page = page.Skip(request.StartIndex);
        }
        page = page.Take(request.Count ?? DashboardUIHelpers.DefaultDataGridResultCount);

        return ValueTask.FromResult(new GridItemsProviderResult<SpanWaterfallViewModel>
        {
            Items = page.ToList(),
            TotalItemCount = visibleSpanWaterfallViewModels.Count
        });
    }

    private static Icon GetSpanIcon(OtlpSpan span)
    {
        switch (span.Kind)
        {
            case OtlpSpanKind.Server:
                return new Icons.Filled.Size16.Server();
            case OtlpSpanKind.Consumer:
                if (span.Attributes.HasKey("messaging.system"))
                {
                    return new Icons.Filled.Size16.Mailbox();
                }
                else
                {
                    return new Icons.Filled.Size16.ContentSettings();
                }
            default:
                throw new InvalidOperationException($"Unsupported span kind when resolving icon: {span.Kind}");
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (TraceId != _trace?.TraceId)
        {
            UpdateDetailViewData();
            UpdateSubscription();

            // If parameters change after render then the grid is automatically updated.
            // Explicitly update data grid to support navigating between traces via span links.
            await _dataGrid.SafeRefreshDataAsync();
        }

        if (SpanId is not null && _spanWaterfallViewModels is not null)
        {
            var spanVm = _spanWaterfallViewModels.SingleOrDefault(vm => vm.Span.SpanId == SpanId);
            if (spanVm != null)
            {
                await OnShowPropertiesAsync(spanVm, buttonId: null);
            }

            // Navigate to remove ?spanId=xxx in the URL.
            NavigationManager.NavigateTo(DashboardUrls.TraceDetailUrl(TraceId), new NavigationOptions { ReplaceHistoryEntry = true });
        }
    }

    private void UpdateDetailViewData()
    {
        _applications = TelemetryRepository.GetApplications();

        _trace = (TraceId != null) ? TelemetryRepository.GetTrace(TraceId) : null;

        if (_trace == null)
        {
            _spanWaterfallViewModels = null;
            _maxDepth = 0;
            return;
        }

        _spanWaterfallViewModels = SpanWaterfallViewModel.Create(_trace, new SpanWaterfallViewModel.TraceDetailState(OutgoingPeerResolvers, _collapsedSpanIds));
        _maxDepth = _spanWaterfallViewModels.Max(s => s.Depth);
        return;
    }

    private void UpdateSubscription()
    {
        if (_trace == null)
        {
            _tracesSubscription?.Dispose();
            return;
        }

        if (_tracesSubscription is null || _tracesSubscription.ApplicationKey != _trace.FirstSpan.Source.ApplicationKey)
        {
            _tracesSubscription?.Dispose();
            _tracesSubscription = TelemetryRepository.OnNewTraces(_trace.FirstSpan.Source.ApplicationKey, SubscriptionType.Read, () => InvokeAsync(async () =>
            {
                UpdateDetailViewData();
                await InvokeAsync(StateHasChanged);
                await _dataGrid.SafeRefreshDataAsync();
            }));
        }
    }

    private string GetRowClass(SpanWaterfallViewModel viewModel)
    {
        // Test with id rather than the object reference because the data and view model objects are recreated on trace updates.
        if (viewModel.Span.SpanId == SelectedSpan?.Span.SpanId)
        {
            return "selected-row";
        }

        return string.Empty;
    }

    public SpanDetailsViewModel? SelectedSpan { get; set; }

    private async Task OnToggleCollapse(SpanWaterfallViewModel viewModel)
    {
        // View model data is recreated if the trace updates.
        // Persist the collapsed state in a separate list.
        if (viewModel.IsCollapsed)
        {
            viewModel.IsCollapsed = false;
            _collapsedSpanIds.Remove(viewModel.Span.SpanId);
        }
        else
        {
            viewModel.IsCollapsed = true;
            _collapsedSpanIds.Add(viewModel.Span.SpanId);
        }

        await _dataGrid.SafeRefreshDataAsync();
    }

    private async Task OnShowPropertiesAsync(SpanWaterfallViewModel viewModel, string? buttonId)
    {
        _elementIdBeforeDetailsViewOpened = buttonId;

        if (SelectedSpan?.Span.SpanId == viewModel.Span.SpanId)
        {
            await ClearSelectedSpanAsync();
        }
        else
        {
            var entryProperties = viewModel.Span.AllProperties()
                .Select(f => new TelemetryPropertyViewModel { Name = f.DisplayName, Key = f.Key, Value = f.Value })
                .ToList();

            var traceCache = new Dictionary<string, OtlpTrace>(StringComparer.Ordinal);

            var links = viewModel.Span.Links.Select(l => CreateLinkViewModel(l.TraceId, l.SpanId, l.Attributes, traceCache)).ToList();
            var backlinks = viewModel.Span.BackLinks.Select(l => CreateLinkViewModel(l.SourceTraceId, l.SourceSpanId, l.Attributes, traceCache)).ToList();

            var spanDetailsViewModel = new SpanDetailsViewModel
            {
                Span = viewModel.Span,
                Applications = _applications,
                Properties = entryProperties,
                Title = SpanWaterfallViewModel.GetTitle(viewModel.Span, _applications),
                Links = links,
                Backlinks = backlinks,
            };

            SelectedSpan = spanDetailsViewModel;
        }
    }

    private SpanLinkViewModel CreateLinkViewModel(string traceId, string spanId, KeyValuePair<string, string>[] attributes, Dictionary<string, OtlpTrace> traceCache)
    {
        ref var trace = ref CollectionsMarshal.GetValueRefOrAddDefault(traceCache, traceId, out _);
        // Adds to dictionary if not present.
        trace ??= TelemetryRepository.GetTrace(traceId);

        var linkSpan = trace?.Spans.FirstOrDefault(s => s.SpanId == spanId);

        return new SpanLinkViewModel
        {
            TraceId = traceId,
            SpanId = spanId,
            Attributes = attributes,
            Span = linkSpan,
        };
    }

    private async Task ClearSelectedSpanAsync(bool causedByUserAction = false)
    {
        SelectedSpan = null;

        if (_elementIdBeforeDetailsViewOpened is not null && causedByUserAction)
        {
            await JS.InvokeVoidAsync("focusElement", _elementIdBeforeDetailsViewOpened);
        }

        _elementIdBeforeDetailsViewOpened = null;
    }

    private string GetResourceName(OtlpApplicationView app) => OtlpApplication.GetResourceName(app, _applications);

    public void Dispose()
    {
        foreach (var subscription in _peerChangesSubscriptions)
        {
            subscription.Dispose();
        }
        _tracesSubscription?.Dispose();
    }
}

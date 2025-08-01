// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Aspire.Dashboard.Extensions;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Resources;
using Aspire.Dashboard.Telemetry;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Pages;

public partial class TraceDetail : ComponentBase, IComponentWithTelemetry, IDisposable
{
    private const string NameColumn = nameof(NameColumn);
    private const string TicksColumn = nameof(TicksColumn);
    private const string ActionsColumn = nameof(ActionsColumn);

    private readonly List<IDisposable> _peerChangesSubscriptions = new();
    private OtlpTrace? _trace;
    private Subscription? _tracesSubscription;
    private List<SpanWaterfallViewModel>? _spanWaterfallViewModels;
    private int _maxDepth;
    private int _resourceCount;
    private List<OtlpApplication> _applications = default!;
    private readonly List<string> _collapsedSpanIds = [];
    private string? _elementIdBeforeDetailsViewOpened;
    private FluentDataGrid<SpanWaterfallViewModel> _dataGrid = null!;
    private GridColumnManager _manager = null!;
    private IList<GridColumn> _gridColumns = null!;
    private string _filter = string.Empty;

    [Parameter]
    public required string TraceId { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public string? SpanId { get; set; }

    [Inject]
    public required ILogger<TraceDetail> Logger { get; init; }

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

    [Inject]
    public required ComponentTelemetryContextProvider TelemetryContextProvider { get; init; }

    [Inject]
    public required IStringLocalizer<Dashboard.Resources.TraceDetail> Loc { get; init; }

    [Inject]
    public required IStringLocalizer<Dashboard.Resources.StructuredLogs> StructuredLogsLoc { get; init; }

    [Inject]
    public required IStringLocalizer<ControlsStrings> ControlStringsLoc { get; init; }

    [CascadingParameter]
    public required ViewportInformation ViewportInformation { get; set; }

    protected override void OnInitialized()
    {
        TelemetryContextProvider.Initialize(TelemetryContext);

        _gridColumns = [
            new GridColumn(Name: NameColumn, DesktopWidth: "6fr", MobileWidth: "6fr"),
            new GridColumn(Name: TicksColumn, DesktopWidth: "12fr", MobileWidth: "12fr"),
            new GridColumn(Name: ActionsColumn, DesktopWidth: "100px", MobileWidth: null)
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

    // Internal to be used in unit tests
    internal ValueTask<GridItemsProviderResult<SpanWaterfallViewModel>> GetData(GridItemsProviderRequest<SpanWaterfallViewModel> request)
    {
        Debug.Assert(_spanWaterfallViewModels != null);

        var visibleViewModels = new HashSet<SpanWaterfallViewModel>();
        foreach (var viewModel in _spanWaterfallViewModels)
        {
            if (viewModel.IsHidden || visibleViewModels.Contains(viewModel))
            {
                continue;
            }

            if (viewModel.MatchesFilter(_filter, GetResourceName, out var matchedDescendents))
            {
                visibleViewModels.Add(viewModel);
                foreach (var descendent in matchedDescendents.Where(d => !d.IsHidden))
                {
                    visibleViewModels.Add(descendent);
                }
            }
        }

        var page = _spanWaterfallViewModels.Where(visibleViewModels.Contains).AsEnumerable();
        var totalItemCount = page.Count();
        if (request.StartIndex > 0)
        {
            page = page.Skip(request.StartIndex);
        }
        page = page.Take(request.Count ?? DashboardUIHelpers.DefaultDataGridResultCount);

        return ValueTask.FromResult(new GridItemsProviderResult<SpanWaterfallViewModel>
        {
            Items = page.ToList(),
            TotalItemCount = totalItemCount
        });
    }

    private string? GetPageTitle()
    {
        if (_trace is null)
        {
            return null;
        }

        var headerSpan = _trace.RootOrFirstSpan;
        return $"{GetResourceName(headerSpan.Source)}: {headerSpan.Name}";
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

        Logger.LogInformation("Getting trace '{TraceId}'.", TraceId);
        _trace = (TraceId != null) ? TelemetryRepository.GetTrace(TraceId) : null;

        if (_trace == null)
        {
            Logger.LogInformation("Couldn't find trace '{TraceId}'.", TraceId);
            _spanWaterfallViewModels = null;
            _maxDepth = 0;
            _resourceCount = 0;
            return;
        }

        // Get logs for the trace. Note that there isn't a limit on this query so all logs are returned.
        // There is a limit on the number of logs stored by the dashboard so this is implicitly limited.
        // If there are performance issues with displaying all logs then consider adding a limit to this query.
        var logsContext = new GetLogsContext
        {
            ApplicationKey = null,
            Count = int.MaxValue,
            StartIndex = 0,
            Filters = [new TelemetryFilter
            {
                Field = KnownStructuredLogFields.TraceIdField,
                Condition = FilterCondition.Equals,
                Value = _trace.TraceId
            }]
        };
        var result = TelemetryRepository.GetLogs(logsContext);

        Logger.LogInformation("Trace '{TraceId}' has {SpanCount} spans.", _trace.TraceId, _trace.Spans.Count);
        _spanWaterfallViewModels = SpanWaterfallViewModel.Create(_trace, result.Items, new SpanWaterfallViewModel.TraceDetailState(OutgoingPeerResolvers.ToArray(), _collapsedSpanIds));
        _maxDepth = _spanWaterfallViewModels.Max(s => s.Depth);

        var apps = new HashSet<OtlpApplication>();
        foreach (var span in _trace.Spans)
        {
            apps.Add(span.Source.Application);
            if (span.UninstrumentedPeer != null)
            {
                apps.Add(span.UninstrumentedPeer);
            }
        }
        _resourceCount = apps.Count;
    }

    private async Task HandleAfterFilterBindAsync()
    {
        SelectedData = null;
        await InvokeAsync(StateHasChanged);

        await InvokeAsync(_dataGrid.SafeRefreshDataAsync);
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
                if (_trace == null)
                {
                    return;
                }

                // Only update trace if required.
                if (TelemetryRepository.HasUpdatedTrace(_trace))
                {
                    UpdateDetailViewData();
                    StateHasChanged();
                    await _dataGrid.SafeRefreshDataAsync();
                }
                else
                {
                    Logger.LogTrace("Trace '{TraceId}' is unchanged.", TraceId);
                }
            }));
        }
    }

    private string GetRowClass(SpanWaterfallViewModel viewModel)
    {
        // Test with id rather than the object reference because the data and view model objects are recreated on trace updates.
        if (SelectedData?.SpanViewModel is { } selectedSpan && selectedSpan.Span.SpanId == viewModel.Span.SpanId)
        {
            return "selected-row";
        }
        else if (SelectedData?.LogEntryViewModel is { } selectedLog && viewModel.SpanLogs.Any(l => l.LogEntry.InternalId == selectedLog.LogEntry.InternalId))
        {
            return "selected-row";
        }

        return string.Empty;
    }

    public TraceDetailSelectedDataViewModel? SelectedData { get; set; }

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

        UpdateDetailViewData();
        await _dataGrid.SafeRefreshDataAsync();
    }

    private async Task OnShowPropertiesAsync(SpanWaterfallViewModel viewModel, string? buttonId)
    {
        _elementIdBeforeDetailsViewOpened = buttonId;

        if (SelectedData?.SpanViewModel?.Span.SpanId == viewModel.Span.SpanId)
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

            SelectedData = new TraceDetailSelectedDataViewModel
            {
                SpanViewModel = spanDetailsViewModel
            };
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
        SelectedData = null;

        if (_elementIdBeforeDetailsViewOpened is not null && causedByUserAction)
        {
            await JS.InvokeVoidAsync("focusElement", _elementIdBeforeDetailsViewOpened);
        }

        _elementIdBeforeDetailsViewOpened = null;
    }

    private string GetResourceName(OtlpApplicationView app) => OtlpApplication.GetResourceName(app, _applications);

    private async Task ToggleSpanLogsAsync(OtlpLogEntry logEntry)
    {
        if (SelectedData?.LogEntryViewModel?.LogEntry.InternalId == logEntry.InternalId)
        {
            await ClearSelectedSpanAsync();
        }
        else
        {
            SelectedData = new TraceDetailSelectedDataViewModel
            {
                LogEntryViewModel = new StructureLogsDetailsViewModel { LogEntry = logEntry }
            };
        }
    }

    public void Dispose()
    {
        foreach (var subscription in _peerChangesSubscriptions)
        {
            subscription.Dispose();
        }
        _tracesSubscription?.Dispose();
        TelemetryContext.Dispose();
    }

    // IComponentWithTelemetry impl
    public ComponentTelemetryContext TelemetryContext { get; } = new(ComponentType.Page, TelemetryComponentIds.TraceDetail);
}

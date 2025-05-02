// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Controls;

public partial class SpanDetails : IDisposable
{
    [Parameter, EditorRequired]
    public required SpanDetailsViewModel ViewModel { get; set; }

    [Inject]
    public required IDialogService DialogService { get; init; }

    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    [Inject]
    public required TelemetryRepository TelemetryRepository { get; init; }

    [Inject]
    public required IJSRuntime JS { get; init; }

    private IQueryable<TelemetryPropertyViewModel> FilteredItems =>
        ViewModel.Properties.Where(ApplyFilter).AsQueryable();

    private IQueryable<TelemetryPropertyViewModel> FilteredContextItems =>
        _contextAttributes.Where(ApplyFilter).AsQueryable();

    private IQueryable<TelemetryPropertyViewModel> FilteredResourceItems =>
        ViewModel.Span.Source.AllProperties().Select(p => new TelemetryPropertyViewModel { Name = p.DisplayName, Key = p.Key, Value = p.Value })
            .Where(ApplyFilter).AsQueryable();

    private IQueryable<OtlpSpanEvent> FilteredSpanEvents =>
        ViewModel.Span.Events.Where(e => e.Name.Contains(_filter, StringComparison.CurrentCultureIgnoreCase)).OrderBy(e => e.Time).AsQueryable();

    private IQueryable<SpanLinkViewModel> FilteredSpanLinks =>
        ViewModel.Links.Where(e => e.SpanId.Contains(_filter, StringComparison.CurrentCultureIgnoreCase)).AsQueryable();

    private IQueryable<SpanLinkViewModel> FilteredSpanBacklinks =>
        ViewModel.Backlinks.Where(e => e.SpanId.Contains(_filter, StringComparison.CurrentCultureIgnoreCase)).AsQueryable();

    private bool _isSpanEventsExpanded;
    private bool _isSpanLinksExpanded;
    private bool _isSpanBacklinksExpanded;

    private string _filter = "";
    private List<TelemetryPropertyViewModel> _contextAttributes = null!;
    private bool _dataChanged;
    private SpanDetailsViewModel? _viewModel;

    private ColumnResizeLabels _resizeLabels = ColumnResizeLabels.Default;
    private ColumnSortLabels _sortLabels = ColumnSortLabels.Default;

    private readonly CancellationTokenSource _cts = new();

    private bool ApplyFilter(TelemetryPropertyViewModel vm)
    {
        return vm.Name.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) ||
            vm.Value?.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) == true;
    }

    protected override void OnInitialized()
    {
        (_resizeLabels, _sortLabels) = DashboardUIHelpers.CreateGridLabels(Loc);
    }

    protected override void OnParametersSet()
    {
        if (!ReferenceEquals(ViewModel, _viewModel))
        {
            // Only set data changed flag if the item being view changes.
            if (!string.Equals(ViewModel.Span.SpanId, _viewModel?.Span.SpanId, StringComparisons.OtlpSpanId))
            {
                _dataChanged = true;
            }

            _viewModel = ViewModel;

            _contextAttributes =
            [
                new TelemetryPropertyViewModel { Name = "Source", Key = KnownSourceFields.NameField, Value = _viewModel.Span.Scope.Name }
            ];
            if (!string.IsNullOrEmpty(_viewModel.Span.Scope.Version))
            {
                _contextAttributes.Add(new TelemetryPropertyViewModel { Name = "Version", Key = KnownSourceFields.VersionField, Value = _viewModel.Span.Scope.Version });
            }
            if (!string.IsNullOrEmpty(_viewModel.Span.ParentSpanId))
            {
                _contextAttributes.Add(new TelemetryPropertyViewModel { Name = "ParentId", Key = KnownTraceFields.ParentIdField, Value = _viewModel.Span.ParentSpanId });
            }
            if (!string.IsNullOrEmpty(_viewModel.Span.TraceId))
            {
                _contextAttributes.Add(new TelemetryPropertyViewModel { Name = "TraceId", Key = KnownTraceFields.TraceIdField, Value = _viewModel.Span.TraceId });
            }

            // Collapse details sections when they have no data.
            _isSpanEventsExpanded = _viewModel.Span.Events.Any();
            _isSpanLinksExpanded = _viewModel.Span.Links.Any();
            _isSpanBacklinksExpanded = _viewModel.Span.BackLinks.Any();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_dataChanged)
        {
            if (!firstRender)
            {
                await JS.InvokeVoidAsync("scrollToTop", ".property-grid-container");
            }

            _dataChanged = false;
        }
    }

    public async Task OnViewDetailsAsync(SpanLinkViewModel linkVM)
    {
        var available = await MetricsHelpers.WaitForSpanToBeAvailableAsync(
            traceId: linkVM.TraceId,
            spanId: linkVM.SpanId,
            getSpan: TelemetryRepository.GetSpan,
            DialogService,
            InvokeAsync,
            DialogsLoc,
            _cts.Token).ConfigureAwait(false);

        if (available)
        {
            NavigationManager.NavigateTo(DashboardUrls.TraceDetailUrl(linkVM.TraceId, spanId: linkVM.SpanId));
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}

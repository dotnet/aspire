// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

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

    private IQueryable<SpanPropertyViewModel> FilteredItems =>
        ViewModel.Properties.Where(ApplyFilter).AsQueryable();

    private IQueryable<SpanPropertyViewModel> FilteredContextItems =>
        _contextAttributes.Select(p => new SpanPropertyViewModel { Name = p.Key, Value = p.Value })
            .Where(ApplyFilter).AsQueryable();

    private IQueryable<SpanPropertyViewModel> FilteredResourceItems =>
        ViewModel.Span.Source.AllProperties().Select(p => new SpanPropertyViewModel { Name = p.Key, Value = p.Value })
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
    private List<KeyValuePair<string, string>> _contextAttributes = null!;

    private readonly CancellationTokenSource _cts = new();

    private bool ApplyFilter(SpanPropertyViewModel vm)
    {
        return vm.Name.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) ||
            vm.Value?.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) == true;
    }

    protected override void OnParametersSet()
    {
        _contextAttributes =
        [
            new KeyValuePair<string, string>("Source", ViewModel.Span.Scope.ScopeName)
        ];
        if (!string.IsNullOrEmpty(ViewModel.Span.Scope.Version))
        {
            _contextAttributes.Add(new KeyValuePair<string, string>("Version", ViewModel.Span.Scope.Version));
        }
        if (!string.IsNullOrEmpty(ViewModel.Span.ParentSpanId))
        {
            _contextAttributes.Add(new KeyValuePair<string, string>("ParentId", ViewModel.Span.ParentSpanId));
        }
        if (!string.IsNullOrEmpty(ViewModel.Span.TraceId))
        {
            _contextAttributes.Add(new KeyValuePair<string, string>("TraceId", ViewModel.Span.TraceId));
        }

        // Collapse details sections when they have no data.
        _isSpanEventsExpanded = ViewModel.Span.Events.Any();
        _isSpanLinksExpanded = ViewModel.Span.Links.Any();
        _isSpanBacklinksExpanded = ViewModel.Span.BackLinks.Any();
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

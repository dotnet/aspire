// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Controls.Chart;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Dialogs;

public class DataRow
{
    public required string Name { get; set; }
    public bool Selected { get; set; }
}

public partial class ManageDataDialog : IDialogContentComponent, IDisposable
{
    [Inject]
    public required BrowserTimeProvider TimeProvider { get; init; }

    [Inject]
    public required IDialogService DialogService { get; init; }

    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    [Inject]
    public required TelemetryRepository TelemetryRepository { get; init; }

    private readonly List<DataRow> _dataRows = new()
    {
        new DataRow { Name = "#1", Selected = true },
        new DataRow { Name = "#2", Selected = false },
        new DataRow { Name = "#3", Selected = true },
    };

    public IEnumerable<DataRow> SelectedItems { get; set; } = Enumerable.Empty<DataRow>();

    public IQueryable<DataRow> MetricView => _dataRows.AsQueryable();

    private readonly CancellationTokenSource _cts = new();

    protected override void OnInitialized()
    {
        SelectedItems = _dataRows.Where(p => p.Selected);
    }

    public async Task OnViewDetailsAsync(ChartExemplar exemplar)
    {
        var available = await TraceLinkHelpers.WaitForSpanToBeAvailableAsync(
            traceId: exemplar.TraceId,
            spanId: exemplar.SpanId,
            getSpan: TelemetryRepository.GetSpan,
            DialogService,
            InvokeAsync,
            Loc,
            _cts.Token).ConfigureAwait(false);

        if (available)
        {
            NavigationManager.NavigateTo(DashboardUrls.TraceDetailUrl(exemplar.TraceId, spanId: exemplar.SpanId));
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}

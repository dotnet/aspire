// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Components.Controls.Chart;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Dialogs;

public partial class ExemplarsDialog : IDisposable
{
    [CascadingParameter]
    public FluentDialog Dialog { get; set; } = default!;

    [Parameter]
    public ExemplarsDialogViewModel Content { get; set; } = default!;

    [Inject]
    public required BrowserTimeProvider TimeProvider { get; init; }

    [Inject]
    public required IDialogService DialogService { get; init; }

    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    [Inject]
    public required TelemetryRepository TelemetryRepository { get; init; }

    public IQueryable<ChartExemplar> MetricView => Content.Exemplars.AsQueryable();

    private readonly CancellationTokenSource _cts = new();

    public async Task OnViewDetailsAsync(ChartExemplar exemplar)
    {
        var available = await MetricsHelpers.WaitForSpanToBeAvailableAsync(
            traceId: exemplar.TraceId,
            spanId: exemplar.SpanId,
            getSpan: (traceId, spanId) => MetricsHelpers.GetSpan(TelemetryRepository, traceId, spanId),
            DialogService,
            InvokeAsync,
            Loc,
            _cts.Token).ConfigureAwait(false);

        if (available)
        {
            NavigationManager.NavigateTo(DashboardUrls.TraceDetailUrl(exemplar.TraceId, spanId: exemplar.SpanId));
        }
    }

    private string GetTitle(ChartExemplar exemplar)
    {
        return (exemplar.Span != null)
            ? SpanWaterfallViewModel.GetTitle(exemplar.Span, Content.Applications)
            : $"{Loc[nameof(Resources.Dialogs.ExemplarsDialogTrace)]}: {OtlpHelpers.ToShortenedId(exemplar.TraceId)}";
    }

    private string FormatMetricValue(double? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var formattedValue = value.Value.ToString("F3", CultureInfo.CurrentCulture);
        if (!string.IsNullOrEmpty(Content.Instrument.Unit))
        {
            formattedValue += Content.Instrument.Unit.TrimStart('{').TrimEnd('}');
        }

        return formattedValue;
    }

    public void Dispose()
    {
        _cts.Dispose();
    }
}

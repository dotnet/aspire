// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Dialogs;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components;

public partial class LogMessageColumnDisplay
{
    [Inject]
    public required IDialogService DialogService { get; init; }

    [Inject]
    public required TelemetryRepository TelemetryRepository { get; init; }

    [Inject]
    public required IStringLocalizer<Aspire.Dashboard.Resources.Dialogs> DialogsLoc { get; init; }

    [Parameter, EditorRequired]
    public required OtlpLogEntry LogEntry { get; set; }

    [Parameter, EditorRequired]
    public required string FilterText { get; set; }

    [Parameter, EditorRequired]
    public required List<OtlpResource> Resources { get; set; }

    [CascadingParameter]
    public required ViewportInformation ViewportInformation { get; set; }

    private string? _exceptionText;

    protected override void OnInitialized()
    {
        _exceptionText = OtlpLogEntry.GetExceptionText(LogEntry);
    }

    private async Task LaunchGenAiVisualizerAsync()
    {
        var available = await TraceLinkHelpers.WaitForSpanToBeAvailableAsync(
            LogEntry.TraceId,
            LogEntry.SpanId,
            TelemetryRepository.GetSpan,
            DialogService,
            InvokeAsync,
            DialogsLoc,
            CancellationToken.None).ConfigureAwait(false);

        if (available)
        {
            var span = TelemetryRepository.GetSpan(LogEntry.TraceId, LogEntry.SpanId)!;

            var logsContext = new GetLogsContext
            {
                ResourceKey = null,
                Count = int.MaxValue,
                StartIndex = 0,
                Filters = [new TelemetryFilter
                {
                    Field = KnownStructuredLogFields.SpanIdField,
                    Condition = FilterCondition.Equals,
                    Value = LogEntry.SpanId
                }]
            };
            var result = TelemetryRepository.GetLogs(logsContext);

            await GenAIVisualizerDialog.OpenDialogAsync(
                ViewportInformation,
                DialogService,
                DialogsLoc,
                span,
                result.Items,
                LogEntry.InternalId,
                TelemetryRepository,
                Resources);
        }
    }
}

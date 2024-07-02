// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Resources;
using Aspire.Dashboard.Utils;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Model;

public static class MetricsHelpers
{
    public static OtlpSpan? GetSpan(TelemetryRepository telemetryRepository, string traceId, string spanId)
    {
        var trace = telemetryRepository.GetTrace(traceId);
        if (trace == null)
        {
            return null;
        }

        return trace.Spans.FirstOrDefault(s => s.SpanId == spanId);
    }

    public static async Task<bool> WaitForSpanToBeAvailableAsync(
        string traceId,
        string spanId,
        Func<string, string, OtlpSpan?> getSpan,
        IDialogService dialogService,
        Func<Func<Task>, Task> dispatcher,
        IStringLocalizer<Dialogs> loc,
        CancellationToken cancellationToken)
    {
        var span = getSpan(traceId, spanId);

        // Exemplar span isn't loaded yet. Display a dialog until the data is ready or the user cancels the dialog.
        if (span == null)
        {
            using var cts = new CancellationTokenSource();
            using var registration = cancellationToken.Register(cts.Cancel);

            var reference = await dialogService.ShowMessageBoxAsync(new DialogParameters<MessageBoxContent>()
            {
                Content = new MessageBoxContent
                {
                    Intent = MessageBoxIntent.Info,
                    Icon = new Icons.Filled.Size24.Info(),
                    IconColor = Color.Info,
                    Message = string.Format(CultureInfo.InvariantCulture, loc[nameof(Dialogs.OpenTraceDialogMessage)], OtlpHelpers.ToShortenedId(traceId)),
                },
                DialogType = DialogType.MessageBox,
                PrimaryAction = string.Empty,
                SecondaryAction = loc[nameof(Dialogs.OpenTraceDialogCancelButtonText)]
            }).ConfigureAwait(false);

            // Task that polls for the span to be available.
            var waitForTraceTask = Task.Run(async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    span = getSpan(traceId, spanId);
                    if (span != null)
                    {
                        await dispatcher(async () =>
                        {
                            await reference.CloseAsync(DialogResult.Ok<bool>(true)).ConfigureAwait(false);
                        }).ConfigureAwait(false);
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromSeconds(0.5), cts.Token).ConfigureAwait(false);
                    }
                }
            }, cts.Token);

            var result = await reference.Result.ConfigureAwait(false);
            cts.Cancel();

            await TaskHelpers.WaitIgnoreCancelAsync(waitForTraceTask).ConfigureAwait(false);

            if (result.Cancelled)
            {
                // Dialog was canceled before span was ready. Exit without navigating.
                return false;
            }
        }

        return true;
    }
}

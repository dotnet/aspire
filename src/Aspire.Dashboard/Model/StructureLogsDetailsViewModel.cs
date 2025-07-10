// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Model;
using Microsoft.Extensions.Localization;

namespace Aspire.Dashboard.Model;

public sealed class StructureLogsDetailsViewModel
{
    public required OtlpLogEntry LogEntry { get; init; }

    public static string GetEventName(OtlpLogEntry logEntry, IStringLocalizer<Dashboard.Resources.StructuredLogs> loc)
    {
        var eventName = OtlpHelpers.GetValue(logEntry.Attributes, "event.name")
                ?? OtlpHelpers.GetValue(logEntry.Attributes, "logrecord.event.name")
                ?? loc[nameof(Dashboard.Resources.StructuredLogs.StructuredLogsEntryDetails)];

        return eventName;
    }
}

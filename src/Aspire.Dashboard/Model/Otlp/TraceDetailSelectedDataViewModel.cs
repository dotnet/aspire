// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model.Otlp;

public sealed class TraceDetailSelectedDataViewModel
{
    public SpanDetailsViewModel? SpanViewModel { get; init; }
    public StructureLogsDetailsViewModel? LogEntryViewModel { get; init; }
}

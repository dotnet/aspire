// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Model;

namespace Aspire.Dashboard.Model.Otlp;

public sealed class SpanLogEntryViewModel
{
    public required int Index { get; init; }
    public required OtlpLogEntry LogEntry { get; init; }
    public required double LeftOffset { get; init; }
}

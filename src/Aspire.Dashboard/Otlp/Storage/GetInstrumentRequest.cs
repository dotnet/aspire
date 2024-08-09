// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Otlp.Storage;

public sealed class GetInstrumentRequest
{
    public required string InstrumentName { get; init; }
    public required ApplicationKey ApplicationKey { get; init; }
    public required string MeterName { get; init; }
    public DateTime? StartTime { get; init; }
    public DateTime? EndTime { get; init; }
}

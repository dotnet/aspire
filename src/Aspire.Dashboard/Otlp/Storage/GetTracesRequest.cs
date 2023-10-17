// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Model;

namespace Aspire.Dashboard.Otlp.Storage;

public sealed class GetTracesRequest
{
    public required string? ApplicationServiceId { get; init; }
    public required int StartIndex { get; init; }
    public required int? Count { get; init; }
    public required string FilterText { get; init; }
}

public sealed class GetTracesResponse
{
    public required PagedResult<OtlpTrace> PagedResult { get; init; }
    public required TimeSpan MaxDuration { get; init; }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Otlp.Storage;

public sealed class GetTracesRequest
{
    public required ApplicationKey? ApplicationKey { get; init; }
    public required int StartIndex { get; init; }
    public required int? Count { get; init; }
    public required string FilterText { get; init; }
}

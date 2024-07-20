// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model.Otlp;

namespace Aspire.Dashboard.Otlp.Storage;

public sealed class GetLogsContext
{
    public required ApplicationKey? ApplicationKey { get; init; }
    public required int StartIndex { get; init; }
    public required int? Count { get; init; }
    public required List<LogFilter> Filters { get; init; }
}

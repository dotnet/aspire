// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Configuration;

namespace Aspire.Dashboard.Otlp.Model;

public sealed class OtlpContext
{
    public required ILogger Logger { get; init; }
    public required TelemetryLimitOptions Options { get; init; }
}

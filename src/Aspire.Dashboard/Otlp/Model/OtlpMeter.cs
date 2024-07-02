// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Dashboard.Configuration;
using OpenTelemetry.Proto.Common.V1;

namespace Aspire.Dashboard.Otlp.Model;

[DebuggerDisplay("MeterName = {MeterName}")]
public class OtlpMeter(InstrumentationScope scope, TelemetryLimitOptions options)
{
    public string MeterName { get; init; } = scope.Name;
    public string Version { get; init; } = scope.Version;

    public ReadOnlyMemory<KeyValuePair<string, string>> Attributes { get; } = scope.Attributes.ToKeyValuePairs(options);
}

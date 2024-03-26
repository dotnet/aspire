// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Dashboard.Configuration;
using OpenTelemetry.Proto.Common.V1;

namespace Aspire.Dashboard.Otlp.Model;

[DebuggerDisplay("MeterName = {MeterName}")]
public class OtlpMeter
{
    public string MeterName { get; init; }
    public string Version { get; init; }

    public ReadOnlyMemory<KeyValuePair<string, string>> Attributes { get; }

    public OtlpMeter(InstrumentationScope scope, TelemetryLimitOptions options)
    {
        MeterName = scope.Name;
        Version = scope.Version;
        Attributes = scope.Attributes.ToKeyValuePairs(options);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using OpenTelemetry.Proto.Common.V1;

namespace Aspire.Dashboard.Otlp.Model;

[DebuggerDisplay("MeterName = {MeterName}, Instruments = {Instruments.Count}")]
public class OtlpMeter
{
    public string MeterName { get; init; }
    public string Version { get; init; }

    public KeyValuePair<string, string>[] Properties { get; }

    public OtlpMeter(InstrumentationScope scope)
    {
        MeterName = scope.Name;
        Version = scope.Version;
        Properties = scope.Attributes.ToKeyValuePairs();
    }
}

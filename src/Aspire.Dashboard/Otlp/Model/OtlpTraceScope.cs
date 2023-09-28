// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using OpenTelemetry.Proto.Common.V1;

namespace Aspire.Dashboard.Otlp.Model;

/// <summary>
/// The Scope of a TraceSource, maps to the name of the ActivitySource in .NET
/// </summary>
public class OtlpTraceScope
{
    public string ScopeName { get; }
    public string Version { get; }

    public KeyValuePair<string, string>[] Properties { get; }

    public string ServiceProperties => Properties.ConcatProperties();

    public OtlpTraceScope(InstrumentationScope scope)
    {
        ScopeName = scope.Name;

        Properties = scope.Attributes.ToKeyValuePairs();
        Version = scope.Version;
    }
}

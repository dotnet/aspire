// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using OpenTelemetry.Proto.Common.V1;

namespace Aspire.Dashboard.Otlp.Model;

/// <summary>
/// The Scope of a TraceSource, maps to the name of the ActivitySource in .NET
/// </summary>
public class OtlpScope
{
    public static readonly OtlpScope Empty = new OtlpScope();

    public string ScopeName { get; }
    public string Version { get; }

    public ReadOnlyMemory<KeyValuePair<string, string>> Attributes { get; }

    private OtlpScope()
    {
        ScopeName = string.Empty;
        Attributes = Array.Empty<KeyValuePair<string, string>>();
        Version = string.Empty;
    }

    public OtlpScope(InstrumentationScope scope, OtlpContext context)
    {
        ScopeName = scope.Name;

        Attributes = scope.Attributes.ToKeyValuePairs(context);
        Version = scope.Version;
    }
}

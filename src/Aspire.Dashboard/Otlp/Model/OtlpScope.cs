// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Dashboard.Otlp.Model;

[DebuggerDisplay("Name = {Name}")]
public sealed class OtlpScope
{
    private const string UnknownScopeName = "unknown";

    public static readonly OtlpScope Empty = new OtlpScope(name: UnknownScopeName, version: string.Empty, attributes: []);

    public string Name { get; }
    public string Version { get; }
    public KeyValuePair<string, string>[] Attributes { get; }

    public OtlpScope(string name, string version, KeyValuePair<string, string>[] attributes)
    {
        Name = name is { Length: > 0 } ? name : UnknownScopeName;
        Version = version;
        Attributes = attributes;
    }
}

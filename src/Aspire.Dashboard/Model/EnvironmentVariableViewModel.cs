// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public sealed class EnvironmentVariableViewModel(string name, string? value, bool fromSpec)
{
    public string Name { get; } = name;
    public string? Value { get; } = value;
    public bool FromSpec { get; } = fromSpec;

    public bool IsValueMasked { get; set; } = true;
}

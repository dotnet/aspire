// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public sealed class EnvironmentVariableViewModel
{
    public required string Name { get; init; }
    public required string? Value { get; init; }
    public required bool FromSpec { get; init; }

    public bool IsValueMasked { get; set; } = true;
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Dashboard;

internal sealed class EnvironmentVariableSnapshot
{
    public required string Name { get; init; }
    public required string? Value { get; init; }
    public required bool FromSpec { get; init; }
}

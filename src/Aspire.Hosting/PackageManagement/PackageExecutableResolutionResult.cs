// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.PackageManagement;

internal sealed class PackageExecutableResolutionResult
{
    public required string PackageId { get; init; }

    public required string PackageVersion { get; init; }

    public required string PackageDirectory { get; init; }

    public required string ExecutablePath { get; init; }

    public required string Command { get; init; }

    public required string WorkingDirectory { get; init; }

    public IReadOnlyList<string> Arguments { get; init; } = [];
}
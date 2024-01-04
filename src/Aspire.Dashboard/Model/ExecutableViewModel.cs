// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Aspire.Dashboard.Model;

/// <summary>
/// Immutable snapshot of executable state at a point in time.
/// </summary>
public class ExecutableViewModel : ResourceViewModel
{
    public override string ResourceType => "Executable";

    public required int? ProcessId { get; init; }
    public required string? ExecutablePath { get; init; }
    public required string? WorkingDirectory { get; init; }
    public required ImmutableArray<string>? Arguments { get; init; }
    public required string? StdOutFile { get; init; }
    public required string? StdErrFile { get; init; }
}


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Globalization;
using Aspire.Dashboard.Model;
using Google.Protobuf.WellKnownTypes;

namespace Aspire.Hosting.Dashboard;

/// <summary>
/// Immutable snapshot of an executable's state at a point in time.
/// </summary>
public class ExecutableSnapshot : ResourceSnapshot
{
    public override string ResourceType => KnownResourceTypes.Executable;

    public required int? ProcessId { get; init; }
    public required string? ExecutablePath { get; init; }
    public required string? WorkingDirectory { get; init; }
    public required ImmutableArray<string>? Arguments { get; init; }
    public required string? StdOutFile { get; init; }
    public required string? StdErrFile { get; init; }

    protected override IEnumerable<(string Key, Value Value)> GetCustomData()
    {
        yield return (ResourceDataKeys.Executable.Path, Value.ForString(ExecutablePath));
        yield return (ResourceDataKeys.Executable.WorkDir, Value.ForString(WorkingDirectory));
        yield return (ResourceDataKeys.Executable.Args, Arguments is null ? Value.ForNull() : Value.ForList(Arguments.Value.Select(arg => Value.ForString(arg)).ToArray()));
        yield return (ResourceDataKeys.Executable.Pid, ProcessId is null ? Value.ForNull() : Value.ForString(ProcessId.Value.ToString("N", CultureInfo.InvariantCulture)));
        // TODO decide whether to send StdOut/StdErr file paths or not, and what we could use them for in the client.
    }
}


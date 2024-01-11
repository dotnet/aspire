// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Aspire.Dashboard.Model;
using Google.Protobuf.WellKnownTypes;

namespace Aspire.Hosting.Dashboard;

/// <summary>
/// Immutable snapshot of a container's state at a point in time.
/// </summary>
internal sealed class ContainerSnapshot : ResourceSnapshot
{
    public override string ResourceType => KnownResourceTypes.Container;

    public required string? ContainerId { get; init; }
    public required string Image { get; init; }
    public required ImmutableArray<int> Ports { get; init; }
    public required string? Command { get; init; }
    public required ImmutableArray<string>? Args { get; init; }

    protected override IEnumerable<(string Key, Value Value)> GetProperties()
    {
        yield return (KnownProperties.Container.Id, ContainerId is null ? Value.ForNull() : Value.ForString(ContainerId));
        yield return (KnownProperties.Container.Image, Value.ForString(Image));
        yield return (KnownProperties.Container.Ports, Value.ForList(Ports.Select(port => Value.ForNumber(port)).ToArray()));
        yield return (KnownProperties.Container.Command, Command is null ? Value.ForNull() : Value.ForString(Command));
        yield return (KnownProperties.Container.Args, Args is null ? Value.ForNull() : Value.ForList(Args.Value.Select(port => Value.ForString(port)).ToArray()));
    }
}

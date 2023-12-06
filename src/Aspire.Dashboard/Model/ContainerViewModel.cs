// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Aspire.Dashboard.Utils;
using Google.Protobuf.WellKnownTypes;

namespace Aspire.Dashboard.Model;

/// <summary>
/// Immutable snapshot of container state at a point in time.
/// </summary>
public class ContainerViewModel : ResourceViewModel
{
    public override string ResourceType => "Container";

    public required string? ContainerId { get; init; }
    public required string Image { get; init; }
    public required ImmutableArray<int> Ports { get; init; }
    public required string? Command { get; init; }
    public required ImmutableArray<string>? Args { get; init; }

    internal override bool MatchesFilter(string filter)
    {
        return base.MatchesFilter(filter) || Image.Contains(filter, StringComparisons.UserTextSearch);
    }

    protected override IEnumerable<(string Key, Value Value)> GetCustomData()
    {
        yield return (ResourceDataKeys.Container.Id, Value.ForString(ContainerId));
        yield return (ResourceDataKeys.Container.Image, Value.ForString(Image));
        yield return (ResourceDataKeys.Container.Ports, Value.ForList(Ports.Select(port => Value.ForNumber(port)).ToArray()));
    }
}

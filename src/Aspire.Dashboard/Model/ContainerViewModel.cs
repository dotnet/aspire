// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Aspire.Dashboard.Extensions;

namespace Aspire.Dashboard.Model;

public class ContainerViewModel : ResourceViewModel
{
    public override string ResourceType => "Container";
    public string? ContainerId { get; init; }
    public required string Image { get; init; }
    public ImmutableArray<int> Ports { get; init; }
    public string? Command { get; init; }
    public ImmutableArray<string>? Args { get; init; }

    internal override bool MatchesFilter(string filter)
    {
        return base.MatchesFilter(filter) || Image.Contains(filter, StringComparisons.UserTextSearch);
    }
}

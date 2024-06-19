// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using Aspire.Dashboard.Extensions;
using Aspire.Dashboard.Otlp.Model;
using Google.Protobuf.WellKnownTypes;

namespace Aspire.Dashboard.Model;

[DebuggerDisplay("Name = {Name}, ResourceType = {ResourceType}, State = {State}, Properties = {Properties.Count}")]
public sealed class ResourceViewModel
{
    public required string Name { get; init; }
    public required string ResourceType { get; init; }
    public required string DisplayName { get; init; }
    public required string Uid { get; init; }
    public required string? State { get; init; }
    public required string? StateStyle { get; init; }
    public required DateTime? CreationTimeStamp { get; init; }
    public required ImmutableArray<EnvironmentVariableViewModel> Environment { get; init; }
    public required ImmutableArray<UrlViewModel> Urls { get; init; }
    public required FrozenDictionary<string, Value> Properties { get; init; }
    public required ImmutableArray<CommandViewModel> Commands { get; init; }
    public KnownResourceState? KnownState { get; init; }

    internal bool MatchesFilter(string filter)
    {
        // TODO let ResourceType define the additional data values we include in searches
        return Name.Contains(filter, StringComparisons.UserTextSearch);
    }

    public static string GetResourceName(ResourceViewModel resource, ConcurrentDictionary<string, ResourceViewModel> allResources)
    {
        var count = 0;
        foreach (var (_, item) in allResources)
        {
            if (item.IsHiddenState())
            {
                continue;
            }

            if (item.DisplayName == resource.DisplayName)
            {
                count++;
                if (count >= 2)
                {
                    // There are multiple resources with the same display name so they're part of a replica set.
                    // Need to change the name so it includes an ID to tell them apart.
                    // DCP automatically includes a suffix on a kind of resource name, however we don't want it
                    // because we can't share it between resource pages and telemetry pages.
                    // Instead, combine resource name with the UID.
                    // Before: catalogservice-mbrpbvo
                    // After: catalogservice-{truncated-guid}
                    return $"{resource.DisplayName}-{OtlpHelpers.TruncateString(resource.Uid, maxLength: 7)}";
                }
            }
        }

        return resource.DisplayName;
    }
}

public sealed class CommandViewModel
{
    public string CommandType { get; }
    public string DisplayName { get; }
    public string? ConfirmationMessage { get; }
    public Value? Parameter { get; }

    public CommandViewModel(string commandType, string displayName, string? confirmationMessage, Value? parameter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandType);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        CommandType = commandType;
        DisplayName = displayName;
        ConfirmationMessage = confirmationMessage;
        Parameter = parameter;
    }
}

public sealed class EnvironmentVariableViewModel
{
    public string Name { get; }
    public string? Value { get; }
    public bool FromSpec { get; }

    public bool IsValueMasked { get; set; } = true;

    public EnvironmentVariableViewModel(string name, string? value, bool fromSpec)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        Value = value;
        FromSpec = fromSpec;
    }
}

public sealed class UrlViewModel
{
    public string Name { get; }
    public Uri Url { get; }
    public bool IsInternal { get; }

    public UrlViewModel(string name, Uri url, bool isInternal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(url);

        Name = name;
        Url = url;
        IsInternal = isInternal;
    }
}

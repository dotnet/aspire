// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using Aspire.Dashboard.Extensions;
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
    public required ImmutableArray<OwnerViewModel> Owners { get; init; }

    public KnownResourceState? KnownState { get; init; }

    internal bool MatchesFilter(string filter)
    {
        // TODO let ResourceType define the additional data values we include in searches
        return Name.Contains(filter, StringComparisons.UserTextSearch);
    }

    internal OwnerViewModel? GetReplicaSetOrDefault()
    {
        return Owners.FirstOrDefault(owner => string.Equals(owner.Kind, KnownOwnerProperties.ExecutableReplicaSetKind, StringComparisons.ResourceOwnerKind));
    }

    public static string GetResourceName(ResourceViewModel resource, IDictionary<string, ResourceViewModel> allResources)
    {
        if (resource.GetReplicaSetOrDefault() is { } resourceReplicaSet)
        {
            var count = 0;
            foreach (var (_, item) in allResources)
            {
                if (item.IsHiddenState())
                {
                    continue;
                }

                if (item.GetReplicaSetOrDefault() is { } itemReplicaSet && string.Equals(itemReplicaSet.Uid, resourceReplicaSet.Uid, StringComparisons.ResourceOwnerUid))
                {
                    count++;
                    if (count >= 2)
                    {
                        // There are multiple resources that are part of a replica set.
                        // Need to use the name which has a unique ID to tell them apart.
                        return resource.Name;
                    }
                }
            }
        }

        return resource.DisplayName;
    }
}

[DebuggerDisplay("CommandType = {CommandType}, DisplayName = {DisplayName}")]
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

[DebuggerDisplay("Name = {Name}, Value = {Value}, FromSpec = {FromSpec}, IsValueMasked = {IsValueMasked}")]
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

[DebuggerDisplay("Name = {Name}, Url = {Url}, IsInternal = {IsInternal}")]
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

public record OwnerViewModel(string Kind, string Name, string Uid);

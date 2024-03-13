// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
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
    public required DateTime? CreationTimeStamp { get; init; }
    public required ImmutableArray<EnvironmentVariableViewModel> Environment { get; init; }
    public required ImmutableArray<EndpointViewModel> Endpoints { get; init; }
    public required ImmutableArray<ResourceServiceViewModel> Services { get; init; }
    public required int? ExpectedEndpointsCount { get; init; }
    public required FrozenDictionary<string, Value> Properties { get; init; }
    public required ImmutableArray<CommandViewModel> Commands { get; init; }

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
            if (item.State == ResourceStates.HiddenState)
            {
                continue;
            }

            if (item.DisplayName == resource.DisplayName)
            {
                count++;
                if (count >= 2)
                {
                    return resource.Name;
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

public sealed class EndpointViewModel
{
    public string EndpointUrl { get; }
    public string ProxyUrl { get; }

    public EndpointViewModel(string endpointUrl, string proxyUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(proxyUrl);

        EndpointUrl = endpointUrl;
        ProxyUrl = proxyUrl;
    }
}

public sealed class ResourceServiceViewModel
{
    public string Name { get; }
    public string? AllocatedAddress { get; }
    public int? AllocatedPort { get; }

    public string AddressAndPort { get; }

    public ResourceServiceViewModel(string name, string? allocatedAddress, int? allocatedPort)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        AllocatedAddress = allocatedAddress;
        AllocatedPort = allocatedPort;
        AddressAndPort = $"{allocatedAddress}:{allocatedPort}";
    }
}

public static class ResourceStates
{
    public const string FinishedState = "Finished";
    public const string ExitedState = "Exited";
    public const string FailedToStartState = "FailedToStart";
    public const string StartingState = "Starting";
    public const string RunningState = "Running";
    public const string HiddenState = "Hidden";
}

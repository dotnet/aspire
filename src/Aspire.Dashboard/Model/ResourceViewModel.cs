// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Collections.Immutable;
using Google.Protobuf.WellKnownTypes;

namespace Aspire.Dashboard.Model;

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

    internal bool MatchesFilter(string filter)
    {
        // TODO let ResourceType define the additional data values we include in searches
        return Name.Contains(filter, StringComparisons.UserTextSearch);
    }

    public static string GetResourceName(ResourceViewModel resource, IEnumerable<ResourceViewModel> allResources)
    {
        var count = 0;
        foreach (var item in allResources)
        {
            if (item.DisplayName == resource.DisplayName)
            {
                count++;
                if (count >= 2)
                {
                    return ResourceFormatter.GetName(resource.DisplayName, resource.Uid);
                }
            }
        }

        return resource.DisplayName;
    }
}

public sealed class EnvironmentVariableViewModel(string name, string? value, bool fromSpec)
{
    public string Name { get; } = name;
    public string? Value { get; } = value;
    public bool FromSpec { get; } = fromSpec;

    public bool IsValueMasked { get; set; } = true;
}

public sealed class EndpointViewModel(string endpointUrl, string proxyUrl)
{
    public string EndpointUrl { get; } = endpointUrl;
    public string ProxyUrl { get; } = proxyUrl;
}

public sealed class ResourceServiceViewModel(string name, string? allocatedAddress, int? allocatedPort)
{
    public string Name { get; } = name;
    public string? AllocatedAddress { get; } = allocatedAddress;
    public int? AllocatedPort { get; } = allocatedPort;

    public string AddressAndPort { get; } = $"{allocatedAddress}:{allocatedPort}";
}

public static class ResourceStates
{
    public const string FinishedState = "Finished";
    public const string ExitedState = "Exited";
}

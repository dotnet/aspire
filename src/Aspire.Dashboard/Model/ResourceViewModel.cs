// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Aspire.Dashboard.Utils;
using Google.Protobuf.WellKnownTypes;

namespace Aspire.Dashboard.Model;

/// <summary>
/// Base class for immutable snapshots of resource state at a point in time.
/// </summary>
public abstract class ResourceViewModel
{
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public required string Uid { get; init; }
    public required string? State { get; init; }
    public required DateTime? CreationTimeStamp { get; init; }
    public required ImmutableArray<EnvironmentVariableViewModel> Environment { get; init; }
    public required ImmutableArray<EndpointViewModel> Endpoints { get; init; }
    public required ImmutableArray<ResourceServiceSnapshot> Services { get; init; }
    public required int? ExpectedEndpointsCount { get; init; }

    public abstract string ResourceType { get; }

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

    internal virtual bool MatchesFilter(string filter)
    {
        return Name.Contains(filter, StringComparisons.UserTextSearch);
    }

    protected abstract IEnumerable<(string Key, Value Value)> GetCustomData();

    public IEnumerable<(string Key, Value Value)> Data
    {
        get
        {
            yield return (ResourceDataKeys.Resource.Uid, Value.ForString(Uid));
            yield return (ResourceDataKeys.Resource.Name, Value.ForString(Name));
            yield return (ResourceDataKeys.Resource.Type, Value.ForString(ResourceType));
            yield return (ResourceDataKeys.Resource.DisplayName, Value.ForString(DisplayName));
            yield return (ResourceDataKeys.Resource.State, Value.ForString(State));
            yield return (ResourceDataKeys.Resource.CreateTime, CreationTimeStamp is null ? Value.ForNull() : Value.ForString(CreationTimeStamp.Value.ToString("O")));

            foreach (var pair in GetCustomData())
            {
                yield return pair;
            }
        }
    }
}

public sealed class ResourceServiceSnapshot(string name, string? allocatedAddress, int? allocatedPort)
{
    public string Name { get; } = name;
    public string? AllocatedAddress { get; } = allocatedAddress;
    public int? AllocatedPort { get; } = allocatedPort;
    public string AddressAndPort { get; } = $"{allocatedAddress}:{allocatedPort}";
}

public sealed record EndpointViewModel(string EndpointUrl, string ProxyUrl);

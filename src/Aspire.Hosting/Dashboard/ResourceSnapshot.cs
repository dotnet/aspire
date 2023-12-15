// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Aspire.Dashboard.Model;
using Google.Protobuf.WellKnownTypes;

namespace Aspire.Hosting.Dashboard;

public abstract class ResourceSnapshot
{
    public abstract string ResourceType { get; }

    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public required string Uid { get; init; }
    public required string? State { get; init; }
    public required DateTime? CreationTimeStamp { get; init; }
    public required ImmutableArray<EnvironmentVariableSnapshot> Environment { get; init; }
    public required ImmutableArray<EndpointSnapshot> Endpoints { get; init; }
    public required ImmutableArray<ResourceServiceSnapshot> Services { get; init; }
    public required int? ExpectedEndpointsCount { get; init; }

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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Globalization;
using Aspire.Dashboard.Model;
using Google.Protobuf.WellKnownTypes;

namespace Aspire.Hosting.Dashboard;

internal abstract class ResourceSnapshot
{
    public abstract string ResourceType { get; }

    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public required string Uid { get; init; }
    public required string? State { get; init; }
    public required int? ExitCode { get; init; }
    public required DateTime? CreationTimeStamp { get; init; }
    public required ImmutableArray<EnvironmentVariableSnapshot> Environment { get; init; }
    public required ImmutableArray<EndpointSnapshot> Endpoints { get; init; }
    public required ImmutableArray<ResourceServiceSnapshot> Services { get; init; }
    public required int? ExpectedEndpointsCount { get; init; }

    protected abstract IEnumerable<(string Key, Value Value)> GetProperties();

    public IEnumerable<(string Name, Value Value)> Properties
    {
        get
        {
            yield return (KnownProperties.Resource.Uid, Value.ForString(Uid));
            yield return (KnownProperties.Resource.Name, Value.ForString(Name));
            yield return (KnownProperties.Resource.Type, Value.ForString(ResourceType));
            yield return (KnownProperties.Resource.DisplayName, Value.ForString(DisplayName));
            yield return (KnownProperties.Resource.State, Value.ForString(State));
            yield return (KnownProperties.Resource.ExitCode, ExitCode is null ? Value.ForNull() : Value.ForString(ExitCode.Value.ToString("N", CultureInfo.InvariantCulture)));
            yield return (KnownProperties.Resource.CreateTime, CreationTimeStamp is null ? Value.ForNull() : Value.ForString(CreationTimeStamp.Value.ToString("O")));

            foreach (var pair in GetProperties())
            {
                yield return pair;
            }
        }
    }
}

internal sealed class EnvironmentVariableSnapshot(string name, string? value, bool isFromSpec)
{
    public string Name { get; } = name;
    public string? Value { get; } = value;
    public bool IsFromSpec { get; } = isFromSpec;
}

internal sealed record EndpointSnapshot(string endpointUrl, string proxyUrl)
{
    public string EndpointUrl { get; } = endpointUrl;
    public string ProxyUrl { get; } = proxyUrl;
}

internal sealed class ResourceServiceSnapshot(string name, string? allocatedAddress, int? allocatedPort)
{
    public string Name { get; } = name;
    public string? AllocatedAddress { get; } = allocatedAddress;
    public int? AllocatedPort { get; } = allocatedPort;
}

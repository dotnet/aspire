// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Globalization;
using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.ResourceService.Proto.V1;
using Google.Protobuf.WellKnownTypes;

namespace Aspire.Hosting.Dashboard;

internal abstract class ResourceSnapshot
{
    public abstract string ResourceType { get; }

    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public required string Uid { get; init; }
    public required string? State { get; init; }
    public required string? StateStyle { get; init; }
    public required int? ExitCode { get; init; }
    public required DateTime? CreationTimeStamp { get; init; }
    public required DateTime? StartTimeStamp { get; init; }
    public required DateTime? StopTimeStamp { get; init; }
    public required ImmutableArray<EnvironmentVariableSnapshot> Environment { get; init; }
    public required ImmutableArray<VolumeSnapshot> Volumes { get; init; }
    public required ImmutableArray<UrlSnapshot> Urls { get; init; }
    public required HealthStateKind? HealthState { get; set; }
    public required ImmutableArray<ResourceCommandSnapshot> Commands { get; init; }

    protected abstract IEnumerable<(string Key, Value Value, bool IsSensitive)> GetProperties();

    public IEnumerable<(string Name, Value Value, bool IsSensitive)> Properties
    {
        get
        {
            yield return (KnownProperties.Resource.Uid, Value.ForString(Uid), IsSensitive: false);
            yield return (KnownProperties.Resource.Name, Value.ForString(Name), IsSensitive: false);
            yield return (KnownProperties.Resource.Type, Value.ForString(ResourceType), IsSensitive: false);
            yield return (KnownProperties.Resource.DisplayName, Value.ForString(DisplayName), IsSensitive: false);
            yield return (KnownProperties.Resource.State, State is null ? Value.ForNull() : Value.ForString(State), IsSensitive: false);
            yield return (KnownProperties.Resource.ExitCode, ExitCode is null ? Value.ForNull() : Value.ForString(ExitCode.Value.ToString("D", CultureInfo.InvariantCulture)), IsSensitive: false);
            yield return (KnownProperties.Resource.CreateTime, CreationTimeStamp is null ? Value.ForNull() : Value.ForString(CreationTimeStamp.Value.ToString("O")), IsSensitive: false);
            yield return (KnownProperties.Resource.StartTime, StartTimeStamp is null ? Value.ForNull() : Value.ForString(StartTimeStamp.Value.ToString("O")), IsSensitive: false);
            yield return (KnownProperties.Resource.StopTime, StopTimeStamp is null ? Value.ForNull() : Value.ForString(StopTimeStamp.Value.ToString("O")), IsSensitive: false);
            yield return (KnownProperties.Resource.HealthState, HealthState is null ? Value.ForNull() : Value.ForString(HealthState.ToString()), IsSensitive: false);

            foreach (var property in GetProperties())
            {
                yield return property;
            }
        }
    }
}

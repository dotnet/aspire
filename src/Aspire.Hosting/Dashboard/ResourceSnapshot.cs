// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Globalization;
using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;
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

    public required ImmutableArray<UrlSnapshot> Urls { get; init; }

    protected abstract IEnumerable<(string Key, Value Value)> GetProperties();

    public IEnumerable<(string Name, Value Value)> Properties
    {
        get
        {
            yield return (KnownProperties.Resource.Uid, Value.ForString(Uid));
            yield return (KnownProperties.Resource.Name, Value.ForString(Name));
            yield return (KnownProperties.Resource.Type, Value.ForString(ResourceType));
            yield return (KnownProperties.Resource.DisplayName, Value.ForString(DisplayName));
            yield return (KnownProperties.Resource.State, State is null ? Value.ForNull() : Value.ForString(State));
            yield return (KnownProperties.Resource.ExitCode, ExitCode is null ? Value.ForNull() : Value.ForString(ExitCode.Value.ToString("D", CultureInfo.InvariantCulture)));
            yield return (KnownProperties.Resource.CreateTime, CreationTimeStamp is null ? Value.ForNull() : Value.ForString(CreationTimeStamp.Value.ToString("O")));

            foreach (var pair in GetProperties())
            {
                yield return pair;
            }
        }
    }
}

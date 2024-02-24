// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Google.Protobuf.WellKnownTypes;

namespace Aspire.Hosting.Dashboard;

internal class GenericResourceSnapshot(DashboardResourceState state) : ResourceSnapshot
{
    // Default to the resource type name without the "Resource" suffix.
    public override string ResourceType => state.ResourceType;

    protected override IEnumerable<(string Key, Value Value)> GetProperties()
    {
        foreach (var (key, value) in state.Properties)
        {
            yield return (key, Value.ForString(value));
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Google.Protobuf.WellKnownTypes;

namespace Aspire.Hosting.Dashboard;

internal sealed class GenericResourceSnapshot(CustomResourceSnapshot state) : ResourceSnapshot
{
    public override string ResourceType => state.ResourceType;

    protected override IEnumerable<(string Key, Value Value, bool IsSensitive)> GetProperties()
    {
        foreach (var (key, value, isSensitive) in state.Properties)
        {
            var result = ConvertToValue(value);

            yield return (key, result, isSensitive);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Google.Protobuf.WellKnownTypes;

namespace Aspire.Hosting.Dashboard;

internal class GenericResourceSnapshot(CustomResourceSnapshot state) : ResourceSnapshot
{
    public override string ResourceType => state.ResourceType;

    protected override IEnumerable<(string Key, Value Value)> GetProperties()
    {
        foreach (var (key, value) in state.Properties)
        {
            var result = value switch
            {
                string s => Value.ForString(s),
                int i => Value.ForNumber(i),
                IEnumerable<string> list => Value.ForList(list.Select(Value.ForString).ToArray()),
                IEnumerable<int> list => Value.ForList(list.Select(i => Value.ForNumber(i)).ToArray()),
                null => Value.ForNull(),
                _ => Value.ForString(value.ToString())
            };

            yield return (key, result);
        }
    }
}

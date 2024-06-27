// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Otlp.Storage;

public readonly record struct ApplicationKey(string Name, string InstanceId)
{
    public bool EqualsCompositeName(string name)
    {
        if (name == null)
        {
            return false;
        }

        // Composite name has the format "{Name}-{InstanceId}".
        if (name.Length != Name.Length + InstanceId.Length + 1)
        {
            return false;
        }

        if (!name.AsSpan(0, Name.Length).Equals(Name, StringComparisons.ResourceName))
        {
            return false;
        }
        if (name[Name.Length] != '-')
        {
            return false;
        }
        if (!name.AsSpan(Name.Length + 1, InstanceId.Length).Equals(InstanceId, StringComparisons.ResourceName))
        {
            return false;
        }

        return true;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Otlp.Storage;

public readonly record struct ApplicationKey(string Name, string? InstanceId) : IComparable<ApplicationKey>
{
    public static ApplicationKey Create(string name, string instanceId)
    {
        // Check if instanceId combines name, e.g.
        // name = api
        // instanceId = api-123
        // In this situation, subtract name from the instanceId.
        if (instanceId.Length >= name.Length + 2 &&
            instanceId.StartsWith(name, StringComparisons.ResourceName) &&
            instanceId[name.Length] == '-')
        {
            return new ApplicationKey(name, instanceId.Substring(name.Length + 1));
        }

        // Fall back to splitting based on a dash delimiter.
        // This could fail because there could be a dash in either the name or the instance id.
        // At this point we're doing our best guess. It's better than throwing an error.
        var separator = instanceId.LastIndexOf('-');
        if (separator == -1)
        {
            return new ApplicationKey(Name: instanceId, InstanceId: null);
        }

        return new ApplicationKey(Name: instanceId.Substring(0, separator), InstanceId: instanceId.Substring(separator + 1));
    }

    public int CompareTo(ApplicationKey other)
    {
        var c = string.Compare(Name, other.Name, StringComparisons.ResourceName);
        if (c != 0)
        {
            return c;
        }

        return string.Compare(InstanceId, other.InstanceId, StringComparisons.ResourceName);
    }

    public bool EqualsCompositeName(string name)
    {
        if (name == null)
        {
            return false;
        }

        if (InstanceId != null)
        {
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
        }
        else
        {
            // InstanceId is null so just match on name.
            // This is used to match all instances of an app with the matching name.
            return string.Equals(Name, name, StringComparisons.ResourceName);
        }

        return true;
    }

    public override string ToString()
    {
        if (InstanceId == null)
        {
            return Name;
        }

        return $"{Name}-{InstanceId}";
    }
}

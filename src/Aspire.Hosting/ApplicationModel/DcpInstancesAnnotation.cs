// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Keep track of instances from DCP.
/// </summary>
internal class DcpInstancesAnnotation : IResourceAnnotation
{
    public ImmutableArray<DcpInstance> Instances { get; }

    public DcpInstancesAnnotation(ImmutableArray<DcpInstance> instances)
    {
        Instances = instances;
    }

    public DcpInstancesAnnotation WithDifferentResourceName(string resourceName)
    {
        var updatedInstances = Instances.Select(instance => new DcpInstance(resourceName + "-" + instance.Suffix, instance.Suffix, instance.Index)).ToImmutableArray();
        return new DcpInstancesAnnotation(updatedInstances);
    }
}

internal sealed record DcpInstance(string Name, string Suffix, int Index);

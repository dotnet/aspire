// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;

namespace Aspire.Dashboard.Model;

[DebuggerDisplay("Type = {Type}, InstanceId = {InstanceId}, ReplicaSetName = {ReplicaSetName}")]
public class ResourceTypeDetails : IEquatable<ResourceTypeDetails>
{
    private ResourceTypeDetails(OtlpApplicationType type, string? instanceId, string? replicaSetName)
    {
        Type = type;
        InstanceId = instanceId;
        ReplicaSetName = replicaSetName;
    }

    public OtlpApplicationType Type { get; }
    public string? InstanceId { get; }
    public string? ReplicaSetName { get; }

    public ApplicationKey GetApplicationKey()
    {
        if (ReplicaSetName == null)
        {
            throw new InvalidOperationException($"Can't get ApplicationKey from resource type details '{ToString()}' because {nameof(ReplicaSetName)} is null.");
        }
        if (InstanceId != null)
        {
            return ApplicationKey.Create(InstanceId);
        }

        return new ApplicationKey(ReplicaSetName, InstanceId);
    }

    public static ResourceTypeDetails CreateApplicationGrouping(string groupingName, bool isReplicaSet)
    {
        return new ResourceTypeDetails(OtlpApplicationType.ResourceGrouping, instanceId: null, replicaSetName: isReplicaSet ? groupingName : null);
    }

    public static ResourceTypeDetails CreateSingleton(string instanceId, string replicaSetName)
    {
        return new ResourceTypeDetails(OtlpApplicationType.Singleton, instanceId, replicaSetName: replicaSetName);
    }

    public static ResourceTypeDetails CreateReplicaInstance(string instanceId, string replicaSetName)
    {
        return new ResourceTypeDetails(OtlpApplicationType.Instance, instanceId, replicaSetName);
    }

    public override string ToString()
    {
        return $"Type = {Type}, InstanceId = {InstanceId}, ReplicaSetName = {ReplicaSetName}";
    }

    public bool Equals(ResourceTypeDetails? other)
    {
        if (other == null)
        {
            return false;
        }

        if (Type != other.Type || InstanceId != other.InstanceId || ReplicaSetName != other.ReplicaSetName)
        {
            return false;
        }

        return true;
    }
}

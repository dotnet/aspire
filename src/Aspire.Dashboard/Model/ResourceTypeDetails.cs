// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Dashboard.Otlp.Model;

namespace Aspire.Dashboard.Model;

[DebuggerDisplay("Type = {Type}, InstanceId = {InstanceId}, ReplicaSetName = {ReplicaSetName}")]
public class ResourceTypeDetails
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

    public static ResourceTypeDetails CreateReplicaSet(string replicaSetName)
    {
        return new ResourceTypeDetails(OtlpApplicationType.ReplicaSet, instanceId: null, replicaSetName);
    }

    public static ResourceTypeDetails CreateSingleton(string instanceId)
    {
        return new ResourceTypeDetails(OtlpApplicationType.Singleton, instanceId, replicaSetName: null);
    }

    public static ResourceTypeDetails CreateReplicaInstance(string instanceId, string replicaSetName)
    {
        return new ResourceTypeDetails(OtlpApplicationType.ReplicaInstance, instanceId, replicaSetName);
    }
}

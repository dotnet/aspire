// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Model;

namespace Aspire.Dashboard.Model;

public class ResourceTypeDetails
{
    public ResourceTypeDetails(OtlpApplicationType? type, string? instanceId)
    {
        // Double check that the replica's have the right type.
        // TODO: This feels pretty hacky. Consider refactoring.
        if (type == OtlpApplicationType.Replica && !typeof(ResourceTypeDetails).IsAssignableFrom(GetType()))
        {
            throw new InvalidOperationException("Create a ReplicaTypeDetails instance for replica types");
        }

        Type = type;
        InstanceId = instanceId;
    }

    public OtlpApplicationType? Type { get; }
    public string? InstanceId { get; }
}

public class ReplicaTypeDetails : ResourceTypeDetails
{
    public ReplicaTypeDetails(OtlpApplicationType? type, string? instanceId, string replicaSetName)
        : base(type, instanceId)
    {
        ReplicaSetName = replicaSetName;
    }

    public string ReplicaSetName { get; }
}


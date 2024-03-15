// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Dcp.Model;

using System.Text.Json.Serialization;
using k8s.Models;

internal sealed class ExecutableTemplate : IAnnotationHolder
{
    // Labels to apply to child Executable objects
    [JsonPropertyName("labels")]
    public IDictionary<string, string>? Labels { get; set; }

    // Annotations to apply to child Executable objects
    [JsonPropertyName("annotations")]
    public IDictionary<string, string>? Annotations { get; set; }

    // Spec for the child Executable
    [JsonPropertyName("spec")]
    public ExecutableSpec Spec { get; set; } = new ExecutableSpec();

    public void Annotate(string annotationName, string value)
    {
        if (Annotations is null)
        {
            Annotations = new Dictionary<string, string>();
        }

        Annotations[annotationName] = value;
    }

    public void AnnotateAsObjectList<TValue>(string annotationName, TValue value)
    {
        if (Annotations is null)
        {
            Annotations = new Dictionary<string, string>();
        }

        CustomResource.AnnotateAsObjectList(Annotations, annotationName, value);
    }
}

internal sealed class ExecutableReplicaSetSpec
{
    // Number of desired child Executable objects
    [JsonPropertyName("replicas")]
    public int Replicas { get; set; } = 1;

    // Template describing the configuration of child Executable objects created by the replica set
    [JsonPropertyName("template")]
    public ExecutableTemplate Template { get; set; } = new ExecutableTemplate();
}

internal sealed class ExecutableReplicaSetStatus : V1Status
{
    // Total number of observed child executables
    [JsonPropertyName("observedReplicas")]
    public int? ObservedReplicas { get; set; }

    // Total number of current running child Executables
    [JsonPropertyName("runningReplicas")]
    public int? RunningReplicas { get; set; }

    // Total number of current Executable replicas that failed to start
    [JsonPropertyName("failedReplicas")]
    public int? FailedReplicas { get; set; }

    // Total number of current child Executables that have finished running
    [JsonPropertyName("finishedReplicas")]
    public int? FinishedReplicas { get; set; }

    // Last time the replica set was scaled up or down by the controller
    [JsonPropertyName("lastScaleTime")]
    public DateTimeOffset? LastScaleTime { get; set; }
}

internal sealed class ExecutableReplicaSet : CustomResource<ExecutableReplicaSetSpec, ExecutableReplicaSetStatus>
{
    [JsonConstructor]
    public ExecutableReplicaSet(ExecutableReplicaSetSpec spec) : base(spec) { }

    public static ExecutableReplicaSet Create(string name, int replicas, string executablePath)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(replicas, nameof(replicas));

        var ers = new ExecutableReplicaSet(new ExecutableReplicaSetSpec
        {
            Replicas = replicas
        });
        ers.Kind = Dcp.ExecutableReplicaSetKind;
        ers.ApiVersion = Dcp.GroupVersion.ToString();
        ers.Metadata.Name = name;
        ers.Metadata.NamespaceProperty = string.Empty;
        ers.Spec.Template.Spec.ExecutablePath = executablePath;

        return ers;
    }
}

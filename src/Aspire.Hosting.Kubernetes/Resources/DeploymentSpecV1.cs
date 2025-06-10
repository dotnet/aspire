// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the specification of a Kubernetes Deployment resource.
/// </summary>
[YamlSerializable]
public sealed class DeploymentSpecV1
{
    /// <summary>
    /// Indicates whether the deployment is paused.
    /// When set to true, the deployment will not trigger any new rollouts or updates to the replicas until the value is set back to false.
    /// </summary>
    [YamlMember(Alias = "paused")]
    public bool? Paused { get; set; }

    /// <summary>
    /// Gets or sets the template for the deployment, which defines the desired state of the Pod,
    /// including metadata and specifications. This property allows for configuring the properties
    /// that Pods created from this template will inherit.
    /// </summary>
    [YamlMember(Alias = "template")]
    public PodTemplateSpecV1 Template { get; set; } = new();

    /// <summary>
    /// Gets or sets the label selector for the deployment.
    /// The label selector defines how to identify the set of resources (such as pods)
    /// to which the deployment applies. It is used to match the desired pods against
    /// a label-based query.
    /// </summary>
    [YamlMember(Alias = "selector")]
    public LabelSelectorV1 Selector { get; set; } = new();

    /// <summary>
    /// Specifies the minimum number of seconds for which a newly created pod should be ready
    /// without any of its containers crashing, for it to be considered available. This setting
    /// can affect the deployment's overall readiness status, ensuring a buffer period before
    /// marking pods as ready for serving traffic. Defaults to 0, indicating immediate readiness
    /// checking after the pod enters the Ready state.
    /// </summary>
    [YamlMember(Alias = "minReadySeconds")]
    public int? MinReadySeconds { get; set; }

    /// <summary>
    /// Specifies the maximum duration, in seconds, that a deployment process should run
    /// without progressing before it is considered failed. If the deployment does not
    /// make progress during this time, it will be marked as failed. This helps to enforce
    /// deployment stability and avoid long-running deployments that do not complete.
    /// </summary>
    [YamlMember(Alias = "progressDeadlineSeconds")]
    public int? ProgressDeadlineSeconds { get; set; }

    /// <summary>
    /// Gets or sets the desired number of pod replicas for this deployment.
    /// If null, the default value defined by the server will be used.
    /// </summary>
    [YamlMember(Alias = "replicas")]
    public int? Replicas { get; set; } = 1;

    /// <summary>
    /// Specifies the number of old ReplicaSets to retain for a Deployment.
    /// When set, this field controls the maximum number of revisions kept in the history
    /// to allow rollback. If not specified, a default value will be used by the system.
    /// </summary>
    [YamlMember(Alias = "revisionHistoryLimit")]
    public int? RevisionHistoryLimit { get; set; } = 3;

    /// <summary>
    /// Gets or sets the deployment strategy which defines how to replace existing pods with new ones.
    /// </summary>
    [YamlMember(Alias = "strategy")]
    public DeploymentStrategyV1 Strategy { get; set; } = new();
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Docker.Resources.ServiceNodes.Swarm;

/// <summary>
/// Represents the deployment configuration for a Docker service. This class is used to define various aspects such as replication, mode, resource constraints, updates, and restart policies.
/// </summary>
[YamlSerializable]
public sealed class Deploy
{
    /// <summary>
    /// Represents the number of task replicas for a service node deployment.
    /// The replicas define the desired count of independently running instances
    /// of the service within the deployment.
    /// </summary>
    [YamlMember(Alias = "replicas")]
    public int? Replicas { get; set; }

    /// <summary>
    /// Gets or sets the deployment mode for the service.
    /// Specifies how tasks are scheduled on nodes.
    /// Common values include "replicated" for distributing tasks across nodes
    /// or "global" for running a task on every node in the cluster.
    /// </summary>
    [YamlMember(Alias = "mode")]
    public string? Mode { get; set; }

    /// <summary>
    /// Represents the resource configurations for a deployable service within Docker.
    /// </summary>
    /// <remarks>
    /// The <c>Resources</c> class is used to define resources such as CPU and memory
    /// that are allocated to or reserved for a service. It includes configurations for
    /// both resource limits and reservations.
    /// </remarks>
    /// <seealso cref="ResourceSpec"/>
    [YamlMember(Alias = "resources")]
    public Resources? Resources { get; set; }

    /// <summary>
    /// Specifies the placement constraints and preferences for service deployment.
    /// </summary>
    /// <remarks>
    /// The <c>Placement</c> property defines the rules for how the service should be placed
    /// on nodes within a Docker Swarm. This includes constraints that must be satisfied
    /// and preferences to guide the scheduler.
    /// </remarks>
    [YamlMember(Alias = "placement")]
    public Placement? Placement { get; set; }

    /// <summary>
    /// Represents the update configuration used during service deployments.
    /// </summary>
    /// <remarks>
    /// The <c>UpdateConfig</c> property defines the parameters associated with updating services.
    /// It allows configuration of update behavior such as parallelism, delays, failure actions, and monitoring.
    /// </remarks>
    /// <seealso cref="Swarm.UpdateConfig"/>
    [YamlMember(Alias = "update_config")]
    public UpdateConfig? UpdateConfig { get; set; }

    /// <summary>
    /// Specifies the restart policy for a Docker service.
    /// </summary>
    /// <remarks>
    /// The RestartPolicy defines conditions under which the service containers
    /// will be restarted, as well as parameters like delay between restarts,
    /// maximum restart attempts, and a time window for evaluating restart conditions.
    /// </remarks>
    /// <seealso cref="Swarm.RestartPolicy.Condition"/>
    /// <seealso cref="Swarm.RestartPolicy.Delay"/>
    /// <seealso cref="Swarm.RestartPolicy.MaxAttempts"/>
    /// <seealso cref="Swarm.RestartPolicy.Window"/>
    [YamlMember(Alias = "restart_policy")]
    public RestartPolicy? RestartPolicy { get; set; }

    /// <summary>
    /// Represents the label configurations for a deployable service in Docker.
    /// </summary>
    /// <remarks>
    /// This property is used to define additional metadata, typically in the form of key-value pairs,
    /// that can be attached to services for organizational or descriptive purposes.
    /// </remarks>
    [YamlMember(Alias = "labels")]
    public LabelSpecs? Labels { get; set; }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Docker.Resources.ServiceNodes.Swarm;

/// <summary>
/// Defines the restart policy for a Docker service in a Swarm cluster.
/// </summary>
/// <remarks>
/// This class specifies the conditions, delay, maximum attempts, and time window
/// involved in restarting a Docker service container within a Swarm deployment.
/// </remarks>
[YamlSerializable]
public sealed class RestartPolicy
{
    /// <summary>
    /// Specifies the condition under which a service's container will be restarted.
    /// </summary>
    /// <remarks>
    /// This property determines the criteria for restarting a container. Possible values may include "none", "on-failure", or "any", which dictate when a restart should happen (e.g., never, only on failures, or always).
    /// </remarks>
    [YamlMember(Alias = "condition")]
    public string? Condition { get; set; }

    /// <summary>
    /// Specifies the delay duration between restart attempts of the service container.
    /// </summary>
    /// <remarks>
    /// The delay defines the time interval to wait before attempting to restart a service container
    /// after a failure. It is expressed as a time duration string (e.g., "5s" for 5 seconds).
    /// </remarks>
    [YamlMember(Alias = "delay")]
    public string? Delay { get; set; }

    /// <summary>
    /// Specifies the maximum number of restart attempts allowed for a container as part of the restart policy.
    /// </summary>
    /// <remarks>
    /// Defines the upper limit for how many times Docker will attempt to restart the container
    /// before giving up. If not set, the system default behavior or an unlimited restart attempts policy
    /// may be applied. This setting is useful for handling scenarios where a service is repeatedly failing
    /// and prevents infinite restart loops.
    /// </remarks>
    [YamlMember(Alias = "max_attempts")]
    public int? MaxAttempts { get; set; }

    /// <summary>
    /// Defines the time window for evaluating the restart conditions in a Docker service restart policy.
    /// </summary>
    /// <remarks>
    /// This property specifies the duration (e.g., in seconds or any supported time format) during which
    /// restart attempts are counted towards the maximum attempts allowed. If the service exits and restarts
    /// within this specified window, it contributes to the count of restart attempts under the restart policy.
    /// </remarks>
    [YamlMember(Alias = "window")]
    public string? Window { get; set; }
}

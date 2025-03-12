// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Aspire.Hosting.Docker.Resources.ServiceNodes;

/// <summary>
/// Represents the health check configuration for a container.
/// </summary>
[YamlSerializable]
public sealed class Healthcheck
{
    /// <summary>
    /// Represents the command or set of commands to be executed as part of the health check for a service node.
    /// This property is defined as a list of strings, where each string is a command or argument
    /// contributing to the health check operation.
    /// </summary>
    [YamlMember(Alias = "test", ScalarStyle = ScalarStyle.Folded, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public List<string> Test { get; set; } = [];

    /// <summary>
    /// Specifies the duration between health check executions for a service.
    /// Accepts a string representation of the time interval in a supported format.
    /// Used to configure the frequency at which the health check is performed.
    /// </summary>
    [YamlMember(Alias = "interval")]
    public required string Interval { get; set; }

    /// <summary>
    /// Specifies the maximum duration to wait for a healthcheck to complete.
    /// </summary>
    [YamlMember(Alias = "timeout")]
    public required string Timeout { get; set; }

    /// <summary>
    /// Specifies the number of retries to be attempted for the health check before marking it as failed.
    /// </summary>
    /// <remarks>
    /// This property indicates how many times the system should retry the health check command
    /// if it fails before considering the health check unsuccessful.
    /// The value must be a non-negative integer or null if not specified.
    /// </remarks>
    [YamlMember(Alias = "retries")]
    public int? Retries { get; set; }

    /// <summary>
    /// Gets or sets the duration to wait after a container starts before attempting health checks.
    /// This property specifies the initial delay before the first health check is performed, which
    /// can be useful in cases where the application within the container requires some time to initialize
    /// before it can be properly checked for health status.
    /// </summary>
    [YamlMember(Alias = "start_period")]
    public required string StartPeriod { get; set; }
}

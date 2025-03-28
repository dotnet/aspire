// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a probe configuration for Kubernetes containers.
/// A probe is used to determine the health and readiness of a container
/// by defining checks that can consist of various actions such as HTTP requests,
/// executing commands, GRPC actions, or assessing TCP socket connectivity.
/// </summary>
[YamlSerializable]
public sealed class ProbeV1
{
    /// <summary>
    /// Gets or sets the execution action associated with a probe.
    /// </summary>
    /// <remarks>
    /// Represents a directive to execute a specific command within a container.
    /// This can be used to implement actions such as custom health checks or performing tasks
    /// during the lifecycle of a container. The execution is defined by an instance of
    /// <see cref="ExecActionV1"/>, which specifies the command to be run.
    /// </remarks>
    [YamlMember(Alias = "exec")]
    public ExecActionV1? Exec { get; set; }

    /// <summary>
    /// Represents a GRPC-based action within a Kubernetes probe configuration.
    /// </summary>
    /// <remarks>
    /// This property defines the GRPC action for health checks in Kubernetes resources. It specifies
    /// the target service name and port using a <see cref="GrpcActionV1"/> instance.
    /// </remarks>
    [YamlMember(Alias = "grpc")]
    public GrpcActionV1? Grpc { get; set; }

    /// <summary>
    /// Gets or sets the failure threshold for the probe.
    /// </summary>
    /// <remarks>
    /// The failure threshold specifies the number of consecutive probe failures
    /// that are allowed before the container is considered unhealthy. This value
    /// is used to determine when to take action based on the probe's outcome, such as
    /// restarting the container or marking it as failed.
    /// </remarks>
    [YamlMember(Alias = "failureThreshold")]
    public int? FailureThreshold { get; set; }

    /// <summary>
    /// Gets or sets the minimum consecutive successes for the probe to be considered successful after it has previously failed.
    /// </summary>
    /// <remarks>
    /// The value of this property is used to determine how many successive successful probes are required to declare
    /// a previously failed resource as healthy again. This is typically applied in health or readiness probes within Kubernetes.
    /// A higher value indicates a stricter threshold for recovery.
    /// </remarks>
    [YamlMember(Alias = "successThreshold")]
    public int? SuccessThreshold { get; set; }

    /// <summary>
    /// Specifies the duration in seconds to wait before initiating the probe for the first time.
    /// </summary>
    /// <remarks>
    /// This property is used to define a delay before the probe starts checking the status of a container.
    /// It is especially useful in scenarios where the application in the container requires time to initialize
    /// before it can respond to the probe successfully.
    /// </remarks>
    [YamlMember(Alias = "initialDelaySeconds")]
    public int? InitialDelaySeconds { get; set; }

    /// <summary>
    /// Gets or sets the period in seconds between probe executions.
    /// </summary>
    /// <remarks>
    /// The value represents the interval between successive executions of the specified probe,
    /// such as HTTP, TCP, or GRPC probing tasks. A lower value increases the frequency of probe checks,
    /// while a higher value reduces it. By default, this setting might be aligned with predefined
    /// Kubernetes timing configurations.
    /// </remarks>
    [YamlMember(Alias = "periodSeconds")]
    public int? PeriodSeconds { get; set; }

    /// <summary>
    /// Gets or sets the optional duration in seconds the system will wait for the pod to terminate gracefully
    /// after a probe triggers its termination. If the pod does not terminate within this time frame,
    /// it may be forcefully killed. A null value indicates that the termination grace period is not explicitly set.
    /// </summary>
    [YamlMember(Alias = "terminationGracePeriodSeconds")]
    public long? TerminationGracePeriodSeconds { get; set; }

    /// <summary>
    /// Specifies the number of seconds that a probe will wait for a response before timing out.
    /// </summary>
    /// <remarks>
    /// The <c>TimeoutSeconds</c> property defines the maximum duration, in seconds, that the probe
    /// will wait for an operation to complete before considering it a failure. A timeout value that
    /// is too low may lead to false negatives, whereas a value that is too high may delay failure detection.
    /// This property is optional and, if not specified, the default value will be used as defined by the system.
    /// </remarks>
    [YamlMember(Alias = "timeoutSeconds")]
    public int? TimeoutSeconds { get; set; }

    /// <summary>
    /// Represents a configuration for an HTTP GET request action as part of a Kubernetes probe mechanism.
    /// </summary>
    /// <remarks>
    /// This property specifies the behavior of an HTTP GET request when used for health checks, event handling,
    /// or other similar functionalities within Kubernetes resources. It typically includes the scheme, host,
    /// HTTP headers, path, and port necessary to construct the request. The configuration helps determine
    /// service or application availability by probing specified endpoints.
    /// </remarks>
    [YamlMember(Alias = "httpGet")]
    public HttpGetActionV1? HttpGet { get; set; }

    /// <summary>
    /// TcpSocket specifies an action based on a TCP socket connection.
    /// </summary>
    /// <remarks>
    /// Defines a probe action that performs a TCP connection to a specified host and port.
    /// It is commonly used in Kubernetes health checks to determine the availability of a service.
    /// </remarks>
    [YamlMember(Alias = "tcpSocket")]
    public TcpSocketActionV1? TcpSocket { get; set; }
}

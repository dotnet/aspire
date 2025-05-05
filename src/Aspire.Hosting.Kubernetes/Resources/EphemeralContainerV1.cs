// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents an ephemeral container specification in a Kubernetes Pod.
/// Ephemeral containers enable running short-lived operations or debugging within an already running Pod.
/// </summary>
[YamlSerializable]
public sealed class EphemeralContainerV1
{
    /// <summary>
    /// Gets the list of commands to be executed within the ephemeral container.
    /// This property represents the command-line instructions or entrypoint
    /// which will override the default entrypoint of the container's image.
    /// </summary>
    [YamlMember(Alias = "command")]
    public List<string> Command { get; } = [];

    /// <summary>
    /// Gets or sets the container image to be used by the ephemeral container.
    /// The Image property specifies the Docker container image, including the repository,
    /// image name, and optionally the tag or digest. This property is required
    /// to define which container image will be pulled and deployed.
    /// </summary>
    [YamlMember(Alias = "image")]
    public string Image { get; set; } = null!;

    /// <summary>
    /// Specifies the lifecycle settings for the ephemeral container.
    /// This property represents actions that the system should take
    /// in response to specific container lifecycle events, such as
    /// PostStart and PreStop handlers. These handlers allow you to
    /// define behavior that executes before or after certain
    /// lifecycle milestones, enhancing container manageability.
    /// </summary>
    [YamlMember(Alias = "lifecycle")]
    public LifecycleV1? Lifecycle { get; set; }

    /// <summary>
    /// Represents the liveness probe configuration for a container.
    /// The liveness probe is used by the Kubernetes system to determine if the container is still running.
    /// If the liveness probe fails, the container will be restarted.
    /// This property defines the specifics of how the liveness check is performed, such as command execution, GRPC actions, or configuration thresholds.
    /// </summary>
    [YamlMember(Alias = "livenessProbe")]
    public ProbeV1? LivenessProbe { get; set; }

    /// <summary>
    /// Gets or sets the name of the ephemeral container.
    /// This is a required property that uniquely identifies the container
    /// within the pod specification.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Represents the readiness probe configuration for the ephemeral container.
    /// A readiness probe is used to determine if the container is ready to accept traffic.
    /// It can be configured using different actions such as executing a command, sending HTTP requests,
    /// or other custom logic. This helps in controlling whether a container should be added to the
    /// load balancer based on its current state.
    /// </summary>
    [YamlMember(Alias = "readinessProbe")]
    public ProbeV1? ReadinessProbe { get; set; }

    /// <summary>
    /// Specifies the startup probe configuration for the container.
    /// A startup probe is used to determine whether the application within the container
    /// has started successfully. It is particularly useful for applications that take a
    /// long time to start. If the startup probe fails, the container is killed and subject
    /// to the pod's restart policy. The probe configuration can include parameters such
    /// as initial delay, timeout, period between checks, and thresholds for success or failure.
    /// </summary>
    [YamlMember(Alias = "startupProbe")]
    public ProbeV1? StartupProbe { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the container's standard input (stdin) will be closed after the first time it's opened.
    /// When set to true, stdin will be available only once, closing automatically after the first use. This can be used for containers
    /// that require user input during initialization or execution, but do not need stdin to remain open throughout the container's lifecycle.
    /// </summary>
    [YamlMember(Alias = "stdinOnce")]
    public bool? StdinOnce { get; set; }

    /// <summary>
    /// Gets or sets the name of the target container where the ephemeral container
    /// will be attached. This property specifies the existing container in the pod
    /// that the ephemeral container is intended to target, typically used for debugging
    /// or troubleshooting.
    /// </summary>
    [YamlMember(Alias = "targetContainerName")]
    public string? TargetContainerName { get; set; }

    /// <summary>
    /// Represents a list of environment variable sources for the container.
    /// Environment variables can be populated using ConfigMap or Secret objects.
    /// Each item in the list is an instance of <see cref="EnvFromSourceV1"/>,
    /// which specifies the source of the environment variables and an optional prefix to prepend to this container's environment variables.
    /// </summary>
    [YamlMember(Alias = "envFrom")]
    public List<EnvFromSourceV1> EnvFrom { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether standard input (stdin) should be opened.
    /// When enabled, the container can accept input via stdin.
    /// </summary>
    [YamlMember(Alias = "stdin")]
    public bool? Stdin { get; set; }

    /// <summary>
    /// Gets or sets the working directory for the container.
    /// This specifies the directory where the command will execute.
    /// If not set, the container runtime's default working directory will be used.
    /// </summary>
    [YamlMember(Alias = "workingDir")]
    public string? WorkingDir { get; set; }

    /// <summary>
    /// Represents the list of arguments to be passed to the container at runtime.
    /// This property is used to explicitly specify the arguments for the container's entrypoint command.
    /// If not specified, the container runtime will use the default arguments defined in the image.
    /// </summary>
    [YamlMember(Alias = "args")]
    public List<string> Args { get; } = [];

    /// <summary>
    /// Gets the list of ports associated with the ephemeral container.
    /// Each port is defined as an instance of the <see cref="ContainerPortV1"/> class, which specifies the configuration
    /// options such as the container port number, host port number, protocol, and associated host IP address.
    /// </summary>
    [YamlMember(Alias = "ports")]
    public List<ContainerPortV1> Ports { get; } = [];

    /// <summary>
    /// Represents the compute resource requirements for the ephemeral container.
    /// </summary>
    [YamlMember(Alias = "resources")]
    public ResourceRequirementsV1? Resources { get; set; }

    /// <summary>
    /// Represents a collection of volume devices mapped within the container.
    /// Each entry in this collection describes a specific mapping of a raw block
    /// device to a container using the <see cref="VolumeDeviceV1"/> class.
    /// </summary>
    [YamlMember(Alias = "volumeDevices")]
    public List<VolumeDeviceV1> VolumeDevices { get; } = [];

    /// <summary>
    /// Represents the list of volume mounts for the container.
    /// Each volume mount specifies how a volume should be mounted into the container's filesystem.
    /// </summary>
    [YamlMember(Alias = "volumeMounts")]
    public List<VolumeMountV1> VolumeMounts { get; } = [];

    /// <summary>
    /// Gets or sets the security context for the container, which defines the security settings
    /// and privileges applicable to the container instance. These settings include information
    /// such as user and group IDs, file system access levels, and privilege escalation permissions.
    /// The security context provides control over container's runtime environment by applying
    /// specific security policies.
    /// </summary>
    [YamlMember(Alias = "securityContext")]
    public SecurityContextV1? SecurityContext { get; set; }

    /// <summary>
    /// Env is a collection of environment variables defined for the container.
    /// It represents the list of key-value pairs that provide configuration or additional information
    /// to the container processes at runtime.
    /// </summary>
    /// <remarks>
    /// Env is a list of type EnvVarV1 that can include direct key-value pairs or dynamically
    /// derived values from other Kubernetes resources such as ConfigMaps, Secrets, or downward API fields.
    /// These environment variables are essential for passing environment-specific configurations into the container.
    /// </remarks>
    [YamlMember(Alias = "env")]
    public List<EnvVarV1> Env { get; } = [];

    /// <summary>
    /// Defines the policy for pulling container images.
    /// Possible values include:
    /// - "Always": Always pull the image even if it is present locally.
    /// - "IfNotPresent": Pull the image only if it is not present locally. This is the default value.
    /// - "Never": Never pull the image; use the image that is already present locally.
    /// This property determines how Kubernetes fetches container images when deploying a pod.
    /// </summary>
    [YamlMember(Alias = "imagePullPolicy")]
    public string ImagePullPolicy { get; set; } = "IfNotPresent";

    /// <summary>
    /// Gets the list of container resize policies applied to the ephemeral container.
    /// </summary>
    /// <remarks>
    /// The property represents a collection of <see cref="ContainerResizePolicyV1"/> objects
    /// that define specific resize policies, including the resource being managed and
    /// the restart behavior. These policies allow for detailed control over container
    /// resource adjustments within a Kubernetes environment.
    /// </remarks>
    [YamlMember(Alias = "resizePolicy")]
    public List<ContainerResizePolicyV1> ResizePolicy { get; } = [];

    /// <summary>
    /// Gets or sets the restart policy for the ephemeral container.
    /// Specifies the behavior for restarting the container in case of failure or termination.
    /// Common values include "Always", "OnFailure", or "Never".
    /// </summary>
    [YamlMember(Alias = "restartPolicy")]
    public string? RestartPolicy { get; set; }

    /// <summary>
    /// Specifies how the termination message of the container is populated.
    /// It can be set to control the source of termination messages, choosing
    /// between "File" or "FallbackToLogsOnError".
    /// - "File": The termination message is read from the file at the path
    /// specified by the `TerminationMessagePath` property.
    /// - "FallbackToLogsOnError": The termination message will default to
    /// container logs if the file at `TerminationMessagePath` is empty
    /// and the container exits with an error.
    /// Defaults to "File" if not explicitly set.
    /// </summary>
    [YamlMember(Alias = "terminationMessagePolicy")]
    public string? TerminationMessagePolicy { get; set; }

    /// <summary>
    /// Specifies the file path where the termination message for the container will be written.
    /// This message typically contains information about the container's termination state, such as
    /// the reason for termination or the exit code. If not specified, a default path is used.
    /// </summary>
    [YamlMember(Alias = "terminationMessagePath")]
    public string? TerminationMessagePath { get; set; }

    /// <summary>
    /// Specifies whether a terminal (TTY) should be allocated for the container.
    /// </summary>
    [YamlMember(Alias = "tty")]
    public bool? Tty { get; set; }
}

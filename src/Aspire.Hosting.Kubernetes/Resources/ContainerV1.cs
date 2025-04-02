// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a container configuration for a Kubernetes pod.
/// </summary>
/// <remarks>
/// This class defines the properties and behaviors of a container in a Kubernetes deployment.
/// It supports detailed customization, including environmental variables, volume mounts,
/// lifecycle management, probes, and security context settings.
/// </remarks>
[YamlSerializable]
public sealed class ContainerV1
{
    /// <summary>
    /// Gets the command to be executed by the container.
    /// Represents an optional list of strings that overrides the default
    /// entrypoint of the container's image. Each string in the list
    /// corresponds to a command line argument.
    /// </summary>
    [YamlMember(Alias = "command")]
    public List<string> Command { get; } = [];

    /// <summary>
    /// Gets or sets the Docker image name for the container.
    /// Represents the image to be used in the container, including the repository name, image name, and optional tag.
    /// </summary>
    [YamlMember(Alias = "image")]
    public string Image { get; set; } = null!;

    /// <summary>
    /// Gets or sets the lifecycle definition for the container, specifying actions to take in response to container lifecycle events, such as `PostStart` and `PreStop`.
    /// </summary>
    [YamlMember(Alias = "lifecycle")]
    public LifecycleV1? Lifecycle { get; set; }

    /// <summary>
    /// Represents the liveness probe configuration for a container.
    /// This probe is used to determine if the container is still running and healthy.
    /// If the liveness probe fails, the container is restarted.
    /// The probe can be configured to use different mechanisms, including:
    /// - Executing a specific command inside the container.
    /// - Performing an HTTP GET request to a specified endpoint.
    /// - Performing a gRPC health check request to a specified service.
    /// The probe supports additional parameters such as timeouts, failure thresholds,
    /// and retry intervals to ensure flexible and reliable health checks.
    /// </summary>
    [YamlMember(Alias = "livenessProbe")]
    public ProbeV1? LivenessProbe { get; set; }

    /// <summary>
    /// Gets or sets the name of the container.
    /// This property represents the unique name assigned to the container
    /// within the pod's definition in a Kubernetes deployment.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Defines the readiness probe configuration for a container.
    /// The readiness probe determines whether the container is ready to accept network traffic.
    /// A container passing its readiness probe is considered healthy and capable of serving network requests.
    /// This property accepts an instance of <see cref="ProbeV1"/>, enabling you to configure the
    /// specific checks, such as HTTP GET, command execution, or GRPC, to assess the readiness state.
    /// </summary>
    [YamlMember(Alias = "readinessProbe")]
    public ProbeV1? ReadinessProbe { get; set; }

    /// <summary>
    /// Gets or sets the startup probe configuration for the container.
    /// This probe is used to determine whether the application within the container
    /// has started successfully. The startup probe is executed to check the operational
    /// state of the application before the container is marked as ready, ensuring
    /// it can handle traffic before proceeding with normal operations.
    /// </summary>
    [YamlMember(Alias = "startupProbe")]
    public ProbeV1? StartupProbe { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the container's standard input (stdin) will be provided only once.
    /// When set to true, the container's stdin will remain open until the first read, after which it will be closed.
    /// Defaults to null, which indicates the property is not explicitly set.
    /// </summary>
    [YamlMember(Alias = "stdinOnce")]
    public bool? StdinOnce { get; set; }

    /// <summary>
    /// Represents a list of environment variable sources for the container.
    /// Each item allows referencing a ConfigMap or Secret to populate environment
    /// variables within the container. Environment variables defined from these
    /// sources can include an optional prefix.
    /// </summary>
    [YamlMember(Alias = "envFrom")]
    public List<EnvFromSourceV1> EnvFrom { get; } = [];

    /// <summary>
    /// Gets or sets a value that determines whether the container's standard input (stdin) stream is kept open.
    /// This allows the container to receive input through the stdin stream after the application inside the container starts running.
    /// </summary>
    [YamlMember(Alias = "stdin")]
    public bool? Stdin { get; set; }

    /// <summary>
    /// Gets or sets the working directory for the container.
    /// </summary>
    /// <remarks>
    /// This property specifies the working directory within the container's filesystem.
    /// If not set, the default working directory as defined by the container's image will be used.
    /// </remarks>
    [YamlMember(Alias = "workingDir")]
    public string? WorkingDir { get; set; }

    /// <summary>
    /// Represents the arguments to pass to the container's entrypoint.
    /// These arguments are added after the container's `command` property.
    /// If `command` is specified, these arguments are passed exactly as provided;
    /// otherwise, they are appended to the container's default entrypoint command.
    /// </summary>
    [YamlMember(Alias = "args")]
    public List<string> Args { get; } = [];

    /// <summary>
    /// Represents a collection of ports exposed by the container and associated configurations.
    /// </summary>
    /// <remarks>
    /// Each port in the collection is defined as an instance of <see cref="ContainerPortV1"/> which contains
    /// details such as the container port number, protocol, host port number, host IP, and an optional name.
    /// </remarks>
    [YamlMember(Alias = "ports")]
    public List<ContainerPortV1> Ports { get; } = [];

    /// <summary>
    /// Represents the resource requirements for a container, including memory and CPU limits and requests.
    /// The Resources property allows specifying the compute resources needed for container execution.
    /// </summary>
    [YamlMember(Alias = "resources")]
    public ResourceRequirementsV1? Resources { get; set; }

    /// <summary>
    /// Represents a collection of VolumeDeviceV1 objects that describe mappings of raw block devices within a container.
    /// </summary>
    [YamlMember(Alias = "volumeDevices")]
    public List<VolumeDeviceV1> VolumeDevices { get; } = [];

    /// <summary>
    /// Represents a collection of volume mounts for the container.
    /// Each volume mount specifies how a volume will be mounted and accessed within the container.
    /// </summary>
    [YamlMember(Alias = "volumeMounts")]
    public List<VolumeMountV1> VolumeMounts { get; } = [];

    /// <summary>
    /// Gets or sets the security context for a container.
    /// </summary>
    /// <remarks>
    /// Represents the security options applied to the container, including capabilities, user/group configurations,
    /// and root filesystem attributes. This property is used to configure and customize security-related settings,
    /// such as user privileges, filesystem permissions, and security profiles (AppArmor, Seccomp).
    /// </remarks>
    [YamlMember(Alias = "securityContext")]
    public SecurityContextV1? SecurityContext { get; set; }

    /// <summary>
    /// Represents a list of environment variables defined for the container.
    /// These environment variables can be used within the container during runtime.
    /// </summary>
    [YamlMember(Alias = "env")]
    public List<EnvVarV1> Env { get; } = [];

    /// <summary>
    /// Specifies the image pull policy for the container.
    /// Determines when the container's image should be pulled from the repository.
    /// Common values are "Always", "IfNotPresent", and "Never".
    /// The default value is "IfNotPresent", which means the image will only be pulled
    /// if it is not present locally.
    /// </summary>
    [YamlMember(Alias = "imagePullPolicy")]
    public string ImagePullPolicy { get; set; } = "IfNotPresent";

    /// <summary>
    /// Specifies the resize policies associated with a container in a Kubernetes environment.
    /// </summary>
    /// <remarks>
    /// This property holds a list of <see cref="ContainerResizePolicyV1"/> objects,
    /// each defining the resource-related resizing specifications and restart policies for the container.
    /// </remarks>
    [YamlMember(Alias = "resizePolicy")]
    public List<ContainerResizePolicyV1> ResizePolicy { get; } = [];

    /// <summary>
    /// Defines the restart policy for the container.
    /// This property indicates how the container should behave when it terminates.
    /// Possible values include "Always", "OnFailure", and "Never", which dictate if the container
    /// should be restarted unconditionally, only on failure, or not restarted at all, respectively.
    /// </summary>
    [YamlMember(Alias = "restartPolicy")]
    public string? RestartPolicy { get; set; }

    /// <summary>
    /// Specifies the path where the termination message of the container will be written.
    /// This property allows the container to provide additional information about its termination,
    /// such as error messages or exit codes. If configured, the message written to this path will
    /// be retrievable by the container manager or monitoring systems.
    /// </summary>
    [YamlMember(Alias = "terminationMessagePath")]
    public string? TerminationMessagePath { get; set; }

    /// <summary>
    /// Gets or sets the policy for capturing the termination message for the container.
    /// Defines how termination messages should be created and made available to the container.
    /// Possible values may include 'File' or 'FallbackToLogsOnError'.
    /// A 'File' policy captures the termination message from a specific file inside the container,
    /// while 'FallbackToLogsOnError' captures logs if the message file is inaccessible or unavailable.
    /// </summary>
    [YamlMember(Alias = "terminationMessagePolicy")]
    public string? TerminationMessagePolicy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a TTY (teletypewriter) is allocated for the container.
    /// When set to true, a TTY is allocated, enabling interactive command execution.
    /// It is typically used for containers that require a terminal-like interaction.
    /// </summary>
    [YamlMember(Alias = "tty")]
    public bool? Tty { get; set; }
}

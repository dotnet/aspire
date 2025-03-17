// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the specification of a Kubernetes Pod defined in version 1.
/// This class is used to configure various attributes of a pod, including containers, networking, scheduling, and security settings.
/// </summary>
[YamlSerializable]
public sealed class PodSpecV1
{
    /// <summary>
    /// Indicates whether the pod should use the host's IPC namespace.
    /// When set to true, the containers in the pod will share the IPC namespace with the host,
    /// allowing IPC resources (e.g., semaphores, message queues) to be accessible between the
    /// host and pod containers. This may be useful for certain applications that require
    /// inter-process communication with the host system. Use this property with caution as it
    /// exposes IPC resources on the host and might increase the risk of security vulnerabilities.
    /// </summary>
    [YamlMember(Alias = "hostIPC")]
    public bool? HostIpc { get; set; }

    /// <summary>
    /// Specifies whether the pod should use the host's process ID (PID) namespace.
    /// When set to true, processes in the pod will share the host's PID namespace,
    /// allowing visibility and interaction with processes on the host.
    /// This can be useful for certain debugging or monitoring scenarios, but may pose a security risk.
    /// Use with caution in multi-tenant clusters or environments.
    /// </summary>
    [YamlMember(Alias = "hostPID")]
    public bool? HostPid { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the hostname of the pod should be set as its fully qualified domain name (FQDN).
    /// If true, the FQDN will be used as the pod's hostname. If false or unset, the hostname will not be modified.
    /// </summary>
    [YamlMember(Alias = "setHostnameAsFQDN")]
    public bool? SetHostnameAsFqdn { get; set; }

    /// <summary>
    /// Represents the resource overhead associated with running a Pod.
    /// This property stores a dictionary where keys correspond to resource types
    /// (e.g., "cpu", "memory") and values represent the quantity of the respective resource.
    /// This overhead is taken into account when scheduling the Pod to ensure that sufficient
    /// resources are available on a node.
    /// </summary>
    [YamlMember(Alias = "overhead")]
    public Dictionary<string, string> Overhead { get; } = [];

    /// <summary>
    /// Gets or sets the hostname of the pod.
    /// This property specifies the hostname field of the Kubernetes PodSpec configuration.
    /// If set, the value will be used as the hostname of the pod, overriding the default hostname
    /// which is generally derived from the pod name.
    /// </summary>
    [YamlMember(Alias = "hostname")]
    public string? Hostname { get; set; } = null!;

    /// <summary>
    /// Specifies the name of the node on which the Pod should be scheduled.
    /// This is a binding decision that indicates the node where the Pod is intended to run.
    /// If not set, the scheduler automatically assigns a node for the Pod.
    /// </summary>
    [YamlMember(Alias = "nodeName")]
    public string? NodeName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the name of the PriorityClass associated with the pod.
    /// This property is used to assign a priority to the pod. The priority
    /// determines the scheduling order for the pod and its preemption
    /// behavior in cases of resource contention. The PriorityClass must
    /// be configured in the cluster beforehand.
    /// </summary>
    [YamlMember(Alias = "priorityClassName")]
    public string? PriorityClassName { get; set; } = null!;

    /// <summary>
    /// Specifies the RuntimeClass to use for running the pod.
    /// A RuntimeClass defines the container runtime configuration for the pods, such as enabling
    /// specific runtimes (e.g., gVisor, Kata Containers) or customizing runtime behaviors.
    /// This property is beneficial for tailoring runtime environments to meet specific
    /// workload requirements or enhance security. If not specified, the default RuntimeClass for
    /// the Kubernetes cluster will be used.
    /// </summary>
    [YamlMember(Alias = "runtimeClassName")]
    public string? RuntimeClassName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the name of the scheduler to be used for this pod.
    /// This property allows specifying a custom scheduler for the pod instead
    /// of relying on the default scheduler. It is optional and defaults to null
    /// if not specified.
    /// </summary>
    [YamlMember(Alias = "schedulerName")]
    public string? SchedulerName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the name of the Kubernetes Service Account that the pod should use.
    /// This account can provide access to specific Kubernetes resources or external services
    /// within the cluster. If left null or empty, the default Service Account in the pod's
    /// namespace will be used.
    /// </summary>
    [YamlMember(Alias = "serviceAccountName")]
    public string? ServiceAccountName { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether the pod should share a single process namespace
    /// with all containers within the pod. If enabled, processes within one container can view
    /// and interact with processes in other containers, subject to namespace and security constraints.
    /// </summary>
    [YamlMember(Alias = "shareProcessNamespace")]
    public bool? ShareProcessNamespace { get; set; }

    /// <summary>
    /// Represents the DNS configuration of a pod, encapsulating parameters
    /// that adjust the behavior or setup of DNS resolution for the pod.
    /// </summary>
    [YamlMember(Alias = "dnsConfig")]
    public PodDnsConfigV1? DnsConfig { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the pod uses the host network namespace.
    /// If set to true, the pod will have access to the host's network interfaces and IP.
    /// This is useful for applications requiring access to the host network but can have
    /// implications for security and network isolation.
    /// </summary>
    [YamlMember(Alias = "hostNetwork")]
    public bool? HostNetwork { get; set; }

    /// <summary>
    /// Determines whether the service account token should be automatically mounted to the pod or not.
    /// If set to true, it enables automatic mounting of the service account token.
    /// If set to false, the token will not be automatically mounted.
    /// Optional and defaults to the service account's value if not specified.
    /// </summary>
    [YamlMember(Alias = "automountServiceAccountToken")]
    public bool? AutomountServiceAccountToken { get; set; }

    /// <summary>
    /// Gets or sets the subdomain for the Pod.
    /// When specified, this allows the Pod to be part of a DNS subdomain.
    /// The subdomain must conform to DNS subdomain naming rules.
    /// This is typically used to configure a Pod's fully qualified domain name (FQDN).
    /// </summary>
    [YamlMember(Alias = "subdomain")]
    public string? Subdomain { get; set; } = null!;

    /// <summary>
    /// Gets a dictionary of key-value pairs used to specify the node selector for the pod.
    /// The node selector enables you to define specific keys and values that a node must
    /// have for the pod to be scheduled on it.
    /// </summary>
    [YamlMember(Alias = "nodeSelector")]
    public Dictionary<string, string> NodeSelector { get; } = [];

    /// <summary>
    /// Specifies the duration in seconds relative to the start time that the pod may be active on the node.
    /// If the deadline is exceeded, the pod will be terminated by the system.
    /// This property can be useful for enforcing time limits for long-running pods.
    /// </summary>
    [YamlMember(Alias = "activeDeadlineSeconds")]
    public long? ActiveDeadlineSeconds { get; set; }

    /// <summary>
    /// Represents the primary list of containers within the pod.
    /// These are the application-level containers that run within the desired Pod,
    /// and their specifications define the workloads the pod can execute.
    /// </summary>
    [YamlMember(Alias = "containers")]
    public List<ContainerV1> Containers { get; } = [];

    /// <summary>
    /// Indicates whether information about services should be injected into the pod's environment variables.
    /// This property determines if service environment variables, such as `<service_name/>_SERVICE_HOST`
    /// and `<service_name/>_SERVICE_PORT`, are made available to containers within the pod.
    /// </summary>
    [YamlMember(Alias = "enableServiceLinks")]
    public bool? EnableServiceLinks { get; set; }

    /// <summary>
    /// Represents the list of ephemeral containers within the pod specification.
    /// Ephemeral containers are used for troubleshooting purpose and do not persist across pod restarts.
    /// This property allows the specification of multiple ephemeral containers inside a pod.
    /// </summary>
    [YamlMember(Alias = "ephemeralContainers")]
    public List<EphemeralContainerV1> EphemeralContainers { get; } = [];

    /// <summary>
    /// Gets the list of HostAliasV1 objects that define custom mapping of IP addresses to hostnames.
    /// This property can be used to provide additional entries for the Pod's /etc/hosts file,
    /// enabling the Pod to resolve specified hostnames to the corresponding IP addresses without DNS.
    /// </summary>
    [YamlMember(Alias = "hostAliases")]
    public List<HostAliasV1> HostAliases { get; } = [];

    /// <summary>
    /// Determines whether the container(s) in the pod will run with the same user ID and group ID settings
    /// as the host. If set to true, containers will share the same host user namespace. If set to false
    /// or omitted, the containers will have their user and group IDs isolated within their own namespace.
    /// This setting impacts security configurations and should be used with caution.
    /// </summary>
    [YamlMember(Alias = "hostUsers")]
    public bool? HostUsers { get; set; }

    /// <summary>
    /// Specifies a list of references to secrets to use for pulling images for the containers
    /// defined in the pod. Each item in the list is a reference to a secret containing
    /// credentials for accessing a private image registry.
    /// This allows customization of authentication methods for image pulling,
    /// especially in secure or restricted environments.
    /// </summary>
    [YamlMember(Alias = "imagePullSecrets")]
    public List<LocalObjectReferenceV1> ImagePullSecrets { get; } = [];

    /// <summary>
    /// Represents a collection of initialization containers that run before the main containers
    /// in the pod are started. These containers are executed sequentially and must complete
    /// successfully before any normal containers are started. Initialization containers can
    /// perform setup tasks, such as loading configuration files or initializing data stores,
    /// required by the primary application containers.
    /// </summary>
    [YamlMember(Alias = "initContainers")]
    public List<ContainerV1> InitContainers { get; } = [];

    /// <summary>
    /// Gets or sets the operating system parameters for the pod.
    /// This property allows specifying details about the target operating
    /// system environment where the pod will run, if applicable.
    /// </summary>
    [YamlMember(Alias = "os")]
    public PodOsv1? Os { get; set; }

    /// <summary>
    /// Gets the list of readiness gates for the Pod.
    /// A readiness gate specifies additional conditions that must be met for the Pod
    /// to be considered ready beyond the default conditions.
    /// </summary>
    [YamlMember(Alias = "readinessGates")]
    public List<PodReadinessGateV1> ReadinessGates { get; } = [];

    /// <summary>
    /// Gets the list of resource claims associated with the pod.
    /// Resource claims define the resource needs of the pod, such as specific volumes or compute resources,
    /// allowing it to dynamically allocate resources from the cluster.
    /// </summary>
    [YamlMember(Alias = "resourceClaims")]
    public List<PodResourceClaimV1> ResourceClaims { get; } = [];

    /// <summary>
    /// Represents a list of scheduling gates for a pod. Scheduling gates are used to control
    /// the scheduling process by requiring specific conditions to be met before a pod
    /// is eligible for scheduling.
    /// </summary>
    [YamlMember(Alias = "schedulingGates")]
    public List<PodSchedulingGateV1> SchedulingGates { get; } = [];

    /// <summary>
    /// Gets or sets the duration in seconds that is allowed for a pod to terminate after receiving a termination signal.
    /// This property defines the grace period before forcefully killing the pod's containers, allowing processes to shut down gracefully.
    /// If set to null, the system default termination grace period will be used.
    /// </summary>
    [YamlMember(Alias = "terminationGracePeriodSeconds")]
    public long? TerminationGracePeriodSeconds { get; set; }

    /// <summary>
    /// Represents the tolerations applied to a pod.
    /// Tolerations are used to allow (but do not require) the scheduling of pods
    /// onto nodes with matching taints. A pod can tolerate a taint by matching
    /// its key, value, and effect as specified in the toleration.
    /// This property includes a collection of <see cref="TolerationV1"/> objects.
    /// </summary>
    [YamlMember(Alias = "tolerations")]
    public List<TolerationV1> Tolerations { get; } = [];

    /// <summary>
    /// Gets the list of topology spread constraints for the pod.
    /// Topology spread constraints define how the pods are distributed
    /// across the topology domains based on configured parameters.
    /// </summary>
    [YamlMember(Alias = "topologySpreadConstraints")]
    public List<TopologySpreadConstraintV1> TopologySpreadConstraints { get; } = [];

    /// <summary>
    /// Gets the list of volumes that can be mounted by containers in a Pod.
    /// Each volume in this list represents a storage resource that can be used
    /// inside the Pod for data persistence or sharing between containers.
    /// </summary>
    [YamlMember(Alias = "volumes")]
    public List<VolumeV1> Volumes { get; } = [];

    /// <summary>
    /// Gets or sets the security context for a pod.
    /// The security context defines the security attributes
    /// applied to the entire pod, including user and group IDs,
    /// SELinux options, and windows security settings when applicable.
    /// </summary>
    [YamlMember(Alias = "securityContext")]
    public PodSecurityContextV1? SecurityContext { get; set; }

    /// <summary>
    /// Gets or sets the name of the service account to be used in the pod.
    /// A service account allows pods to access the Kubernetes API and other resources
    /// with appropriate permissions, and is useful for defining the identity under
    /// which the pod will run in the cluster.
    /// </summary>
    [YamlMember(Alias = "serviceAccount")]
    public string? ServiceAccount { get; set; } = null!;

    /// <summary>
    /// Gets or sets the affinity rules for the pod.
    /// Affinity specifies scheduling constraints for a pod to ensure
    /// it is scheduled onto a node that satisfies certain conditions.
    /// This can include nodeSelector-like or pod affinity/anti-affinity constraints.
    /// </summary>
    [YamlMember(Alias = "affinity")]
    public AffinityV1? Affinity { get; set; }

    /// <summary>
    /// Gets or sets the DNS policy for the pod. This property determines the DNS settings applied
    /// to the pod, such as how DNS resolution is configured.
    /// Common values include "ClusterFirst" and "Default".
    /// </summary>
    [YamlMember(Alias = "dnsPolicy")]
    public string? DnsPolicy { get; set; } = null!;

    /// <summary>
    /// Indicates the preemption policy for the pod.
    /// This property defines whether a pod is eligible to preempt other pods for resources
    /// when scheduling or if it can be preempted itself.
    /// Acceptable values are "PreemptLowerPriority" to allow preemption or "Never" to prevent it.
    /// If not specified, the default behavior is to allow preemptions.
    /// </summary>
    [YamlMember(Alias = "preemptionPolicy")]
    public string? PreemptionPolicy { get; set; } = null!;

    /// <summary>
    /// Gets or sets the priority value of the Pod.
    /// Priority is an integer value that determines the scheduling precedence of the Pod.
    /// Higher values correspond to higher priority, which influences the order in which
    /// Pods are scheduled when resources are constrained.
    /// This property is optional and, if not specified, the priority of the Pod is determined
    /// by its priority class or system default settings.
    /// </summary>
    [YamlMember(Alias = "priority")]
    public int? Priority { get; set; }

    /// <summary>
    /// Specifies the restart behavior for all containers within the pod.
    /// Determines when the containers should be restarted following termination.
    /// Common values include "Always", "OnFailure", and "Never".
    /// </summary>
    [YamlMember(Alias = "restartPolicy")]
    public string? RestartPolicy { get; set; } = null!;
}

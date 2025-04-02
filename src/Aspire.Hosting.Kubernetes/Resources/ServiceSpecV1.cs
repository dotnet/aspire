// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Defines the specification for a Kubernetes Service resource.
/// </summary>
/// <remarks>
/// This class represents various attributes that define the behavior and configuration
/// of a Kubernetes Service, such as type, IP addresses, load balancer settings, traffic
/// policies, selectors, ports, and session affinity options.
/// </remarks>
[YamlSerializable]
public sealed class ServiceSpecV1
{
    /// <summary>
    /// Specifies the IP address assigned to the Kubernetes Service within the cluster.
    /// </summary>
    /// <remarks>
    /// The ClusterIP is used internally within the Kubernetes cluster to allow services
    /// to communicate with each other. If set to "None", the service will not be assigned
    /// a ClusterIP and will act as a headless service, suitable for direct pod communication.
    /// This property may be null when the cluster IP is not explicitly specified.
    /// </remarks>
    [YamlMember(Alias = "clusterIP")]
    public string? ClusterIp { get; set; }

    /// <summary>
    /// Gets or sets the IP address to assign to the external LoadBalancer of the Service.
    /// </summary>
    /// <remarks>
    /// If specified, this explicitly sets the LoadBalancer's IP address. This property is used when deploying
    /// a Service with type set to "LoadBalancer" in Kubernetes. The actual usage of this field depends on the
    /// cloud provider or environment where the Kubernetes cluster is hosted. If not set, the IP address will
    /// be dynamically assigned by the environment.
    /// </remarks>
    [YamlMember(Alias = "loadBalancerIP")]
    public string? LoadBalancerIp { get; set; }

    /// <summary>
    /// Specifies the external DNS name for the Kubernetes Service associated with a ServiceSpecV1.
    /// </summary>
    /// <remarks>
    /// This property is used with the "ExternalName" service type in Kubernetes to map a service
    /// to an external DNS name rather than an IP or other resources within the cluster.
    /// </remarks>
    [YamlMember(Alias = "externalName")]
    public string? ExternalName { get; set; }

    /// <summary>
    /// Gets or sets the type of the Kubernetes Service.
    /// </summary>
    /// <remarks>
    /// Specifies the type of Service to be created. Common types include:
    /// "ClusterIP", "NodePort", "LoadBalancer", and "ExternalName".
    /// Defaults to "ClusterIP" if not specified.
    /// </remarks>
    [YamlMember(Alias = "type")]
    public string Type { get; set; } = "ClusterIP";

    /// <summary>
    /// Represents the session affinity configuration for a Kubernetes Service.
    /// </summary>
    /// <remarks>
    /// This property specifies the configuration details of session affinity, which ensures that requests
    /// from a client are directed to the same backend pod. This is typically used for stateful applications
    /// requiring client-based stickiness.
    /// </remarks>
    [YamlMember(Alias = "sessionAffinityConfig")]
    public SessionAffinityConfigV1? SessionAffinityConfig { get; set; }

    /// <summary>
    /// Specifies the traffic distribution policy for the Kubernetes Service.
    /// </summary>
    /// <remarks>
    /// This property defines how network traffic is distributed across the service's associated pods or endpoints.
    /// It determines the mechanism or strategy used for traffic routing, which could include methods such as load-balancing or specific traffic shaping configurations.
    /// </remarks>
    [YamlMember(Alias = "trafficDistribution")]
    public string? TrafficDistribution { get; set; }

    /// <summary>
    /// Represents the selector field in the Kubernetes Service specification.
    /// </summary>
    /// <remarks>
    /// The selector is a key-value pair dictionary used to identify a set of pods
    /// that the Service targets. The Service routes traffic to these pods based on the selector definition.
    /// </remarks>
    [YamlMember(Alias = "selector")]
    public Dictionary<string, string> Selector { get; set; } = [];

    /// <summary>
    /// Indicates whether node ports should be automatically allocated for a service of type LoadBalancer.
    /// </summary>
    /// <remarks>
    /// When set to true, this property ensures that node ports are allocated for the load balancer service,
    /// enabling external access through specific ports. This setting is optional and can be used based
    /// on service configuration requirements.
    /// </remarks>
    [YamlMember(Alias = "allocateLoadBalancerNodePorts")]
    public bool? AllocateLoadBalancerNodePorts { get; set; }

    /// <summary>
    /// Represents a list of cluster IP addresses assigned to the Kubernetes Service.
    /// </summary>
    /// <remarks>
    /// The property is used to define multiple IP addresses for the Service, enabling support for more than one IP family.
    /// Commonly used in dual-stack networking configurations where both IPv4 and IPv6 IP addresses are specified.
    /// The property is immutable after creation and is managed by the Kubernetes system.
    /// </remarks>
    [YamlMember(Alias = "clusterIPs")]
    public List<string> ClusterIPs { get; } = [];

    /// <summary>
    /// Represents a list of external IP addresses associated with the service.
    /// </summary>
    /// <remarks>
    /// ExternalIPs specifies additional IP addresses for the service that are externally visible.
    /// These IP addresses are usually outside the cluster and can be used to route traffic to the service.
    /// </remarks>
    [YamlMember(Alias = "externalIPs")]
    public List<string> ExternalIPs { get; } = [];

    /// <summary>
    /// A list of IP families (e.g., IPv4, IPv6) used by the Kubernetes Service.
    /// </summary>
    /// <remarks>
    /// Specifies the IP address families the Service supports or is associated with.
    /// The order of families in the list can influence the assignment of IP addresses
    /// when the Service is created or updated. This is relevant when defining dual-stack services
    /// or specific network configurations requiring certain IP families.
    /// </remarks>
    [YamlMember(Alias = "ipFamilies")]
    public List<string> IpFamilies { get; } = [];

    /// <summary>
    /// Specifies the class of the load balancer to be used for the service.
    /// </summary>
    /// <remarks>
    /// This property allows the user to define a specific load balancer implementation
    /// or configuration class for the service. It is especially useful in environments
    /// where multiple load balancer options are available, enabling customization based
    /// on requirements or provider specifications.
    /// When set, this value is used to select the corresponding load balancer class during service creation.
    /// </remarks>
    [YamlMember(Alias = "loadBalancerClass")]
    public string? LoadBalancerClass { get; set; }

    /// <summary>
    /// Specifies a list of IP address ranges that are allowed to access
    /// the Kubernetes Service when the Service type is "LoadBalancer".
    /// </summary>
    /// <remarks>
    /// The LoadBalancerSourceRanges property defines a list of CIDR (Classless Inter-Domain Routing)
    /// ranges that restrict access to the Service's load balancer. If not set, access is not restricted
    /// based on IP ranges and is determined by the cloud provider or default behavior.
    /// </remarks>
    [YamlMember(Alias = "loadBalancerSourceRanges")]
    public List<string> LoadBalancerSourceRanges { get; } = [];

    /// <summary>
    /// Represents the collection of port configurations for a Kubernetes Service.
    /// </summary>
    /// <remarks>
    /// This property defines the list of ports through which the service can be accessed.
    /// Each port is configured using the <see cref="ServicePortV1"/> class, specifying details such as the port number, protocol, target port, and node port.
    /// It is key in exposing and routing traffic to the appropriate underlying workloads.
    /// </remarks>
    [YamlMember(Alias = "ports")]
    public List<ServicePortV1> Ports { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether to publish the addresses of not-ready pods for a Kubernetes Service.
    /// </summary>
    /// <remarks>
    /// When set to true, the Service will publish the IP addresses of pods even if the pods are not ready.
    /// This can be useful when a Service needs to route traffic to pods during their initialization phase
    /// or when readiness probes are not strictly necessary.
    /// </remarks>
    [YamlMember(Alias = "publishNotReadyAddresses")]
    public bool? PublishNotReadyAddresses { get; set; }

    /// <summary>
    /// Gets or sets the health check node port for the Kubernetes Service.
    /// </summary>
    /// <remarks>
    /// This property specifies the port number that is used for health checks when the external traffic policy is set to "Local".
    /// If not specified, the system will automatically allocate a node port.
    /// </remarks>
    [YamlMember(Alias = "healthCheckNodePort")]
    public int? HealthCheckNodePort { get; set; }

    /// <summary>
    /// Specifies how external traffic is routed to the Service's underlying network endpoints.
    /// </summary>
    /// <remarks>
    /// The property is used to define whether external traffic routed to the Service should preserve
    /// the client source IP address or be routed directly. Acceptable values include:
    /// - "Cluster": Traffic is routed via the cluster network and source IP addresses are not preserved.
    /// - "Local": Traffic is routed directly to the Service endpoints and source IP addresses are preserved.
    /// This setting is particularly useful for load balancing scenarios and for complying with
    /// specific application requirements where source IP preservation is a priority.
    /// </remarks>
    [YamlMember(Alias = "externalTrafficPolicy")]
    public string? ExternalTrafficPolicy { get; set; }

    /// <summary>
    /// Gets or sets the internal traffic policy for the Kubernetes Service.
    /// </summary>
    /// <remarks>
    /// This property defines the policy dictating how traffic is routed internally
    /// within the cluster. It can be used to configure traffic routing behavior
    /// for services with internal communication within the cluster.
    /// </remarks>
    [YamlMember(Alias = "internalTrafficPolicy")]
    public string? InternalTrafficPolicy { get; set; }

    /// <summary>
    /// Defines the IP family policy for a Kubernetes Service in API version V1.
    /// </summary>
    /// <remarks>
    /// This property determines the policy under which IP families (IPv4, IPv6) are assigned to the Service.
    /// It can control whether the Service prefers or requires a specific IP family or supports both.
    /// </remarks>
    [YamlMember(Alias = "ipFamilyPolicy")]
    public string? IpFamilyPolicy { get; set; }

    /// <summary>
    /// Defines the session affinity setting for a Kubernetes Service in API version V1.
    /// </summary>
    /// <remarks>
    /// Session affinity determines how traffic is routed to a service. It helps in maintaining
    /// connections from the same client to the same pod, enabling stateful communication
    /// and enhancing user experience for applications requiring persistent sessions.
    /// </remarks>
    [YamlMember(Alias = "sessionAffinity")]
    public string? SessionAffinity { get; set; }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a PodTemplate resource in Kubernetes.
/// </summary>
/// <remarks>
/// A PodTemplate is a basic unit in Kubernetes that holds Pod specification templates.
/// It allows for defining reusable configurations for Pods, typically used in combination
/// with higher-level controllers like Deployments. The template field inside this resource
/// specifies the desired Pod specification to use.
/// This class derives from the BaseKubernetesResource and supports API version "v1" with
/// the resource kind "PodTemplate". Properties defined in this class allow interaction
/// with the YAML configuration for Kubernetes PodTemplates.
/// </remarks>
[YamlSerializable]
public sealed class PodTemplate() : BaseKubernetesResource("v1", "PodTemplate")
{
    /// <summary>
    /// Gets or sets the template for creating pods in Kubernetes.
    /// </summary>
    /// <remarks>
    /// The Template property represents a PodTemplateSpec object that defines the metadata and specification
    /// for the pod instances created from the pod template. It is used in higher-level Kubernetes
    /// constructs such as Deployments, ReplicaSets, or StatefulSets to define the desired state of pods.
    /// </remarks>
    [YamlMember(Alias = "template")]
    public PodTemplateSpecV1 Template { get; set; } = new();
}

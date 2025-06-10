// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// EnvVarV1 represents an environment variable in a Kubernetes resource.
/// It is used to define key-value pairs or to derive values from other Kubernetes resources
/// through a configurable source.
/// </summary>
/// <remarks>
/// EnvVarV1 contains properties to define the name and value of the environment variable directly,
/// or to set the value dynamically using a source, such as a ConfigMap, Secret, or Field reference.
/// </remarks>
[YamlSerializable]
public sealed class EnvVarV1
{
    /// <summary>
    /// Gets or sets the name of the environment variable.
    /// This property specifies the name of the environment variable and is a required field.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the literal value of the environment variable.
    /// This property is used to specify a constant string value that will be set directly
    /// as the environment variable's value in the container configuration.
    /// </summary>
    [YamlMember(Alias = "value")]
    public string Value { get; set; } = null!;

    /// <summary>
    /// Gets or sets the source for the environment variable value.
    /// This property allows specifying an external source from which the environment variable value
    /// will be derived, such as a key in a ConfigMap, a field reference from the object's metadata,
    /// a resource field (e.g., limits or requests for memory or CPU), or a key in a Secret.
    /// </summary>
    [YamlMember(Alias = "valueFrom")]
    public EnvVarSourceV1? ValueFrom { get; set; }
}

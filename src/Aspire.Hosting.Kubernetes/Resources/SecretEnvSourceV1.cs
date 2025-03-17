// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// SecretEnvSourceV1 represents a reference to a Secret used for populating environment variables in a container.
/// </summary>
/// <remarks>
/// This class provides a way to source environment variable data from a Secret within Kubernetes.
/// A Secret contains sensitive information such as tokens, passwords, or keys that can be used as part of a container's environment.
/// </remarks>
/// <seealso cref="ConfigMapEnvSourceV1" />
/// <seealso cref="EnvFromSourceV1" />
[YamlSerializable]
public sealed class SecretEnvSourceV1
{
    /// <summary>
    /// Gets or sets the name of the referent secret. The secret must exist in the same namespace as the pod.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether the specified Secret is optional.
    /// If set to true, the application will not fail if the Secret is missing.
    /// If set to false or not specified, the Secret must exist for the application to function properly.
    /// </summary>
    [YamlMember(Alias = "optional")]
    public bool? Optional { get; set; } = null!;
}

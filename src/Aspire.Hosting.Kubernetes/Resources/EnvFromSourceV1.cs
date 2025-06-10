// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// EnvFromSourceV1 represents an environment variable source used to populate
/// environment variables in a container.
/// </summary>
/// <remarks>
/// This class allows defining environment variables for a container by sourcing
/// them from either a ConfigMap or a Secret. Optionally, a common prefix can be
/// added to the environment variable keys.
/// </remarks>
/// <example>
/// EnvFromSourceV1 can be used in scenarios where container environments need
/// data from ConfigMaps or Secrets or when a consistent prefix is required for
/// environment variable names within the container.
/// </example>
/// <seealso cref="ConfigMapEnvSourceV1" />
/// <seealso cref="SecretEnvSourceV1" />
[YamlSerializable]
public sealed class EnvFromSourceV1
{
    /// <summary>
    /// Represents a reference to a ConfigMap resource that is used to populate
    /// the environment variables within a container. It provides key-value pairs
    /// from the specified ConfigMap for use as environment variable values.
    /// </summary>
    [YamlMember(Alias = "configMapRef")]
    public ConfigMapEnvSourceV1? ConfigMapRef { get; set; }

    /// <summary>
    /// SecretRef represents a reference to a Secret resource.
    /// It is used to populate environment variables by mapping key-value pairs
    /// from the Secret's Data field to environment variables.
    /// </summary>
    [YamlMember(Alias = "secretRef")]
    public SecretEnvSourceV1? SecretRef { get; set; }

    /// <summary>
    /// The prefix to be added to each environment variable name defined by ConfigMapRef or SecretRef.
    /// This allows easy identification or grouping of environment variables injected from ConfigMap
    /// or Secret sources within the application.
    /// </summary>
    [YamlMember(Alias = "prefix")]
    public string? Prefix { get; set; }
}

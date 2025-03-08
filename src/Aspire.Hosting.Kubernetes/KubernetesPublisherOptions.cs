// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Kubernetes;

/// <summary>
/// Options which control generation of Kubernetes artifacts.
/// </summary>
public sealed class KubernetesPublisherOptions
{
    /// <summary>
    /// Gets or sets the type of output artifacts to be generated, such as "helm".
    /// </summary>
    public string OutputType { get; set; } = KubernetesPublisherOutputType.Helm;

    /// <summary>
    /// Gets or sets the file system path where the generated Kubernetes artifacts will be saved.
    /// </summary>
    public string OutputPath { get; set; } = null!;
}

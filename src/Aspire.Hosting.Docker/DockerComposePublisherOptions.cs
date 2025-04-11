// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.Docker;

/// <summary>
/// Options which control generation of Docker Compose artifacts.
/// </summary>
public sealed class DockerComposePublisherOptions : PublishingOptions
{
    /// <summary>
    /// The container registry to use.
    /// </summary>
    public string? DefaultContainerRegistry { get; set; }

    /// <summary>
    /// The name of an existing network to be used.
    /// </summary>
    public string? ExistingNetworkName { get; set; }

    /// <summary>
    /// Indicates whether to build container images during the publishing process.
    /// </summary>
    public bool BuildImages { get; set; } = true;
}

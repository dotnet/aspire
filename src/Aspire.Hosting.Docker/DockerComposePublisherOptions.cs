// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Docker;

/// <summary>
/// Options which control generation of Docker Compose artifacts.
/// </summary>
public sealed class DockerComposePublisherOptions
{
    /// <summary>
    /// The container registry to use.
    /// </summary>
    public string? DefaultContainerRegistry { get; set; }
}
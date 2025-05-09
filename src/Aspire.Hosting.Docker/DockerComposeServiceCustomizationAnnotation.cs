// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Docker.Resources.ComposeNodes;

namespace Aspire.Hosting.Docker;

/// <summary>
/// Represents an annotation for customizing a Docker Compose service.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DockerComposeServiceCustomizationAnnotation"/> class.
/// </remarks>
/// <param name="configure">The configuration action for customizing the Docker Compose service.</param>
public sealed class DockerComposeServiceCustomizationAnnotation(Action<DockerComposeServiceResource, Service> configure) : IResourceAnnotation
{

    /// <summary>
    /// Gets the configuration action for customizing the Docker Compose service.
    /// </summary>
    public Action<DockerComposeServiceResource, Service> Configure { get; } = configure ?? throw new ArgumentNullException(nameof(configure));
}

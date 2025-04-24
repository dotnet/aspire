// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using Aspire.Hosting.Docker;

namespace Aspire.Hosting;

/// <summary>
/// Extensions for adding a Docker Compose publisher to the application model.
/// </summary>
public static class DockerComposePublisherExtensions
{
    /// <summary>
    /// Adds a Docker Compose publisher to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="Aspire.Hosting.IDistributedApplicationBuilder"/>.</param>
    /// <param name="configureOptions">Callback to configure Docker Compose publisher options.</param>
    public static IDistributedApplicationBuilder AddDockerComposePublisher(this IDistributedApplicationBuilder builder, Action<DockerComposePublisherOptions>? configureOptions = null)
    {
        return builder.AddPublisher<DockerComposePublisher, DockerComposePublisherOptions>("docker-compose", configureOptions);
    }
}

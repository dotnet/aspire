// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Docker;

/// <summary>
/// Extensions for adding a Docker Compose publisher to the application model.
/// </summary>
public static class DockerComposePublisherExtensions
{
    /// <summary>
    /// Adds a Docker Compose publisher to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="Aspire.Hosting.IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the publisher used when using the Aspire CLI.</param>
    /// <param name="configureOptions">Callback to configure Docker Compose publisher options.</param>
    public static void AddDockerCompose(this IDistributedApplicationBuilder builder, string name, Action<DockerComposePublisherOptions>? configureOptions = null)
    {
        builder.AddPublisher<DockerComposePublisher, DockerComposePublisherOptions>(name, configureOptions);
    }
}

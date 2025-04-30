// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Docker;
using Aspire.Hosting.Docker.Resources.ComposeNodes;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for customizing Docker Compose service resources.
/// </summary>
public static class DockerComposeServiceExtensions
{
    /// <summary>
    /// Publishes the specified resource as a Docker Compose service.
    /// </summary>
    /// <typeparam name="T">The type of the resource.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="configure">The configuration action for the Docker Compose service.</param>
    /// <returns>The updated resource builder.</returns>
    /// <remarks>
    /// This method checks if the application is in publish mode. If it is, it adds a customization annotation
    /// that will be applied by the DockerComposeInfrastructure when generating the Docker Compose service.
    /// <example>
    /// <code>
    /// builder.AddContainer("redis", "redis:alpine").PublishAsDockerComposeService((resource, service) =>
    /// {
    ///     service.Name = "redis";
    /// });
    /// </code>
    /// </example>
    /// </remarks>

    public static IResourceBuilder<T> PublishAsDockerComposeService<T>(this IResourceBuilder<T> builder, Action<DockerComposeServiceResource, Service> configure)
        where T : IComputeResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        if (!builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return builder;
        }

        builder.WithAnnotation(new DockerComposeServiceCustomizationAnnotation(configure));

        return builder;
    }
}

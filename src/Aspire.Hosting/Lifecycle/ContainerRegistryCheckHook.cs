// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;

namespace Aspire.Hosting;

internal class ContainerRegistryHook(string requiredRegistry) : IDistributedApplicationLifecycleHook
{
    public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {

        var resourcesWithContainerImages = appModel.Resources
                                                   .SelectMany(r => r.Annotations.OfType<ContainerImageAnnotation>()
                                                                                 .Select(cia => new { Resource = r, Annotation = cia }));

        foreach (var resourceWithContainerImage in resourcesWithContainerImages)
        {
            resourceWithContainerImage.Annotation.Registry = requiredRegistry;
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Extension methods for <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class ContainerRegistryCheckExtensions
{
    /// <summary>
    /// Ensures that all container images are using the specified container registry.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="requiredRegistry">The hostname of the container registry to use for all container images.</param>
    /// <returns></returns>
    public static IDistributedApplicationBuilder WithContainerRegistry(this IDistributedApplicationBuilder builder, string requiredRegistry)
    {
        builder.Services.TryAddLifecycleHook<ContainerRegistryHook>(
            sp => new ContainerRegistryHook(requiredRegistry)
            );
        return builder;
    }
}

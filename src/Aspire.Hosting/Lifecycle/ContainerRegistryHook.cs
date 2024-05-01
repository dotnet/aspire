// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;

namespace Aspire.Hosting;

internal class ContainerRegistryHook(DistributedApplicationOptions options) : IDistributedApplicationLifecycleHook
{
    public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {

        var resourcesWithContainerImages = appModel.Resources
                                                   .SelectMany(r => r.Annotations.OfType<ContainerImageAnnotation>()
                                                                                 .Select(cia => new { Resource = r, Annotation = cia }));

        foreach (var resourceWithContainerImage in resourcesWithContainerImages)
        {
            resourceWithContainerImage.Annotation.Registry = options.ContainerRegistryOverride;
        }

        return Task.CompletedTask;
    }
}

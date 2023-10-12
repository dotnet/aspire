// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

public static class ContainerResourceExtensions
{
    public static IEnumerable<IDistributedApplicationResource> GetContainerResources(this DistributedApplicationModel model)
    {
        foreach (var resource in model.Resources)
        {
            if (resource.Annotations.OfType<ContainerImageAnnotation>().Any())
            {
                yield return resource;
            }
        }
    }

    public static bool IsContainer(this IDistributedApplicationResource resource)
    {
        return resource.Annotations.OfType<ContainerImageAnnotation>().Any();
    }
}

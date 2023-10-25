// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

public static class ContainerResourceExtensions
{
    public static IEnumerable<IResource> GetContainerResources(this DistributedApplicationModel model)
    {
        foreach (var resource in model.Resources)
        {
            if (resource.Annotations.OfType<ContainerImageAnnotation>().Any())
            {
                yield return resource;
            }
        }
    }

    public static bool IsContainer(this IResource resource)
    {
        return resource.Annotations.OfType<ContainerImageAnnotation>().Any();
    }
}

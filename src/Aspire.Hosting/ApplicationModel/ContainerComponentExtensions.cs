// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

public static class ContainerComponentExtensions
{
    public static IEnumerable<IDistributedApplicationComponent> GetContainerComponents(this DistributedApplicationModel model)
    {
        foreach (var component in model.Components)
        {
            if (component.Annotations.OfType<ContainerImageAnnotation>().Any())
            {
                yield return component;
            }
        }
    }

    public static bool IsContainer(this IDistributedApplicationComponent component)
    {
        return component.Annotations.OfType<ContainerImageAnnotation>().Any();
    }
}

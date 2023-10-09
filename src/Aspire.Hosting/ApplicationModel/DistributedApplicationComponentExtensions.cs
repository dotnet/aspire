// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

public static class DistributedApplicationComponentExtensions
{
    public static bool TryGetLastAnnotation<T>(this IDistributedApplicationComponent component, [NotNullWhen(true)] out T? annotation) where T : IDistributedApplicationComponentAnnotation
    {
        if (component.Annotations.OfType<T>().LastOrDefault() is { } lastAnnotation)
        {
            annotation = lastAnnotation;
            return true;
        }
        else
        {
            annotation = default(T);
            return false;
        }
    }

    public static bool TryGetAnnotationsOfType<T>(this IDistributedApplicationComponent component, [NotNullWhen(true)] out IEnumerable<T>? result) where T : IDistributedApplicationComponentAnnotation
    {
        var matchingTypeAnnotations = component.Annotations.OfType<T>();

        if (matchingTypeAnnotations.Any())
        {
            result = matchingTypeAnnotations.ToArray();
            return true;
        }
        else
        {
            result = null;
            return false;
        }
    }

    public static bool TryGetEnvironmentVariables(this IDistributedApplicationComponent component, [NotNullWhen(true)] out IEnumerable<EnvironmentCallbackAnnotation>? environmentVariables)
    {
        return TryGetAnnotationsOfType(component, out environmentVariables);
    }

    public static bool TryGetVolumeMounts(this IDistributedApplicationComponent component, [NotNullWhen(true)] out IEnumerable<VolumeMountAnnotation>? volumeMounts)
    {
        return TryGetAnnotationsOfType<VolumeMountAnnotation>(component, out volumeMounts);
    }

    public static bool TryGetServiceBindings(this IDistributedApplicationComponent component, [NotNullWhen(true)] out IEnumerable<ServiceBindingAnnotation>? serviceBindings)
    {
        return TryGetAnnotationsOfType(component, out serviceBindings);
    }

    public static bool TryGetAllocatedEndPoints(this IDistributedApplicationComponent component, [NotNullWhen(true)] out IEnumerable<AllocatedEndpointAnnotation>? allocatedEndPoints)
    {
        return TryGetAnnotationsOfType(component, out allocatedEndPoints);
    }

    public static bool TryGetContainerImageName(this IDistributedApplicationComponent component, [NotNullWhen(true)] out string? imageName)
    {
        if (component.Annotations.OfType<ContainerImageAnnotation>().LastOrDefault() is { } imageAnnotation)
        {
            var registryPrefix = string.IsNullOrEmpty(imageAnnotation.Registry) ? string.Empty : $"{imageAnnotation.Registry}/";
            var tagSuffix = string.IsNullOrEmpty(imageAnnotation.Tag) ? string.Empty : $":{imageAnnotation.Tag}";
            imageName = $"{registryPrefix}{imageAnnotation.Image}{tagSuffix}";
            return true;
        }

        imageName = null;
        return false;
    }

    public static int GetReplicaCount(this IDistributedApplicationComponent component)
    {
        if (component.TryGetLastAnnotation<ReplicaAnnotation>(out var replicaAnnotation))
        {
            return replicaAnnotation.Replicas;
        }
        else
        {
            return 1;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

public static class DistributedApplicationResourceExtensions
{
    public static bool TryGetLastAnnotation<T>(this IDistributedApplicationResource resource, [NotNullWhen(true)] out T? annotation) where T : IDistributedApplicationResourceAnnotation
    {
        if (resource.Annotations.OfType<T>().LastOrDefault() is { } lastAnnotation)
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

    public static bool TryGetAnnotationsOfType<T>(this IDistributedApplicationResource resource, [NotNullWhen(true)] out IEnumerable<T>? result) where T : IDistributedApplicationResourceAnnotation
    {
        var matchingTypeAnnotations = resource.Annotations.OfType<T>();

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

    public static bool TryGetEnvironmentVariables(this IDistributedApplicationResource resource, [NotNullWhen(true)] out IEnumerable<EnvironmentCallbackAnnotation>? environmentVariables)
    {
        return TryGetAnnotationsOfType(resource, out environmentVariables);
    }

    public static bool TryGetVolumeMounts(this IDistributedApplicationResource resource, [NotNullWhen(true)] out IEnumerable<VolumeMountAnnotation>? volumeMounts)
    {
        return TryGetAnnotationsOfType<VolumeMountAnnotation>(resource, out volumeMounts);
    }

    public static bool TryGetServiceBindings(this IDistributedApplicationResource resource, [NotNullWhen(true)] out IEnumerable<ServiceBindingAnnotation>? serviceBindings)
    {
        return TryGetAnnotationsOfType(resource, out serviceBindings);
    }

    public static bool TryGetAllocatedEndPoints(this IDistributedApplicationResource resource, [NotNullWhen(true)] out IEnumerable<AllocatedEndpointAnnotation>? allocatedEndPoints)
    {
        return TryGetAnnotationsOfType(resource, out allocatedEndPoints);
    }

    public static bool TryGetContainerImageName(this IDistributedApplicationResource resource, [NotNullWhen(true)] out string? imageName)
    {
        if (resource.Annotations.OfType<ContainerImageAnnotation>().LastOrDefault() is { } imageAnnotation)
        {
            var registryPrefix = string.IsNullOrEmpty(imageAnnotation.Registry) ? string.Empty : $"{imageAnnotation.Registry}/";
            var tagSuffix = string.IsNullOrEmpty(imageAnnotation.Tag) ? string.Empty : $":{imageAnnotation.Tag}";
            imageName = $"{registryPrefix}{imageAnnotation.Image}{tagSuffix}";
            return true;
        }

        imageName = null;
        return false;
    }

    public static int GetReplicaCount(this IDistributedApplicationResource resource)
    {
        if (resource.TryGetLastAnnotation<ReplicaAnnotation>(out var replicaAnnotation))
        {
            return replicaAnnotation.Replicas;
        }
        else
        {
            return 1;
        }
    }
}

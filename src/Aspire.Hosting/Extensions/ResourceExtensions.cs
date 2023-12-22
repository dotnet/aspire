// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Provides extension methods for the <see cref="IResource"/> interface.
/// </summary>
public static class ResourceExtensions
{
    /// <summary>
    /// Attempts to get the last annotation of the specified type from the resource.
    /// </summary>
    /// <typeparam name="T">The type of the annotation to get.</typeparam>
    /// <param name="resource">The resource to get the annotation from.</param>
    /// <param name="annotation">When this method returns, contains the last annotation of the specified type from the resource, if found; otherwise, the default value for <typeparamref name="T"/>.</param>
    /// <returns><c>true</c> if the last annotation of the specified type was found in the resource; otherwise, <c>false</c>.</returns>
    public static bool TryGetLastAnnotation<T>(this IResource resource, [NotNullWhen(true)] out T? annotation) where T : IResourceAnnotation
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

    /// <summary>
    /// Attempts to retrieve all annotations of the specified type from the given resource.
    /// </summary>
    /// <typeparam name="T">The type of annotation to retrieve.</typeparam>
    /// <param name="resource">The resource to retrieve annotations from.</param>
    /// <param name="result">When this method returns, contains the annotations of the specified type, if found; otherwise, null.</param>
    /// <returns>true if annotations of the specified type were found; otherwise, false.</returns>
    public static bool TryGetAnnotationsOfType<T>(this IResource resource, [NotNullWhen(true)] out IEnumerable<T>? result) where T : IResourceAnnotation
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

    /// <summary>
    /// Attempts to get the environment variables from the given resource.
    /// </summary>
    /// <param name="resource">The resource to get the environment variables from.</param>
    /// <param name="environmentVariables">The environment variables retrieved from the resource, if any.</param>
    /// <returns>True if the environment variables were successfully retrieved, false otherwise.</returns>
    public static bool TryGetEnvironmentVariables(this IResource resource, [NotNullWhen(true)] out IEnumerable<EnvironmentCallbackAnnotation>? environmentVariables)
    {
        return TryGetAnnotationsOfType(resource, out environmentVariables);
    }

    /// <summary>
    /// Attempts to get the volume mounts for the specified resource.
    /// </summary>
    /// <param name="resource">The resource to get the volume mounts for.</param>
    /// <param name="volumeMounts">When this method returns, contains the volume mounts for the specified resource, if found; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if the volume mounts were successfully retrieved; otherwise, <c>false</c>.</returns>
    public static bool TryGetVolumeMounts(this IResource resource, [NotNullWhen(true)] out IEnumerable<VolumeMountAnnotation>? volumeMounts)
    {
        return TryGetAnnotationsOfType<VolumeMountAnnotation>(resource, out volumeMounts);
    }

    /// <summary>
    /// Attempts to retrieve the endpoints for the given resource.
    /// </summary>
    /// <param name="resource">The resource to retrieve the endpoints for.</param>
    /// <param name="endpoints">The endpoints for the given resource, if found.</param>
    /// <returns>True if the endpoints were found, false otherwise.</returns>
    public static bool TryGetEndpoints(this IResource resource, [NotNullWhen(true)] out IEnumerable<EndpointAnnotation>? endpoints)
    {
        return TryGetAnnotationsOfType(resource, out endpoints);
    }

    /// <summary>
    /// Attempts to get the allocated endpoints for the specified resource.
    /// </summary>
    /// <param name="resource">The resource to get the allocated endpoints for.</param>
    /// <param name="allocatedEndPoints">When this method returns, contains the allocated endpoints for the specified resource, if they exist; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if the allocated endpoints were successfully retrieved; otherwise, <c>false</c>.</returns>
    public static bool TryGetAllocatedEndPoints(this IResource resource, [NotNullWhen(true)] out IEnumerable<AllocatedEndpointAnnotation>? allocatedEndPoints)
    {
        return TryGetAnnotationsOfType(resource, out allocatedEndPoints);
    }

    /// <summary>
    /// Attempts to get the container image name from the given resource.
    /// </summary>
    /// <param name="resource">The resource to get the container image name from.</param>
    /// <param name="imageName">The container image name if found, otherwise null.</param>
    /// <returns>True if the container image name was found, otherwise false.</returns>
    public static bool TryGetContainerImageName(this IResource resource, [NotNullWhen(true)] out string? imageName)
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

    /// <summary>
    /// Gets the number of replicas for the specified resource. Defaults to <c>1</c> if no
    /// <see cref="ReplicaAnnotation" /> is found.
    /// </summary>
    /// <param name="resource">The resource to get the replica count for.</param>
    /// <returns>The number of replicas for the specified resource.</returns>
    public static int GetReplicaCount(this IResource resource)
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

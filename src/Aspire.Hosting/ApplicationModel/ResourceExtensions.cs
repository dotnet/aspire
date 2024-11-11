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
    /// <returns><see langword="true"/> if the last annotation of the specified type was found in the resource; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetLastAnnotation<T>(this IResource resource, [NotNullWhen(true)] out T? annotation) where T : IResourceAnnotation
    {
        if (resource.Annotations.OfType<T>().LastOrDefault() is { } lastAnnotation)
        {
            annotation = lastAnnotation;
            return true;
        }
        else
        {
            annotation = default;
            return false;
        }
    }

    /// <summary>
    /// Attempts to retrieve all annotations of the specified type from the given resource.
    /// </summary>
    /// <typeparam name="T">The type of annotation to retrieve.</typeparam>
    /// <param name="resource">The resource to retrieve annotations from.</param>
    /// <param name="result">When this method returns, contains the annotations of the specified type, if found; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if annotations of the specified type were found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetAnnotationsOfType<T>(this IResource resource, [NotNullWhen(true)] out IEnumerable<T>? result) where T : IResourceAnnotation
    {
        var matchingTypeAnnotations = resource.Annotations.OfType<T>().ToArray();

        if (matchingTypeAnnotations.Length is not 0)
        {
            result = matchingTypeAnnotations;
            return true;
        }
        else
        {
            result = null;
            return false;
        }
    }

    /// <summary>
    /// Gets whether <paramref name="resource"/> has an annotation of type <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">The type of annotation to retrieve.</typeparam>
    /// <param name="resource">The resource to retrieve annotations from.</param>
    /// <returns><see langword="true"/> if an annotation of the specified type was found; otherwise, <see langword="false"/>.</returns>
    public static bool HasAnnotationOfType<T>(this IResource resource) where T : IResourceAnnotation
    {
        return resource.Annotations.Any(a => a is T);
    }

    /// <summary>
    /// Attempts to retrieve all annotations of the specified type from the given resource including from parents.
    /// </summary>
    /// <typeparam name="T">The type of annotation to retrieve.</typeparam>
    /// <param name="resource">The resource to retrieve annotations from.</param>
    /// <param name="result">When this method returns, contains the annotations of the specified type, if found; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if annotations of the specified type were found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetAnnotationsIncludingAncestorsOfType<T>(this IResource resource, [NotNullWhen(true)] out IEnumerable<T>? result) where T : IResourceAnnotation
    {
        if (resource is IResourceWithParent)
        {
            List<T>? annotations = null;

            while (true)
            {
                foreach (var annotation in resource.Annotations.OfType<T>())
                {
                    annotations ??= [];
                    annotations.Add(annotation);
                }

                if (resource is IResourceWithParent child)
                {
                    resource = child.Parent;
                }
                else
                {
                    break;
                }
            }

            result = annotations;
            return annotations is not null;
        }

        return TryGetAnnotationsOfType(resource, out result);
    }

    /// <summary>
    /// Gets whether <paramref name="resource"/> or its ancestors have an annotation of type <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">The type of annotation to retrieve.</typeparam>
    /// <param name="resource">The resource to retrieve annotations from.</param>
    /// <returns><see langword="true"/> if an annotation of the specified type was found; otherwise, <see langword="false"/>.</returns>
    public static bool HasAnnotationIncludingAncestorsOfType<T>(this IResource resource) where T : IResourceAnnotation
    {
        if (resource is IResourceWithParent)
        {
            while (true)
            {
                if (HasAnnotationOfType<T>(resource))
                {
                    return true;
                }

                if (resource is IResourceWithParent child)
                {
                    resource = child.Parent;
                }
                else
                {
                    break;
                }
            }

            return false;
        }

        return HasAnnotationOfType<T>(resource);
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
    /// Get the environment variables from the given resource.
    /// </summary>
    /// <remarks>
    /// This method is useful when you want to make sure the environment variables are added properly to resources, mostly in test situations.
    /// This method has asynchronous behavior when <paramref name = "applicationOperation" /> is <see cref="DistributedApplicationOperation.Run"/>
    /// and environment variables were provided from <see cref="IValueProvider"/> otherwise it will be synchronous.
    /// </remarks>
    /// <param name="resource">The resource to get the environment variables from.</param>
    /// <param name="applicationOperation">The context in which the AppHost is being executed.</param>
    /// <returns>The environment variables retrieved from the resource.</returns>
    /// <example>
    /// Using <see cref="GetEnvironmentVariableValuesAsync(IResourceWithEnvironment, DistributedApplicationOperation)"/> inside
    /// a unit test to validate environment variable values.
    /// <code>
    /// var builder = DistributedApplication.CreateBuilder();
    /// var container = builder.AddContainer("elasticsearch", "library/elasticsearch", "8.14.0")
    ///  .WithEnvironment("discovery.type", "single-node")
    ///  .WithEnvironment("xpack.security.enabled", "true");
    ///
    /// var env = await container.Resource.GetEnvironmentVariableValuesAsync();
    ///
    /// Assert.Collection(env,
    ///     env =>
    ///         {
    ///             Assert.Equal("discovery.type", env.Key);
    ///             Assert.Equal("single-node", env.Value);
    ///         },
    ///         env =>
    ///         {
    ///             Assert.Equal("xpack.security.enabled", env.Key);
    ///             Assert.Equal("true", env.Value);
    ///         });
    /// </code>
    /// </example>
    public static async ValueTask<Dictionary<string, string>> GetEnvironmentVariableValuesAsync(this IResourceWithEnvironment resource,
            DistributedApplicationOperation applicationOperation = DistributedApplicationOperation.Run)
    {
        var environmentVariables = new Dictionary<string, string>();

        if (resource.TryGetEnvironmentVariables(out var callbacks))
        {
            var config = new Dictionary<string, object>();
            var executionContext = new DistributedApplicationExecutionContext(applicationOperation);
            var context = new EnvironmentCallbackContext(executionContext, config);

            foreach (var callback in callbacks)
            {
                await callback.Callback(context).ConfigureAwait(false);
            }

            foreach (var (key, expr) in config)
            {
                var value = (applicationOperation, expr) switch
                {
                    (_, string s) => s,
                    (DistributedApplicationOperation.Run, IValueProvider provider) => await provider.GetValueAsync().ConfigureAwait(false),
                    (DistributedApplicationOperation.Publish, IManifestExpressionProvider provider) => provider.ValueExpression,
                    (_, null) => null,
                    _ => throw new InvalidOperationException($"Unsupported expression type: {expr.GetType()}")
                };

                if (value is not null)
                {
                    environmentVariables[key] = value;
                }
            }
        }

        return environmentVariables;
    }

    /// <summary>
    /// Attempts to get the container mounts for the specified resource.
    /// </summary>
    /// <param name="resource">The resource to get the volume mounts for.</param>
    /// <param name="volumeMounts">When this method returns, contains the volume mounts for the specified resource, if found; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if the volume mounts were successfully retrieved; otherwise, <c>false</c>.</returns>
    public static bool TryGetContainerMounts(this IResource resource, [NotNullWhen(true)] out IEnumerable<ContainerMountAnnotation>? volumeMounts)
    {
        return TryGetAnnotationsOfType<ContainerMountAnnotation>(resource, out volumeMounts);
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
    /// Gets the endpoints for the specified resource.
    /// </summary>
    /// <param name="resource">The <see cref="IResourceWithEndpoints"/> which contains <see cref="EndpointAnnotation"/> annotations.</param>
    /// <returns>An enumeration of <see cref="EndpointReference"/> based on the <see cref="EndpointAnnotation"/> annotations from the resources' <see cref="IResource.Annotations"/> collection.</returns>
    public static IEnumerable<EndpointReference> GetEndpoints(this IResourceWithEndpoints resource)
    {
        if (TryGetAnnotationsOfType<EndpointAnnotation>(resource, out var endpoints))
        {
            return endpoints.Select(e => new EndpointReference(resource, e));
        }

        return [];
    }

    /// <summary>
    /// Gets an endpoint reference for the specified endpoint name.
    /// </summary>
    /// <param name="resource">The <see cref="IResourceWithEndpoints"/> which contains <see cref="EndpointAnnotation"/> annotations.</param>
    /// <param name="endpointName">The name of the endpoint.</param>
    /// <returns>An <see cref="EndpointReference"/> object representing the endpoint reference
    /// for the specified endpoint.</returns>
    public static EndpointReference GetEndpoint(this IResourceWithEndpoints resource, string endpointName)
    {
        return new EndpointReference(resource, endpointName);
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

            if (string.IsNullOrEmpty(imageAnnotation.SHA256))
            {
                var tagSuffix = string.IsNullOrEmpty(imageAnnotation.Tag) ? string.Empty : $":{imageAnnotation.Tag}";
                imageName = $"{registryPrefix}{imageAnnotation.Image}{tagSuffix}";
            }
            else
            {
                var shaSuffix = $"@sha256:{imageAnnotation.SHA256}";
                imageName = $"{registryPrefix}{imageAnnotation.Image}{shaSuffix}";
            }

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

    /// <summary>
    /// Get the arguments from the given resource.
    /// </summary>
    /// <remarks>
    /// This method is useful when you want to make sure the arguments are added properly to resources, mostly in test situations.
    /// This method has asynchronous behavior when arguments were provided from <see cref="IValueProvider"/> otherwise it will be synchronous.
    /// </remarks>
    /// <param name="resource">The resource to get the arguments from.</param>
    /// <returns>The arguments retrieved from the resource.</returns>
    public static async ValueTask<IReadOnlyList<string>> GetArgumentListAsync(this IResourceWithArgs resource)
    {
        var finalArgs = new List<string>();

        if (resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var exeArgsCallbacks))
        {
            var args = new List<object>();
            var commandLineContext = new CommandLineArgsCallbackContext(args, default);

            foreach (var exeArgsCallback in exeArgsCallbacks)
            {
                await exeArgsCallback.Callback(commandLineContext).ConfigureAwait(false);
            }

            foreach (var arg in args)
            {
                var value = arg switch
                {
                    string s => s,
                    IValueProvider valueProvider => await valueProvider.GetValueAsync().ConfigureAwait(false),
                    null => null,
                    _ => throw new InvalidOperationException($"Unexpected value for {arg}")
                };

                if (value is not null)
                {
                    finalArgs.Add(value);
                }
            }
        }

        return finalArgs;
    }

    /// <summary>
    /// Gets the lifetime type of the container for the specified resource.
    /// Defaults to <see cref="ContainerLifetime.Session"/> if no <see cref="ContainerLifetimeAnnotation"/> is found.
    /// </summary>
    /// <param name="resource">The resource to the get the ContainerLifetimeType for.</param>
    /// <returns>
    /// The <see cref="ContainerLifetime"/> from the <see cref="ContainerLifetimeAnnotation"/> for the resource (if the annotation exists).
    /// Defaults to <see cref="ContainerLifetime.Session"/> if the annotation is not set.
    /// </returns>
    internal static ContainerLifetime GetContainerLifetimeType(this IResource resource)
    {
        if (resource.TryGetLastAnnotation<ContainerLifetimeAnnotation>(out var lifetimeAnnotation))
        {
            return lifetimeAnnotation.Lifetime;
        }

        return ContainerLifetime.Session;
    }

    /// <summary>
    /// Get the top resource in the resource hierarchy.
    /// e.g. for a AzureBlobStorageResource, the top resource is the AzureStorageResource.
    /// </summary>
    internal static IResource GetRootResource(this IResource resource) =>
        resource switch
        {
            IResourceWithParent resWithParent => resWithParent.Parent.GetRootResource(),
            _ => resource
        };

    /// <summary>
    /// Gets resolved names for the specified resource.
    /// DCP resources are given a unique suffix as part of the complete name. We want to use that value.
    /// Also, a DCP resource could have multiple instances. All instance names are returned for a resource.
    /// </summary>
    internal static string[] GetResolvedResourceNames(this IResource resource)
    {
        if (resource.TryGetLastAnnotation<DcpInstancesAnnotation>(out var replicaAnnotation) && !replicaAnnotation.Instances.IsEmpty)
        {
            return replicaAnnotation.Instances.Select(i => i.Name).ToArray();
        }
        else
        {
            return [resource.Name];
        }
    }
}

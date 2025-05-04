// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
    /// <param name="resource">The resource to get the environment variables from.</param>
    /// <param name="applicationOperation">The context in which the AppHost is being executed.</param>
    /// <returns>The environment variables retrieved from the resource.</returns>
    /// <remarks>
    /// This method is useful when you want to make sure the environment variables are added properly to resources, mostly in test situations.
    /// This method has asynchronous behavior when <paramref name = "applicationOperation" /> is <see cref="DistributedApplicationOperation.Run"/>
    /// and environment variables were provided from <see cref="IValueProvider"/> otherwise it will be synchronous.
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
    /// </remarks>
    public static async ValueTask<Dictionary<string, string>> GetEnvironmentVariableValuesAsync(this IResourceWithEnvironment resource,
            DistributedApplicationOperation applicationOperation = DistributedApplicationOperation.Run)
    {
        var env = new Dictionary<string, string>();
        var executionContext = new DistributedApplicationExecutionContext(new DistributedApplicationExecutionContextOptions(applicationOperation));
        await resource.ProcessEnvironmentVariableValuesAsync(
            executionContext,
            (key, unprocessed, value, ex) =>
            {
                if (value is string s)
                {
                    env[key] = s;
                }
            },
            NullLogger.Instance).ConfigureAwait(false);

        return env;
    }

    /// <summary>
    /// Get the arguments from the given resource.
    /// </summary>
    /// <param name="resource">The resource to get the arguments from.</param>
    /// <param name="applicationOperation">The context in which the AppHost is being executed.</param>
    /// <returns>The arguments retrieved from the resource.</returns>
    /// <remarks>
    /// This method is useful when you want to make sure the arguments are added properly to resources, mostly in test situations.
    /// This method has asynchronous behavior when <paramref name = "applicationOperation" /> is <see cref="DistributedApplicationOperation.Run"/>
    /// and arguments were provided from <see cref="IValueProvider"/> otherwise it will be synchronous.
    /// <example>
    /// Using <see cref="GetArgumentValuesAsync(IResourceWithArgs, DistributedApplicationOperation)"/> inside
    /// a unit test to validate argument values.
    /// <code>
    /// var builder = DistributedApplication.CreateBuilder();
    /// var container = builder.AddContainer("elasticsearch", "library/elasticsearch", "8.14.0")
    ///  .WithArgs("--discovery.type", "single-node")
    ///  .WithArgs("--xpack.security.enabled", "true");
    ///
    /// var args = await container.Resource.GetArgumentsAsync();
    ///
    /// Assert.Collection(args,
    ///     arg =>
    ///         {
    ///             Assert.Equal("--discovery.type", arg);
    ///         },
    ///         arg =>
    ///         {
    ///             Assert.Equal("--xpack.security.enabled", arg);
    ///         });
    /// </code>
    /// </example>
    /// </remarks>
    public static async ValueTask<string[]> GetArgumentValuesAsync(this IResourceWithArgs resource,
        DistributedApplicationOperation applicationOperation = DistributedApplicationOperation.Run)
    {
        var args = new List<string>();

        var executionContext = new DistributedApplicationExecutionContext(new DistributedApplicationExecutionContextOptions(applicationOperation));
        await resource.ProcessArgumentValuesAsync(
            executionContext,
            (unprocessed, value, ex, _) =>
            {
                if (value is string s)
                {
                    args.Add(s);
                }

            },
            NullLogger.Instance).ConfigureAwait(false);

        return [.. args];
    }

    /// <summary>
    /// Processes argument values for the specified resource in the given execution context.
    /// </summary>
    /// <param name="resource">The resource containing the argument values to process.</param>
    /// <param name="executionContext">The execution context used during the processing of argument values.</param>
    /// <param name="processValue">
    /// A callback invoked for each argument value. This action provides the unprocessed value, processed string representation,
    /// an exception if one occurs, and a boolean indicating the success of processing.
    /// </param>
    /// <param name="logger">The logger used for logging information or errors during the argument processing.</param>
    /// <param name="containerHostName">An optional container host name to consider during processing, if applicable.</param>
    /// <param name="cancellationToken">A token for cancelling the operation, if needed.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async ValueTask ProcessArgumentValuesAsync(
        this IResource resource,
        DistributedApplicationExecutionContext executionContext,
        // (unprocessed, processed, exception, isSensitive)
        Action<object?, string?, Exception?, bool> processValue,
        ILogger logger,
        string? containerHostName = null,
        CancellationToken cancellationToken = default)
    {
        if (resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var callbacks))
        {
            var args = new List<object>();
            var context = new CommandLineArgsCallbackContext(args, cancellationToken)
            {
                Logger = logger,
                ExecutionContext = executionContext
            };

            foreach (var callback in callbacks)
            {
                await callback.Callback(context).ConfigureAwait(false);
            }

            foreach (var a in args)
            {
                try
                {
                    var resolvedValue = (executionContext.Operation, a) switch
                    {
                        (_, string s) => new(s, false),
                        (DistributedApplicationOperation.Run, IValueProvider provider) => await GetValue(key: null, provider, logger, resource.IsContainer(), containerHostName, cancellationToken).ConfigureAwait(false),
                        (DistributedApplicationOperation.Run, IResourceBuilder<IResource> rb) when rb.Resource is IValueProvider provider => await GetValue(key: null, provider, logger, resource.IsContainer(), containerHostName, cancellationToken).ConfigureAwait(false),
                        (DistributedApplicationOperation.Publish, IManifestExpressionProvider provider) => new(provider.ValueExpression, false),
                        (DistributedApplicationOperation.Publish, IResourceBuilder<IResource> rb) when rb.Resource is IManifestExpressionProvider provider => new(provider.ValueExpression, false),
                        (_, { } o) => new(o.ToString(), false),
                        (_, null) => new(null, false),
                    };

                    if (resolvedValue?.Value != null)
                    {
                        processValue(a, resolvedValue.Value, null, resolvedValue.IsSensitive);
                    }
                }
                catch (Exception ex)
                {
                    processValue(a, a.ToString(), ex, false);
                }
            }
        }
    }

    /// <summary>
    /// Processes environment variable values for the specified resource within the given execution context.
    /// </summary>
    /// <param name="resource">The resource from which the environment variables are retrieved and processed.</param>
    /// <param name="executionContext">The execution context to be used for processing the environment variables.</param>
    /// <param name="processValue">An action delegate invoked for each environment variable, providing the key, the unprocessed value, the processed value (if available), and any exception encountered during processing.</param>
    /// <param name="logger">The logger used to log any information or errors during the environment variables processing.</param>
    /// <param name="containerHostName">The optional container host name associated with the resource being processed.</param>
    /// <param name="cancellationToken">A cancellation token to observe during the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static async ValueTask ProcessEnvironmentVariableValuesAsync(
        this IResource resource,
        DistributedApplicationExecutionContext executionContext,
        Action<string, object?, string?, Exception?> processValue,
        ILogger logger,
        string? containerHostName = null,
        CancellationToken cancellationToken = default)
    {
        if (resource.TryGetEnvironmentVariables(out var callbacks))
        {
            var config = new Dictionary<string, object>();
            var context = new EnvironmentCallbackContext(executionContext, resource, config, cancellationToken)
            {
                Logger = logger
            };

            foreach (var callback in callbacks)
            {
                await callback.Callback(context).ConfigureAwait(false);
            }

            foreach (var (key, expr) in config)
            {
                try
                {
                    var resolvedValue = (executionContext.Operation, expr) switch
                    {
                        (_, string s) => new(s, false),
                        (DistributedApplicationOperation.Run, IValueProvider provider) => await GetValue(key, provider, logger, resource.IsContainer(), containerHostName, cancellationToken).ConfigureAwait(false),
                        (DistributedApplicationOperation.Run, IResourceBuilder<IResource> rb) when rb.Resource is IValueProvider provider => await GetValue(key, provider, logger, resource.IsContainer(), containerHostName, cancellationToken).ConfigureAwait(false),
                        (DistributedApplicationOperation.Publish, IManifestExpressionProvider provider) => new(provider.ValueExpression, false),
                        (DistributedApplicationOperation.Publish, IResourceBuilder<IResource> rb) when rb.Resource is IManifestExpressionProvider provider => new(provider.ValueExpression, false),
                        (_, { } o) => new(o.ToString(), false),
                        (_, null) => new(null, false),
                    };

                    if (resolvedValue?.Value is not null)
                    {
                        processValue(key, expr, resolvedValue.Value, null);
                    }
                }
                catch (Exception ex)
                {
                    processValue(key, expr, expr?.ToString(), ex);
                }
            }
        }
    }

    internal static async ValueTask ProcessContainerRuntimeArgValues(
        this IResource resource,
        Action<string?, Exception?> processValue,
        ILogger logger,
        string? containerHostName = null,
        CancellationToken cancellationToken = default)
    {
        // Apply optional extra arguments to the container run command.
        if (resource.TryGetAnnotationsOfType<ContainerRuntimeArgsCallbackAnnotation>(out var runArgsCallback))
        {
            var args = new List<object>();

            var containerRunArgsContext = new ContainerRuntimeArgsCallbackContext(args, cancellationToken);

            foreach (var callback in runArgsCallback)
            {
                await callback.Callback(containerRunArgsContext).ConfigureAwait(false);
            }

            foreach (var arg in args)
            {
                try
                {
                    var value = arg switch
                    {
                        string s => s,
                        IValueProvider valueProvider => (await GetValue(key: null, valueProvider, logger, resource.IsContainer(), containerHostName, cancellationToken).ConfigureAwait(false))?.Value,
                        { } obj => obj.ToString(),
                        null => null
                    };

                    if (value is not null)
                    {
                        processValue(value, null);
                    }
                }
                catch (Exception ex)
                {
                    processValue(arg.ToString(), ex);
                }
            }
        }
    }

    private static async Task<ResolvedValue?> GetValue(string? key, IValueProvider valueProvider, ILogger logger, bool isContainer, string? containerHostName, CancellationToken cancellationToken)
    {
        containerHostName ??= "host.docker.internal";

        var task = ExpressionResolver.ResolveAsync(isContainer, valueProvider, containerHostName, cancellationToken);

        if (!task.IsCompleted)
        {
            if (valueProvider is IResource resource)
            {
                if (key is null)
                {
                    logger.LogInformation("Waiting for value from resource '{ResourceName}'", resource.Name);
                }
                else
                {
                    logger.LogInformation("Waiting for value for environment variable value '{Name}' from resource '{ResourceName}'", key, resource.Name);
                }
            }
            else if (valueProvider is ConnectionStringReference { Resource: var cs })
            {
                logger.LogInformation("Waiting for value for connection string from resource '{ResourceName}'", cs.Name);
            }
            else
            {
                if (key is null)
                {
                    logger.LogInformation("Waiting for value from {ValueProvider}.", valueProvider.ToString());
                }
                else
                {
                    logger.LogInformation("Waiting for value for environment variable value '{Name}' from {ValueProvider}.", key, valueProvider.ToString());
                }
            }
        }

        return await task.ConfigureAwait(false);
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
    /// Attempts to retrieve the URLs for the given resource.
    /// </summary>
    /// <param name="resource">The resource to retrieve the URLs for.</param>
    /// <param name="urls">The URLs for the given resource, if found.</param>
    /// <returns>True if the URLs were found, false otherwise.</returns>
    public static bool TryGetUrls(this IResource resource, [NotNullWhen(true)] out IEnumerable<ResourceUrlAnnotation>? urls)
    {
        return TryGetAnnotationsOfType(resource, out urls);
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
    /// Gets the deployment target for the specified resource, if any. Throws an exception if
    /// there are multiple compute environments and a compute environment is not explicitly specified.
    /// </summary>
#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public static DeploymentTargetAnnotation? GetDeploymentTargetAnnotation(this IResource resource, IComputeEnvironmentResource? computeEnvironmentResource = null)
    {
        if (resource.TryGetAnnotationsOfType<DeploymentTargetAnnotation>(out var deploymentTargetAnnotations))
        {
            var annotations = deploymentTargetAnnotations.ToArray();

            if (computeEnvironmentResource is not null)
            {
                return annotations.SingleOrDefault(a => a.ComputeEnvironment == computeEnvironmentResource);
            }

            if (annotations.Length > 1)
            {
                var computeEnvironmentNames = string.Join(", ", annotations.Select(a => a.ComputeEnvironment?.Name));
                throw new InvalidOperationException($"Resource '{resource.Name}' has multiple compute environments - '{computeEnvironmentNames}'. Please specify a single compute environment using 'WithComputeEnvironment'.");
            }

            return annotations[0];
        }
        return null;
#pragma warning restore ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    }

    /// <summary>
    /// Gets the lifetime type of the container for the specified resource.
    /// Defaults to <see cref="ContainerLifetime.Session"/> if no <see cref="ContainerLifetimeAnnotation"/> is found.
    /// </summary>
    /// <param name="resource">The resource to get the ContainerLifetimeType for.</param>
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
    /// Determines whether the specified resource has a pull policy annotation and retrieves the value if it does.
    /// </summary>
    /// <param name="resource">The resource to check for a ContainerPullPolicy annotation</param>
    /// <param name="pullPolicy">The <see cref="ImagePullPolicy"/> for the annotation</param>
    /// <returns>True if an annotation exists, false otherwise</returns>
    internal static bool TryGetContainerImagePullPolicy(this IResource resource, [NotNullWhen(true)] out ImagePullPolicy? pullPolicy)
    {
        if (resource.TryGetLastAnnotation<ContainerImagePullPolicyAnnotation>(out var pullPolicyAnnotation))
        {
            pullPolicy = pullPolicyAnnotation.ImagePullPolicy;
            return true;
        }

        pullPolicy = null;
        return false;
    }

    /// <summary>
    /// Determines whether a resource has proxy support enabled or not. Container resources may have a <see cref="ProxySupportAnnotation"/> setting that disables proxying for their
    /// endpoints regardless of the endpoint proxy configuration.
    /// </summary>
    /// <param name="resource">The resource to get proxy support for.</param>
    /// <returns>True if the resource supports proxied endpoints/services, false otherwise.</returns>
    internal static bool SupportsProxy(this IResource resource)
    {
        // If the resource doesn't have a ProxySupportAnnotation or the ProxyEnabled property on the annotation is true, then the resource supports proxying.
        return !resource.TryGetLastAnnotation<ProxySupportAnnotation>(out var proxySupportAnnotation) || proxySupportAnnotation.ProxyEnabled;
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

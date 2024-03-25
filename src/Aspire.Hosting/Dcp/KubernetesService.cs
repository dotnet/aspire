// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Aspire.Hosting.Dcp.Model;
using k8s;
using k8s.Exceptions;
using k8s.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace Aspire.Hosting.Dcp;

internal enum DcpApiOperationType
{
    Create = 1,
    List = 2,
    Delete = 3,
    Watch = 4,
    GetLogSubresource = 5,
}

internal interface IKubernetesService
{
    Task<T> CreateAsync<T>(T obj, CancellationToken cancellationToken = default)
        where T : CustomResource;
    Task<List<T>> ListAsync<T>(string? namespaceParameter = null, CancellationToken cancellationToken = default)
        where T : CustomResource;
    Task<T> DeleteAsync<T>(string name, string? namespaceParameter = null, CancellationToken cancellationToken = default)
        where T : CustomResource;
    IAsyncEnumerable<(WatchEventType, T)> WatchAsync<T>(
        string? namespaceParameter = null,
        CancellationToken cancellationToken = default)
        where T : CustomResource;
    Task<Stream> GetLogStreamAsync<T>(
        T obj,
        string logStreamType,
        bool? follow = true,
        bool? timestamps = false,
        CancellationToken cancellationToken = default) where T : CustomResource;
}

internal sealed class KubernetesService(ILogger<KubernetesService> logger, IOptions<DcpOptions> dcpOptions, Locations locations) : IKubernetesService, IDisposable
{
    private static readonly TimeSpan s_initialRetryDelay = TimeSpan.FromMilliseconds(100);
    private static GroupVersion GroupVersion => Model.Dcp.GroupVersion;

    private DcpKubernetesClient? _kubernetes;

    public TimeSpan MaxRetryDuration { get; set; } = TimeSpan.FromSeconds(20);

    public Task<T> CreateAsync<T>(T obj, CancellationToken cancellationToken = default)
        where T : CustomResource
    {
        var resourceType = GetResourceFor<T>();
        var namespaceParameter = obj.Namespace();

        return ExecuteWithRetry(
           DcpApiOperationType.Create,
           resourceType,
           async (kubernetes) =>
           {
               var response = string.IsNullOrEmpty(namespaceParameter)
                ? await kubernetes.CustomObjects.CreateClusterCustomObjectWithHttpMessagesAsync(
                    obj,
                    GroupVersion.Group,
                    GroupVersion.Version,
                    resourceType,
                    cancellationToken: cancellationToken).ConfigureAwait(false)
                : await kubernetes.CustomObjects.CreateNamespacedCustomObjectWithHttpMessagesAsync(
                    obj,
                    GroupVersion.Group,
                    GroupVersion.Version,
                    namespaceParameter,
                    resourceType,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

               return KubernetesJson.Deserialize<T>(response.Body.ToString());
           },
           cancellationToken);
    }

    public Task<List<T>> ListAsync<T>(string? namespaceParameter = null, CancellationToken cancellationToken = default)
        where T : CustomResource
    {
        var resourceType = GetResourceFor<T>();

        return ExecuteWithRetry(
            DcpApiOperationType.List,
            resourceType,
            async (kubernetes) =>
            {
                var response = string.IsNullOrEmpty(namespaceParameter)
                    ? await kubernetes.CustomObjects.ListClusterCustomObjectWithHttpMessagesAsync(
                        GroupVersion.Group,
                        GroupVersion.Version,
                        resourceType,
                        cancellationToken: cancellationToken).ConfigureAwait(false)
                    : await kubernetes.CustomObjects.ListNamespacedCustomObjectWithHttpMessagesAsync(
                        GroupVersion.Group,
                        GroupVersion.Version,
                        namespaceParameter,
                        resourceType,
                        cancellationToken: cancellationToken).ConfigureAwait(false);

                return KubernetesJson.Deserialize<CustomResourceList<T>>(response.Body.ToString()).Items;
            },
            cancellationToken);
    }

    public Task<T> DeleteAsync<T>(string name, string? namespaceParameter = null, CancellationToken cancellationToken = default)
        where T : CustomResource
    {
        var resourceType = GetResourceFor<T>();

        return ExecuteWithRetry(
            DcpApiOperationType.Delete,
            resourceType,
            async (kubernetes) =>
            {
                var response = string.IsNullOrEmpty(namespaceParameter)
                ? await kubernetes.CustomObjects.DeleteClusterCustomObjectWithHttpMessagesAsync(
                    GroupVersion.Group,
                    GroupVersion.Version,
                    resourceType,
                    name,
                    cancellationToken: cancellationToken).ConfigureAwait(false)
                : await kubernetes.CustomObjects.DeleteNamespacedCustomObjectWithHttpMessagesAsync(
                    GroupVersion.Group,
                    GroupVersion.Version,
                    namespaceParameter,
                    resourceType,
                    name,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                return KubernetesJson.Deserialize<T>(response.Body.ToString());
            },
            cancellationToken);
    }

    public async IAsyncEnumerable<(WatchEventType, T)> WatchAsync<T>(
        string? namespaceParameter = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where T : CustomResource
    {
        var resourceType = GetResourceFor<T>();
        var result = await ExecuteWithRetry(
            DcpApiOperationType.Watch,
            resourceType,
            (kubernetes) =>
            {
                var responseTask = string.IsNullOrEmpty(namespaceParameter)
                    ? kubernetes.CustomObjects.ListClusterCustomObjectWithHttpMessagesAsync(
                        GroupVersion.Group,
                        GroupVersion.Version,
                        resourceType,
                        watch: true,
                        cancellationToken: cancellationToken)
                    : kubernetes.CustomObjects.ListNamespacedCustomObjectWithHttpMessagesAsync(
                        GroupVersion.Group,
                        GroupVersion.Version,
                        namespaceParameter,
                        resourceType,
                        watch: true,
                        cancellationToken: cancellationToken);

                return responseTask.WatchAsync<T, object>(null, cancellationToken);
            },
            cancellationToken).ConfigureAwait(false);

        await foreach (var item in result)
        {
            yield return item;
        }
    }

    public Task<Stream> GetLogStreamAsync<T>(
        T obj,
        string logStreamType,
        bool? follow = true,
        bool? timestamps = false,
        CancellationToken cancellationToken = default) where T : CustomResource
    {
        var resourceType = GetResourceFor<T>();

        ImmutableArray<(string name, string value)>? queryParams = [
            (name: "follow", value: follow == true ? "true": "false"),
            (name: "timestamps", value: timestamps == true ? "true" : "false"),
            (name: "source", value: logStreamType)
        ];

        return ExecuteWithRetry(
            DcpApiOperationType.GetLogSubresource,
            resourceType,
            async (kubernetes) =>
            {
                var response = await kubernetes.ReadSubResourceAsStreamAsync(
                    GroupVersion.Group,
                    GroupVersion.Version,
                    resourceType,
                    obj.Metadata.Name,
                    Logs.SubResourceName,
                    obj.Metadata.Namespace(),
                    queryParams,
                    cancellationToken
                ).ConfigureAwait(false);

                return response.Body;
            },
            cancellationToken
        );
    }

    public void Dispose()
    {
        _kubeconfigReadSemaphore?.Dispose();
        _kubernetes?.Dispose();
    }

    private static string GetResourceFor<T>() where T : CustomResource
    {
        if (!Model.Dcp.Schema.TryGet<T>(out var kindWithResource))
        {
            throw new InvalidOperationException($"Unknown custom resource type: {typeof(T).Name}");
        }

        return kindWithResource.Resource;
    }

    private Task<TResult> ExecuteWithRetry<TResult>(
        DcpApiOperationType operationType,
        string resourceType,
        Func<DcpKubernetesClient, TResult> operation,
        CancellationToken cancellationToken)
    {
        return ExecuteWithRetry<TResult>(
            operationType,
            resourceType,
            (DcpKubernetesClient kubernetes) => Task.FromResult(operation(kubernetes)),
            cancellationToken);
    }

    private async Task<TResult> ExecuteWithRetry<TResult>(
        DcpApiOperationType operationType,
        string resourceType,
        Func<DcpKubernetesClient, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        var currentTimestamp = DateTime.UtcNow;
        var delay = s_initialRetryDelay;
        AspireEventSource.Instance.DcpApiCallStart(operationType, resourceType);

        try
        {
            while (true)
            {
                try
                {
                    await EnsureKubernetesAsync(cancellationToken).ConfigureAwait(false);
                    return await operation(_kubernetes!).ConfigureAwait(false);
                }
                catch (Exception e) when (IsRetryable(e))
                {
                    if (DateTime.UtcNow.Subtract(currentTimestamp) > MaxRetryDuration)
                    {
                        AspireEventSource.Instance.DcpApiCallTimeout(operationType, resourceType);
                        throw;
                    }

                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    delay *= 2;
                    AspireEventSource.Instance.DcpApiCallRetry(operationType, resourceType);
                }
            }
        }
        finally
        {
            AspireEventSource.Instance.DcpApiCallStop(operationType, resourceType);
        }

    }

    private static bool IsRetryable(Exception ex) => ex is HttpRequestException || ex is KubeConfigException;

    private readonly SemaphoreSlim _kubeconfigReadSemaphore = new(1);

    private ResiliencePipeline? _resiliencePipeline;

    private ResiliencePipeline GetReadKubeconfigResiliencePipeline()
    {
        if (_resiliencePipeline == null)
        {
            var configurationReadRetry = new RetryStrategyOptions()
            {
                ShouldHandle = new PredicateBuilder().Handle<KubeConfigException>(),
                BackoffType = DelayBackoffType.Constant,
                MaxRetryAttempts = dcpOptions.Value.KubernetesConfigReadRetryCount,
                MaxDelay = TimeSpan.FromSeconds(dcpOptions.Value.KubernetesConfigReadRetryIntervalSeconds),
                OnRetry = (retry) =>
                {
                    logger.LogDebug(
                        retry.Outcome.Exception,
                        "Reading Kubernetes configuration file from '{DcpKubeconfigPath}' failed. Retry pending. (iteration {Iteration}).",
                        locations.DcpKubeconfigPath,
                        retry.AttemptNumber
                        );
                    return ValueTask.CompletedTask;
                }
            };

            _resiliencePipeline = new ResiliencePipelineBuilder().AddRetry(configurationReadRetry).Build();
        }

        return _resiliencePipeline;
    }

    private async Task EnsureKubernetesAsync(CancellationToken cancellationToken = default)
    {
        // Return early before waiting for the semaphore if we can.
        if (_kubernetes != null)
        {
            logger.LogDebug("Kubernetes ensured at first chance shortcut.");
            return;
        }

        while (!await _kubeconfigReadSemaphore.WaitAsync(100, cancellationToken).ConfigureAwait(false))
        {
            logger.LogDebug("Waiting for semaphore to read kubeconfig.");
        }

        // Return late after getting the semaphore but also probably after
        // another thread has gotten the config loaded.

        if (_kubernetes != null)
        {
            logger.LogDebug("Kubernetes ensured at second chance shortcut.");
            _kubeconfigReadSemaphore.Release();
            return;
        }

        try
        {
            // This retry was created in relation to this comment in GitHub:
            //
            //     https://github.com/dotnet/aspire/issues/2422#issuecomment-2016701083
            //
            // It looks like it is possible for us to attempt to read the file before it is written/finished
            // being written. We rely on DCP to write the configuration file but it may happen in parallel to
            // this code executing is its possible the file does not exist, or is still being written by
            // the time we get to it.
            //
            // This retry will retry reading the file 5 times (by default, but configurable) with a pause
            // of 3 seconds between each attempt. This means it could take up to 15 seconds to fail. We emit
            // debug level logs for each retry attempt should we need to help a customer debug this.
            var pipeline = GetReadKubeconfigResiliencePipeline();
            _kubernetes = await pipeline.ExecuteAsync<DcpKubernetesClient>(async (cancellationToken) =>
            {
                logger.LogDebug("Reading Kubernetes configuration from '{DcpKubeconfigPath}'.", locations.DcpKubeconfigPath);
                var fileInfo = new FileInfo(locations.DcpKubeconfigPath);
                var config = await KubernetesClientConfiguration.BuildConfigFromConfigFileAsync(kubeconfig: fileInfo, useRelativePaths: false).ConfigureAwait(false);
                logger.LogDebug("Successfully read Kubernetes configuration from '{DcpKubeconfigPath}'.", locations.DcpKubeconfigPath);

                return new DcpKubernetesClient(config);
            }, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _kubeconfigReadSemaphore.Release();
        }
    }
}

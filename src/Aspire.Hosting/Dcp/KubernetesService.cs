// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Aspire.Hosting.Dcp.Model;
using Aspire.Hosting.Utils;
using k8s;
using k8s.Autorest;
using k8s.Exceptions;
using k8s.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using YamlDotNet.Core;

namespace Aspire.Hosting.Dcp;

internal enum DcpApiOperationType
{
    Create = 1,
    List = 2,
    Delete = 3,
    Watch = 4,
    GetLogSubresource = 5,
    Get = 6,
    Patch = 7,
    ServerStop = 8,
    ResourceCleanup = 9
}

internal interface IKubernetesService
{
    Task<T> GetAsync<T>(string name, string? namespaceParameter = null, CancellationToken cancellationToken = default)
        where T : CustomResource;
    Task<T> CreateAsync<T>(T obj, CancellationToken cancellationToken = default)
        where T : CustomResource;
    Task<T> PatchAsync<T>(T obj, V1Patch patch, CancellationToken cancellationToken = default)
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
    Task StopServerAsync(string resourceCleanup = ResourceCleanup.Full, CancellationToken cancellation = default);
    Task CleanupResourcesAsync(CancellationToken cancellationToken = default);
}

internal sealed class KubernetesService(ILogger<KubernetesService> logger, IOptions<DcpOptions> dcpOptions, Locations locations) : IKubernetesService, IDisposable
{
    // A pseudo-resource type used for log operations on the DCP execution document.
    private const string DcpExecutionResourceType = "DcpExecution";

    private static readonly TimeSpan s_initialRetryDelay = TimeSpan.FromMilliseconds(100);
    private static GroupVersion GroupVersion => Model.Dcp.GroupVersion;
    private readonly SemaphoreSlim _kubeconfigReadSemaphore = new(1);

    private DcpKubernetesClient? _kubernetes;
    private ResiliencePipeline? _resiliencePipeline;
    private bool _disposed;

    public TimeSpan MaxRetryDuration { get; set; } = TimeSpan.FromSeconds(20);

    public Task<T> GetAsync<T>(string name, string? namespaceParameter = null, CancellationToken cancellationToken = default)
        where T : CustomResource
    {
        var resourceType = GetResourceFor<T>();

        return ExecuteWithRetry(
            DcpApiOperationType.Get,
            resourceType,
            async (kubernetes) =>
            {
                var response = string.IsNullOrEmpty(namespaceParameter)
                ? await kubernetes.CustomObjects.GetClusterCustomObjectWithHttpMessagesAsync(
                    GroupVersion.Group,
                    GroupVersion.Version,
                    resourceType,
                    name,
                    cancellationToken: cancellationToken).ConfigureAwait(false)
                : await kubernetes.CustomObjects.GetNamespacedCustomObjectWithHttpMessagesAsync(
                    GroupVersion.Group,
                    GroupVersion.Version,
                    namespaceParameter,
                    resourceType,
                    name,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                return KubernetesJson.Deserialize<T>(response.Body.ToString());
            },
            RetryOnConnectivityAndConflictErrors,
            cancellationToken);
    }

    public Task<T> CreateAsync<T>(T obj, CancellationToken cancellationToken = default)
        where T : CustomResource
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
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
           RetryOnConnectivityErrors,
           cancellationToken);
    }

    public Task<T> PatchAsync<T>(T obj, V1Patch patch, CancellationToken cancellationToken = default)
        where T : CustomResource
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var resourceType = GetResourceFor<T>();
        var namespaceParameter = obj.Namespace();

        return ExecuteWithRetry(
           DcpApiOperationType.Patch,
           resourceType,
           async (kubernetes) =>
           {
               var response = string.IsNullOrEmpty(namespaceParameter)
                ? await kubernetes.CustomObjects.PatchClusterCustomObjectWithHttpMessagesAsync(
                    patch,
                    GroupVersion.Group,
                    GroupVersion.Version,
                    resourceType,
                    obj.Metadata.Name,
                    cancellationToken: cancellationToken).ConfigureAwait(false)
                : await kubernetes.CustomObjects.PatchNamespacedCustomObjectWithHttpMessagesAsync(
                    patch,
                    GroupVersion.Group,
                    GroupVersion.Version,
                    namespaceParameter,
                    resourceType,
                    obj.Metadata.Name,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

               return KubernetesJson.Deserialize<T>(response.Body.ToString());
           },
           RetryOnConnectivityErrors,
           cancellationToken);
    }

    public Task<List<T>> ListAsync<T>(string? namespaceParameter = null, CancellationToken cancellationToken = default)
        where T : CustomResource
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
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
            RetryOnConnectivityAndConflictErrors,
            cancellationToken);
    }

    public Task<T> DeleteAsync<T>(string name, string? namespaceParameter = null, CancellationToken cancellationToken = default)
        where T : CustomResource
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
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
            RetryOnConnectivityAndConflictErrors,
            cancellationToken);
    }

    public async IAsyncEnumerable<(WatchEventType, T)> WatchAsync<T>(
        string? namespaceParameter = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where T : CustomResource
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var resourceType = GetResourceFor<T>();

        // WatchAsync can become unresponsive if running long enough
        // We use a helper to periodically restart the inner watch enumerable
        var innerWatchFactory = ((WatchEventType, T)? lastValue, CancellationToken restartCancellationToken) =>
        {
            return ExecuteWithRetry(
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
                            cancellationToken: restartCancellationToken)
                        : kubernetes.CustomObjects.ListNamespacedCustomObjectWithHttpMessagesAsync(
                            GroupVersion.Group,
                            GroupVersion.Version,
                            namespaceParameter,
                            resourceType,
                            watch: true,
                            cancellationToken: restartCancellationToken);

                    return responseTask.WatchAsync<T, object>(null, restartCancellationToken);
                },
                RetryOnConnectivityAndConflictErrors,
                restartCancellationToken);
        };

        await foreach (var item in PeriodicRestartAsyncEnumerable.CreateAsync(innerWatchFactory, restartInterval: TimeSpan.FromMinutes(5), cancellationToken: cancellationToken).ConfigureAwait(false))
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
        ObjectDisposedException.ThrowIf(_disposed, this);
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
            RetryOnConnectivityAndConflictErrors,
            cancellationToken
        );
    }

    public Task StopServerAsync(string resourceCleanup = ResourceCleanup.Full, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return ExecuteWithRetry(
            DcpApiOperationType.ServerStop,
            DcpExecutionResourceType,
            async (kubernetes) =>
            {
                await kubernetes.PatchExecutionDocumentAsync(
                    new ApiServerExecution
                    {
                        ApiServerStatus = ApiServerStatus.Stopping,
                        ShutdownResourceCleanup = ResourceCleanup.Full
                    },
                    cancellationToken
                    ).ConfigureAwait(false);
                return (object?)null;
            },
            RetryOnConnectivityErrors,
            cancellationToken
        );
    }

    public async Task CleanupResourcesAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var executionDoc = await ExecuteWithRetry(
            DcpApiOperationType.ResourceCleanup,
            DcpExecutionResourceType,
            async (kubernetes) =>
            {
                return await kubernetes.PatchExecutionDocumentAsync(
                    new ApiServerExecution
                    {
                        ApiServerStatus = ApiServerStatus.CleaningResources,
                        ShutdownResourceCleanup = ResourceCleanup.Full
                    },
                    cancellationToken
                    ).ConfigureAwait(false);
            },
            RetryOnConnectivityErrors,
            cancellationToken
        ).ConfigureAwait(false);

        if (executionDoc.ResourcesCleanedUp)
        {
            return;
        }

        var retryPipeline = new ResiliencePipelineBuilder<ApiServerExecution>()
            .AddRetry(new RetryStrategyOptions<ApiServerExecution>()
            {
                ShouldHandle = new PredicateBuilder<ApiServerExecution>()
                    .Handle<Exception>(RetryOnConnectivityErrors)
                    .HandleResult(executionDoc => !executionDoc.ResourcesCleanedUp),
                BackoffType = DelayBackoffType.Constant,
                MaxRetryAttempts = int.MaxValue,
                Delay = s_initialRetryDelay,
                MaxDelay = TimeSpan.FromSeconds(1),
            })
            .Build();

        await retryPipeline.ExecuteAsync(async cancellationContext =>
        {
            return await _kubernetes!.GetExecutionDocumentAsync(cancellationToken).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        _disposed = true;
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
        Func<Exception, bool> isRetryable,
        CancellationToken cancellationToken)
    {
        return ExecuteWithRetry<TResult>(
            operationType,
            resourceType,
            (DcpKubernetesClient kubernetes) => Task.FromResult(operation(kubernetes)),
            isRetryable,
            cancellationToken);
    }

    private async Task<TResult> ExecuteWithRetry<TResult>(
        DcpApiOperationType operationType,
        string resourceType,
        Func<DcpKubernetesClient, Task<TResult>> operation,
        Func<Exception, bool> isRetryable,
        CancellationToken cancellationToken)
    {
        var currentTimestamp = DateTime.UtcNow;
        var delay = s_initialRetryDelay;
        AspireEventSource.Instance.DcpApiCallStart(operationType, resourceType);

        var resiliencePipeline = CreateKubernetesCallResiliencePipeline(operationType, resourceType, isRetryable);

        try
        {
            return await resiliencePipeline.ExecuteAsync<TResult>(async (cancellationToken) =>
            {
                await EnsureKubernetesAsync(cancellationToken).ConfigureAwait(false);
                return await operation(_kubernetes!).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            AspireEventSource.Instance.DcpApiCallStop(operationType, resourceType);
        }
    }

    private static bool RetryOnConnectivityErrors(Exception ex) => ex is HttpRequestException || ex is KubeConfigException;
    private static bool RetryOnConnectivityAndConflictErrors(Exception ex) =>
        ex is HttpRequestException ||
        ex is KubeConfigException ||
        (ex is HttpOperationException hoe && hoe.Response.StatusCode == System.Net.HttpStatusCode.Conflict);

    private ResiliencePipeline CreateKubernetesCallResiliencePipeline(
        DcpApiOperationType operationType,
        string resourceType,
        Func<Exception, bool> isRetryable)
    {
        var resiliencePipeline = new ResiliencePipelineBuilder()
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = MaxRetryDuration,
                OnTimeout = (_) =>
                {
                    AspireEventSource.Instance.DcpApiCallTimeout(operationType, resourceType);
                    return ValueTask.CompletedTask;
                }
            })
            .AddRetry(new RetryStrategyOptions()
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(isRetryable),
                BackoffType = DelayBackoffType.Exponential,
                MaxRetryAttempts = int.MaxValue,
                Delay = s_initialRetryDelay,
                MaxDelay = TimeSpan.FromSeconds(5),
                OnRetry = (retry) =>
                {
                    AspireEventSource.Instance.DcpApiCallRetry(operationType, resourceType);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
        return resiliencePipeline;
    }

    private ResiliencePipeline CreateReadKubeconfigResiliencePipeline()
    {
        if (_resiliencePipeline == null)
        {
            var configurationReadRetry = new RetryStrategyOptions()
            {
                // Handle exceptions caused by races between writing and reading the configuration file.
                // If the file is loaded while it is still being written, this can result in a YamlException being thrown.
                ShouldHandle = new PredicateBuilder().Handle<KubeConfigException>().Handle<YamlException>(),
                BackoffType = DelayBackoffType.Constant,
                MaxRetryAttempts = dcpOptions.Value.KubernetesConfigReadRetryCount,
                MaxDelay = TimeSpan.FromMilliseconds(dcpOptions.Value.KubernetesConfigReadRetryIntervalMilliseconds),
                OnRetry = (retry) =>
                {
                    logger.LogDebug(
                        "Waiting for Kubernetes configuration file at '{DcpKubeconfigPath}' (attempt {Iteration}).",
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
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Return early before waiting for the semaphore if we can.
        if (_kubernetes != null)
        {
            return;
        }

        await _kubeconfigReadSemaphore.WaitAsync(-1, cancellationToken).ConfigureAwait(false);

        try
        {
            // Second chance shortcut if multiple threads got caught.
            if (_kubernetes != null)
            {
                return;
            }

            // We retry reading the kubeconfig file because DCP takes a few moments to write
            // it to disk. This retry pipeline will only be invoked by a single thread the
            // rest will be held at the semaphore.
            var readStopwatch = new Stopwatch();
            readStopwatch.Start();

            var pipeline = CreateReadKubeconfigResiliencePipeline();
            _kubernetes = await pipeline.ExecuteAsync<DcpKubernetesClient>(async (cancellationToken) =>
            {
                var fileInfo = new FileInfo(locations.DcpKubeconfigPath);
                while (!fileInfo.Exists)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(dcpOptions.Value.KubernetesConfigReadRetryIntervalMilliseconds), cancellationToken).ConfigureAwait(false);
                    fileInfo = new FileInfo(locations.DcpKubeconfigPath);
                }

                var config = await KubernetesClientConfiguration.BuildConfigFromConfigFileAsync(kubeconfig: fileInfo, useRelativePaths: false).ConfigureAwait(false);
                readStopwatch.Stop();

                logger.LogDebug(
                    "Successfully read Kubernetes configuration from '{DcpKubeconfigPath}' after {DurationMs} milliseconds.",
                    locations.DcpKubeconfigPath,
                    readStopwatch.ElapsedMilliseconds
                    );

                return new DcpKubernetesClient(config);
            }, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _kubeconfigReadSemaphore.Release();
        }
    }
}

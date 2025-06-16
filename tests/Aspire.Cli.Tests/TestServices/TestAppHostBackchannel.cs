// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Aspire.Cli.Backchannel;

namespace Aspire.Cli.Tests.TestServices;

internal sealed class TestAppHostBackchannel : IAppHostBackchannel
{
    public TaskCompletionSource? PingAsyncCalled { get; set; }
    public Func<long, Task<long>>? PingAsyncCallback { get; set; }

    public TaskCompletionSource? RequestStopAsyncCalled { get; set; }
    public Func<Task>? RequestStopAsyncCallback { get; set; }

    public TaskCompletionSource? GetDashboardUrlsAsyncCalled { get; set; }
    public Func<CancellationToken, Task<(string, string?)>>? GetDashboardUrlsAsyncCallback { get; set; }

    public TaskCompletionSource? GetResourceStatesAsyncCalled { get; set; }
    public Func<CancellationToken, IAsyncEnumerable<RpcResourceState>>? GetResourceStatesAsyncCallback { get; set; }

    public TaskCompletionSource? ConnectAsyncCalled { get; set; }
    public Func<string, CancellationToken, Task>? ConnectAsyncCallback { get; set; }

    public TaskCompletionSource? GetPublishingActivitiesAsyncCalled { get; set; }
    public Func<CancellationToken, IAsyncEnumerable<(string, string, bool, bool)>>? GetPublishingActivitiesAsyncCallback { get; set; }

    public TaskCompletionSource? GetCapabilitiesAsyncCalled { get; set; }
    public Func<CancellationToken, Task<string[]>>? GetCapabilitiesAsyncCallback { get; set; }

    public TaskCompletionSource? GetResourcesAsyncCalled { get; set; }
    public Func<CancellationToken, Task<RpcResourceInfo[]>>? GetResourcesAsyncCallback { get; set; }

    public TaskCompletionSource? GetResourceLogsAsyncCalled { get; set; }
    public Func<string, CancellationToken, IAsyncEnumerable<ResourceLogEntry>>? GetResourceLogsAsyncCallback { get; set; }

    public Task<long> PingAsync(long timestamp, CancellationToken cancellationToken)
    {
        PingAsyncCalled?.SetResult();
        return PingAsyncCallback != null
            ? PingAsyncCallback.Invoke(timestamp)
            : Task.FromResult(timestamp);
    }

    public Task RequestStopAsync(CancellationToken cancellationToken)
    {
        RequestStopAsyncCalled?.SetResult();
        if (RequestStopAsyncCallback != null)
        {
            return RequestStopAsyncCallback.Invoke();
        }
        else
        {
            return Task.CompletedTask;
        }
    }

    public Task<(string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken)> GetDashboardUrlsAsync(CancellationToken cancellationToken)
    {
        GetDashboardUrlsAsyncCalled?.SetResult();
        return GetDashboardUrlsAsyncCallback != null
            ? GetDashboardUrlsAsyncCallback.Invoke(cancellationToken)
            : Task.FromResult<(string, string?)>(("http://localhost:5000/login?t=abcd", "https://monalisa-hot-potato-vrpqrxxrx7x2rxx-5000.app.github.dev/login?t=abcd"));
    }

    public async IAsyncEnumerable<RpcResourceState> GetResourceStatesAsync([EnumeratorCancellation]CancellationToken cancellationToken)
    {
        GetResourceStatesAsyncCalled?.SetResult();

        if (GetResourceStatesAsyncCallback != null)
        {
            var resourceStates = GetResourceStatesAsyncCallback.Invoke(cancellationToken).ConfigureAwait(false);
            await foreach (var resourceState in resourceStates)
            {
                yield return resourceState;
            }
        }
        else
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                yield return new RpcResourceState
                {
                    Resource = "frontend",
                    Type = "Project",
                    State = "Starting",
                    Endpoints = new[] { "http://localhost:5000" },
                    Health = "Healthy"
                };
                yield return new RpcResourceState
                {
                    Resource = "backend",
                    Type = "Project",
                    State = "Running",
                    Endpoints = new[] { "http://localhost:5001" },
                    Health = "Healthy"
                };
            }
        }
    }

    public async Task ConnectAsync(string socketPath, CancellationToken cancellationToken)
    {
        ConnectAsyncCalled?.SetResult();
        if (ConnectAsyncCallback !=  null)
        {
            await ConnectAsyncCallback.Invoke(socketPath, cancellationToken).ConfigureAwait(false);
        }
    }

    public async IAsyncEnumerable<(string Id, string StatusText, bool IsComplete, bool IsError)> GetPublishingActivitiesAsync([EnumeratorCancellation]CancellationToken cancellationToken)
    {
        GetPublishingActivitiesAsyncCalled?.SetResult();
        if (GetPublishingActivitiesAsyncCallback != null)
        {
            var publishingActivities = GetPublishingActivitiesAsyncCallback.Invoke(cancellationToken).ConfigureAwait(false);

            await foreach (var activity in publishingActivities)
            {
                yield return activity;
            }
        }
        else
        {
            yield return ("root-activity", "Publishing artifacts", false, false);
            yield return ("child-1", "Generating YAML goodness", false, false);
            yield return ("child-1", "Generating YAML goodness", true, false);
            yield return ("child-2", "Building image 1", false, false);
            yield return ("child-2", "Building image 1", true, false);
            yield return ("child-2", "Building image 2", false, false);
            yield return ("child-2", "Building image 2", true, false);
            yield return ("root-activity", "Publishing artifacts", true, false);
        }
    }

    public async Task<string[]> GetCapabilitiesAsync(CancellationToken cancellationToken)
    {
        GetCapabilitiesAsyncCalled?.SetResult();
        if (GetCapabilitiesAsyncCallback != null)
        {
            return await GetCapabilitiesAsyncCallback(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            return ["baseline.v2"];
        }
    }

    public async Task<RpcResourceInfo[]> GetResourcesAsync(CancellationToken cancellationToken)
    {
        GetResourcesAsyncCalled?.SetResult();
        if (GetResourcesAsyncCallback != null)
        {
            return await GetResourcesAsyncCallback(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            return Array.Empty<RpcResourceInfo>();
        }
    }

    public async IAsyncEnumerable<ResourceLogEntry> GetResourceLogsAsync(string resourceId, [EnumeratorCancellation]CancellationToken cancellationToken)
    {
        GetResourceLogsAsyncCalled?.SetResult();
        if (GetResourceLogsAsyncCallback != null)
        {
            var logEntries = GetResourceLogsAsyncCallback.Invoke(resourceId, cancellationToken);
            await foreach (var logEntry in logEntries.WithCancellation(cancellationToken))
            {
                yield return logEntry;
            }
        }
        else
        {
            // Return some test log entries
            yield return new ResourceLogEntry { Line = "Test log entry 1", Stream = LogEntryStream.StdOut };
            yield return new ResourceLogEntry { Line = "Test log entry 2", Stream = LogEntryStream.StdErr };
        }
}

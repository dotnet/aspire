// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Aspire.Cli.Backchannel;

namespace Aspire.Cli.Tests.TestServices;

internal sealed class TestAppHostBackchannel : IAppHostBackchannel
{
    public Func<long, long>? PingCallback { get; set; }
    public Action? RequestStopCallback { get; set; }
    public Func<(string, string?)>? GetDashboardUrlsCallback { get; set; }
    public Func<IAsyncEnumerable<(string, string, string, string[])>>? GetResourceStatesCallback { get; set; }
    public Action<Process, string>? ConnectCallback { get; set; }
    public Func<CancellationToken, string[]>? GetPublishersCallback { get; set; }
    public Func<IAsyncEnumerable<(string, string, bool, bool)>>? GetPublishingActivitiesCallback { get; set; }
    public Func<string[]>? GetCapabilitiesCallback { get; set; }

    public Task<long> PingAsync(long timestamp, CancellationToken cancellationToken)
    {
        return PingCallback != null
            ? Task.FromResult(PingCallback.Invoke(timestamp))
            : Task.FromResult(timestamp);
    }

    public Task RequestStopAsync(CancellationToken cancellationToken)
    {
        if (RequestStopCallback != null)
        {
            RequestStopCallback.Invoke();
            return Task.CompletedTask;
        }
        else
        {
            return Task.CompletedTask;
        }
    }

    public Task<(string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken)> GetDashboardUrlsAsync(CancellationToken cancellationToken)
    {
        return GetDashboardUrlsCallback != null
            ? Task.FromResult(GetDashboardUrlsCallback.Invoke())
            : Task.FromResult<(string, string?)>(("http://localhost:5000/login?t=abcd", "https://monalisa-hot-potato-vrpqrxxrx7x2rxx-5000.app.github.dev/login?t=abcd"));
    }

    public async IAsyncEnumerable<(string Resource, string Type, string State, string[] Endpoints)> GetResourceStatesAsync([EnumeratorCancellation]CancellationToken cancellationToken)
    {
        if (GetResourceStatesCallback != null)
        {
            var resourceStates = GetResourceStatesCallback.Invoke();
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
                yield return ("frontend", "Project", "Starting", new[] { "http://localhost:5000" });
                yield return ("backend", "Project", "Running", new[] { "http://localhost:5001" });
            }
        }
    }

    public Task ConnectAsync(Process process, string socketPath, CancellationToken cancellationToken)
    {
        ConnectCallback?.Invoke(process, socketPath);
        return Task.CompletedTask;
    }

    public Task<string[]> GetPublishersAsync(CancellationToken cancellationToken) =>
        Task.FromResult(GetPublishersCallback?.Invoke(cancellationToken) ?? ["manifest"]);

    public IAsyncEnumerable<(string Id, string StatusText, bool IsComplete, bool IsError)> GetPublishingActivitiesAsync(CancellationToken cancellationToken)
    {
        if (GetPublishingActivitiesCallback != null)
        {
            return GetPublishingActivitiesCallback.Invoke();
        }
        return DefaultPublishingActivitiesAsync();
    }

    private static IAsyncEnumerable<(string Id, string StatusText, bool IsComplete, bool IsError)> DefaultPublishingActivitiesAsync()
    {
        var rootId = "root-activity";
        var childActivities = new[]
        {
            (Id: "child-1", Status: "Generating YAML goodness", IsComplete: true, IsError: false),
            (Id: "child-2", Status: "Building image 1", IsComplete: true, IsError: false),
            (Id: "child-3", Status: "Building image 2", IsComplete: true, IsError: false)
        };

        return DefaultPublishingActivitiesAsyncImpl(rootId, childActivities);
    }

#pragma warning disable CS1998
    private static async IAsyncEnumerable<(string Id, string StatusText, bool IsComplete, bool IsError)> DefaultPublishingActivitiesAsyncImpl(string rootId, (string Id, string Status, bool IsComplete, bool IsError)[] childActivities)
    {
        yield return (rootId, "Publishing artifacts", false, false);
        foreach (var child in childActivities)
        {
            yield return (child.Id, child.Status, child.IsComplete, child.IsError);
        }
        yield return (rootId, "Publishing artifacts", true, false);
    }
#pragma warning restore CS1998

    public Task<string[]> GetCapabilitiesAsync(CancellationToken cancellationToken)
    {
        if (GetCapabilitiesCallback != null)
        {
            return Task.FromResult(GetCapabilitiesCallback.Invoke());
        }
        else
        {
            return Task.FromResult(new[] { "baseline.v0" });
        }
    }
}

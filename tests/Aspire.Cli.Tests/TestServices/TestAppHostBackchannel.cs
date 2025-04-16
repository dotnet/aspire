// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Aspire.Cli.Backchannel;

namespace Aspire.Cli.Tests.TestServices;

internal sealed class TestAppHostBackchannel : IAppHostBackchannel
{
    public Func<long, CancellationToken, long>? PingAsyncCallback { get; set; }
    public Action<CancellationToken>? RequestStopAsyncCallback { get; set; }
    public Func<CancellationToken, (string, string?)>? GetDashboardUrlsAsyncCallback { get; set; }
    public Func<CancellationToken, IAsyncEnumerable<(string, string, string, string[])>>? GetResourceStatesAsyncCallback { get; set; }
    public Action<Process, string, CancellationToken>? ConnectAsyncCallback { get; set; }
    public Func<CancellationToken, string[]>? GetPublishersAsyncCallback { get; set; }
    public Func<CancellationToken, IAsyncEnumerable<(string, string, bool, bool)>>? GetPublishingActivitiesAsyncCallback { get; set; }
    public Func<CancellationToken, string[]>? GetCapabilitiesAsyncCallback { get; set; }

    public Task<long> PingAsync(long timestamp, CancellationToken cancellationToken) =>
        Task.FromResult(PingAsyncCallback?.Invoke(timestamp, cancellationToken) ?? timestamp);

    public Task RequestStopAsync(CancellationToken cancellationToken)
    {
        RequestStopAsyncCallback?.Invoke(cancellationToken);
        return Task.CompletedTask;
    }

    public Task<(string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken)> GetDashboardUrlsAsync(CancellationToken cancellationToken) =>
        Task.FromResult(GetDashboardUrlsAsyncCallback?.Invoke(cancellationToken) ?? ("http://localhost", null));

    public IAsyncEnumerable<(string Resource, string Type, string State, string[] Endpoints)> GetResourceStatesAsync(CancellationToken cancellationToken)
    {
        if (GetResourceStatesAsyncCallback != null)
        {
            return GetResourceStatesAsyncCallback.Invoke(cancellationToken);
        }
        return DefaultResourceStatesAsync(cancellationToken);
    }

    private static IAsyncEnumerable<(string Resource, string Type, string State, string[] Endpoints)> DefaultResourceStatesAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        return DefaultResourceStatesAsyncImpl(timer, cancellationToken);
    }

    private static async IAsyncEnumerable<(string Resource, string Type, string State, string[] Endpoints)> DefaultResourceStatesAsyncImpl(PeriodicTimer timer, [EnumeratorCancellation]CancellationToken cancellationToken)
    {
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            yield return ("default-resource", "default-type", "default-state", Array.Empty<string>());
        }
    }

    public Task ConnectAsync(Process process, string socketPath, CancellationToken cancellationToken)
    {
        ConnectAsyncCallback?.Invoke(process, socketPath, cancellationToken);
        return Task.CompletedTask;
    }

    public Task<string[]> GetPublishersAsync(CancellationToken cancellationToken) =>
        Task.FromResult(GetPublishersAsyncCallback?.Invoke(cancellationToken) ?? Array.Empty<string>());

    public IAsyncEnumerable<(string Id, string StatusText, bool IsComplete, bool IsError)> GetPublishingActivitiesAsync(CancellationToken cancellationToken)
    {
        if (GetPublishingActivitiesAsyncCallback != null)
        {
            return GetPublishingActivitiesAsyncCallback.Invoke(cancellationToken);
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

    public Task<string[]> GetCapabilitiesAsync(CancellationToken cancellationToken) =>
        Task.FromResult(GetCapabilitiesAsyncCallback?.Invoke(cancellationToken) ?? new[] { "baseline.v0" });
}

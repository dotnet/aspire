// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Tests.Shared.DashboardModel;

namespace Aspire.Dashboard.Tests.TelemetryRepositoryTests;

public sealed class TestOutgoingPeerResolver : IOutgoingPeerResolver, IDisposable
{
    private readonly Func<KeyValuePair<string, string>[], (string? Name, ResourceViewModel? Resource)>? _onResolve;
    private readonly List<Func<Task>> _callbacks;

    public TestOutgoingPeerResolver(Func<KeyValuePair<string, string>[], (string? Name, ResourceViewModel? Resource)>? onResolve = null)
    {
        _onResolve = onResolve;
        _callbacks = new();
    }

    public void Dispose()
    {
    }

    public IDisposable OnPeerChanges(Func<Task> callback)
    {
        _callbacks.Add(callback);
        return this;
    }

    public async Task InvokePeerChanges()
    {
        foreach (var callback in _callbacks)
        {
            await callback();
        }
    }

    public bool TryResolvePeer(KeyValuePair<string, string>[] attributes, out string? name, out ResourceViewModel? matchedResourced)
    {
        if (_onResolve != null)
        {
            (name, matchedResourced) = _onResolve(attributes);
            return (name != null);
        }

        name = "TestPeer";
        matchedResourced = ModelTestHelpers.CreateResource(appName: "TestPeer");
        return true;
    }
}

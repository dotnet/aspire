// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aspire.Dashboard.Tests.Model;

public sealed class MetricPinStoreTests(ITestOutputHelper testOutputHelper) : IDisposable
{
    private static readonly OtlpInstrumentKey s_duration = new("Microsoft.AspNetCore.Hosting", "http.server.request.duration");
    private static readonly OtlpInstrumentKey s_heap = new("System.Runtime", "dotnet.gc.heap.total_allocated");
    private static readonly OtlpInstrumentKey s_cpu = new("System.Runtime", "dotnet.process.cpu.time");

    private readonly TemporaryWorkspace _workspace = TemporaryWorkspace.Create(testOutputHelper);

    [Fact]
    public void SetPinned_PersistsInPinnedOrderAcrossInstances()
    {
        var databasePath = Path.Combine(_workspace.Path, ResourceTagStore.DatabaseFileName);
        var store = new MetricPinStore(databasePath, NullLogger.Instance);

        Assert.True(store.SetPinned(s_heap, pinned: true));
        Assert.True(store.SetPinned(s_duration, pinned: true));
        Assert.True(store.SetPinned(s_cpu, pinned: true));
        Assert.True(store.SetPinned(s_duration, pinned: false));

        // A new instance reads the saved pins, as after restarting the dashboard.
        store = new MetricPinStore(databasePath, NullLogger.Instance);
        Assert.Equal([s_heap, s_cpu], store.GetPins());
        Assert.True(store.IsPinned(s_heap));
        Assert.False(store.IsPinned(s_duration));

        // Re-pinning moves the instrument to the end because pins are listed in the order they were pinned.
        Assert.True(store.SetPinned(s_duration, pinned: true));
        store = new MetricPinStore(databasePath, NullLogger.Instance);
        Assert.Equal([s_heap, s_cpu, s_duration], store.GetPins());
    }

    [Fact]
    public void SetPinned_RaisesChangedOnlyWhenPinsChange()
    {
        var store = new MetricPinStore(databasePath: null, NullLogger.Instance);
        var changedCount = 0;
        store.Changed += () => changedCount++;

        store.SetPinned(s_heap, pinned: true);
        store.SetPinned(s_heap, pinned: true);
        store.SetPinned(s_cpu, pinned: false);
        store.SetPinned(s_heap, pinned: false);

        Assert.Equal(2, changedCount);
        Assert.Empty(store.GetPins());
    }

    [Fact]
    public void SetPinned_UnreadableDatabase_LeavesPinsUnchanged()
    {
        // A directory where the database file should be makes opening it fail.
        var databasePath = Path.Combine(_workspace.Path, ResourceTagStore.DatabaseFileName);
        Directory.CreateDirectory(databasePath);
        var store = new MetricPinStore(databasePath, NullLogger.Instance);

        Assert.False(store.SetPinned(s_heap, pinned: true));
        Assert.Empty(store.GetPins());
    }

    public void Dispose() => _workspace.Dispose();
}

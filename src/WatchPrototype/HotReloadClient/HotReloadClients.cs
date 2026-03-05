// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.HotReload;

/// <summary>
/// Facilitates Hot Reload updates across multiple clients/processes.
/// </summary>
/// <param name="clients">
/// Clients that handle managed updates and static asset updates if <paramref name="useRefreshServerToApplyStaticAssets"/> is false.
/// </param>
/// <param name="browserRefreshServer">
/// Browser refresh server used to communicate managed code update status and errors to the browser,
/// and to apply static asset updates if <paramref name="useRefreshServerToApplyStaticAssets"/> is true.
/// </param>
/// <param name="useRefreshServerToApplyStaticAssets">
/// True to use <paramref name="browserRefreshServer"/> to apply static asset updates (if available).
/// False to use the <paramref name="clients"/> to apply static asset updates.
/// </param>
internal sealed class HotReloadClients(
    ImmutableArray<(HotReloadClient client, string name)> clients,
    AbstractBrowserRefreshServer? browserRefreshServer,
    bool useRefreshServerToApplyStaticAssets) : IDisposable
{
    /// <summary>
    /// Disposes all clients. Can occur unexpectedly whenever the process exits.
    /// </summary>
    public void Dispose()
    {
        foreach (var (client, _) in clients)
        {
            client.Dispose();
        }
    }

    /// <summary>
    /// True if Hot Reload is implemented via managed agents.
    /// The update itself might not be managed code update, it may be a static asset update implemented via a managed agent.
    /// </summary>
    public bool IsManagedAgentSupported
        => !clients.IsEmpty;

    public bool UseRefreshServerToApplyStaticAssets
        => useRefreshServerToApplyStaticAssets;

    public AbstractBrowserRefreshServer? BrowserRefreshServer
        => browserRefreshServer;

    /// <summary>
    /// Invoked when a rude edit is detected at runtime.
    /// May be invoked multiple times, by each client.
    /// </summary>
    public event Action<int, string> OnRuntimeRudeEdit
    {
        add
        {
            foreach (var (client, _) in clients)
            {
                client.OnRuntimeRudeEdit += value;
            }
        }
        remove
        {
            foreach (var (client, _) in clients)
            {
                client.OnRuntimeRudeEdit -= value;
            }
        }
    }

    internal void ConfigureLaunchEnvironment(IDictionary<string, string> environmentBuilder)
    {
        foreach (var (client, _) in clients)
        {
            client.ConfigureLaunchEnvironment(environmentBuilder);
        }

        browserRefreshServer?.ConfigureLaunchEnvironment(environmentBuilder, enableHotReload: true);
    }

    /// <param name="cancellationToken">Cancellation token. The cancellation should trigger on process terminatation.</param>
    internal void InitiateConnection(CancellationToken cancellationToken)
    {
        foreach (var (client, _) in clients)
        {
            client.InitiateConnection(cancellationToken);
        }
    }

    /// <param name="cancellationToken">Cancellation token. The cancellation should trigger on process terminatation.</param>
    internal async ValueTask WaitForConnectionEstablishedAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(clients.Select(c => c.client.WaitForConnectionEstablishedAsync(cancellationToken)));
    }

    /// <param name="cancellationToken">Cancellation token. The cancellation should trigger on process terminatation.</param>
    public async ValueTask<ImmutableArray<string>> GetUpdateCapabilitiesAsync(CancellationToken cancellationToken)
    {
        if (!IsManagedAgentSupported)
        {
            // empty capabilities will cause rude edit ENC0097: NotSupportedByRuntime.
            return [];
        }

        if (clients is [var (singleClient, _)])
        {
            return await singleClient.GetUpdateCapabilitiesAsync(cancellationToken);
        }

        var results = await Task.WhenAll(clients.Select(c => c.client.GetUpdateCapabilitiesAsync(cancellationToken)));

        // Allow updates that are supported by at least one process.
        // When applying changes we will filter updates applied to a specific process based on their required capabilities.
        return [.. results.SelectMany(r => r).Distinct(StringComparer.Ordinal).OrderBy(c => c)];
    }

    /// <param name="cancellationToken">Cancellation token. The cancellation should trigger on process terminatation.</param>
    public async Task<Task> ApplyManagedCodeUpdatesAsync(ImmutableArray<HotReloadManagedCodeUpdate> updates, CancellationToken applyOperationCancellationToken, CancellationToken cancellationToken)
    {
        // shouldn't be called if there are no clients
        Debug.Assert(IsManagedAgentSupported);

        // Apply to all processes.
        // The module the change is for does not need to be loaded to any of the processes, yet we still consider it successful if the application does not fail.
        // In each process we store the deltas for application when/if the module is loaded to the process later.
        // An error is only reported if the delta application fails, which would be a bug either in the runtime (applying valid delta incorrectly),
        // the compiler (producing wrong delta), or rude edit detection (the change shouldn't have been allowed).

        var applyTasks = await Task.WhenAll(clients.Select(c => c.client.ApplyManagedCodeUpdatesAsync(updates, applyOperationCancellationToken, cancellationToken)));

        return CompleteApplyOperationAsync();

        async Task CompleteApplyOperationAsync()
        {
            var results = await Task.WhenAll(applyTasks);
            if (browserRefreshServer != null && results.All(isSuccess => isSuccess))
            {
                await browserRefreshServer.RefreshBrowserAsync(cancellationToken);
            }
        }
    }

    /// <param name="cancellationToken">Cancellation token. The cancellation should trigger on process terminatation.</param>
    public async ValueTask InitialUpdatesAppliedAsync(CancellationToken cancellationToken)
    {
        // shouldn't be called if there are no clients
        Debug.Assert(IsManagedAgentSupported);

        if (clients is [var (singleClient, _)])
        {
            await singleClient.InitialUpdatesAppliedAsync(cancellationToken);
        }
        else
        {
            await Task.WhenAll(clients.Select(c => c.client.InitialUpdatesAppliedAsync(cancellationToken)));
        }
    }

    /// <param name="cancellationToken">Cancellation token. The cancellation should trigger on process terminatation.</param>
    public async Task<Task> ApplyStaticAssetUpdatesAsync(IEnumerable<StaticWebAsset> assets, CancellationToken applyOperationCancellationToken, CancellationToken cancellationToken)
    {
        if (useRefreshServerToApplyStaticAssets)
        {
            Debug.Assert(browserRefreshServer != null);
            return browserRefreshServer.UpdateStaticAssetsAsync(assets.Select(static a => a.RelativeUrl), applyOperationCancellationToken).AsTask();
        }

        // shouldn't be called if there are no clients
        Debug.Assert(IsManagedAgentSupported);

        var updates = new List<HotReloadStaticAssetUpdate>();

        foreach (var asset in assets)
        {
            try
            {
                updates.Add(await HotReloadStaticAssetUpdate.CreateAsync(asset, cancellationToken));
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                clients.First().client.Logger.LogError("Failed to read file {FilePath}: {Message}", asset.FilePath, e.Message);
                continue;
            }
        }

        return await ApplyStaticAssetUpdatesAsync([.. updates], applyOperationCancellationToken, cancellationToken);
    }

    /// <param name="cancellationToken">Cancellation token. The cancellation should trigger on process terminatation.</param>
    public async ValueTask<Task> ApplyStaticAssetUpdatesAsync(ImmutableArray<HotReloadStaticAssetUpdate> updates, CancellationToken applyOperationCancellationToken, CancellationToken cancellationToken)
    {
        // shouldn't be called if there are no clients
        Debug.Assert(IsManagedAgentSupported);
        Debug.Assert(!useRefreshServerToApplyStaticAssets);

        var applyTasks = await Task.WhenAll(clients.Select(c => c.client.ApplyStaticAssetUpdatesAsync(updates, applyOperationCancellationToken, cancellationToken)));

        return Task.WhenAll(applyTasks);
    }

    /// <param name="cancellationToken">Cancellation token. The cancellation should trigger on process terminatation.</param>
    public ValueTask ReportCompilationErrorsInApplicationAsync(ImmutableArray<string> compilationErrors, CancellationToken cancellationToken)
        => browserRefreshServer?.ReportCompilationErrorsInBrowserAsync(compilationErrors, cancellationToken) ?? ValueTask.CompletedTask;
}

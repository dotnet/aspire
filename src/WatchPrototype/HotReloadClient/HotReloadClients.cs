// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.HotReload;

internal sealed class HotReloadClients(ImmutableArray<(HotReloadClient client, string name)> clients, AbstractBrowserRefreshServer? browserRefreshServer) : IDisposable
{
    public HotReloadClients(HotReloadClient client, AbstractBrowserRefreshServer? browserRefreshServer)
        : this([(client, "")], browserRefreshServer)
    {
    }

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

    /// <summary>
    /// All clients share the same loggers.
    /// </summary>
    public ILogger ClientLogger
        => clients.First().client.Logger;

    /// <summary>
    /// All clients share the same loggers.
    /// </summary>
    public ILogger AgentLogger
        => clients.First().client.AgentLogger;

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
    /// <param name="isInitial">True if the updates are initial updates applied automatically when a process starts.</param>
    public async ValueTask ApplyManagedCodeUpdatesAsync(ImmutableArray<HotReloadManagedCodeUpdate> updates, bool isProcessSuspended, bool isInitial, CancellationToken cancellationToken)
    {
        var anyFailure = false;

        if (clients is [var (singleClient, _)])
        {
            anyFailure = await singleClient.ApplyManagedCodeUpdatesAsync(updates, isProcessSuspended, cancellationToken) == ApplyStatus.Failed;
        }
        else
        {
            // Apply to all processes.
            // The module the change is for does not need to be loaded to any of the processes, yet we still consider it successful if the application does not fail.
            // In each process we store the deltas for application when/if the module is loaded to the process later.
            // An error is only reported if the delta application fails, which would be a bug either in the runtime (applying valid delta incorrectly),
            // the compiler (producing wrong delta), or rude edit detection (the change shouldn't have been allowed).

            var results = await Task.WhenAll(clients.Select(c => c.client.ApplyManagedCodeUpdatesAsync(updates, isProcessSuspended, cancellationToken)));

            var index = 0;
            foreach (var status in results)
            {
                var (client, name) = clients[index++];

                switch (status)
                {
                    case ApplyStatus.Failed:
                        anyFailure = true;
                        break;

                    case ApplyStatus.AllChangesApplied:
                        break;

                    case ApplyStatus.SomeChangesApplied:
                        client.Logger.LogWarning("Some changes not applied to {Name} because they are not supported by the runtime.", name);
                        break;

                    case ApplyStatus.NoChangesApplied:
                        client.Logger.LogWarning("No changes applied to {Name} because they are not supported by the runtime.", name);
                        break;
                }
            }
        }

        if (!anyFailure)
        {
            // Only report status for updates made directly by the user, not for initial updates.
            if (!isInitial)
            {
                // all clients share the same loggers, pick any:
                var logger = clients[0].client.Logger;
                logger.Log(LogEvents.HotReloadSucceeded);
            }

            if (browserRefreshServer != null)
            {
                await browserRefreshServer.RefreshBrowserAsync(cancellationToken);
            }
        }
    }

    /// <param name="cancellationToken">Cancellation token. The cancellation should trigger on process terminatation.</param>
    public async ValueTask InitialUpdatesAppliedAsync(CancellationToken cancellationToken)
    {
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
    public async Task ApplyStaticAssetUpdatesAsync(IEnumerable<StaticWebAsset> assets, CancellationToken cancellationToken)
    {
        if (browserRefreshServer != null)
        {
            await browserRefreshServer.UpdateStaticAssetsAsync(assets.Select(static a => a.RelativeUrl), cancellationToken);
        }
        else
        {
            var updates = new List<HotReloadStaticAssetUpdate>();

            foreach (var asset in assets)
            {
                try
                {
                    ClientLogger.LogDebug("Loading asset '{Url}' from '{Path}'.", asset.RelativeUrl, asset.FilePath);
                    updates.Add(await HotReloadStaticAssetUpdate.CreateAsync(asset, cancellationToken));
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    ClientLogger.LogError("Failed to read file {FilePath}: {Message}", asset.FilePath, e.Message);
                    continue;
                }
            }

            await ApplyStaticAssetUpdatesAsync([.. updates], isProcessSuspended: false, cancellationToken);
        }
    }

    /// <param name="cancellationToken">Cancellation token. The cancellation should trigger on process terminatation.</param>
    public async ValueTask ApplyStaticAssetUpdatesAsync(ImmutableArray<HotReloadStaticAssetUpdate> updates, bool isProcessSuspended, CancellationToken cancellationToken)
    {
        if (clients is [var (singleClient, _)])
        {
            await singleClient.ApplyStaticAssetUpdatesAsync(updates, isProcessSuspended, cancellationToken);
        }
        else
        {
            await Task.WhenAll(clients.Select(c => c.client.ApplyStaticAssetUpdatesAsync(updates, isProcessSuspended, cancellationToken)));
        }
    }

    /// <param name="cancellationToken">Cancellation token. The cancellation should trigger on process terminatation.</param>
    public ValueTask ReportCompilationErrorsInApplicationAsync(ImmutableArray<string> compilationErrors, CancellationToken cancellationToken)
        => browserRefreshServer?.ReportCompilationErrorsInBrowserAsync(compilationErrors, cancellationToken) ?? ValueTask.CompletedTask;
}

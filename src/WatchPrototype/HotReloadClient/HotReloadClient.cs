// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.HotReload;

internal abstract class HotReloadClient(ILogger logger, ILogger agentLogger) : IDisposable
{
    /// <summary>
    /// List of modules that can't receive changes anymore.
    /// A module is added when a change is requested for it that is not supported by the runtime.
    /// </summary>
    private readonly HashSet<Guid> _frozenModules = [];

    public readonly ILogger Logger = logger;
    public readonly ILogger AgentLogger = agentLogger;

    private int _updateBatchId;

    /// <summary>
    /// Updates that were sent over to the agent while the process has been suspended.
    /// </summary>
    private readonly object _pendingUpdatesGate = new();
    private Task _pendingUpdates = Task.CompletedTask;

    /// <summary>
    /// Invoked when a rude edit is detected at runtime.
    /// </summary>
    public event Action<int, string>? OnRuntimeRudeEdit;

    // for testing
    internal Task PendingUpdates
        => _pendingUpdates;

    /// <summary>
    /// .NET Framework runtime does not support adding MethodImpl entries, therefore the capability is not in the baseline capability set.
    /// All other runtimes (.NET and Mono) support it and rather than servicing all of them we include the capability here.
    /// </summary>
    protected static ImmutableArray<string> AddImplicitCapabilities(IEnumerable<string> capabilities)
        => [.. capabilities, "AddExplicitInterfaceImplementation"];

    public abstract void ConfigureLaunchEnvironment(IDictionary<string, string> environmentBuilder);

    /// <summary>
    /// Initiates connection with the agent in the target process.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token. The cancellation should trigger on process terminatation.</param>
    public abstract void InitiateConnection(CancellationToken cancellationToken);

    /// <summary>
    /// Waits until the connection with the agent is established.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token. The cancellation should trigger on process terminatation.</param>
    public abstract Task WaitForConnectionEstablishedAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns update capabilities of the target process.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token. The cancellation should trigger on process terminatation.</param>
    public abstract Task<ImmutableArray<string>> GetUpdateCapabilitiesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Applies managed code updates to the target process.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token. The cancellation should trigger on process terminatation.</param>
    public abstract Task<ApplyStatus> ApplyManagedCodeUpdatesAsync(ImmutableArray<HotReloadManagedCodeUpdate> updates, bool isProcessSuspended, CancellationToken cancellationToken);

    /// <summary>
    /// Applies static asset updates to the target process.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token. The cancellation should trigger on process terminatation.</param>
    public abstract Task<ApplyStatus> ApplyStaticAssetUpdatesAsync(ImmutableArray<HotReloadStaticAssetUpdate> updates, bool isProcessSuspended, CancellationToken cancellationToken);

    /// <summary>
    /// Notifies the agent that the initial set of updates has been applied and the user code in the process can start executing.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token. The cancellation should trigger on process terminatation.</param>
    public abstract Task InitialUpdatesAppliedAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Disposes the client. Can occur unexpectedly whenever the process exits.
    /// </summary>
    public abstract void Dispose();

    protected void RuntimeRudeEditDetected(int errorCode, string message)
        => OnRuntimeRudeEdit?.Invoke(errorCode, message);

    public static void ReportLogEntry(ILogger logger, string message, AgentMessageSeverity severity)
    {
        var level = severity switch
        {
            AgentMessageSeverity.Error => LogLevel.Error,
            AgentMessageSeverity.Warning => LogLevel.Warning,
            _ => LogLevel.Debug
        };

        logger.Log(level, message);
    }

    protected async Task<IReadOnlyList<HotReloadManagedCodeUpdate>> FilterApplicableUpdatesAsync(ImmutableArray<HotReloadManagedCodeUpdate> updates, CancellationToken cancellationToken)
    {
        var availableCapabilities = await GetUpdateCapabilitiesAsync(cancellationToken);
        var applicableUpdates = new List<HotReloadManagedCodeUpdate>();

        foreach (var update in updates)
        {
            if (_frozenModules.Contains(update.ModuleId))
            {
                // can't update frozen module:
                continue;
            }

            if (update.RequiredCapabilities.Except(availableCapabilities).Any())
            {
                // required capability not available:
                _frozenModules.Add(update.ModuleId);
            }
            else
            {
                applicableUpdates.Add(update);
            }
        }

        return applicableUpdates;
    }

    protected async ValueTask<TResult> SendAndReceiveUpdateAsync<TResult>(
        Func<int, CancellationToken, ValueTask<TResult>> send,
        bool isProcessSuspended,
        TResult suspendedResult,
        CancellationToken cancellationToken)
        where TResult : struct
    {
        var batchId = _updateBatchId++;

        Task previous;
        lock (_pendingUpdatesGate)
        {
            previous = _pendingUpdates;

            if (isProcessSuspended)
            {
                _pendingUpdates = Task.Run(async () =>
                {
                    await previous;
                    _ = await send(batchId, cancellationToken);
                }, cancellationToken);

                return suspendedResult;
            }
        }

        await previous;
        return await send(batchId, cancellationToken);
    }
}

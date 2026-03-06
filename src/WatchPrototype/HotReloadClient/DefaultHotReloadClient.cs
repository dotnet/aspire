// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.HotReload
{
    internal sealed class DefaultHotReloadClient(ILogger logger, ILogger agentLogger, string startupHookPath, bool handlesStaticAssetUpdates, ClientTransport transport)
        : HotReloadClient(logger, agentLogger)
    {
        private Task<ImmutableArray<string>>? _capabilitiesTask;
        private bool _managedCodeUpdateFailedOrCancelled;

        // The status of the last update response.
        private TaskCompletionSource<bool> _updateStatusSource = new();

        public override void Dispose()
        {
            transport.Dispose();
        }

        /// <summary>
        /// The transport used for communication with the agent, for testing.
        /// </summary>
        internal ClientTransport Transport => transport;

        public override void InitiateConnection(CancellationToken cancellationToken)
        {
            // It is important to establish the connection (WaitForConnectionAsync) before we return,
            // otherwise the client wouldn't be able to connect.
            // However, we don't want to wait for the task to complete, so that we can start the client process.
            _capabilitiesTask = ConnectAsync();

            async Task<ImmutableArray<string>> ConnectAsync()
            {
                try
                {
                    await transport.WaitForConnectionAsync(cancellationToken);

                    // Read the initialization response (capabilities) from the agent.
                    var initResponse = await transport.ReadAsync(cancellationToken);
                    if (initResponse == null)
                    {
                        return [];
                    }

                    using var r = initResponse.Value;
                    if (r.Type != ResponseType.InitializationResponse)
                    {
                        Logger.LogError("Expected initialization response, got: {ResponseType}", r.Type);
                        return [];
                    }

                    var capabilities = (await ClientInitializationResponse.ReadAsync(r.Data, cancellationToken)).Capabilities;

                    if (string.IsNullOrEmpty(capabilities))
                    {
                        return [];
                    }

                    var result = AddImplicitCapabilities(capabilities.Split(' '));

                    Logger.Log(LogEvents.Capabilities, string.Join(" ", result));

                    // fire and forget:
                    _ = ListenForResponsesAsync(cancellationToken);

                    return result;
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    // Don't report a warning when cancelled. The process has terminated or the host is shutting down in that case.
                    // Best effort: There is an inherent race condition due to time between the process exiting and the cancellation token triggering.
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        Logger.LogError("Failed to read capabilities: {Message}", e.Message);
                    }

                    return [];
                }
            }
        }

        private async Task ListenForResponsesAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var response = await transport.ReadAsync(cancellationToken);
                    if (response == null)
                    {
                        return;
                    }

                    using var r = response.Value;

                    switch (r.Type)
                    {
                        case ResponseType.UpdateResponse:
                            // update request can't be issued again until the status is read and a new source is created:
                            _updateStatusSource.SetResult(await ReadUpdateResponseAsync(r, cancellationToken));
                            break;

                        case ResponseType.HotReloadExceptionNotification:
                            var notification = await HotReloadExceptionCreatedNotification.ReadAsync(r.Data, cancellationToken);
                            RuntimeRudeEditDetected(notification.Code, notification.Message);
                            break;

                        default:
                            // can't continue, the stream is in undefined state:
                            Logger.LogError("Unexpected response received from the agent: {ResponseType}", r.Type);
                            return;
                    }
                }
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    Logger.LogError("Failed to read response: {Exception}", e.ToString());
                }
            }
        }

        [MemberNotNull(nameof(_capabilitiesTask))]
        private Task<ImmutableArray<string>> GetCapabilitiesTask()
            => _capabilitiesTask ?? throw new InvalidOperationException();

        [MemberNotNull(nameof(_capabilitiesTask))]
        private void RequireReadyForUpdates()
        {
            // should only be called after connection has been created:
            _ = GetCapabilitiesTask();
        }

        public override void ConfigureLaunchEnvironment(IDictionary<string, string> environmentBuilder)
        {
            environmentBuilder[AgentEnvironmentVariables.DotNetModifiableAssemblies] = "debug";

            // HotReload startup hook should be loaded before any other startup hooks:
            environmentBuilder.InsertListItem(AgentEnvironmentVariables.DotNetStartupHooks, startupHookPath, Path.PathSeparator);

            transport.ConfigureEnvironment(environmentBuilder);
        }

        public override Task WaitForConnectionEstablishedAsync(CancellationToken cancellationToken)
            => GetCapabilitiesTask();

        public override Task<ImmutableArray<string>> GetUpdateCapabilitiesAsync(CancellationToken cancellationToken)
            => GetCapabilitiesTask();

        private ResponseLoggingLevel ResponseLoggingLevel
            => Logger.IsEnabled(LogLevel.Debug) ? ResponseLoggingLevel.Verbose : ResponseLoggingLevel.WarningsAndErrors;

        public async override Task<Task<bool>> ApplyManagedCodeUpdatesAsync(ImmutableArray<HotReloadManagedCodeUpdate> updates, CancellationToken applyOperationCancellationToken, CancellationToken cancellationToken)
        {
            RequireReadyForUpdates();

            if (_managedCodeUpdateFailedOrCancelled)
            {
                Logger.LogDebug("Previous changes failed to apply. Further changes are not applied to this process.");
                return Task.FromResult(false);
            }

            var applicableUpdates = await FilterApplicableUpdatesAsync(updates, cancellationToken);
            if (applicableUpdates.Count == 0)
            {
                Logger.LogDebug("No updates applicable to this process");
                return Task.FromResult(true);
            }

            var request = new ManagedCodeUpdateRequest(ToRuntimeUpdates(applicableUpdates), ResponseLoggingLevel);

            // Only cancel apply operation when the process exits:
            var updateCompletionTask = QueueUpdateBatchRequest(request, applyOperationCancellationToken);

            return CompleteApplyOperationAsync();

            async Task<bool> CompleteApplyOperationAsync()
            {
                if (await updateCompletionTask)
                {
                    return true;
                }

                Logger.LogWarning("Further changes won't be applied to this process.");
                _managedCodeUpdateFailedOrCancelled = true;
                transport.Dispose();

                return false;
            }

            static ImmutableArray<RuntimeManagedCodeUpdate> ToRuntimeUpdates(IEnumerable<HotReloadManagedCodeUpdate> updates)
                => [.. updates.Select(static update => new RuntimeManagedCodeUpdate(update.ModuleId,
                   ImmutableCollectionsMarshal.AsArray(update.MetadataDelta)!,
                   ImmutableCollectionsMarshal.AsArray(update.ILDelta)!,
                   ImmutableCollectionsMarshal.AsArray(update.PdbDelta)!,
                   ImmutableCollectionsMarshal.AsArray(update.UpdatedTypes)!))];
        }

        public override async Task<Task<bool>> ApplyStaticAssetUpdatesAsync(ImmutableArray<HotReloadStaticAssetUpdate> updates, CancellationToken processExitedCancellationToken, CancellationToken cancellationToken)
        {
            if (!handlesStaticAssetUpdates)
            {
                // The client has no concept of static assets.
                return Task.FromResult(true);
            }

            RequireReadyForUpdates();

            var completionTasks = updates.Select(update =>
            {
                var request = new StaticAssetUpdateRequest(
                    new RuntimeStaticAssetUpdate(
                        update.AssemblyName,
                        update.RelativePath,
                        ImmutableCollectionsMarshal.AsArray(update.Content)!,
                        update.IsApplicationProject),
                    ResponseLoggingLevel);

                Logger.LogDebug("Sending static file update request for asset '{Url}'.", update.RelativePath);

                // Only cancel apply operation when the process exits:
                return QueueUpdateBatchRequest(request, processExitedCancellationToken);
            });

            return CompleteApplyOperationAsync();

            async Task<bool> CompleteApplyOperationAsync()
            {
                var results = await Task.WhenAll(completionTasks);
                return results.All(isSuccess => isSuccess);
            }
        }

        private Task<bool> QueueUpdateBatchRequest<TRequest>(TRequest request, CancellationToken applyOperationCancellationToken)
            where TRequest : IUpdateRequest
        {
            return QueueUpdateBatch(
                sendAndReceive: async batchId =>
                {
                    await transport.WriteAsync((byte)request.Type, request.WriteAsync, applyOperationCancellationToken);

                    var success = await ReceiveUpdateResponseAsync(applyOperationCancellationToken);
                    Logger.Log(success ? LogEvents.UpdateBatchCompleted : LogEvents.UpdateBatchFailed, batchId);
                    return success;
                },
                applyOperationCancellationToken);
        }

        private async ValueTask<bool> ReceiveUpdateResponseAsync(CancellationToken cancellationToken)
        {
            var result = await _updateStatusSource.Task;
            _updateStatusSource = new TaskCompletionSource<bool>();
            return result;
        }

        private async ValueTask<bool> ReadUpdateResponseAsync(ClientTransportResponse r, CancellationToken cancellationToken)
        {
            var (success, log) = await UpdateResponse.ReadAsync(r.Data, cancellationToken);

            await foreach (var (message, severity) in log)
            {
                ReportLogEntry(AgentLogger, message, severity);
            }

            return success;
        }

        public override async Task InitialUpdatesAppliedAsync(CancellationToken cancellationToken)
        {
            RequireReadyForUpdates();

            if (_managedCodeUpdateFailedOrCancelled)
            {
                return;
            }

            try
            {
                await transport.WriteAsync((byte)RequestType.InitialUpdatesCompleted, writePayload: null, cancellationToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                // Transport might throw another exception when forcibly closed on process termination.
                // Don't report an error when cancelled. The process has terminated or the host is shutting down in that case.
                // Best effort: There is an inherent race condition due to time between the process exiting and the cancellation token triggering.
                if (!cancellationToken.IsCancellationRequested)
                {
                    Logger.LogError("Failed to send {RequestType}: {Message}", nameof(RequestType.InitialUpdatesCompleted), e.Message);
                }
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.HotReload
{
    internal sealed class DefaultHotReloadClient(ILogger logger, ILogger agentLogger, string startupHookPath, bool enableStaticAssetUpdates)
        : HotReloadClient(logger, agentLogger)
    {
        private readonly string _namedPipeName = Guid.NewGuid().ToString("N");

        private Task<ImmutableArray<string>>? _capabilitiesTask;
        private NamedPipeServerStream? _pipe;
        private bool _managedCodeUpdateFailedOrCancelled;

        // The status of the last update response.
        private TaskCompletionSource<bool> _updateStatusSource = new();

        public override void Dispose()
        {
            DisposePipe();
        }

        private void DisposePipe()
        {
            if (_pipe != null)
            {
                Logger.LogDebug("Disposing agent communication pipe");

                // Dispose the pipe but do not set it to null, so that any in-progress 
                // operations throw the appropriate exception type.
                _pipe.Dispose();
            }
        }

        // for testing
        internal string NamedPipeName
            => _namedPipeName;

        public override void InitiateConnection(CancellationToken cancellationToken)
        {
#if NET
            var options = PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly;
#else
            var options = PipeOptions.Asynchronous;
#endif
            _pipe = new NamedPipeServerStream(_namedPipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, options);

            // It is important to establish the connection (WaitForConnectionAsync) before we return,
            // otherwise the client wouldn't be able to connect.
            // However, we don't want to wait for the task to complete, so that we can start the client process.
            _capabilitiesTask = ConnectAsync();

            async Task<ImmutableArray<string>> ConnectAsync()
            {
                try
                {
                    Logger.LogDebug("Waiting for application to connect to pipe {NamedPipeName}.", _namedPipeName);

                    await _pipe.WaitForConnectionAsync(cancellationToken);

                    // When the client connects, the first payload it sends is the initialization payload which includes the apply capabilities.

                    var capabilities = (await ClientInitializationResponse.ReadAsync(_pipe, cancellationToken)).Capabilities;

                    var result = AddImplicitCapabilities(capabilities.Split(' '));

                    Logger.Log(LogEvents.Capabilities, string.Join(" ", result));

                    // fire and forget:
                    _ = ListenForResponsesAsync(cancellationToken);

                    return result;
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    ReportPipeReadException(e, "capabilities", cancellationToken);
                    return [];
                }
            }
        }

        private void ReportPipeReadException(Exception e, string responseType, CancellationToken cancellationToken)
        {
            // Don't report a warning when cancelled or the pipe has been disposed. The process has terminated or the host is shutting down in that case.
            // Best effort: There is an inherent race condition due to time between the process exiting and the cancellation token triggering.
            if (e is ObjectDisposedException or EndOfStreamException || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            Logger.LogError("Failed to read {ResponseType} from the pipe: {Message}", responseType, e.Message);
        }

        private async Task ListenForResponsesAsync(CancellationToken cancellationToken)
        {
            Debug.Assert(_pipe != null);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var type = (ResponseType)await _pipe.ReadByteAsync(cancellationToken);

                    switch (type)
                    {
                        case ResponseType.UpdateResponse:
                            // update request can't be issued again until the status is read and a new source is created:
                            _updateStatusSource.SetResult(await ReadUpdateResponseAsync(cancellationToken));
                            break;

                        case ResponseType.HotReloadExceptionNotification:
                            var notification = await HotReloadExceptionCreatedNotification.ReadAsync(_pipe, cancellationToken);
                            RuntimeRudeEditDetected(notification.Code, notification.Message);
                            break;

                        default:
                            // can't continue, the pipe is in undefined state:
                            Logger.LogError("Unexpected response received from the agent: {ResponseType}", type);
                            return;
                    }
                }
            }
            catch (Exception e)
            {
                ReportPipeReadException(e, "response", cancellationToken);
            }
        }

        [MemberNotNull(nameof(_capabilitiesTask))]
        private Task<ImmutableArray<string>> GetCapabilitiesTask()
            => _capabilitiesTask ?? throw new InvalidOperationException();

        [MemberNotNull(nameof(_pipe))]
        [MemberNotNull(nameof(_capabilitiesTask))]
        private void RequireReadyForUpdates()
        {
            // should only be called after connection has been created:
            _ = GetCapabilitiesTask();

            Debug.Assert(_pipe != null);
        }

        public override void ConfigureLaunchEnvironment(IDictionary<string, string> environmentBuilder)
        {
            environmentBuilder[AgentEnvironmentVariables.DotNetModifiableAssemblies] = "debug";

            // HotReload startup hook should be loaded before any other startup hooks:
            environmentBuilder.InsertListItem(AgentEnvironmentVariables.DotNetStartupHooks, startupHookPath, Path.PathSeparator);

            environmentBuilder[AgentEnvironmentVariables.DotNetWatchHotReloadNamedPipeName] = _namedPipeName;
        }

        public override Task WaitForConnectionEstablishedAsync(CancellationToken cancellationToken)
            => GetCapabilitiesTask();

        public override Task<ImmutableArray<string>> GetUpdateCapabilitiesAsync(CancellationToken cancellationToken)
            => GetCapabilitiesTask();

        private ResponseLoggingLevel ResponseLoggingLevel
            => Logger.IsEnabled(LogLevel.Debug) ? ResponseLoggingLevel.Verbose : ResponseLoggingLevel.WarningsAndErrors;

        public override async Task<ApplyStatus> ApplyManagedCodeUpdatesAsync(ImmutableArray<HotReloadManagedCodeUpdate> updates, bool isProcessSuspended, CancellationToken cancellationToken)
        {
            RequireReadyForUpdates();

            if (_managedCodeUpdateFailedOrCancelled)
            {
                Logger.LogDebug("Previous changes failed to apply. Further changes are not applied to this process.");
                return ApplyStatus.Failed;
            }

            var applicableUpdates = await FilterApplicableUpdatesAsync(updates, cancellationToken);
            if (applicableUpdates.Count == 0)
            {
                Logger.LogDebug("No updates applicable to this process");
                return ApplyStatus.NoChangesApplied;
            }

            var request = new ManagedCodeUpdateRequest(ToRuntimeUpdates(applicableUpdates), ResponseLoggingLevel);

            var success = false;
            try
            {
                success = await SendAndReceiveUpdateAsync(request, isProcessSuspended, cancellationToken);
            }
            finally
            {
                if (!success)
                {
                    // Don't report a warning when cancelled. The process has terminated or the host is shutting down in that case.
                    // Best effort: There is an inherent race condition due to time between the process exiting and the cancellation token triggering.
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        Logger.LogWarning("Further changes won't be applied to this process.");
                    }

                    _managedCodeUpdateFailedOrCancelled = true;
                    DisposePipe();
                }
            }

            if (success)
            {
                Logger.Log(LogEvents.UpdatesApplied, applicableUpdates.Count, updates.Length);
            }

            return
                !success ? ApplyStatus.Failed :
                (applicableUpdates.Count < updates.Length) ? ApplyStatus.SomeChangesApplied : ApplyStatus.AllChangesApplied;

            static ImmutableArray<RuntimeManagedCodeUpdate> ToRuntimeUpdates(IEnumerable<HotReloadManagedCodeUpdate> updates)
                => [.. updates.Select(static update => new RuntimeManagedCodeUpdate(update.ModuleId,
                   ImmutableCollectionsMarshal.AsArray(update.MetadataDelta)!,
                   ImmutableCollectionsMarshal.AsArray(update.ILDelta)!,
                   ImmutableCollectionsMarshal.AsArray(update.PdbDelta)!,
                   ImmutableCollectionsMarshal.AsArray(update.UpdatedTypes)!))];
        }

        public async override Task<ApplyStatus> ApplyStaticAssetUpdatesAsync(ImmutableArray<HotReloadStaticAssetUpdate> updates, bool isProcessSuspended, CancellationToken cancellationToken)
        {
            if (!enableStaticAssetUpdates)
            {
                // The client has no concept of static assets.
                return ApplyStatus.AllChangesApplied;
            }

            RequireReadyForUpdates();

            var appliedUpdateCount = 0;

            foreach (var update in updates)
            {
                var request = new StaticAssetUpdateRequest(
                    new RuntimeStaticAssetUpdate(
                        update.AssemblyName,
                        update.RelativePath,
                        ImmutableCollectionsMarshal.AsArray(update.Content)!,
                        update.IsApplicationProject),
                    ResponseLoggingLevel);

                Logger.LogDebug("Sending static file update request for asset '{Url}'.", update.RelativePath);

                var success = await SendAndReceiveUpdateAsync(request, isProcessSuspended, cancellationToken);
                if (success)
                {
                    appliedUpdateCount++;
                }
            }

            Logger.Log(LogEvents.UpdatesApplied, appliedUpdateCount, updates.Length);

            return
                (appliedUpdateCount == 0) ? ApplyStatus.Failed :
                (appliedUpdateCount < updates.Length) ? ApplyStatus.SomeChangesApplied : ApplyStatus.AllChangesApplied;
        }

        private ValueTask<bool> SendAndReceiveUpdateAsync<TRequest>(TRequest request, bool isProcessSuspended, CancellationToken cancellationToken)
            where TRequest : IUpdateRequest
        {
            // Should not initialized:
            Debug.Assert(_pipe != null);

            return SendAndReceiveUpdateAsync(
                send: SendAndReceiveAsync,
                isProcessSuspended,
                suspendedResult: true,
                cancellationToken);

            async ValueTask<bool> SendAndReceiveAsync(int batchId, CancellationToken cancellationToken)
            {
                Logger.LogDebug("Sending update batch #{UpdateId}", batchId);

                try
                {
                    await WriteRequestAsync(cancellationToken);

                    if (await ReceiveUpdateResponseAsync(cancellationToken))
                    {
                        Logger.LogDebug("Update batch #{UpdateId} completed.", batchId);
                        return true;
                    }

                    Logger.LogDebug("Update batch #{UpdateId} failed.", batchId);
                }
                catch (Exception e)
                {
                    // Don't report an error when cancelled. The process has terminated or the host is shutting down in that case.
                    // Best effort: There is an inherent race condition due to time between the process exiting and the cancellation token triggering.
                    if (cancellationToken.IsCancellationRequested)
                    {
                        Logger.LogDebug("Update batch #{UpdateId} canceled.", batchId);
                    }
                    else
                    {
                        Logger.LogError("Update batch #{UpdateId} failed with error: {Message}", batchId, e.Message);
                        Logger.LogDebug("Update batch #{UpdateId} exception stack trace: {StackTrace}", batchId, e.StackTrace);
                    }
                }

                return false;
            }

            async ValueTask WriteRequestAsync(CancellationToken cancellationToken)
            {
                await _pipe.WriteAsync((byte)request.Type, cancellationToken);
                await request.WriteAsync(_pipe, cancellationToken);
                await _pipe.FlushAsync(cancellationToken);
            }
        }

        private async ValueTask<bool> ReceiveUpdateResponseAsync(CancellationToken cancellationToken)
        {
            var result = await _updateStatusSource.Task;
            _updateStatusSource = new TaskCompletionSource<bool>();
            return result;
        }

        private async ValueTask<bool> ReadUpdateResponseAsync(CancellationToken cancellationToken)
        {
            // Should be initialized:
            Debug.Assert(_pipe != null);

            var (success, log) = await UpdateResponse.ReadAsync(_pipe, cancellationToken);

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
                await _pipe.WriteAsync((byte)RequestType.InitialUpdatesCompleted, cancellationToken);
                await _pipe.FlushAsync(cancellationToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                // Pipe might throw another exception when forcibly closed on process termination.
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

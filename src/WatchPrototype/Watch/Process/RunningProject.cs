// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.Build.Graph;
using Microsoft.DotNet.HotReload;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch
{
    internal delegate ValueTask RestartOperation(CancellationToken cancellationToken);

    internal sealed class RunningProject(
        ProjectGraphNode projectNode,
        ProjectOptions options,
        HotReloadClients clients,
        ILogger clientLogger,
        RunningProcess process,
        RestartOperation restartOperation,
        ImmutableArray<string> managedCodeUpdateCapabilities) : IAsyncDisposable
    {
        private volatile int _isRestarting;

        public ProjectGraphNode ProjectNode => projectNode;
        public ProjectOptions Options => options;
        public HotReloadClients Clients => clients;
        public ILogger ClientLogger => clientLogger;
        public ImmutableArray<string> ManagedCodeUpdateCapabilities => managedCodeUpdateCapabilities;
        public RunningProcess Process => process;

        /// <summary>
        /// Set to true when the process termination is being requested so that it can be auto-restarted.
        /// </summary>
        public bool IsRestarting => _isRestarting != 0;

        /// <summary>
        /// Disposes the project. Can occur unexpectedly whenever the process exits.
        /// Must only be called once per project.
        /// </summary>
        /// <param name="isExiting">When invoked in <see cref="ProcessSpec.OnExit"/> handler.</param>
        public async ValueTask DisposeAsync(bool isExiting)
        {
            // disposes communication channels:
            clients.Dispose();

            await process.DisposeAsync(isExiting);
        }

        ValueTask IAsyncDisposable.DisposeAsync()
            => DisposeAsync(isExiting: false);

        /// <summary>
        /// Marks the <see cref="RunningProject"/> as restarting.
        /// Subsequent process termination will be treated as a restart.
        /// </summary>
        /// <returns>True if the project hasn't been int restarting state prior the call.</returns>
        public bool InitiateRestart()
            => Interlocked.Exchange(ref _isRestarting, 1) == 0;

        /// <summary>
        /// Terminates the process in preparation for a restart.
        /// </summary>
        public Task TerminateForRestartAsync()
        {
            InitiateRestart();
            return process.TerminateAsync();
        }

        public async Task CompleteApplyOperationAsync(Task applyTask)
        {
            try
            {
                await applyTask;
            }
            catch (OperationCanceledException)
            {
                // Do not report error.
            }
            catch (Exception e)
            {
                // Handle all exceptions. If one process is terminated or fails to apply changes
                // it shouldn't prevent applying updates to other processes.

                ClientLogger.LogError("Failed to apply updates to process {Process}: {Exception}", process.Id, e.ToString());
            }
        }

        /// <summary>
        /// Triggers restart operation.
        /// </summary>
        public async ValueTask RestartAsync(CancellationToken cancellationToken)
        {
            ClientLogger.Log(MessageDescriptor.ProjectRestarting);
            await restartOperation(cancellationToken);
            ClientLogger.Log(MessageDescriptor.ProjectRestarted);
        }

        public RestartOperation GetRelaunchOperation()
            => new(async cancellationToken =>
            {
                ClientLogger.Log(MessageDescriptor.ProjectRelaunching);
                await restartOperation(cancellationToken);
                ClientLogger.Log(MessageDescriptor.ProjectRelaunched);
            });
    }
}

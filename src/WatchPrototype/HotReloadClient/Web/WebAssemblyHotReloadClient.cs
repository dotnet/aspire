// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.HotReload
{
    internal sealed class WebAssemblyHotReloadClient(
        ILogger logger,
        ILogger agentLogger,
        AbstractBrowserRefreshServer browserRefreshServer,
        ImmutableArray<string> projectHotReloadCapabilities,
        Version projectTargetFrameworkVersion,
        bool suppressBrowserRequestsForTesting)
        : HotReloadClient(logger, agentLogger)
    {
        private static readonly ImmutableArray<string> s_defaultCapabilities60 =
            ["Baseline"];

        private static readonly ImmutableArray<string> s_defaultCapabilities70 =
            ["Baseline", "AddMethodToExistingType", "AddStaticFieldToExistingType", "NewTypeDefinition", "ChangeCustomAttributes"];

        private static readonly ImmutableArray<string> s_defaultCapabilities80 =
            ["Baseline", "AddMethodToExistingType", "AddStaticFieldToExistingType", "NewTypeDefinition", "ChangeCustomAttributes",
             "AddInstanceFieldToExistingType", "GenericAddMethodToExistingType", "GenericUpdateMethod", "UpdateParameters", "GenericAddFieldToExistingType"];

        private static readonly ImmutableArray<string> s_defaultCapabilities90 =
            s_defaultCapabilities80;

        private readonly ImmutableArray<string> _capabilities = GetUpdateCapabilities(logger, projectHotReloadCapabilities, projectTargetFrameworkVersion);

        private static ImmutableArray<string> GetUpdateCapabilities(ILogger logger, ImmutableArray<string> projectHotReloadCapabilities, Version projectTargetFrameworkVersion)
        {
            var capabilities = projectHotReloadCapabilities.IsEmpty
                ? projectTargetFrameworkVersion.Major switch
                {
                    9 => s_defaultCapabilities90,
                    8 => s_defaultCapabilities80,
                    7 => s_defaultCapabilities70,
                    6 => s_defaultCapabilities60,
                    _ => [],
                }
                : projectHotReloadCapabilities;

            if (capabilities is not [])
            {
                capabilities = AddImplicitCapabilities(capabilities);
            }

            var capabilitiesStr = string.Join(", ", capabilities);
            if (projectHotReloadCapabilities.IsEmpty)
            {
                logger.LogDebug("Project specifies capabilities: {Capabilities}.", capabilitiesStr);
            }
            else
            {
                logger.LogDebug("Using capabilities based on project target framework version: '{Version}': {Capabilities}.", projectTargetFrameworkVersion, capabilitiesStr);
            }

            return capabilities;
        }

        public override void Dispose()
        {
            // Do nothing.
        }

        public override void ConfigureLaunchEnvironment(IDictionary<string, string> environmentBuilder)
        {
            // the environment is configued via browser refesh server
        }

        public override void InitiateConnection(CancellationToken cancellationToken)
        {
        }

        public override async Task WaitForConnectionEstablishedAsync(CancellationToken cancellationToken)
            // Wait for the browser connection to be established. Currently we need the browser to be running in order to apply changes.
            => await browserRefreshServer.WaitForClientConnectionAsync(cancellationToken);

        public override Task<ImmutableArray<string>> GetUpdateCapabilitiesAsync(CancellationToken cancellationToken)
            => Task.FromResult(_capabilities);

        public override async Task<Task<bool>> ApplyManagedCodeUpdatesAsync(ImmutableArray<HotReloadManagedCodeUpdate> updates, CancellationToken applyOperationCancellationToken, CancellationToken cancellationToken)
        {
            var applicableUpdates = await FilterApplicableUpdatesAsync(updates, cancellationToken);
            if (applicableUpdates.Count == 0)
            {
                return Task.FromResult(true);
            }

            // When testing abstract away the browser and pretend all changes have been applied:
            if (suppressBrowserRequestsForTesting)
            {
                return Task.FromResult(true);
            }

            // Make sure to send the same update to all browsers, the only difference is the shared secret.
            var deltas = updates.Select(static update => new JsonDelta
            {
                ModuleId = update.ModuleId,
                MetadataDelta = ImmutableCollectionsMarshal.AsArray(update.MetadataDelta)!,
                ILDelta = ImmutableCollectionsMarshal.AsArray(update.ILDelta)!,
                PdbDelta = ImmutableCollectionsMarshal.AsArray(update.PdbDelta)!,
                UpdatedTypes = ImmutableCollectionsMarshal.AsArray(update.UpdatedTypes)!,
            }).ToArray();

            var loggingLevel = Logger.IsEnabled(LogLevel.Debug) ? ResponseLoggingLevel.Verbose : ResponseLoggingLevel.WarningsAndErrors;

            // If no browser is connected we assume the changes have been applied.
            // If at least one browser suceeds we consider the changes successfully applied.
            // TODO: 
            // The refresh server should remember the deltas and apply them to browsers connected in future.
            // Currently the changes are remembered on the dev server and sent over there from the browser.
            // If no browser is connected the changes are not sent though.

            return QueueUpdateBatch(
                sendAndReceive: async batchId =>
                {
                    var result = await browserRefreshServer.SendAndReceiveAsync(
                        request: sharedSecret => new JsonApplyManagedCodeUpdatesRequest
                        {
                            SharedSecret = sharedSecret,
                            UpdateId = batchId,
                            Deltas = deltas,
                            ResponseLoggingLevel = (int)loggingLevel
                        },
                        response: new ResponseFunc<bool>((value, logger) =>
                        {
                            var success = ReceiveUpdateResponse(value, logger);
                            Logger.Log(success ? LogEvents.UpdateBatchCompleted : LogEvents.UpdateBatchFailed, batchId);
                            return success;
                        }),
                        applyOperationCancellationToken);

                    return result ?? false;
                },
                applyOperationCancellationToken);
        }

        public override Task<Task<bool>> ApplyStaticAssetUpdatesAsync(ImmutableArray<HotReloadStaticAssetUpdate> updates, CancellationToken applyOperationCancellationToken, CancellationToken cancellationToken)
            // static asset updates are handled by browser refresh server:
            => Task.FromResult(Task.FromResult(true));

        private static bool ReceiveUpdateResponse(ReadOnlySpan<byte> value, ILogger logger)
        {
            var data = AbstractBrowserRefreshServer.DeserializeJson<JsonApplyDeltasResponse>(value);

            foreach (var entry in data.Log)
            {
                ReportLogEntry(logger, entry.Message, (AgentMessageSeverity)entry.Severity);
            }

            return data.Success;
        }

        public override Task InitialUpdatesAppliedAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;

        private readonly struct JsonApplyManagedCodeUpdatesRequest
        {
            public string Type => "ApplyManagedCodeUpdates";
            public string? SharedSecret { get; init; }

            public int UpdateId { get; init; }
            public JsonDelta[] Deltas { get; init; }
            public int ResponseLoggingLevel { get; init; }
        }

        private readonly struct JsonDelta
        {
            public Guid ModuleId { get; init; }
            public byte[] MetadataDelta { get; init; }
            public byte[] ILDelta { get; init; }
            public byte[] PdbDelta { get; init; }
            public int[] UpdatedTypes { get; init; }
        }

        private readonly struct JsonApplyDeltasResponse
        {
            public bool Success { get; init; }
            public IEnumerable<JsonLogEntry> Log { get; init; }
        }

        private readonly struct JsonLogEntry
        {
            public string Message { get; init; }
            public int Severity { get; init; }
        }

        private readonly struct JsonGetApplyUpdateCapabilitiesRequest
        {
            public string Type => "GetApplyUpdateCapabilities";
        }
    }
}

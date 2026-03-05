// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.HotReload;

internal sealed class Listener(Transport transport, IHotReloadAgent agent, Action<string> log)
{
    /// <summary>
    /// Messages to the client sent after the initial <see cref="ClientInitializationResponse"/> is sent
    /// need to be sent while holding this lock in order to synchronize
    /// 1) responses to requests received from the client (e.g. <see cref="UpdateResponse"/>) or
    /// 2) notifications sent to the client that may be triggered at arbitrary times (e.g. <see cref="HotReloadExceptionCreatedNotification"/>).
    /// </summary>
    private readonly SemaphoreSlim _messageToClientLock = new(initialCount: 1);

    public Task Listen(CancellationToken cancellationToken)
    {
        // Connect to the pipe synchronously.
        //
        // If a debugger is attached and there is a breakpoint in the startup code connecting asynchronously would
        // set up a race between this code connecting to the server, and the breakpoint being hit. If the breakpoint
        // hits first, applying changes will throw an error that the client is not connected.
        //
        // Updates made before the process is launched need to be applied before loading the affected modules.

        log($"Connecting to Hot Reload server via {transport.DisplayName}.");

        try
        {
            // block execution of the app until initial updates are applied:
            InitializeAsync(cancellationToken).GetAwaiter().GetResult();
        }
        catch (Exception e)
        {
            if (e is not OperationCanceledException)
            {
                log(e.Message);
            }

            transport.Dispose();
            agent.Dispose();

            return Task.CompletedTask;
        }

        return Task.Run(async () =>
        {
            try
            {
                await ReceiveAndApplyUpdatesAsync(initialUpdates: false, cancellationToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                log(e.Message);
            }
            finally
            {
                transport.Dispose();
                agent.Dispose();
            }
        }, cancellationToken);
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        agent.Reporter.Report("Writing capabilities: " + agent.Capabilities, AgentMessageSeverity.Verbose);

        await transport.SendAsync(new ClientInitializationResponse(agent.Capabilities), cancellationToken);

        // Apply updates made before this process was launched to avoid executing unupdated versions of the affected modules.

        // We should only receive ManagedCodeUpdate when when the debugger isn't attached,
        // otherwise the initialization should send InitialUpdatesCompleted immediately.
        // The debugger itself applies these updates when launching process with the debugger attached.
        await ReceiveAndApplyUpdatesAsync(initialUpdates: true, cancellationToken);
    }

    private async Task ReceiveAndApplyUpdatesAsync(bool initialUpdates, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            using var request = await transport.ReceiveAsync(cancellationToken);
            if (request.Stream == null)
            {
                break;
            }

            var payloadType = (RequestType)await request.Stream.ReadByteAsync(cancellationToken);
            switch (payloadType)
            {
                case RequestType.ManagedCodeUpdate:
                    await ReadAndApplyManagedCodeUpdateAsync(request.Stream, cancellationToken);
                    break;

                case RequestType.StaticAssetUpdate:
                    await ReadAndApplyStaticAssetUpdateAsync(request.Stream, cancellationToken);
                    break;

                case RequestType.InitialUpdatesCompleted when initialUpdates:
                    return;

                default:
                    // can't continue, the pipe content is in an unknown state
                    throw new InvalidOperationException($"Unexpected payload type: {payloadType}");
            }
        }
    }

    private async ValueTask ReadAndApplyManagedCodeUpdateAsync(Stream stream, CancellationToken cancellationToken)
    {
        var request = await ManagedCodeUpdateRequest.ReadAsync(stream, cancellationToken);

        bool success;
        try
        {
            agent.ApplyManagedCodeUpdates(request.Updates);
            success = true;
        }
        catch (Exception e)
        {
            agent.Reporter.Report($"The runtime failed to applying the change: {e.Message}", AgentMessageSeverity.Error);
            agent.Reporter.Report("Further changes won't be applied to this process.", AgentMessageSeverity.Warning);
            success = false;
        }

        var logEntries = agent.Reporter.GetAndClearLogEntries(request.ResponseLoggingLevel);

        await SendResponseAsync(new UpdateResponse(logEntries, success), cancellationToken);
    }

    private async ValueTask ReadAndApplyStaticAssetUpdateAsync(Stream stream, CancellationToken cancellationToken)
    {
        var request = await StaticAssetUpdateRequest.ReadAsync(stream, cancellationToken);

        try
        {
            agent.ApplyStaticAssetUpdate(request.Update);
        }
        catch (Exception e)
        {
            agent.Reporter.Report($"Failed to apply static asset update: {e.Message}", AgentMessageSeverity.Error);
        }

        var logEntries = agent.Reporter.GetAndClearLogEntries(request.ResponseLoggingLevel);

        // Updating static asset only invokes ContentUpdate metadata update handlers.
        // Failures of these handlers are reported to the log and ignored.
        // Therefore, this request always succeeds.
        await SendResponseAsync(new UpdateResponse(logEntries, success: true), cancellationToken);
    }

    internal async ValueTask SendResponseAsync<T>(T response, CancellationToken cancellationToken)
        where T : IResponse
    {
        try
        {
            await _messageToClientLock.WaitAsync(cancellationToken);
            await transport.SendAsync(response, cancellationToken);
        }
        finally
        {
            _messageToClientLock.Release();
        }
    }
}

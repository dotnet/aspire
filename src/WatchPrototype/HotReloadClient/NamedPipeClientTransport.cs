// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.HotReload;

/// <summary>
/// Named pipe transport for communication between dotnet-watch and the hot reload agent.
/// Used for local processes where named pipes are available.
/// </summary>
internal sealed class NamedPipeClientTransport : ClientTransport
{
    private readonly ILogger _logger;
    private readonly string _namedPipeName;
    private readonly NamedPipeServerStream _pipe;

    public NamedPipeClientTransport(ILogger logger)
    {
        _logger = logger;
        _namedPipeName = Guid.NewGuid().ToString("N");

#if NET
        var options = PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly;
#else
        var options = PipeOptions.Asynchronous;
#endif
        _pipe = new NamedPipeServerStream(_namedPipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, options);
    }

    /// <summary>
    /// The named pipe name, for testing.
    /// </summary>
    internal string NamedPipeName => _namedPipeName;

    public override void ConfigureEnvironment(IDictionary<string, string> env)
    {
        env[AgentEnvironmentVariables.DotNetWatchHotReloadNamedPipeName] = _namedPipeName;
    }

    public override async Task WaitForConnectionAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Waiting for application to connect to pipe '{NamedPipeName}'.", _namedPipeName);

        try
        {
            await _pipe.WaitForConnectionAsync(cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            // The process may die while we're waiting for the connection and the pipe may be disposed.
            // Log and let subsequent ReadAsync return null gracefully.
            if (IsExpectedPipeException(e, cancellationToken))
            {
                _logger.LogDebug("Pipe connection ended: {Message}", e.Message);
                return;
            }

            throw;
        }
    }

    /// <summary>
    /// Returns true if the exception is expected when the pipe is disposed or the process has terminated.
    /// On Unix named pipes can also throw SocketException with ErrorCode 125 (Operation canceled) when disposed.
    /// </summary>
    private static bool IsExpectedPipeException(Exception e, CancellationToken cancellationToken)
    {
        return e is ObjectDisposedException or EndOfStreamException or SocketException { ErrorCode: 125 }
            || cancellationToken.IsCancellationRequested;
    }

    public override async ValueTask WriteAsync(byte type, Func<Stream, CancellationToken, ValueTask>? writePayload, CancellationToken cancellationToken)
    {
        await _pipe.WriteAsync(type, cancellationToken);

        if (writePayload != null)
        {
            await writePayload(_pipe, cancellationToken);
        }

        await _pipe.FlushAsync(cancellationToken);
    }

    public override async ValueTask<ClientTransportResponse?> ReadAsync(CancellationToken cancellationToken)
    {
        try
        {
            var type = (ResponseType)await _pipe.ReadByteAsync(cancellationToken);
            return new ClientTransportResponse(type, _pipe, disposeStream: false);
        }
        catch (Exception e) when (e is not OperationCanceledException && IsExpectedPipeException(e, cancellationToken))
        {
            // Pipe has been disposed or the process has terminated.
            return null;
        }
    }

    public override void Dispose()
    {
        _logger.LogDebug("Disposing agent communication pipe");

        // Dispose the pipe but do not set it to null, so that any in-progress
        // operations throw the appropriate exception type.
        _pipe.Dispose();
    }
}

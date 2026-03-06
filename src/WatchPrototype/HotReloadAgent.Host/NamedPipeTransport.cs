// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.HotReload;

internal sealed class NamedPipeTransport(string pipeName, Action<string> log, int timeoutMS) : Transport(log)
{
    private readonly NamedPipeClientStream _pipeClient = new(serverName: ".", pipeName, PipeDirection.InOut, PipeOptions.CurrentUserOnly | PipeOptions.Asynchronous);

    public override void Dispose()
        => _pipeClient.Dispose();

    public override string DisplayName
        => $"pipe {pipeName}";

    public override async ValueTask SendAsync(IResponse response, CancellationToken cancellationToken)
    {
        if (response.Type == ResponseType.InitializationResponse)
        {
            try
            {
                _pipeClient.Connect(timeoutMS);
            }
            catch (TimeoutException)
            {
                throw new TimeoutException($"Failed to connect in {timeoutMS}ms.");
            }
        }

        await _pipeClient.WriteAsync((byte)response.Type, cancellationToken);
        await response.WriteAsync(_pipeClient, cancellationToken);
    }

    public override ValueTask<RequestStream> ReceiveAsync(CancellationToken cancellationToken)
        => new(new RequestStream(_pipeClient.IsConnected ? _pipeClient : null, disposeOnCompletion: false));
}

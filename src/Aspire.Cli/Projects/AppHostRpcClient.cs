// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipes;
using System.Net.Sockets;
using Aspire.Cli.Backchannel;
using Aspire.Hosting.Ats;
using StreamJsonRpc;

namespace Aspire.Cli.Projects;

/// <summary>
/// Implementation of <see cref="IAppHostRpcClient"/> using JSON-RPC over sockets/pipes.
/// </summary>
internal sealed class AppHostRpcClient : IAppHostRpcClient
{
    private readonly Stream _stream;
    private readonly JsonRpc _jsonRpc;

    private AppHostRpcClient(Stream stream, JsonRpc jsonRpc)
    {
        _stream = stream;
        _jsonRpc = jsonRpc;
    }

    /// <summary>
    /// Creates and connects an RPC client to the specified socket path.
    /// </summary>
    public static async Task<AppHostRpcClient> ConnectAsync(string socketPath, CancellationToken cancellationToken)
    {
        var stream = await ConnectToServerAsync(socketPath, cancellationToken);

        var formatter = BackchannelJsonSerializerContext.CreateRpcMessageFormatter();
        var handler = new HeaderDelimitedMessageHandler(stream, stream, formatter);
        var jsonRpc = new JsonRpc(handler);
        jsonRpc.StartListening();

        return new AppHostRpcClient(stream, jsonRpc);
    }

    // ═══════════════════════════════════════════════════════════════
    // TYPED WRAPPERS
    // ═══════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public Task<RuntimeSpec> GetRuntimeSpecAsync(string languageId, CancellationToken cancellationToken)
        => _jsonRpc.InvokeWithCancellationAsync<RuntimeSpec>("getRuntimeSpec", [languageId], cancellationToken);

    /// <inheritdoc />
    public Task<Dictionary<string, string>> ScaffoldAppHostAsync(
        string languageId, string targetPath, string? projectName, CancellationToken cancellationToken)
        => _jsonRpc.InvokeWithCancellationAsync<Dictionary<string, string>>(
            "scaffoldAppHost", [languageId, targetPath, projectName], cancellationToken);

    /// <inheritdoc />
    public Task<Dictionary<string, string>> GenerateCodeAsync(string languageId, CancellationToken cancellationToken)
        => _jsonRpc.InvokeWithCancellationAsync<Dictionary<string, string>>(
            "generateCode", [languageId], cancellationToken);

    // ═══════════════════════════════════════════════════════════════
    // GENERIC INVOKE
    // ═══════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public Task<T> InvokeAsync<T>(string methodName, object?[] parameters, CancellationToken cancellationToken)
        => _jsonRpc.InvokeWithCancellationAsync<T>(methodName, parameters, cancellationToken);

    /// <inheritdoc />
    public Task InvokeAsync(string methodName, object?[] parameters, CancellationToken cancellationToken)
        => _jsonRpc.InvokeWithCancellationAsync(methodName, parameters, cancellationToken);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _jsonRpc.Dispose();
        await _stream.DisposeAsync();
    }

    // ═══════════════════════════════════════════════════════════════
    // CONNECTION LOGIC
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Connects to the RPC server using platform-appropriate transport.
    /// </summary>
    private static async Task<Stream> ConnectToServerAsync(string socketPath, CancellationToken cancellationToken)
    {
        var connected = false;
        var startTime = DateTimeOffset.UtcNow;
        const int ConnectionTimeoutSeconds = 30;

        if (OperatingSystem.IsWindows())
        {
            var pipeClient = new NamedPipeClientStream(".", socketPath, PipeDirection.InOut, PipeOptions.Asynchronous);

            while (!connected && (DateTimeOffset.UtcNow - startTime) < TimeSpan.FromSeconds(ConnectionTimeoutSeconds))
            {
                try
                {
                    await pipeClient.ConnectAsync(cancellationToken).ConfigureAwait(false);
                    connected = true;
                }
                catch (TimeoutException)
                {
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                }
                catch (IOException)
                {
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                }
            }

            if (!connected)
            {
                pipeClient.Dispose();
                throw new InvalidOperationException($"Failed to connect to RPC server at {socketPath}");
            }

            return pipeClient;
        }
        else
        {
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            var endpoint = new UnixDomainSocketEndPoint(socketPath);

            while (!connected && (DateTimeOffset.UtcNow - startTime) < TimeSpan.FromSeconds(ConnectionTimeoutSeconds))
            {
                try
                {
                    await socket.ConnectAsync(endpoint, cancellationToken).ConfigureAwait(false);
                    connected = true;
                }
                catch (SocketException)
                {
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                }
            }

            if (!connected)
            {
                socket.Dispose();
                throw new InvalidOperationException($"Failed to connect to RPC server at {socketPath}");
            }

            return new NetworkStream(socket, ownsSocket: true);
        }
    }
}

/// <summary>
/// Factory for creating <see cref="IAppHostRpcClient"/> instances.
/// </summary>
internal sealed class AppHostRpcClientFactory : IAppHostRpcClientFactory
{
    /// <inheritdoc />
    public async Task<IAppHostRpcClient> ConnectAsync(string socketPath, CancellationToken cancellationToken)
    {
        return await AppHostRpcClient.ConnectAsync(socketPath, cancellationToken);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text.Json.Nodes;
using StreamJsonRpc;

namespace Aspire.Hosting.RemoteHost;

internal sealed class RemoteAppHostService : IAsyncDisposable
{
    private readonly RpcOperations _operations;
    private readonly JsonRpcCallbackInvoker _callbackInvoker;
    private readonly CancellationTokenSource _cts = new();

    public RemoteAppHostService()
    {
        var objectRegistry = new ObjectRegistry();
        _callbackInvoker = new JsonRpcCallbackInvoker();
        _operations = new RpcOperations(objectRegistry, _callbackInvoker);
    }

    /// <summary>
    /// Signals that the service should stop accepting new instructions.
    /// </summary>
    public void RequestCancellation() => _cts.Cancel();

    /// <summary>
    /// Sets the JSON-RPC connection for callback invocation.
    /// </summary>
    public void SetClientConnection(JsonRpc clientRpc)
    {
        _callbackInvoker.SetConnection(clientRpc);
    }

    [JsonRpcMethod("ping")]
#pragma warning disable CA1822 // Mark members as static - JSON-RPC methods must be instance methods
    public string Ping()
#pragma warning restore CA1822
    {
        return "pong";
    }

    #region Object Marshalling

    [JsonRpcMethod("invokeMethod")]
    public JsonNode? InvokeMethod(string objectId, string methodName, JsonObject? args)
        => _operations.InvokeMethod(objectId, methodName, args);

    [JsonRpcMethod("invokeStaticMethod")]
    public JsonNode? InvokeStaticMethod(string assemblyName, string typeName, string methodName, JsonObject? args)
        => _operations.InvokeStaticMethod(assemblyName, typeName, methodName, args);

    [JsonRpcMethod("createObject")]
    public JsonNode? CreateObject(string assemblyName, string typeName, JsonObject? args)
        => _operations.CreateObject(assemblyName, typeName, args);

    [JsonRpcMethod("getProperty")]
    public JsonNode? GetProperty(string objectId, string propertyName)
        => _operations.GetProperty(objectId, propertyName);

    [JsonRpcMethod("setProperty")]
    public void SetProperty(string objectId, string propertyName, JsonNode? value)
        => _operations.SetProperty(objectId, propertyName, value);

    [JsonRpcMethod("getIndexer")]
    public JsonNode? GetIndexer(string objectId, JsonNode key)
        => _operations.GetIndexer(objectId, key);

    [JsonRpcMethod("setIndexer")]
    public void SetIndexer(string objectId, JsonNode key, JsonNode? value)
        => _operations.SetIndexer(objectId, key, value);

    [JsonRpcMethod("unregisterObject")]
    public void UnregisterObject(string objectId)
        => _operations.UnregisterObject(objectId);

    [JsonRpcMethod("createCancellationToken")]
    public JsonObject CreateCancellationToken()
        => _operations.CreateCancellationToken();

    [JsonRpcMethod("cancel")]
    public bool Cancel(string cancellationTokenId)
        => _operations.CancelToken(cancellationTokenId);

    #endregion

    #region Static Members

    [JsonRpcMethod("getStaticProperty")]
    public JsonNode? GetStaticProperty(string assemblyName, string typeName, string propertyName)
        => _operations.GetStaticProperty(assemblyName, typeName, propertyName);

    [JsonRpcMethod("setStaticProperty")]
    public void SetStaticProperty(string assemblyName, string typeName, string propertyName, JsonNode? value)
        => _operations.SetStaticProperty(assemblyName, typeName, propertyName, value);

    #endregion

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _cts.Dispose();
        await _operations.DisposeAsync().ConfigureAwait(false);
    }
}

internal sealed class JsonRpcServer : IAsyncDisposable
{
    private readonly string _socketPath;
    private readonly RemoteAppHostService _service;
    private Socket? _listenSocket;
    private bool _disposed;
    private int _activeClientCount;
    private bool _hasHadClient;

    /// <summary>
    /// Called when all clients have disconnected (and at least one client had connected).
    /// </summary>
    public Action? OnAllClientsDisconnected { get; set; }

    public JsonRpcServer(string socketPath)
    {
        _socketPath = socketPath;
        _service = new RemoteAppHostService();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"Starting JsonRpc server on Unix domain socket: {_socketPath}");
        Console.WriteLine("Server will continue running until stopped manually.");

        // Delete existing socket file if it exists
        if (File.Exists(_socketPath))
        {
            File.Delete(_socketPath);
        }

        // Ensure the directory exists
        var directory = Path.GetDirectoryName(_socketPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var endpoint = new UnixDomainSocketEndPoint(_socketPath);
        _listenSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        _listenSocket.Bind(endpoint);
        _listenSocket.Listen(10);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                Console.WriteLine("Waiting for client connection...");

                var clientSocket = await _listenSocket.AcceptAsync(cancellationToken).ConfigureAwait(false);

                Console.WriteLine("Client connected!");
                Interlocked.Increment(ref _activeClientCount);
                _hasHadClient = true;

                // Handle the connection in a separate task
                _ = Task.Run(() => HandleClientAsync(clientSocket, cancellationToken), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Server shutdown requested.");
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in server loop: {ex.Message}");
                Console.WriteLine("Retrying in 1 second...");
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            }
        }

        Console.WriteLine("Server stopped.");
    }

    private async Task HandleClientAsync(Socket clientSocket, CancellationToken cancellationToken)
    {
        var clientId = Guid.NewGuid().ToString("N")[..8]; // Short client identifier
        var disconnectReason = "unknown";

        try
        {
            using var networkStream = new NetworkStream(clientSocket, ownsSocket: true);

            // Use System.Text.Json formatter instead of the default Newtonsoft.Json formatter
            var formatter = new SystemTextJsonFormatter();
            var handler = new HeaderDelimitedMessageHandler(networkStream, networkStream, formatter);
            using var jsonRpc = new JsonRpc(handler, _service);
            jsonRpc.StartListening();

            // Enable bidirectional communication - allow .NET to call back to TypeScript
            _service.SetClientConnection(jsonRpc);

            Console.WriteLine($"JsonRpc connection established for client {clientId} (bidirectional)");

            // Wait for the connection to be closed by the client, an error, or cancellation
            using var registration = cancellationToken.Register(() =>
            {
                disconnectReason = "server shutdown";
                try { jsonRpc.Dispose(); }
                catch { /* ignore disposal errors during cancellation */ }
            });

            try
            {
                await jsonRpc.Completion.ConfigureAwait(false);
                disconnectReason = "graceful disconnect";
            }
            catch (ConnectionLostException)
            {
                disconnectReason = "connection lost (client disconnected unexpectedly)";
            }
            catch (ObjectDisposedException)
            {
                // This happens when server shutdown causes jsonRpc.Dispose()
                disconnectReason ??= "server shutdown";
            }
            catch (IOException ex) when (ex.InnerException is SocketException)
            {
                disconnectReason = "socket closed (client terminated)";
            }

            Console.WriteLine($"Client {clientId}: {disconnectReason}");
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Client {clientId} socket error: {ex.SocketErrorCode} - {ex.Message}");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Client {clientId} I/O error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Client {clientId} unexpected error: {ex.GetType().Name} - {ex.Message}");
        }
        finally
        {
            // Clean up socket - wrap in try-catch since it might already be disposed
            try
            {
                if (clientSocket.Connected)
                {
                    clientSocket.Shutdown(SocketShutdown.Both);
                }
            }
            catch (SocketException)
            {
                // Socket already closed, ignore
            }
            catch (ObjectDisposedException)
            {
                // Socket already disposed, ignore
            }

            try
            {
                clientSocket.Close();
            }
            catch
            {
                // Ignore errors during close
            }

            Console.WriteLine($"Connection cleanup completed for client {clientId}");

            // Decrement active client count and check if all clients disconnected
            var remaining = Interlocked.Decrement(ref _activeClientCount);
            Console.WriteLine($"Active clients remaining: {remaining}");

            if (remaining == 0 && _hasHadClient)
            {
                Console.WriteLine("All clients have disconnected.");
                try
                {
                    OnAllClientsDisconnected?.Invoke();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in OnAllClientsDisconnected callback: {ex.Message}");
                }
            }
        }
    }

    public void Stop()
    {
        _listenSocket?.Close();
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;

            Stop();
            _listenSocket?.Dispose();

            // Dispose the service which will stop all running apps
            Console.WriteLine("Disposing RemoteAppHostService...");
            await _service.DisposeAsync().ConfigureAwait(false);

            // Clean up socket file
            if (File.Exists(_socketPath))
            {
                try
                {
                    File.Delete(_socketPath);
                }
                catch { }
            }

            Console.WriteLine("JsonRpcServer disposed.");
        }
    }
}

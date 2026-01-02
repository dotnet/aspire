// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using StreamJsonRpc;

namespace Aspire.Hosting.RemoteHost;

internal sealed class RemoteAppHostService : IAsyncDisposable
{
    private readonly RpcOperations _operations;
    private readonly JsonRpcCallbackInvoker _callbackInvoker;
    private readonly CancellationTokenSource _cts = new();
    private readonly string? _authToken;
    private bool _isAuthenticated;

    /// <summary>
    /// Creates a new RemoteAppHostService.
    /// </summary>
    /// <param name="authToken">
    /// Optional authentication token. If provided, clients must call authenticate()
    /// with this token before any other RPC methods are allowed.
    /// If null, authentication is disabled.
    /// </param>
    public RemoteAppHostService(string? authToken = null)
    {
        _authToken = authToken;
        _isAuthenticated = authToken == null; // If no token, auto-authenticate
        var objectRegistry = new ObjectRegistry();
        _callbackInvoker = new JsonRpcCallbackInvoker();
        _operations = new RpcOperations(objectRegistry, _callbackInvoker);

        // Diagnostic logging for security configuration
        Console.WriteLine("[RPC] Security Configuration:");
        if (authToken == null)
        {
            Console.WriteLine("[RPC]   Authentication: DISABLED - no token configured");
        }
        else
        {
            var tokenPreview = authToken.Length > 8 ? authToken[..8] + "..." : authToken;
            Console.WriteLine($"[RPC]   Authentication: ENABLED - token starts with: {tokenPreview}");
        }
        Console.WriteLine("[RPC]   Allowed assembly prefixes:");
        foreach (var prefix in SecurityPolicy.AllowedAssemblyPrefixes)
        {
            Console.WriteLine($"[RPC]     - {prefix}*");
        }
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

    /// <summary>
    /// Authenticates the client with the server.
    /// Must be called before any other RPC methods if the server was started with an auth token.
    /// </summary>
    /// <param name="token">The authentication token.</param>
    /// <returns>True if authentication succeeded.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown if the token is invalid.</exception>
    [JsonRpcMethod("authenticate")]
    public bool Authenticate(string token)
    {
        if (_authToken == null)
        {
            // No auth required
            _isAuthenticated = true;
            return true;
        }

        // Use constant-time comparison to prevent timing attacks
        if (CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(token),
            System.Text.Encoding.UTF8.GetBytes(_authToken)))
        {
            _isAuthenticated = true;
            Console.WriteLine("[RPC] Client authenticated successfully");
            return true;
        }

        Console.WriteLine("[RPC] SECURITY: Authentication failed - invalid token");
        throw new UnauthorizedAccessException("Access denied.");
    }

    /// <summary>
    /// Throws if the client is not authenticated.
    /// </summary>
    private void RequireAuthentication()
    {
        if (!_isAuthenticated)
        {
            Console.WriteLine("[RPC] SECURITY: Blocked unauthenticated RPC call");
            throw new UnauthorizedAccessException("Access denied.");
        }
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
    {
        RequireAuthentication();
        return _operations.InvokeMethod(objectId, methodName, args);
    }

    [JsonRpcMethod("invokeStaticMethod")]
    public JsonNode? InvokeStaticMethod(string assemblyName, string typeName, string methodName, JsonObject? args)
    {
        RequireAuthentication();
        return _operations.InvokeStaticMethod(assemblyName, typeName, methodName, args);
    }

    [JsonRpcMethod("createObject")]
    public JsonNode? CreateObject(string assemblyName, string typeName, JsonObject? args)
    {
        RequireAuthentication();
        return _operations.CreateObject(assemblyName, typeName, args);
    }

    [JsonRpcMethod("getProperty")]
    public JsonNode? GetProperty(string objectId, string propertyName)
    {
        RequireAuthentication();
        return _operations.GetProperty(objectId, propertyName);
    }

    [JsonRpcMethod("setProperty")]
    public void SetProperty(string objectId, string propertyName, JsonNode? value)
    {
        RequireAuthentication();
        _operations.SetProperty(objectId, propertyName, value);
    }

    [JsonRpcMethod("getIndexer")]
    public JsonNode? GetIndexer(string objectId, JsonNode key)
    {
        RequireAuthentication();
        return _operations.GetIndexer(objectId, key);
    }

    [JsonRpcMethod("setIndexer")]
    public void SetIndexer(string objectId, JsonNode key, JsonNode? value)
    {
        RequireAuthentication();
        _operations.SetIndexer(objectId, key, value);
    }

    [JsonRpcMethod("unregisterObject")]
    public void UnregisterObject(string objectId)
    {
        RequireAuthentication();
        _operations.UnregisterObject(objectId);
    }

    [JsonRpcMethod("createCancellationToken")]
    public JsonObject CreateCancellationToken()
    {
        RequireAuthentication();
        return _operations.CreateCancellationToken();
    }

    [JsonRpcMethod("cancel")]
    public bool Cancel(string cancellationTokenId)
    {
        RequireAuthentication();
        return _operations.CancelToken(cancellationTokenId);
    }

    #endregion

    #region Static Members

    [JsonRpcMethod("getStaticProperty")]
    public JsonNode? GetStaticProperty(string assemblyName, string typeName, string propertyName)
    {
        RequireAuthentication();
        return _operations.GetStaticProperty(assemblyName, typeName, propertyName);
    }

    [JsonRpcMethod("setStaticProperty")]
    public void SetStaticProperty(string assemblyName, string typeName, string propertyName, JsonNode? value)
    {
        RequireAuthentication();
        _operations.SetStaticProperty(assemblyName, typeName, propertyName, value);
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        Console.WriteLine("[RPC] RemoteAppHostService disposing...");
        _cts.Cancel();
        _cts.Dispose();
        await _operations.DisposeAsync().ConfigureAwait(false);
        Console.WriteLine("[RPC] RemoteAppHostService disposed.");
    }
}

internal sealed class JsonRpcServer : IAsyncDisposable
{
    private readonly string _socketPath;
    private readonly string? _authToken;
    private Socket? _listenSocket;
    private bool _disposed;
    private int _activeClientCount;
    private bool _hasHadClient;

    /// <summary>
    /// Called when all clients have disconnected (and at least one client had connected).
    /// </summary>
    public Action? OnAllClientsDisconnected { get; set; }

    /// <summary>
    /// Creates a new JsonRpcServer.
    /// </summary>
    /// <param name="socketPath">Path to the Unix domain socket.</param>
    /// <param name="authToken">
    /// Optional authentication token. If provided, clients must call authenticate()
    /// with this token before any other RPC methods are allowed.
    /// </param>
    public JsonRpcServer(string socketPath, string? authToken = null)
    {
        _socketPath = socketPath;
        _authToken = authToken;
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

        // M3: Set restrictive permissions on socket file (owner read/write only)
        // This prevents other users on the system from connecting to the socket
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            File.SetUnixFileMode(_socketPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }

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

        // Create a dedicated service instance for this client connection
        // Each client has its own authentication state and object registry
        Console.WriteLine($"[RPC] Creating new RemoteAppHostService for client {clientId}");
        var clientService = new RemoteAppHostService(_authToken);
        // Discard pattern to satisfy CA2007 while ensuring disposal
        await using var _ = clientService.ConfigureAwait(false);

        try
        {
            using var networkStream = new NetworkStream(clientSocket, ownsSocket: true);

            // Use System.Text.Json formatter instead of the default Newtonsoft.Json formatter
            var formatter = new SystemTextJsonFormatter();
            var handler = new HeaderDelimitedMessageHandler(networkStream, networkStream, formatter);
            using var jsonRpc = new JsonRpc(handler, clientService);
            jsonRpc.StartListening();

            // Enable bidirectional communication - allow .NET to call back to TypeScript
            clientService.SetClientConnection(jsonRpc);

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

            // Each client's service is disposed when HandleClientAsync completes
            // No shared service to dispose here
            await Task.CompletedTask.ConfigureAwait(false);

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

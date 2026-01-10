// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipes;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text.Json.Nodes;
using Aspire.Hosting.Ats;
using Aspire.Hosting.RemoteHost.Ats;
using StreamJsonRpc;

namespace Aspire.Hosting.RemoteHost;

internal sealed class RemoteAppHostService : IAsyncDisposable
{
    private readonly JsonRpcCallbackInvoker _callbackInvoker;
    private readonly CancellationTokenSource _cts = new();
    private readonly string? _authToken;
    private bool _isAuthenticated;

    // ATS (Aspire Type System) components
    private readonly HandleRegistry _handleRegistry;
    private readonly AtsCallbackProxyFactory _callbackProxyFactory;
    private readonly CapabilityDispatcher _capabilityDispatcher;

    /// <summary>
    /// Creates a new RemoteAppHostService.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan for ATS capabilities and handles.</param>
    /// <param name="authToken">
    /// Optional authentication token. If provided, clients must call authenticate()
    /// with this token before any other RPC methods are allowed.
    /// If null, authentication is disabled.
    /// </param>
    public RemoteAppHostService(IEnumerable<Assembly> assemblies, string? authToken = null)
    {
        _authToken = authToken;
        _isAuthenticated = authToken == null; // If no token, auto-authenticate
        _callbackInvoker = new JsonRpcCallbackInvoker();

        // Initialize ATS components with provided assemblies
        _handleRegistry = new HandleRegistry();
        _callbackProxyFactory = new AtsCallbackProxyFactory(_callbackInvoker, _handleRegistry);
        _capabilityDispatcher = new CapabilityDispatcher(_handleRegistry, assemblies, _callbackProxyFactory);

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

    #region ATS Capabilities

    /// <summary>
    /// Invokes an ATS capability by ID.
    /// </summary>
    /// <param name="capabilityId">The capability ID (e.g., "aspire.redis/addRedis@1").</param>
    /// <param name="args">The arguments as a JSON object.</param>
    /// <returns>The result as JSON, or an error object.</returns>
    [JsonRpcMethod("invokeCapability")]
    public async Task<JsonNode?> InvokeCapabilityAsync(string capabilityId, JsonObject? args)
    {
        RequireAuthentication();
        Console.WriteLine($"[RPC] >> invokeCapability({capabilityId})");
        Console.WriteLine($"[RPC]    args: {args?.ToJsonString() ?? "null"}");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var result = await _capabilityDispatcher.InvokeAsync(capabilityId, args).ConfigureAwait(false);
            Console.WriteLine($"[RPC]    result: {result?.ToJsonString() ?? "null"}");
            return result;
        }
        catch (CapabilityException ex)
        {
            Console.WriteLine($"[RPC]    CapabilityException: {ex.Error.Code} - {ex.Error.Message}");
            if (ex.Error.Details != null)
            {
                Console.WriteLine($"[RPC]    Details: param={ex.Error.Details.Parameter}, expected={ex.Error.Details.Expected}, actual={ex.Error.Details.Actual}");
            }
            // Return structured error
            return new JsonObject
            {
                ["$error"] = ex.Error.ToJsonObject()
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPC]    Exception: {ex.GetType().Name} - {ex.Message}");
            Console.WriteLine($"[RPC]    StackTrace: {ex.StackTrace}");
            // Wrap unexpected errors
            var error = new AtsError
            {
                Code = AtsErrorCodes.InternalError,
                Message = ex.Message,
                Capability = capabilityId
            };
            return new JsonObject
            {
                ["$error"] = error.ToJsonObject()
            };
        }
        finally
        {
            Console.WriteLine($"[RPC] << invokeCapability({capabilityId}) completed in {sw.ElapsedMilliseconds}ms");
        }
    }

    /// <summary>
    /// Gets all registered ATS capability IDs.
    /// </summary>
    /// <returns>An array of capability IDs.</returns>
    [JsonRpcMethod("getCapabilities")]
    public JsonArray GetCapabilities()
    {
        RequireAuthentication();
        Console.WriteLine("[RPC] >> getCapabilities()");
        try
        {
            var result = new JsonArray();
            foreach (var capabilityId in _capabilityDispatcher.GetCapabilityIds())
            {
                result.Add(capabilityId);
            }
            return result;
        }
        finally
        {
            Console.WriteLine("[RPC] << getCapabilities() completed");
        }
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        Console.WriteLine("[RPC] RemoteAppHostService disposing...");
        _cts.Cancel();
        _cts.Dispose();
        _callbackProxyFactory.Dispose();
        await _handleRegistry.DisposeAsync().ConfigureAwait(false);
        Console.WriteLine("[RPC] RemoteAppHostService disposed.");
    }
}

internal sealed class JsonRpcServer : IAsyncDisposable
{
    private readonly string _socketPath;
    private readonly string? _authToken;
    private readonly IEnumerable<Assembly> _atsAssemblies;
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
    /// <param name="atsAssemblies">The assemblies to scan for ATS capabilities and handles.</param>
    /// <param name="authToken">
    /// Optional authentication token. If provided, clients must call authenticate()
    /// with this token before any other RPC methods are allowed.
    /// </param>
    public JsonRpcServer(string socketPath, IEnumerable<Assembly> atsAssemblies, string? authToken = null)
    {
        _socketPath = socketPath;
        _atsAssemblies = atsAssemblies;
        _authToken = authToken;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (OperatingSystem.IsWindows())
        {
            await StartNamedPipeServerAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await StartUnixSocketServerAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    [SupportedOSPlatform("windows")]
    private async Task StartNamedPipeServerAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"Starting JsonRpc server on named pipe: {_socketPath}");
        Console.WriteLine("Server will continue running until stopped manually.");

        // Create pipe security that only allows the current user to connect
        // This is equivalent to the Unix socket permission (owner read/write only)
        var pipeSecurity = new PipeSecurity();
        var currentUser = WindowsIdentity.GetCurrent().User;
        if (currentUser != null)
        {
            pipeSecurity.AddAccessRule(new PipeAccessRule(
                currentUser,
                PipeAccessRights.FullControl,
                AccessControlType.Allow));
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                Console.WriteLine("Waiting for client connection...");

                // Create a new named pipe server for each connection with security restrictions
                var pipeServer = NamedPipeServerStreamAcl.Create(
                    _socketPath,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous,
                    inBufferSize: 0,
                    outBufferSize: 0,
                    pipeSecurity);

                await pipeServer.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);

                Console.WriteLine("Client connected!");
                Interlocked.Increment(ref _activeClientCount);
                _hasHadClient = true;

                // Handle the connection in a separate task - pipe stream is owned by handler
                _ = Task.Run(() => HandleClientStreamAsync(pipeServer, ownsStream: true, cancellationToken), cancellationToken);
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

    private async Task StartUnixSocketServerAsync(CancellationToken cancellationToken)
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

                // Handle the connection in a separate task - NetworkStream owns the socket
                var stream = new NetworkStream(clientSocket, ownsSocket: true);
                _ = Task.Run(() => HandleClientStreamAsync(stream, ownsStream: true, cancellationToken), cancellationToken);
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

    private async Task HandleClientStreamAsync(Stream clientStream, bool ownsStream, CancellationToken cancellationToken)
    {
        var clientId = Guid.NewGuid().ToString("N")[..8]; // Short client identifier
        var disconnectReason = "unknown";

        // Create a dedicated service instance for this client connection
        // Each client has its own authentication state and handle registry
        Console.WriteLine($"[RPC] Creating new RemoteAppHostService for client {clientId}");
        var clientService = new RemoteAppHostService(_atsAssemblies, _authToken);
        // Discard pattern to satisfy CA2007 while ensuring disposal
        await using var _ = clientService.ConfigureAwait(false);

        try
        {
            // Use System.Text.Json formatter instead of the default Newtonsoft.Json formatter
            var formatter = new SystemTextJsonFormatter();
            var handler = new HeaderDelimitedMessageHandler(clientStream, clientStream, formatter);
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
            catch (IOException)
            {
                disconnectReason = "stream closed (client terminated)";
            }

            Console.WriteLine($"Client {clientId}: {disconnectReason}");
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
            // Clean up stream if we own it
            if (ownsStream)
            {
                try
                {
                    clientStream.Dispose();
                }
                catch
                {
                    // Ignore errors during close
                }
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

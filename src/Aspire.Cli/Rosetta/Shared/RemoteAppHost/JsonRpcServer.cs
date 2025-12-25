// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using StreamJsonRpc;

namespace RemoteAppHost;

public class RemoteAppHostService : IAsyncDisposable
{
    private readonly InstructionProcessor _instructionProcessor = new();
    private readonly CancellationTokenSource _cts = new();

    /// <summary>
    /// Signals that the service should stop accepting new instructions.
    /// </summary>
    public void RequestCancellation() => _cts.Cancel();

    [JsonRpcMethod("executeInstruction")]
    public async Task<object?> ExecuteInstructionAsync(string instructionJson)
    {
        try
        {
            return await _instructionProcessor.ExecuteInstructionAsync(instructionJson, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Instruction execution was cancelled");
            return new { success = false, error = "Operation cancelled" };
        }
        catch (ObjectDisposedException)
        {
            Console.WriteLine("Instruction processor has been disposed");
            return new { success = false, error = "Service is shutting down" };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing instruction: {ex.Message}");
            return new { success = false, error = ex.Message };
        }
    }

    [JsonRpcMethod("ping")]
    public string Ping()
    {
        return "pong";
    }

    public async ValueTask DisposeAsync()
    {
        // Cancel any in-flight operations
        _cts.Cancel();
        _cts.Dispose();

        await _instructionProcessor.DisposeAsync();
    }
}

public class JsonRpcServer : IAsyncDisposable
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

                var clientSocket = await _listenSocket.AcceptAsync(cancellationToken);

                Console.WriteLine("Client connected!");
                Interlocked.Increment(ref _activeClientCount);
                _hasHadClient = true;

                // Handle the connection in a separate task
                _ = Task.Run(async () => await HandleClientAsync(clientSocket, cancellationToken), cancellationToken);
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
                await Task.Delay(1000, cancellationToken);
            }
        }

        Console.WriteLine("Server stopped.");
    }

    private async Task HandleClientAsync(Socket clientSocket, CancellationToken cancellationToken)
    {
        var clientId = Guid.NewGuid().ToString("N")[..8]; // Short client identifier

        try
        {
            using var networkStream = new NetworkStream(clientSocket, ownsSocket: true);
            using var jsonRpc = JsonRpc.Attach(networkStream, _service);

            Console.WriteLine($"JsonRpc connection established for client {clientId}");

            // Wait for the connection to be closed by the client, an error, or cancellation
            using var registration = cancellationToken.Register(() => jsonRpc.Dispose());
            await jsonRpc.Completion;

            Console.WriteLine($"Client {clientId} disconnected gracefully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling client {clientId}: {ex.Message}");
        }
        finally
        {
            try
            {
                clientSocket?.Close();
                Console.WriteLine($"Connection cleanup completed for client {clientId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during cleanup for client {clientId}: {ex.Message}");
            }

            // Decrement active client count and check if all clients disconnected
            var remaining = Interlocked.Decrement(ref _activeClientCount);
            Console.WriteLine($"Active clients remaining: {remaining}");

            if (remaining == 0 && _hasHadClient)
            {
                Console.WriteLine("All clients have disconnected.");
                OnAllClientsDisconnected?.Invoke();
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
            await _service.DisposeAsync();

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

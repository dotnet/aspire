// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using StreamJsonRpc;

namespace RemoteAppHost;

public class RemoteAppHostService
{
    private readonly InstructionProcessor _instructionProcessor = new();

    [JsonRpcMethod("executeInstruction")]
    public async Task<object?> ExecuteInstructionAsync(string instructionJson)
    {
        try
        {
            return await _instructionProcessor.ExecuteInstructionAsync(instructionJson);
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
}

public class JsonRpcServer : IDisposable
{
    private readonly string _socketPath;
    private readonly RemoteAppHostService _service;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private Socket? _listenSocket;
    private bool _disposed;

    public JsonRpcServer(string socketPath)
    {
        _socketPath = socketPath;
        _service = new RemoteAppHostService();
    }

    public async Task StartAsync()
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

        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                Console.WriteLine("Waiting for client connection...");

                var clientSocket = await _listenSocket.AcceptAsync(_cancellationTokenSource.Token);

                Console.WriteLine("Client connected!");

                // Handle the connection in a separate task
                _ = Task.Run(async () => await HandleClientAsync(clientSocket), _cancellationTokenSource.Token);
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
                await Task.Delay(1000, _cancellationTokenSource.Token);
            }
        }

        Console.WriteLine("Server stopped.");
    }

    private async Task HandleClientAsync(Socket clientSocket)
    {
        var clientId = Guid.NewGuid().ToString("N")[..8]; // Short client identifier

        try
        {
            using var networkStream = new NetworkStream(clientSocket, ownsSocket: true);
            using var jsonRpc = JsonRpc.Attach(networkStream, _service);

            Console.WriteLine($"JsonRpc connection established for client {clientId}");

            // Wait for the connection to be closed by the client or an error
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
        }

        Console.WriteLine("Ready for next client connection...");
    }

    public void Stop()
    {
        _cancellationTokenSource.Cancel();
        _listenSocket?.Close();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _cancellationTokenSource.Dispose();
            _listenSocket?.Dispose();

            // Clean up socket file
            if (File.Exists(_socketPath))
            {
                try
                {
                    File.Delete(_socketPath);
                }
                catch { }
            }

            _disposed = true;
        }
    }
}

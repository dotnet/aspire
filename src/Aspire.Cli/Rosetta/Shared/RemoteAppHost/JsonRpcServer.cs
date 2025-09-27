// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.using System.IO.Pipes;

using System.IO.Pipes;
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

public class JsonRpcServer
{
    private readonly string _pipeName;
    private readonly RemoteAppHostService _service;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public JsonRpcServer(string pipeName = "RemoteAppHost")
    {
        _pipeName = pipeName;
        _service = new RemoteAppHostService();
    }

    public async Task StartAsync()
    {
        Console.WriteLine($"Starting JsonRpc server on named pipe: {_pipeName}");
        Console.WriteLine("Server will continue running until stopped manually.");
        
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                var pipeServer = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                Console.WriteLine("Waiting for client connection...");
                
                await pipeServer.WaitForConnectionAsync(_cancellationTokenSource.Token);
                
                Console.WriteLine("Client connected!");

                // Handle the connection in a separate task
                _ = Task.Run(async () => await HandleClientAsync(pipeServer), _cancellationTokenSource.Token);
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

    private async Task HandleClientAsync(NamedPipeServerStream pipeStream)
    {
        var clientId = Guid.NewGuid().ToString("N")[..8]; // Short client identifier
        
        try
        {
            using var jsonRpc = JsonRpc.Attach(pipeStream, _service);
            
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
                pipeStream?.Close();
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
    }
}

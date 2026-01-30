// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipes;
using System.Text.Json;

namespace Microsoft.DotNet.Watch;

internal static class AspireResourceLauncher
{
    /// <summary>
    /// Connects to the server via named pipe, sends resource options as JSON, and waits for ACK.
    /// </summary>
    public static async Task<int> LaunchAsync(AspireResourceWatchOptions options)
    {
        try
        {
            using var pipeClient = new NamedPipeClientStream(
                serverName: ".",
                options.ServerPipeName,
                PipeDirection.InOut,
                PipeOptions.CurrentUserOnly | PipeOptions.Asynchronous);

            await pipeClient.ConnectAsync();

            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(options);

            // Write length-prefixed JSON
            var lengthBytes = BitConverter.GetBytes(jsonBytes.Length);
            await pipeClient.WriteAsync(lengthBytes);
            await pipeClient.WriteAsync(jsonBytes);
            await pipeClient.FlushAsync();

            // Wait for ACK byte
            var ackBuffer = new byte[1];
            var bytesRead = await pipeClient.ReadAsync(ackBuffer);

            if (bytesRead == 0)
            {
                Console.Error.WriteLine("Server closed connection without sending ACK.");
                return 1;
            }

            return ackBuffer[0];
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to communicate with server: {ex.Message}");
            return 1;
        }
    }
}

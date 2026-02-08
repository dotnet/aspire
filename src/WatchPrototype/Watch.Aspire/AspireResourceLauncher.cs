// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipes;
using System.Text.Json;
using Microsoft.DotNet.HotReload;

namespace Microsoft.DotNet.Watch;

internal static class AspireResourceLauncher
{
    public const byte Version = 1;

    // Output message type bytes
    public const byte OutputTypeStdout = 1;
    public const byte OutputTypeStderr = 2;
    public const byte OutputTypeExit = 0;

    /// <summary>
    /// Connects to the server via named pipe, sends resource options as JSON, waits for ACK,
    /// then stays alive proxying stdout/stderr from the server back to the console.
    /// </summary>
    public static async Task<int> LaunchAsync(AspireResourceWatchOptions options, CancellationToken cancellationToken)
    {
        try
        {
            Console.Error.WriteLine($"Connecting to {options.ServerPipeName}...");

            using var pipeClient = new NamedPipeClientStream(
                serverName: ".",
                options.ServerPipeName,
                PipeDirection.InOut,
                PipeOptions.CurrentUserOnly | PipeOptions.Asynchronous);

            // Timeout ensures we don't hang indefinitely if the server isn't ready or the pipe name is wrong.
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(60));
            await pipeClient.ConnectAsync(timeoutCts.Token);

            var request = new LaunchResourceRequest()
            {
                EntryPoint = options.EntryPoint,
                ApplicationArguments = options.ApplicationArguments,
                EnvironmentVariables = options.EnvironmentVariables,
                LaunchProfile = options.LaunchProfile,
                TargetFramework = options.TargetFramework,
                NoLaunchProfile = options.NoLaunchProfile,
            };

            await pipeClient.WriteAsync(Version, cancellationToken);

            var json = Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes(request));
            await pipeClient.WriteAsync(json, cancellationToken);

            // Wait for ACK byte
            var status = await pipeClient.ReadByteAsync(cancellationToken);
            if (status == 0)
            {
                Console.Error.WriteLine("Server closed connection without sending ACK.");
                return 1;
            }

            Console.Error.WriteLine("Request sent. Waiting for output...");

            // Stay alive and proxy output from the server
            return await ProxyOutputAsync(pipeClient, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return 0;
        }
        catch (EndOfStreamException)
        {
            // Pipe disconnected - server shut down
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to communicate with server: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> ProxyOutputAsync(NamedPipeClientStream pipe, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            byte typeByte;
            try
            {
                typeByte = await pipe.ReadByteAsync(cancellationToken);
            }
            catch (EndOfStreamException)
            {
                // Pipe closed, server shut down
                return 0;
            }

            var content = await pipe.ReadStringAsync(cancellationToken);

            switch (typeByte)
            {
                case OutputTypeStdout:
                    Console.Out.WriteLine(content);
                    break;
                case OutputTypeStderr:
                    Console.Error.WriteLine(content);
                    break;
                case OutputTypeExit:
                    // Don't exit — the server may restart the process (e.g. after a rude edit)
                    // and continue writing to the same pipe. We stay alive until the pipe closes.
                    break;
            }
        }

        return 0;
    }
}

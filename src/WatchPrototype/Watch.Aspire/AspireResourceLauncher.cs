// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipes;
using System.Text.Json;
using Microsoft.DotNet.HotReload;

namespace Microsoft.DotNet.Watch;

internal static class AspireResourceLauncher
{
    public const byte Version = 1;

    /// <summary>
    /// Connects to the server via named pipe, sends resource options as JSON, and waits for ACK.
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

            await pipeClient.ConnectAsync(cancellationToken);

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

            Console.Error.WriteLine("Request sent.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to communicate with server: {ex.Message}");
            return 1;
        }
    }
}

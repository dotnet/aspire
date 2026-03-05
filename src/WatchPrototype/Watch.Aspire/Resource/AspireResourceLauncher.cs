// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.CommandLine;
using System.IO.Pipes;
using System.Text.Json;
using Microsoft.DotNet.HotReload;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch;

internal sealed class AspireResourceLauncher(
    GlobalOptions globalOptions,
    EnvironmentOptions environmentOptions,
    string serverPipeName,
    string entryPoint,
    ImmutableArray<string> applicationArguments,
    IReadOnlyDictionary<string, string> environmentVariables,
    Optional<string?> launchProfileName,
    TimeSpan pipeConnectionTimeout)
    : AspireLauncher(globalOptions, environmentOptions)
{
    internal const string LogMessagePrefix = "aspire watch resource";

    public const byte Version = 1;

    // Output message type bytes
    public const byte OutputTypeStdout = 1;
    public const byte OutputTypeStderr = 2;

    public string ServerPipeName => serverPipeName;
    public string EntryPoint => entryPoint;
    public ImmutableArray<string> ApplicationArguments => applicationArguments;
    public IReadOnlyDictionary<string, string> EnvironmentVariables => environmentVariables;
    public Optional<string?> LaunchProfileName => launchProfileName;

    public static AspireResourceLauncher? TryCreate(ParseResult parseResult, AspireResourceCommandDefinition command)
    {
        var serverPipeName = parseResult.GetValue(command.ServerOption)!;
        var entryPointPath = parseResult.GetValue(command.EntryPointOption)!;
        var applicationArguments = parseResult.GetValue(command.ApplicationArguments) ?? [];
        var environmentVariables = parseResult.GetValue(command.EnvironmentOption) ?? ImmutableDictionary<string, string>.Empty;
        var noLaunchProfile = parseResult.GetValue(command.NoLaunchProfileOption);
        var launchProfile = parseResult.GetValue(command.LaunchProfileOption);

        var globalOptions = new GlobalOptions()
        {
            LogLevel = command.GetLogLevel(parseResult),
            NoHotReload = false,
            NonInteractive = true,
        };

        return new AspireResourceLauncher(
            globalOptions,
            // SDK directory is not needed for the resource launcher since it doesn't interact with MSBuild:
            EnvironmentOptions.FromEnvironment(sdkDirectory: null, LogMessagePrefix),
            serverPipeName: serverPipeName,
            entryPoint: entryPointPath,
            applicationArguments: [.. applicationArguments],
            environmentVariables: environmentVariables,
            launchProfileName: noLaunchProfile ? Optional<string?>.NoValue : launchProfile,
            pipeConnectionTimeout: AspireEnvironmentVariables.PipeConnectionTimeout);
    }

    /// <summary>
    /// Connects to the server via named pipe, sends resource options as JSON, waits for ACK,
    /// then stays alive proxying stdout/stderr from the server back to the console.
    /// </summary>
    public override async Task<int> LaunchAsync(CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogDebug("Connecting to {ServerPipeName}...", ServerPipeName);

            using var pipeClient = new NamedPipeClientStream(
                serverName: ".",
                ServerPipeName,
                PipeDirection.InOut,
                PipeOptions.CurrentUserOnly | PipeOptions.Asynchronous);

            // Timeout ensures we don't hang indefinitely if the server isn't ready or the pipe name is wrong.
            using var connectionCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            connectionCancellationSource.CancelAfter(pipeConnectionTimeout);
            await pipeClient.ConnectAsync(connectionCancellationSource.Token);

            var request = new LaunchResourceRequest()
            {
                EntryPoint = EntryPoint,
                ApplicationArguments = ApplicationArguments,
                EnvironmentVariables = EnvironmentVariables,
                LaunchProfileName = LaunchProfileName,
            };

            await pipeClient.WriteAsync(Version, cancellationToken);

            var json = Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes(request));
            await pipeClient.WriteAsync(json, cancellationToken);

            // Wait for ACK byte
            var status = await pipeClient.ReadByteAsync(cancellationToken);
            if (status == 0)
            {
                Logger.LogDebug("Server closed connection without sending ACK.");
                return 1;
            }

            Logger.LogDebug("Request sent. Waiting for output...");

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
            Logger.LogDebug("Failed to communicate with server: {Message}", ex.Message);
            return 1;
        }
    }

    private async Task<int> ProxyOutputAsync(NamedPipeClientStream pipe, CancellationToken cancellationToken)
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

            var output = typeByte switch
            {
                OutputTypeStdout => Console.Out,
                OutputTypeStderr => Console.Error,
                _ => throw new InvalidOperationException($"Unexpected output type: '{typeByte:X2}'")
            };

            output.WriteLine(content);
        }

        return 0;
    }
}

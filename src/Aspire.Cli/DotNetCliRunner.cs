// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using StreamJsonRpc;

namespace Aspire.Cli;

internal sealed class DotNetCliRunner(ILogger<DotNetCliRunner> logger, CliRpcTarget cliRpcTarget)
{
    internal Func<int> GetCurrentProcessId { get; set; } = () => Environment.ProcessId;

    public async Task<int> RunAsync(FileInfo projectFile, string[] args, CancellationToken cancellationToken)
    {
        string[] cliArgs = ["run", "--project", projectFile.FullName, "--", ..args];
        return await ExecuteAsync(cliArgs, true, cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> InstallTemplateAsync(string packageName, string version, bool force, CancellationToken cancellationToken)
    {
        string[] forceArgs = force ? ["--force"] : [];
        string[] cliArgs = ["new", "install", $"{packageName}::{version}", ..forceArgs];
        return await ExecuteAsync(cliArgs, false, cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> NewProjectAsync(string templateName, string name, string outputPath, CancellationToken cancellationToken)
    {
        string[] cliArgs = ["new", templateName, "--name", name, "--output", outputPath];
        return await ExecuteAsync(cliArgs, false, cancellationToken).ConfigureAwait(false);
    }

    internal static string GetBackchannelSocketPath()
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var dotnetCliPath = Path.Combine(homeDirectory, ".dotnet", "aspire", "cli", "backchannels");

        if (!Directory.Exists(dotnetCliPath))
        {
            Directory.CreateDirectory(dotnetCliPath);
        }

        var uniqueSocketPathSegment = Guid.NewGuid().ToString("N");
        var socketPath = Path.Combine(dotnetCliPath, $"cli.sock.{uniqueSocketPathSegment}");
        return socketPath;
    }

    private async Task StartBackchannelAsync(string socketPath, CancellationToken cancellationToken)
    {
        try
        {
            using var serverSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            var endpoint = new UnixDomainSocketEndPoint(socketPath);
            serverSocket.Bind(endpoint);
            serverSocket.Listen(1);

            using var clientSocket = await serverSocket.AcceptAsync(cancellationToken).ConfigureAwait(false);
            using var stream = new NetworkStream(clientSocket, true);
            var rpc = JsonRpc.Attach(stream, cliRpcTarget);

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
            do
            {
                var sendTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var responseTimestamp = await rpc.InvokeAsync<long>("PingAsync", sendTimestamp).ConfigureAwait(false);
                Debug.Assert(sendTimestamp == responseTimestamp);
                var roundtripMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - sendTimestamp;
                logger.LogDebug("PingAsync round trip time is: {RoundtripMilliseconds} ms", roundtripMilliseconds);
            } while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException ex)
        {
            logger.LogDebug(ex, "Shutting down AppHost backchannel because of cancellation.");
            return;
        }
        catch (Exception ex)
        {
            _ = ex;
        }
    }

    public async Task<int> ExecuteAsync(string[] args, bool startBackchannel, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var a in args)
        {
            startInfo.ArgumentList.Add(a);
        }

        // The CLI and the AppHost can communicate to one another using a RPC protocol that makes
        // use of StreamJsonRpc. Not all commands need the backchannel so we selectively enable it.
        // When it is enabled we signal the path to the unix socket or named pipe on the
        // ASPIRE_CLI_BACKCHANNEL_PATH environment variable.
        if (startBackchannel)
        {
            var socketPath = GetBackchannelSocketPath();
            _ = StartBackchannelAsync(socketPath, cancellationToken);
            startInfo.EnvironmentVariables["ASPIRE_CLI_BACKCHANNEL_PATH"] = socketPath;
        }

        // The AppHost uses this environment variable to signal to the CliOrphanDetector which process
        // it should monitor in order to know when to stop the CLI. As long as the process still exists
        // the orphan detector will allow the CLI to keep running. If the environment variable does
        // not exist the orphan detector will exit.
        startInfo.EnvironmentVariables["ASPIRE_CLI_PID"] = GetCurrentProcessId().ToString(CultureInfo.InvariantCulture);

        using var process = new Process { StartInfo = startInfo };
        var started = process.Start();

        if (!started)
        {
            return ExitCodeConstants.FailedToDotnetRunAppHost;
        }

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        if (!process.HasExited)
        {
            process.Kill(false);
        }

        return process.ExitCode;
    }
}
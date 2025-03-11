// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using StreamJsonRpc;

namespace Aspire.Cli;

internal sealed class DotNetCliRunner(CliRpcTarget cliRpcTarget)
{
    internal Func<int> GetCurrentProcessId { get; set; } = () => Environment.ProcessId;

    public async Task<int> RunAsync(FileInfo projectFile, string[] args, CancellationToken cancellationToken)
    {
        string[] cliArgs = ["run", "--project", projectFile.FullName, "--", ..args];
        return await ExecuteAsync(cliArgs, cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> InstallTemplateAsync(string packageName, string version, bool force, CancellationToken cancellationToken)
    {
        string[] forceArgs = force ? ["--force"] : [];
        string[] cliArgs = ["new", "install", $"{packageName}::{version}", ..forceArgs];
        return await ExecuteAsync(cliArgs, cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> NewProjectAsync(string templateName, string name, string outputPath, CancellationToken cancellationToken)
    {
        string[] cliArgs = ["new", templateName, "--name", name, "--output", outputPath];
        return await ExecuteAsync(cliArgs, cancellationToken).ConfigureAwait(false);
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
        using var serverSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        var endpoint = new UnixDomainSocketEndPoint(socketPath);
        serverSocket.Bind(endpoint);
        serverSocket.Listen(1);

        using var clientSocket = await serverSocket.AcceptAsync(cancellationToken).ConfigureAwait(false);
        using var stream = new NetworkStream(clientSocket, true);
        var rpc = JsonRpc.Attach(stream, cliRpcTarget);
        rpc.StartListening();
    }

    public async Task<int> ExecuteAsync(string[] args, CancellationToken cancellationToken)
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

        var socketPath = GetBackchannelSocketPath();
        _ = StartBackchannelAsync(socketPath, cancellationToken);

        // The AppHost uses this environment variable to signal to the CliOrphanDetector which process
        // it should monitor in order to know when to stop the CLI. As long as the process still exists
        // the orphan detector will allow the CLI to keep running. If the environment variable does
        // not exist the orphan detector will exit.
        startInfo.EnvironmentVariables["ASPIRE_CLI_PID"] = GetCurrentProcessId().ToString(CultureInfo.InvariantCulture);
        startInfo.EnvironmentVariables["ASPIRE_CLI_BACKCHANNEL_PATH"] = Environment.GetEnvironmentVariable("ASPIRE_CLI_BACKCHANNEL_PATH") ?? string.Empty;

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
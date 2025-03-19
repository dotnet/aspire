// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StreamJsonRpc;

namespace Aspire.Cli;

internal sealed class DotNetCliRunner(ILogger<DotNetCliRunner> logger, CliRpcTarget cliRpcTarget)
{
    internal Func<int> GetCurrentProcessId { get; set; } = () => Environment.ProcessId;

    public async Task<int> RunAsync(FileInfo projectFile, string[] args, IDictionary<string, string>? env, CancellationToken cancellationToken)
    {
        string[] cliArgs = ["run", "--project", projectFile.FullName, "--", ..args];
        return await ExecuteAsync(
            args: cliArgs,
            env: env,
            workingDirectory: projectFile.Directory!,
            startBackchannel: true,
            streamsCallback: null,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> InstallTemplateAsync(string packageName, string version, string? source, bool force, CancellationToken cancellationToken)
    {
        List<string> cliArgs = ["new", "install", $"{packageName}::{version}"];

        if (force)
        {
            cliArgs.Add("--force");
        }

        if (source is not null)
        {
            cliArgs.Add("--nuget-source");
            cliArgs.Add(source);
        }

        return await ExecuteAsync(
            args: [.. cliArgs],
            env: null,
            workingDirectory: new DirectoryInfo(Environment.CurrentDirectory),
            startBackchannel: false,
            streamsCallback: null,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> NewProjectAsync(string templateName, string name, string outputPath, CancellationToken cancellationToken)
    {
        string[] cliArgs = ["new", templateName, "--name", name, "--output", outputPath];
        return await ExecuteAsync(
            args: cliArgs,
            env: null,
            workingDirectory: new DirectoryInfo(Environment.CurrentDirectory),
            startBackchannel: false,
            streamsCallback: null,
            cancellationToken: cancellationToken).ConfigureAwait(false);
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
            logger.LogDebug("Starting AppHost backchannel on socket path: {SocketPath}", socketPath);

            using var serverSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            var endpoint = new UnixDomainSocketEndPoint(socketPath);
            serverSocket.Bind(endpoint);
            serverSocket.Listen(1);

            logger.LogDebug("Waiting for AppHost to connect to backchannel on socket path: {SocketPath}", socketPath);

            using var clientSocket = await serverSocket.AcceptAsync(cancellationToken).ConfigureAwait(false);

            logger.LogDebug("AppHost to connected to backchannel on socket path: {SocketPath}", socketPath);

            using var stream = new NetworkStream(clientSocket, true);
            var rpc = JsonRpc.Attach(stream, cliRpcTarget);

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
            do
            {
                var sendTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                logger.LogDebug("Sending PingAsync to AppHost backchannel at {SocketPath}", socketPath);

                var responseTimestamp = await rpc.InvokeAsync<long>("PingAsync", sendTimestamp).ConfigureAwait(false);
                Debug.Assert(sendTimestamp == responseTimestamp);
                var roundtripMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - sendTimestamp;

                logger.LogDebug("AppHost PingAsync round trip time is: {RoundtripMilliseconds} ms", roundtripMilliseconds);
            } while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException ex)
        {
            logger.LogDebug(ex, "Shutting down AppHost backchannel because of cancellation.");
            return;
        }
        catch (StreamJsonRpc.ConnectionLostException ex) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug(ex, "Ignoring ConnectionLostException because of cancellation.");
            return;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AppHost backchannel failed unexpectedly.");
            return;
        }
    }

    public async Task<int> ExecuteAsync(string[] args, IDictionary<string, string>? env, DirectoryInfo workingDirectory, bool startBackchannel, Action<StreamWriter, StreamReader, StreamReader>? streamsCallback, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory.FullName,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        if (env is not null)
        {
            foreach (var envKvp in env)
            {
                startInfo.EnvironmentVariables[envKvp.Key] = envKvp.Value;
            }
        }

        foreach (var a in args)
        {
            startInfo.ArgumentList.Add(a);
        }

        // The CLI and the AppHost can communicate to one another using a RPC protocol that makes
        // use of StreamJsonRpc. Not all commands need the backchannel so we selectively enable it.
        // When it is enabled we signal the path to the unix socket or named pipe on the
        // ASPIRE_LAUNCHER_BACKCHANNEL_PATH environment variable.
        if (startBackchannel)
        {
            var socketPath = GetBackchannelSocketPath();
            _ = StartBackchannelAsync(socketPath, cancellationToken);
            startInfo.EnvironmentVariables["ASPIRE_LAUNCHER_BACKCHANNEL_PATH"] = socketPath;
        }

        // The AppHost uses this environment variable to signal to the CliOrphanDetector which process
        // it should monitor in order to know when to stop the CLI. As long as the process still exists
        // the orphan detector will allow the CLI to keep running. If the environment variable does
        // not exist the orphan detector will exit.
        startInfo.EnvironmentVariables["ASPIRE_CLI_PID"] = GetCurrentProcessId().ToString(CultureInfo.InvariantCulture);

        using var process = new Process { StartInfo = startInfo };

        logger.LogDebug("Running dotnet with args: {Args}", string.Join(" ", args));

        var started = process.Start();

        if (streamsCallback is null)
        {
            var pendingStdoutStreamForwarder = Task.Run(async () => {
                await ForwardStreamToLoggerAsync(
                    process.StandardOutput,
                    "stdout",
                    process,
                    cancellationToken).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);

            var pendingStderrStreamForwarder = Task.Run(async () => {
                await ForwardStreamToLoggerAsync(
                    process.StandardError,
                    "stderr",
                    process,
                    cancellationToken).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);
        }

        if (!started)
        {
            logger.LogDebug("Failed to start dotnet process with args: {Args}", string.Join(" ", args));
            return ExitCodeConstants.FailedToDotnetRunAppHost;
        }
        else
        {
            logger.LogDebug("Started dotnet with PID: {ProcessId}", process.Id);
        }

        // This is so that callers can get a handle to the raw stream output. This is important
        // because some commmands (like package search) return JSON data that we need to parse.
        streamsCallback?.Invoke(process.StandardInput, process.StandardOutput, process.StandardError);

        logger.LogDebug("Waiting for dotnet process to exit with PID: {ProcessId}", process.Id);

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        if (!process.HasExited)
        {
            logger.LogDebug("dotnet process with PID: {ProcessId} has not exited, killing it.", process.Id);
            process.Kill(false);
        }
        else
        {
            logger.LogDebug("dotnet process with PID: {ProcessId} has exited with code: {ExitCode}", process.Id, process.ExitCode);
        }

        return process.ExitCode;

        async Task ForwardStreamToLoggerAsync(StreamReader reader, string identifier, Process process, CancellationToken cancellationToken)
        {
            logger.LogDebug(
                "Starting to forward stream with identifier '{Identifier}' on process '{ProcessId}' to logger",
                identifier,
                process.Id
                );

            while (!cancellationToken.IsCancellationRequested && !reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                logger.LogDebug(
                    "dotnet({ProcessId}) {Identifier}: {Line}",
                    process.Id,
                    identifier,
                    line
                    );
            }
        }
    }

    public async Task<int> AddPackageAsync(FileInfo projectFilepath, string packageName, string packageVersion, CancellationToken cancellationToken)
    {
        string[] cliArgs = [
            "add",
            projectFilepath.FullName,
            "package",
            packageName,
            "--version",
            packageVersion
        ];

        logger.LogInformation("Adding package {PackageName} with version {PackageVersion} to project {ProjectFilePath}", packageName, packageVersion, projectFilepath.FullName);

        var result = await ExecuteAsync(
            args: cliArgs,
            env: null,
            workingDirectory: projectFilepath.Directory!,
            startBackchannel: false,
            streamsCallback: (_, _, _) => { },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result != 0)
        {
            logger.LogError("Failed to add package {PackageName} with version {PackageVersion} to project {ProjectFilePath}. See debug logs for more details.", packageName, packageVersion, projectFilepath.FullName);
        }
        else
        {
            logger.LogInformation("Package {PackageName} with version {PackageVersion} added to project {ProjectFilePath}", packageName, packageVersion, projectFilepath.FullName);
        }

        return result;
    }

    public async Task<(int ExitCode, NuGetPackage[]? Packages)> SearchPackagesAsync(FileInfo projectFilePath, string query, bool prerelease, int take, int skip, string? nugetSource, CancellationToken cancellationToken)
    {
        List<string> cliArgs = [
            "package",
            "search",
            query,
            "--take",
            take.ToString(CultureInfo.InvariantCulture),
            "--skip",
            skip.ToString(CultureInfo.InvariantCulture),
            "--format",
            "json"
        ];

        if (nugetSource is not null)
        {
            cliArgs.Add("--source");
            cliArgs.Add(nugetSource);
        }

        if (prerelease)
        {
            cliArgs.Add("--prerelease");
        }

        string? stdout = null;
        string? stderr = null;

        var result = await ExecuteAsync(
            args: cliArgs.ToArray(),
            env: null,
            workingDirectory: projectFilePath.Directory!,
            startBackchannel: false,
            streamsCallback: (_, output, _) => {
                // We need to read the output of the streams
                // here otherwise th process will never exit.
                stdout = output.ReadToEnd();
                stderr = output.ReadToEnd();
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result != 0)
        {
            logger.LogError(
                "Failed to search for packages. See debug logs for more details. Stderr: {Stderr}, Stdout: {Stdout}",
                stderr,
                stdout
                );
            return (result, null);
        }
        else
        {
            if (stdout is null)
            {
                logger.LogError("Failed to read stdout from the process. This should never happen.");
                return (ExitCodeConstants.FailedToAddPackage, null);
            }

            var foundPackages = new List<NuGetPackage>();
            var document = JsonDocument.Parse(stdout);

            var searchResultsArray = document.RootElement.GetProperty("searchResult");

            foreach (var sourceResult in searchResultsArray.EnumerateArray())
            {
                var source = sourceResult.GetProperty("sourceName").GetString();
                var sourcePackagesArray = sourceResult.GetProperty("packages");

                foreach (var packageResult in sourcePackagesArray.EnumerateArray())
                {
                    var id = packageResult.GetProperty("id").GetString();
                    var version = packageResult.GetProperty("latestVersion").GetString();

                    foundPackages.Add(new NuGetPackage
                    {
                        Id = id!,
                        Version = version!,
                        Source = source!
                    });
                }
            }

            return (result, foundPackages.ToArray());
        }
    }
}

internal class NuGetPackage
{
    public string Id { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}
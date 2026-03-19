// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace Aspire.Extension.EndToEndTests.Infrastructure;

/// <summary>
/// Manages a Docker container running VS Code via <c>code serve-web</c>.
/// </summary>
internal sealed partial class VsCodeContainer : IAsyncDisposable
{
    private const string ImageName = "aspire-vscode-e2e";
    private const int ContainerPort = 8000;
    private const int Hex1bPortBase = 9222;
    private const int Hex1bPortCount = 4;
    private const string ReadyPattern = @"Web UI available at (.+)";

    private readonly ITestOutputHelper _output;
    private readonly string _dockerfilePath;
    private readonly AspireBuildArtifacts? _artifacts;
    private readonly bool _mountDockerSocket;
    private string? _containerId;
    private int _hostPort;
    private int _hex1bHostPortBase;
    private int _nextHex1bPortOffset;

    public VsCodeContainer(ITestOutputHelper output, AspireBuildArtifacts? artifacts = null, bool mountDockerSocket = false)
    {
        _output = output;
        _artifacts = artifacts;
        _mountDockerSocket = mountDockerSocket;

        // Find the Dockerfile relative to the repo root
        var repoRoot = FindRepoRoot();
        _dockerfilePath = Path.Combine(repoRoot, "tests", "Shared", "Docker", "Dockerfile.e2e-vscode");

        if (!File.Exists(_dockerfilePath))
        {
            throw new FileNotFoundException($"Dockerfile not found at {_dockerfilePath}");
        }
    }

    /// <summary>
    /// Gets the URL where VS Code is available after <see cref="StartAsync"/> completes.
    /// </summary>
    public string? Url { get; private set; }

    /// <summary>
    /// Gets the Docker container ID, available after <see cref="StartAsync"/> completes.
    /// </summary>
    public string? ContainerId => _containerId;

    /// <summary>
    /// Builds the Docker image and starts the container.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await BuildImageAsync(cancellationToken);
        await RunContainerAsync(cancellationToken);
    }

    private async Task BuildImageAsync(CancellationToken cancellationToken)
    {
        _output.WriteLine($"Building Docker image '{ImageName}' from {_dockerfilePath}...");

        var repoRoot = FindRepoRoot();
        var result = await RunDockerAsync(
            $"build -f {_dockerfilePath} -t {ImageName} {repoRoot}",
            timeout: TimeSpan.FromMinutes(10),
            cancellationToken: cancellationToken);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Docker build failed (exit code {result.ExitCode}):\n{result.StdErr}");
        }

        _output.WriteLine("Docker image built successfully.");
    }

    private async Task RunContainerAsync(CancellationToken cancellationToken)
    {
        // Use a random port to avoid conflicts
        _hostPort = GetRandomPort();
        _hex1bHostPortBase = GetRandomPort();

        _output.WriteLine($"Starting container on port {_hostPort}...");
        _output.WriteLine($"  Hex1b WebSocket ports: {_hex1bHostPortBase}-{_hex1bHostPortBase + Hex1bPortCount - 1} (host) → {Hex1bPortBase}-{Hex1bPortBase + Hex1bPortCount - 1} (container)");

        // Build volume mount and port arguments
        var volumes = new List<string>();
        var portMappings = new List<string>
        {
            $"-p {_hostPort}:{ContainerPort}",
        };

        // Map a range of ports for hex1b WebSocket connections
        for (int i = 0; i < Hex1bPortCount; i++)
        {
            portMappings.Add($"-p {_hex1bHostPortBase + i}:{Hex1bPortBase + i}");
        }

        if (_mountDockerSocket)
        {
            volumes.Add("-v /var/run/docker.sock:/var/run/docker.sock");
            _output.WriteLine("  Docker socket: mounted");
        }

        if (_artifacts is not null)
        {
            volumes.Add($"-v {_artifacts.CliPublishDirectory}:{AspireBuildArtifacts.ContainerPaths.CliMount}:ro");
            volumes.Add($"-v {_artifacts.VsixPath}:{AspireBuildArtifacts.ContainerPaths.VsixMount}:ro");
            volumes.Add($"-v {_artifacts.PackagesDirectory}:{AspireBuildArtifacts.ContainerPaths.PackagesMount}:ro");
            volumes.Add($"-v {_artifacts.NuGetConfigPath}:{AspireBuildArtifacts.ContainerPaths.NuGetConfigMount}:ro");
            _output.WriteLine($"  CLI binary:    {_artifacts.CliPublishDirectory}");
            _output.WriteLine($"  VSIX:          {_artifacts.VsixPath}");
            _output.WriteLine($"  NuGet pkgs:    {_artifacts.PackagesDirectory}");
            _output.WriteLine($"  NuGet config:  {_artifacts.NuGetConfigPath}");
        }

        var volumeArgs = string.Join(" ", volumes);
        var portArgs = string.Join(" ", portMappings);
        var result = await RunDockerAsync(
            $"run -d {portArgs} {volumeArgs} {ImageName}",
            timeout: TimeSpan.FromSeconds(30),
            cancellationToken: cancellationToken);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Docker run failed (exit code {result.ExitCode}):\n{result.StdErr}");
        }

        _containerId = result.StdOut.Trim();
        _output.WriteLine($"Container started: {_containerId[..12]}");

        // Wait for VS Code to be ready by monitoring container logs
        Url = await WaitForReadyAsync(cancellationToken);
        _output.WriteLine($"VS Code available at: {Url}");
    }

    private async Task<string> WaitForReadyAsync(CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromMinutes(3);
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token);

        var regex = ReadyRegex();
        var startTime = Stopwatch.GetTimestamp();

        while (!linkedCts.Token.IsCancellationRequested)
        {
            var logs = await RunDockerAsync(
                $"logs {_containerId}",
                timeout: TimeSpan.FromSeconds(10),
                cancellationToken: linkedCts.Token);

            // Check both stdout and stderr (VS Code may log to either)
            var allOutput = logs.StdOut + logs.StdErr;
            var match = regex.Match(allOutput);

            if (match.Success)
            {
                var internalUrl = match.Groups[1].Value.Trim();

                // Replace the container's internal port with our mapped host port
                // The internal URL will be like http://0.0.0.0:8000 or http://localhost:8000
                return $"http://localhost:{_hostPort}";
            }

            var elapsed = Stopwatch.GetElapsedTime(startTime);
            _output.WriteLine($"Waiting for VS Code to be ready... ({elapsed.TotalSeconds:F0}s)");

            await Task.Delay(TimeSpan.FromSeconds(2), linkedCts.Token);
        }

        // Dump final logs for debugging
        var finalLogs = await RunDockerAsync($"logs {_containerId}",
            timeout: TimeSpan.FromSeconds(5),
            cancellationToken: CancellationToken.None);
        _output.WriteLine($"Container stdout:\n{finalLogs.StdOut}");
        _output.WriteLine($"Container stderr:\n{finalLogs.StdErr}");

        throw new TimeoutException(
            $"VS Code did not become ready within {timeout.TotalMinutes} minutes");
    }

    public async ValueTask DisposeAsync()
    {
        if (_containerId is not null)
        {
            _output.WriteLine($"Stopping container {_containerId[..12]}...");

            try
            {
                await RunDockerAsync($"stop -t 5 {_containerId}",
                    timeout: TimeSpan.FromSeconds(15),
                    cancellationToken: CancellationToken.None);

                await RunDockerAsync($"rm -f {_containerId}",
                    timeout: TimeSpan.FromSeconds(10),
                    cancellationToken: CancellationToken.None);
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Warning: container cleanup failed: {ex.Message}");
            }

            _containerId = null;
        }
    }

    private static async Task<DockerResult> RunDockerAsync(
        string arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                stdOut.AppendLine(e.Data);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                stdErr.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token);

        try
        {
            await process.WaitForExitAsync(linkedCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"Docker command timed out after {timeout}: docker {arguments}");
        }

        return new DockerResult(process.ExitCode, stdOut.ToString(), stdErr.ToString());
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Aspire.slnx")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not find repo root (looking for Aspire.slnx)");
    }

    private static int GetRandomPort()
    {
        // Use a TcpListener to find a free port
        var listener = new System.Net.Sockets.TcpListener(
            System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    [GeneratedRegex(ReadyPattern)]
    private static partial Regex ReadyRegex();

    /// <summary>
    /// Runs a command inside the container via <c>docker exec</c>.
    /// </summary>
    public async Task<DockerResult> ExecAsync(string command, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        if (_containerId is null)
        {
            throw new InvalidOperationException("Container is not running.");
        }

        return await RunDockerAsync(
            $"exec {_containerId} bash -c \"{command.Replace("\"", "\\\"")}\"",
            timeout: timeout ?? TimeSpan.FromSeconds(30),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Allocates the next available hex1b port pair (container port + host port).
    /// Returns the container port to use in <c>hex1b terminal start --passthru --port PORT --bind 0.0.0.0</c>
    /// and the host-side WebSocket URI for connecting from the test.
    /// </summary>
    public (int containerPort, Uri websocketUri) AllocateHex1bPort()
    {
        if (_nextHex1bPortOffset >= Hex1bPortCount)
        {
            throw new InvalidOperationException(
                $"All {Hex1bPortCount} hex1b ports have been allocated. Increase Hex1bPortCount if more are needed.");
        }

        var offset = _nextHex1bPortOffset++;
        var containerPort = Hex1bPortBase + offset;
        var hostPort = _hex1bHostPortBase + offset;

        var uri = new Uri($"ws://localhost:{hostPort}/ws/attach");
        _output.WriteLine($"Allocated hex1b port: container={containerPort}, host={hostPort}, uri={uri}");
        return (containerPort, uri);
    }

    /// <summary>
    /// Waits for a hex1b WebSocket endpoint to become available by attempting TCP connections.
    /// </summary>
    public async Task WaitForHex1bAsync(Uri websocketUri, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(60);
        using var timeoutCts = new CancellationTokenSource(effectiveTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token);

        var port = websocketUri.Port;
        _output.WriteLine($"Waiting for hex1b WebSocket on port {port}...");
        var startTime = Stopwatch.GetTimestamp();

        while (!linkedCts.Token.IsCancellationRequested)
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await socket.ConnectAsync("localhost", port, linkedCts.Token);
                var elapsed = Stopwatch.GetElapsedTime(startTime);
                _output.WriteLine($"Hex1b WebSocket port {port} is ready ({elapsed.TotalSeconds:F1}s)");
                return;
            }
            catch (SocketException)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500), linkedCts.Token);
            }
        }

        throw new TimeoutException(
            $"Hex1b WebSocket endpoint at port {port} did not become available within {effectiveTimeout.TotalSeconds}s");
    }

    /// <summary>
    /// Resets the hex1b port allocation counter so ports can be reused
    /// (e.g., after killing previous hex1b processes).
    /// </summary>
    public void ResetHex1bPorts()
    {
        _nextHex1bPortOffset = 0;
    }

    /// <summary>
    /// Copies a file from the container to the host via <c>docker cp</c>.
    /// </summary>
    public async Task CopyFromContainerAsync(string containerPath, string hostPath, CancellationToken cancellationToken = default)
    {
        if (_containerId is null)
        {
            throw new InvalidOperationException("Container is not running.");
        }

        var hostDir = Path.GetDirectoryName(hostPath);
        if (hostDir is not null)
        {
            Directory.CreateDirectory(hostDir);
        }

        var result = await RunDockerAsync(
            $"cp {_containerId}:{containerPath} {hostPath}",
            timeout: TimeSpan.FromSeconds(30),
            cancellationToken: cancellationToken);

        if (result.ExitCode != 0)
        {
            _output.WriteLine($"Warning: docker cp failed: {result.StdErr}");
        }
    }

    internal sealed record DockerResult(int ExitCode, string StdOut, string StdErr);
}

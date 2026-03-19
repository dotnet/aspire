// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
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
    private const string ReadyPattern = @"Web UI available at (.+)";

    private readonly ITestOutputHelper _output;
    private readonly string _dockerfilePath;
    private string? _containerId;
    private int _hostPort;

    public VsCodeContainer(ITestOutputHelper output)
    {
        _output = output;

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

        _output.WriteLine($"Starting container on port {_hostPort}...");

        var result = await RunDockerAsync(
            $"run -d -p {_hostPort}:{ContainerPort} {ImageName}",
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

    private sealed record DockerResult(int ExitCode, string StdOut, string StdErr);
}

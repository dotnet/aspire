// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using Aspire.Cli.DotNet;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Dcp;

/// <summary>
/// Launches DCP instances owned by the CLI, with the DCP process monitoring
/// the CLI process instead of the AppHost process.
/// </summary>
internal sealed class DcpLauncher : IDcpLauncher
{
    private const int LoggingSocketConnectionBacklog = 3;
    private const int KubeconfigWaitTimeoutMs = 30000; // 30 seconds
    private const int KubeconfigPollIntervalMs = 100;

    private readonly IDcpSessionManager _sessionManager;
    private readonly ILogger<DcpLauncher> _logger;

    private Process? _dcpProcess;
    private DcpSession? _session;
    private Task? _logProcessorTask;
    private CancellationTokenSource? _logCancellation;

    public DcpLauncher(IDcpSessionManager sessionManager, ILogger<DcpLauncher> logger)
    {
        _sessionManager = sessionManager;
        _logger = logger;
    }

    public async Task<DcpSession> LaunchAsync(AppHostInfo appHostInfo, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(appHostInfo.DcpCliPath))
        {
            throw new InvalidOperationException("DCP CLI path is not available. The AppHost may not have the DCP orchestration package installed.");
        }

        if (!File.Exists(appHostInfo.DcpCliPath))
        {
            throw new FileNotFoundException($"DCP executable not found at '{appHostInfo.DcpCliPath}'.", appHostInfo.DcpCliPath);
        }

        _session = _sessionManager.CreateSession();
        _logCancellation = new CancellationTokenSource();

        // Set up log socket
        Socket? logSocket = null;
        try
        {
            logSocket = CreateLoggingSocket(_session.LogSocketPath);
            logSocket.Listen(LoggingSocketConnectionBacklog);

            // Start log processor in background (don't pass cancellationToken to Task.Run since it's long-running)
            _logProcessorTask = Task.Run(() => ProcessLogsToFileAsync(logSocket, _logCancellation.Token), CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create logging socket. DCP logs will not be captured.");
        }

        // Build DCP arguments
        // Critical: --monitor {CLI_PID} instead of AppHost PID
        var arguments = new StringBuilder();
        arguments.Append(CultureInfo.InvariantCulture, $"start-apiserver --monitor {Environment.ProcessId} --detach --kubeconfig \"{_session.KubeconfigPath}\"");

        if (!string.IsNullOrEmpty(appHostInfo.ContainerRuntime))
        {
            arguments.Append(CultureInfo.InvariantCulture, $" --container-runtime \"{appHostInfo.ContainerRuntime}\"");
        }

        _logger.LogDebug("Starting DCP with arguments: {Arguments}", arguments.ToString());

        // Build environment variables
        var env = new Dictionary<string, string>
        {
            ["DCP_SESSION_FOLDER"] = _session.SessionDir
        };

        if (!string.IsNullOrEmpty(appHostInfo.DcpExtensionsPath))
        {
            env["DCP_EXTENSIONS_PATH"] = appHostInfo.DcpExtensionsPath;
        }

        if (!string.IsNullOrEmpty(appHostInfo.DcpBinPath))
        {
            env["DCP_BIN_PATH"] = appHostInfo.DcpBinPath;
        }

        if (logSocket != null)
        {
            env["DCP_LOG_SOCKET"] = _session.LogSocketPath;
        }

        // Copy environment variables from current process (except ASP.NET Core specific ones)
        var doNotInherit = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ASPNETCORE_URLS",
            "DOTNET_LAUNCH_PROFILE",
            "ASPNETCORE_ENVIRONMENT",
            "DOTNET_ENVIRONMENT"
        };

        foreach (var key in Environment.GetEnvironmentVariables().Keys.Cast<string>())
        {
            if (!doNotInherit.Contains(key) && !env.ContainsKey(key))
            {
                env[key] = Environment.GetEnvironmentVariable(key)!;
            }
        }

        // Start DCP process
        var startInfo = new ProcessStartInfo
        {
            FileName = appHostInfo.DcpCliPath,
            Arguments = arguments.ToString(),
            WorkingDirectory = Directory.GetCurrentDirectory(),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var (key, value) in env)
        {
            startInfo.Environment[key] = value;
        }

        _dcpProcess = Process.Start(startInfo);

        if (_dcpProcess == null)
        {
            throw new InvalidOperationException("Failed to start DCP process.");
        }

        _logger.LogInformation("Started DCP process with PID {ProcessId}, monitoring CLI PID {CliPid}", _dcpProcess.Id, Environment.ProcessId);

        // Forward stdout/stderr to logger (long-running tasks, don't pass cancellation)
        _ = Task.Run(async () =>
        {
            try
            {
                string? line;
                while ((line = await _dcpProcess.StandardOutput.ReadLineAsync(CancellationToken.None).ConfigureAwait(false)) != null)
                {
                    _logger.LogDebug("[DCP stdout] {Line}", line);
                }
            }
            catch
            {
                // Ignore errors reading stdout
            }
        }, CancellationToken.None);

        _ = Task.Run(async () =>
        {
            try
            {
                string? line;
                while ((line = await _dcpProcess.StandardError.ReadLineAsync(CancellationToken.None).ConfigureAwait(false)) != null)
                {
                    _logger.LogWarning("[DCP stderr] {Line}", line);
                }
            }
            catch
            {
                // Ignore errors reading stderr
            }
        }, CancellationToken.None);

        // Wait for kubeconfig to be created
        await WaitForKubeconfigAsync(_session.KubeconfigPath, cancellationToken);

        return _session;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logCancellation?.Cancel();

        if (_dcpProcess != null && !_dcpProcess.HasExited)
        {
            _logger.LogDebug("Stopping DCP process with PID {ProcessId}", _dcpProcess.Id);

            try
            {
                // Send SIGINT on Unix, taskkill on Windows
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    _dcpProcess.Kill(entireProcessTree: true);
                }
                else
                {
                    // Send SIGINT (Ctrl+C equivalent)
                    var killResult = Process.Start(new ProcessStartInfo
                    {
                        FileName = "/bin/kill",
                        Arguments = $"-2 {_dcpProcess.Id}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                    killResult?.WaitForExit();
                }

                // Wait up to 5 seconds for graceful exit
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

                try
                {
                    await _dcpProcess.WaitForExitAsync(linked.Token);
                }
                catch (OperationCanceledException) when (timeout.IsCancellationRequested)
                {
                    // Force kill if still running
                    _logger.LogWarning("DCP process did not exit gracefully, forcing termination.");
                    _dcpProcess.Kill(entireProcessTree: true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error while stopping DCP process.");
            }
        }

        if (_logProcessorTask != null)
        {
            try
            {
                await _logProcessorTask.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken);
            }
            catch
            {
                // Ignore errors waiting for log processor
            }
        }

        _session?.Dispose();
    }

    private async Task WaitForKubeconfigAsync(string kubeconfigPath, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Waiting for kubeconfig at {Path}", kubeconfigPath);

        using var timeout = new CancellationTokenSource(KubeconfigWaitTimeoutMs);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        while (!linked.IsCancellationRequested)
        {
            if (File.Exists(kubeconfigPath))
            {
                // Also verify the file is not empty
                var info = new FileInfo(kubeconfigPath);
                if (info.Length > 0)
                {
                    _logger.LogDebug("Kubeconfig found at {Path} ({Size} bytes)", kubeconfigPath, info.Length);
                    return;
                }
            }

            try
            {
                await Task.Delay(KubeconfigPollIntervalMs, linked.Token);
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
                throw new TimeoutException($"Timed out waiting for kubeconfig at '{kubeconfigPath}'. DCP may have failed to start.");
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    private static Socket CreateLoggingSocket(string socketPath)
    {
        var directoryName = Path.GetDirectoryName(socketPath);
        if (!string.IsNullOrEmpty(directoryName))
        {
            if (OperatingSystem.IsWindows())
            {
                Directory.CreateDirectory(directoryName);
            }
            else
            {
                Directory.CreateDirectory(directoryName, UnixFileMode.UserExecute | UnixFileMode.UserWrite | UnixFileMode.UserRead);
            }
        }

        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        socket.Bind(new UnixDomainSocketEndPoint(socketPath));

        return socket;
    }

    private async Task ProcessLogsToFileAsync(Socket socket, CancellationToken cancellationToken)
    {
        // Create log file in ~/.aspire/cli/logs/
        var logsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".aspire", "cli", "logs");
        Directory.CreateDirectory(logsDir);

        var logFile = Path.Combine(logsDir, $"dcp-{DateTime.UtcNow:yyyyMMdd-HHmmss}.log");
        _logger.LogDebug("Writing DCP logs to {LogFile}", logFile);

        using var fileStream = new FileStream(logFile, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var writer = new StreamWriter(fileStream) { AutoFlush = true };

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var acceptedSocket = await socket.AcceptAsync(cancellationToken);
                _ = Task.Run(() => ProcessSocketConnectionAsync(acceptedSocket, writer, cancellationToken), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error accepting log socket connection.");
            }
        }

        socket.Dispose();
    }

    private async Task ProcessSocketConnectionAsync(Socket socket, StreamWriter writer, CancellationToken cancellationToken)
    {
        using var stream = new NetworkStream(socket, ownsSocket: true);
        using var reader = new StreamReader(stream);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line == null)
                {
                    break;
                }

                await writer.WriteLineAsync(line);
                _logger.LogDebug("DCP log: {Line}", line);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                break;
            }
        }
    }
}

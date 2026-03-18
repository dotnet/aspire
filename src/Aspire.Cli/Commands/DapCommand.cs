// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Aspire.Cli.Configuration;
using Aspire.Cli.DebugAdapter;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Aspire.DebugAdapter.Protocol;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Commands;

/// <summary>
/// Connection mode for the downstream debug adapter.
/// </summary>
internal enum DownstreamConnectionMode
{
    /// <summary>
    /// Use stdin/stdout to communicate with the downstream adapter.
    /// </summary>
    Stdio,

    /// <summary>
    /// Bind a callback port and pass it to the downstream adapter via {port} template.
    /// </summary>
    Callback
}

/// <summary>
/// A command that acts as a debug adapter protocol middleware, bridging between
/// an IDE (upstream) and a downstream debug adapter process.
/// </summary>
internal sealed class DapCommand : BaseCommand
{
    private static readonly TimeSpan s_gracefulShutdownTimeout = TimeSpan.FromSeconds(5);

    private readonly ILogger<DapCommand> _logger;
    private readonly IAppHostProjectFactory _appHostProjectFactory;
    private readonly ILanguageDiscovery _languageDiscovery;
    private readonly IServiceProvider _serviceProvider;
    private StreamWriter? _logFileWriter;
    private readonly Lock _logLock = new();

    private static readonly Option<DownstreamConnectionMode> s_modeOption = new("--mode", "-m")
    {
        Description = "Connection mode for the downstream adapter (stdio or callback)",
        DefaultValueFactory = _ => DownstreamConnectionMode.Stdio
    };

    private static readonly Argument<string?> s_commandArgument = new("command")
    {
        Description = "Path to the downstream debug adapter executable to launch",
        Arity = ArgumentArity.ZeroOrOne
    };

    private static readonly Argument<string[]> s_commandArgsArgument = new("arguments")
    {
        Description = "Arguments to pass to the downstream debug adapter. Use {port} as a placeholder for the callback port in callback mode.",
        Arity = ArgumentArity.ZeroOrMore
    };

    private static readonly Option<FileInfo?> s_logFileOption = new("--log-file", "-l")
    {
        Description = "Path to a file where diagnostic logs will be written"
    };

    private static readonly Option<string> s_adapterIdOption = new("--adapter-id", "-a")
    {
        Description = "The adapter ID to use when forwarding initialize requests to the downstream debugger (e.g., coreclr, python, node)",
        DefaultValueFactory = _ => "coreclr"
    };

    private static readonly Option<bool> s_polyglotOption = new("--polyglot", "-p")
    {
        Description = "Enable polyglot mode: start an AppHost server and inject environment variables for non-.NET languages"
    };

    private static readonly Option<bool> s_ideSessionServerOption = new("--ide-session-server", "-s")
    {
        Description = "Enable IDE session server: start an HTTPS server for DCP to connect for run session management"
    };

    private static readonly Option<string?> s_bridgeSocketOption = new("--bridge-socket")
    {
        Description = "Path to DCP's debug bridge Unix domain socket (enables bridge mode)"
    };

    private static readonly Option<string?> s_bridgeTokenOption = new("--bridge-token")
    {
        Description = "Bearer token for authenticating with the debug bridge"
    };

    private static readonly Option<string?> s_bridgeSessionIdOption = new("--bridge-session-id")
    {
        Description = "Session ID for the debug bridge handshake (from DCP)"
    };

    private static readonly Option<string?> s_bridgeRunIdOption = new("--bridge-run-id")
    {
        Description = "Run ID for correlating the debug session with the IDE session server"
    };

    private static readonly Option<bool> s_checkOption = new("--check")
    {
        Description = "Check whether the DAP middleware is available and exit. Writes a JSON object to stdout."
    };

    /// <summary>
    /// Suppress update notifications since DAP protocol must not have extraneous stdout output.
    /// </summary>
    protected override bool UpdateNotificationsEnabled => false;

    public DapCommand(
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        IInteractionService interactionService,
        AspireCliTelemetry telemetry,
        IAppHostProjectFactory appHostProjectFactory,
        ILanguageDiscovery languageDiscovery,
        IServiceProvider serviceProvider,
        ILogger<DapCommand> logger)
        : base("dap", "Run as a debug adapter protocol middleware", features, updateNotifier, executionContext, interactionService, telemetry)
    {
        _logger = logger;
        _appHostProjectFactory = appHostProjectFactory;
        _languageDiscovery = languageDiscovery;
        _serviceProvider = serviceProvider;

        Options.Add(s_modeOption);
        Options.Add(s_logFileOption);
        Options.Add(s_adapterIdOption);
        Options.Add(s_polyglotOption);
        Options.Add(s_ideSessionServerOption);
        Options.Add(s_bridgeSocketOption);
        Options.Add(s_bridgeTokenOption);
        Options.Add(s_bridgeSessionIdOption);
        Options.Add(s_bridgeRunIdOption);
        Options.Add(s_checkOption);
        Arguments.Add(s_commandArgument);
        Arguments.Add(s_commandArgsArgument);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        // Handle --check: report DAP availability and exit immediately
        var check = parseResult.GetValue(s_checkOption);
        if (check)
        {
            Console.Out.Write("{\"dapSupported\":true}");
            Console.Out.Flush();
            return ExitCodeConstants.Success;
        }

        var command = parseResult.GetValue(s_commandArgument);
        if (string.IsNullOrEmpty(command))
        {
            LogToFile("No downstream adapter command specified");
            _logger.LogError("The 'command' argument is required when not using --check");
            return ExitCodeConstants.InvalidCommand;
        }

        var commandArgs = parseResult.GetValue(s_commandArgsArgument) ?? [];
        var mode = parseResult.GetValue(s_modeOption);
        var logFile = parseResult.GetValue(s_logFileOption);
        var adapterId = parseResult.GetValue(s_adapterIdOption)!;
        var polyglotMode = parseResult.GetValue(s_polyglotOption);
        var ideSessionServerMode = parseResult.GetValue(s_ideSessionServerOption);
        var bridgeSocket = parseResult.GetValue(s_bridgeSocketOption);
        var bridgeToken = parseResult.GetValue(s_bridgeTokenOption);
        var bridgeSessionId = parseResult.GetValue(s_bridgeSessionIdOption);
        var bridgeRunId = parseResult.GetValue(s_bridgeRunIdOption);

        // Set up file logging if requested
        if (logFile is not null)
        {
            logFile.Directory?.Create();
            _logFileWriter = new StreamWriter(logFile.FullName, append: true) { AutoFlush = true };
            LogToFile($"=== DAP middleware starting (mode={mode}, adapterId={adapterId}, polyglot={polyglotMode}, ideSessionServer={ideSessionServerMode}, bridge={bridgeSocket is not null}) ===");
        }

        try
        {
            // Check if this is bridge mode (connecting to DCP's debug bridge)
            if (!string.IsNullOrEmpty(bridgeSocket))
            {
                if (string.IsNullOrEmpty(bridgeToken))
                {
                    LogToFile("Bridge mode requires --bridge-token");
                    _logger.LogError("Bridge mode requires --bridge-token");
                    return ExitCodeConstants.InvalidCommand;
                }
                if (string.IsNullOrEmpty(bridgeSessionId))
                {
                    LogToFile("Bridge mode requires --bridge-session-id");
                    _logger.LogError("Bridge mode requires --bridge-session-id");
                    return ExitCodeConstants.InvalidCommand;
                }
                if (string.IsNullOrEmpty(bridgeRunId))
                {
                    LogToFile("Bridge mode requires --bridge-run-id");
                    _logger.LogError("Bridge mode requires --bridge-run-id");
                    return ExitCodeConstants.InvalidCommand;
                }

                var modeString = mode.ToString().ToLowerInvariant();
                return await RunBridgeModeAsync(bridgeSocket, bridgeToken, bridgeSessionId, bridgeRunId, modeString, adapterId, command, commandArgs, cancellationToken);
            }

            // Join arguments into a single string for ProcessStartInfo
            var launchArgs = string.Join(" ", commandArgs.Select(EscapeArgument));

            _logger.LogDebug("Starting DAP middleware with mode={Mode}, adapterId={AdapterId}, polyglot={Polyglot}, ideSessionServer={IdeSessionServer}, command={Command}, args={Args}",
                mode, adapterId, polyglotMode, ideSessionServerMode, command, launchArgs);

            return mode switch
            {
                DownstreamConnectionMode.Stdio => await RunStdioModeAsync(command, launchArgs, adapterId, polyglotMode, ideSessionServerMode, cancellationToken),
                DownstreamConnectionMode.Callback => await RunCallbackModeAsync(command, commandArgs, adapterId, polyglotMode, ideSessionServerMode, cancellationToken),
                _ => throw new InvalidOperationException($"Unknown connection mode: {mode}")
            };
        }
        catch (Exception ex)
        {
            LogToFile($"ERROR: {ex}");
            throw;
        }
        finally
        {
            _logFileWriter?.Dispose();
            _logFileWriter = null;
        }
    }

    private void LogToFile(string message)
    {
        lock (_logLock)
        {
            _logFileWriter?.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] {message}");
        }
    }

    /// <summary>
    /// Escapes an argument for use in a command line string.
    /// </summary>
    private static string EscapeArgument(string arg)
    {
        // If the argument contains spaces or quotes, wrap it in quotes and escape internal quotes
        if (arg.Contains(' ', StringComparison.Ordinal) || arg.Contains('"', StringComparison.Ordinal))
        {
            return $"\"{arg.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
        }
        return arg;
    }

    private async Task<int> RunStdioModeAsync(string command, string launchArgs, string adapterId, bool polyglotMode, bool ideSessionServerMode, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Launching downstream adapter in stdio mode: {Command} {Args}", command, launchArgs);

        var startInfo = new ProcessStartInfo(command, launchArgs)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };

        try
        {
            if (!process.Start())
            {
                LogToFile("Failed to start downstream debug adapter process (Start() returned false)");
                _logger.LogError("Failed to start downstream debug adapter process");
                return ExitCodeConstants.FailedToLaunchDebugAdapter;
            }
        }
        catch (Exception ex)
        {
            LogToFile($"Exception starting downstream adapter: {ex}");
            _logger.LogError(ex, "Failed to start downstream debug adapter process");
            return ExitCodeConstants.FailedToLaunchDebugAdapter;
        }

        _logger.LogDebug("Downstream adapter started with PID {Pid}", process.Id);

        // Forward stderr from downstream adapter to our stderr for IDE logging
        var stderrTask = ForwardStderrAsync(process, cancellationToken);

        try
        {
            return await RunMiddlewareAsync(
                process.StandardOutput.BaseStream,
                process.StandardInput.BaseStream,
                process,
                adapterId,
                polyglotMode,
                ideSessionServerMode,
                disposeOnComplete: null,
                cancellationToken);
        }
        finally
        {
            await stderrTask;
        }
    }

    private async Task<int> RunCallbackModeAsync(string command, string[] commandArgs, string adapterId, bool polyglotMode, bool ideSessionServerMode, CancellationToken cancellationToken)
    {
        // Bind to an OS-assigned port
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        _logger.LogDebug("Callback listener started on port {Port}", port);

        // Expand {port} placeholder in each argument and join
        var portString = port.ToString(CultureInfo.InvariantCulture);
        var expandedArgs = string.Join(" ", commandArgs.Select(arg =>
            EscapeArgument(arg.Replace("{port}", portString, StringComparison.OrdinalIgnoreCase))));

        _logger.LogDebug("Launching downstream adapter in callback mode: {Command} {Args}", command, expandedArgs);

        var startInfo = new ProcessStartInfo(command, expandedArgs)
        {
            RedirectStandardInput = false,
            RedirectStandardOutput = false,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };

        try
        {
            if (!process.Start())
            {
                listener.Stop();
                LogToFile("Failed to start downstream debug adapter process (Start() returned false)");
                _logger.LogError("Failed to start downstream debug adapter process");
                return ExitCodeConstants.FailedToLaunchDebugAdapter;
            }
        }
        catch (Exception ex)
        {
            listener.Stop();
            LogToFile($"Exception starting downstream adapter: {ex}");
            _logger.LogError(ex, "Failed to start downstream debug adapter process");
            return ExitCodeConstants.FailedToLaunchDebugAdapter;
        }

        _logger.LogDebug("Downstream adapter started with PID {Pid}, waiting for callback connection", process.Id);

        // Forward stderr from downstream adapter to our stderr for IDE logging
        var stderrTask = ForwardStderrAsync(process, cancellationToken);

        TcpClient? client = null;
        try
        {
            // Wait for the downstream adapter to connect back
            client = await listener.AcceptTcpClientAsync(cancellationToken);
            _logger.LogDebug("Downstream adapter connected via callback");

            var stream = client.GetStream();

            return await RunMiddlewareAsync(
                stream,
                stream,
                process,
                adapterId,
                polyglotMode,
                ideSessionServerMode,
                disposeOnComplete: client,
                cancellationToken);
        }
        finally
        {
            client?.Dispose();
            listener.Stop();
            await stderrTask;
        }
    }

    private async Task<int> RunMiddlewareAsync(
        Stream downstreamIn,
        Stream downstreamOut,
        Process downstreamProcess,
        string adapterId,
        bool polyglotMode,
        bool ideSessionServerMode,
        IDisposable? disposeOnComplete,
        CancellationToken cancellationToken)
    {
        // Use console stdin/stdout for upstream (IDE) communication
        var upstreamIn = Console.OpenStandardInput();
        var upstreamOut = Console.OpenStandardOutput();

        // Create stream transports for the new middleware API
        var clientTransport = new StreamMessageTransport(upstreamIn, upstreamOut);
        var hostTransport = new StreamMessageTransport(downstreamIn, downstreamOut);

        var middleware = new AspireDebugAdapterMiddleware();

        // Configure the adapter ID for the downstream debugger
        middleware.SetDownstreamAdapterId(adapterId);

        // Wire up logging if log file is enabled
        middleware.SetLogCallback(LogToFile);

        // Start IDE session server if requested
        IdeSessionServer.SessionServer? ideSessionServer = null;
        if (ideSessionServerMode)
        {
            try
            {
                ideSessionServer = await IdeSessionServer.SessionServer.CreateAsync(_logger, cancellationToken);
                LogToFile($"IDE session server started at {ideSessionServer.ConnectionInfo.Port}");

                // Configure the middleware to use the IDE session server
                middleware.SetIdeSessionServer(ideSessionServer);
            }
            catch (Exception ex)
            {
                LogToFile($"Failed to start IDE session server: {ex}");
                _logger.LogError(ex, "Failed to start IDE session server");
                // Non-fatal - continue without the server
            }
        }

        // Enable polyglot mode if requested
        if (polyglotMode)
        {
            middleware.SetPolyglotMode(_appHostProjectFactory, _languageDiscovery);
        }

        // Enable backchannel support for dashboard URLs
        middleware.SetBackchannelSupport(_serviceProvider);

        // Create a linked CTS that we can cancel on signals
        using var shutdownCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Handle Ctrl+C gracefully
        void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            _logger.LogDebug("Ctrl+C detected, initiating graceful shutdown");
            e.Cancel = true; // Prevent immediate termination
            shutdownCts.Cancel();
        }
        Console.CancelKeyPress += OnCancelKeyPress;

        // Handle SIGTERM (kill command) on Unix systems
        PosixSignalRegistration? sigtermRegistration = null;
        if (!OperatingSystem.IsWindows())
        {
            sigtermRegistration = PosixSignalRegistration.Create(PosixSignal.SIGTERM, context =>
            {
                _logger.LogDebug("SIGTERM received, initiating graceful shutdown");
                context.Cancel = true; // Prevent immediate termination
                shutdownCts.Cancel();
            });
        }

        try
        {
            // Run the middleware asynchronously until either side disconnects or cancellation is requested
            LogToFile("Starting middleware.RunAsync...");
            await middleware.RunAsync(clientTransport, hostTransport, shutdownCts.Token).ConfigureAwait(false);
            LogToFile("Middleware completed normally");

            _logger.LogDebug("Middleware completed, performing cleanup");
            return ExitCodeConstants.Success;
        }
        catch (Exception ex)
        {
            LogToFile($"Exception in middleware: {ex}");
            throw;
        }
        finally
        {
            Console.CancelKeyPress -= OnCancelKeyPress;
            sigtermRegistration?.Dispose();
            await ShutdownDownstreamAsync(downstreamProcess);
            disposeOnComplete?.Dispose();

            if (ideSessionServer is not null)
            {
                await ideSessionServer.DisposeAsync();
            }
        }
    }

    private async Task ShutdownDownstreamAsync(Process process)
    {
        if (process.HasExited)
        {
            _logger.LogDebug("Downstream process already exited with code {ExitCode}", process.ExitCode);
            return;
        }

        _logger.LogDebug("Attempting graceful shutdown of downstream process");

        try
        {
            // Give the process time to exit gracefully
            using var cts = new CancellationTokenSource(s_gracefulShutdownTimeout);
            await process.WaitForExitAsync(cts.Token);
            _logger.LogDebug("Downstream process exited gracefully with code {ExitCode}", process.ExitCode);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Downstream process did not exit within grace period, force killing");
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to kill downstream process");
            }
        }
    }

    private async Task ForwardStderrAsync(Process process, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && !process.HasExited)
            {
                var line = await process.StandardError.ReadLineAsync(cancellationToken);
                if (line is null)
                {
                    break;
                }

                LogToFile($"[downstream stderr] {line}");
                _logger.LogDebug("[downstream] {Line}", line);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            LogToFile($"Exception forwarding stderr from downstream adapter: {ex}");
            _logger.LogDebug(ex, "Error forwarding stderr from downstream adapter");
        }
    }

    /// <summary>
    /// Runs in bridge mode, connecting to DCP's debug bridge socket and proxying DAP messages.
    /// </summary>
    private async Task<int> RunBridgeModeAsync(
        string bridgeSocket,
        string bridgeToken,
        string bridgeSessionId,
        string bridgeRunId,
        string modeString,
        string adapterId,
        string command,
        string[] commandArgs,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting bridge mode: socket={Socket}, sessionId={SessionId}", bridgeSocket, bridgeSessionId);
        LogToFile($"Bridge mode: connecting to {bridgeSocket} with session {bridgeSessionId}");

        // Connect to the Unix domain socket
        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        try
        {
            var endpoint = new UnixDomainSocketEndPoint(bridgeSocket);
            await socket.ConnectAsync(endpoint, cancellationToken);
            LogToFile("Connected to bridge socket");
        }
        catch (Exception ex)
        {
            LogToFile($"Failed to connect to bridge socket: {ex}");
            _logger.LogError(ex, "Failed to connect to debug bridge socket at {Socket}", bridgeSocket);
            socket.Dispose();
            return ExitCodeConstants.FailedToLaunchDebugAdapter;
        }

        var networkStream = new NetworkStream(socket, ownsSocket: true);

        try
        {
            // Build the handshake request
            var handshakeRequest = new BridgeHandshakeRequest
            {
                Token = bridgeToken,
                SessionId = bridgeSessionId,
                RunId = bridgeRunId,
                DebugAdapterConfig = new BridgeDebugAdapterConfig
                {
                    Args = [command, .. commandArgs],
                    Mode = modeString
                }
            };

            // Send handshake (length-prefixed JSON)
            var handshakeJson = System.Text.Json.JsonSerializer.Serialize(handshakeRequest, BridgeJsonContext.Default.BridgeHandshakeRequest);
            LogToFile($"Sending handshake: {handshakeJson}");

            var jsonBytes = System.Text.Encoding.UTF8.GetBytes(handshakeJson);
            var lengthBytes = BitConverter.GetBytes(jsonBytes.Length);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthBytes);
            }

            await networkStream.WriteAsync(lengthBytes, cancellationToken);
            await networkStream.WriteAsync(jsonBytes, cancellationToken);
            await networkStream.FlushAsync(cancellationToken);
            LogToFile("Handshake sent, waiting for response...");

            // Read handshake response (length-prefixed JSON)
            var responseLengthBytes = new byte[4];
            var bytesRead = await networkStream.ReadAsync(responseLengthBytes.AsMemory(), cancellationToken);
            if (bytesRead < 4)
            {
                LogToFile($"Failed to read response length: only got {bytesRead} bytes");
                _logger.LogError("Failed to read handshake response length from bridge");
                return ExitCodeConstants.FailedToLaunchDebugAdapter;
            }

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(responseLengthBytes);
            }
            var responseLength = BitConverter.ToInt32(responseLengthBytes);
            LogToFile($"Response length: {responseLength}");

            if (responseLength <= 0 || responseLength > 65536)
            {
                LogToFile($"Invalid response length: {responseLength}");
                _logger.LogError("Invalid handshake response length: {Length}", responseLength);
                return ExitCodeConstants.FailedToLaunchDebugAdapter;
            }

            var responseBytes = new byte[responseLength];
            var totalRead = 0;
            while (totalRead < responseLength)
            {
                var read = await networkStream.ReadAsync(responseBytes.AsMemory(totalRead, responseLength - totalRead), cancellationToken);
                if (read == 0)
                {
                    LogToFile($"Connection closed while reading response, got {totalRead}/{responseLength} bytes");
                    _logger.LogError("Connection closed while reading handshake response");
                    return ExitCodeConstants.FailedToLaunchDebugAdapter;
                }
                totalRead += read;
            }

            var responseJson = System.Text.Encoding.UTF8.GetString(responseBytes);
            LogToFile($"Handshake response: {responseJson}");

            var response = System.Text.Json.JsonSerializer.Deserialize(responseJson, BridgeJsonContext.Default.BridgeHandshakeResponse);
            if (response is null || !response.Success)
            {
                LogToFile($"Handshake failed: {response?.Error ?? "null response"}");
                _logger.LogError("Debug bridge handshake failed: {Error}", response?.Error ?? "null response");
                return ExitCodeConstants.FailedToLaunchDebugAdapter;
            }

            LogToFile("Handshake successful, starting DAP proxy...");

            // Now proxy DAP messages between stdin/stdout and the bridge socket
            return await ProxyDapMessagesAsync(networkStream, adapterId, cancellationToken);
        }
        catch (Exception ex)
        {
            LogToFile($"Bridge mode error: {ex}");
            _logger.LogError(ex, "Error in bridge mode");
            return ExitCodeConstants.FailedToLaunchDebugAdapter;
        }
        finally
        {
            await networkStream.DisposeAsync();
        }
    }

    /// <summary>
    /// Proxies DAP messages between stdin/stdout (IDE) and the bridge stream (DCP).
    /// </summary>
    private async Task<int> ProxyDapMessagesAsync(Stream bridgeStream, string adapterId, CancellationToken cancellationToken)
    {
        var upstreamIn = Console.OpenStandardInput();
        var upstreamOut = Console.OpenStandardOutput();

        // Create stream transports for bidirectional proxy
        var clientTransport = new StreamMessageTransport(upstreamIn, upstreamOut);
        var hostTransport = new StreamMessageTransport(bridgeStream, bridgeStream);

        // Use the middleware to proxy DAP messages with adapter ID rewriting
        var middleware = new AspireDebugAdapterMiddleware();
        middleware.SetDownstreamAdapterId(adapterId);
        middleware.SetLogCallback(LogToFile);

        using var shutdownCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            _logger.LogDebug("Ctrl+C detected in bridge mode, initiating shutdown");
            e.Cancel = true;
            shutdownCts.Cancel();
        }
        Console.CancelKeyPress += OnCancelKeyPress;

        PosixSignalRegistration? sigtermRegistration = null;
        if (!OperatingSystem.IsWindows())
        {
            sigtermRegistration = PosixSignalRegistration.Create(PosixSignal.SIGTERM, context =>
            {
                _logger.LogDebug("SIGTERM received in bridge mode, initiating shutdown");
                context.Cancel = true;
                shutdownCts.Cancel();
            });
        }

        try
        {
            LogToFile("Starting DAP proxy between IDE and bridge...");
            await middleware.RunAsync(clientTransport, hostTransport, shutdownCts.Token).ConfigureAwait(false);
            LogToFile("DAP proxy completed normally");
            return ExitCodeConstants.Success;
        }
        catch (OperationCanceledException)
        {
            LogToFile("DAP proxy was cancelled");
            return ExitCodeConstants.Success;
        }
        catch (Exception ex)
        {
            LogToFile($"Exception in DAP proxy: {ex}");
            throw;
        }
        finally
        {
            Console.CancelKeyPress -= OnCancelKeyPress;
            sigtermRegistration?.Dispose();
        }
    }
}

/// <summary>
/// Handshake request for the debug bridge.
/// </summary>
internal sealed class BridgeHandshakeRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("token")]
    public required string Token { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("session_id")]
    public required string SessionId { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("run_id")]
    public required string RunId { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("debug_adapter_config")]
    public required BridgeDebugAdapterConfig DebugAdapterConfig { get; set; }
}

/// <summary>
/// Debug adapter configuration for the bridge handshake.
/// </summary>
internal sealed class BridgeDebugAdapterConfig
{
    [System.Text.Json.Serialization.JsonPropertyName("args")]
    public required string[] Args { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("mode")]
    public string Mode { get; set; } = "stdio";
}

/// <summary>
/// Handshake response from the debug bridge.
/// </summary>
internal sealed class BridgeHandshakeResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("success")]
    public bool Success { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>
/// JSON serializer context for bridge handshake types.
/// </summary>
[System.Text.Json.Serialization.JsonSerializable(typeof(BridgeHandshakeRequest))]
[System.Text.Json.Serialization.JsonSerializable(typeof(BridgeHandshakeResponse))]
internal sealed partial class BridgeJsonContext : System.Text.Json.Serialization.JsonSerializerContext
{
}

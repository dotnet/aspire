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

    private static readonly Option<DownstreamConnectionMode> s_modeOption = new("--mode", "-m")
    {
        Description = "Connection mode for the downstream adapter (stdio or callback)",
        DefaultValueFactory = _ => DownstreamConnectionMode.Stdio
    };

    private static readonly Argument<string> s_commandArgument = new("command")
    {
        Description = "Path to the downstream debug adapter executable to launch"
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
        Arguments.Add(s_commandArgument);
        Arguments.Add(s_commandArgsArgument);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var command = parseResult.GetValue(s_commandArgument)!;
        var commandArgs = parseResult.GetValue(s_commandArgsArgument) ?? [];
        var mode = parseResult.GetValue(s_modeOption);
        var logFile = parseResult.GetValue(s_logFileOption);
        var adapterId = parseResult.GetValue(s_adapterIdOption)!;
        var polyglotMode = parseResult.GetValue(s_polyglotOption);

        // Set up file logging if requested
        if (logFile is not null)
        {
            logFile.Directory?.Create();
            _logFileWriter = new StreamWriter(logFile.FullName, append: true) { AutoFlush = true };
            LogToFile($"=== DAP middleware starting (mode={mode}, adapterId={adapterId}, polyglot={polyglotMode}) ===");
        }

        try
        {
            // Join arguments into a single string for ProcessStartInfo
            var launchArgs = string.Join(" ", commandArgs.Select(EscapeArgument));

            _logger.LogDebug("Starting DAP middleware with mode={Mode}, adapterId={AdapterId}, polyglot={Polyglot}, command={Command}, args={Args}",
                mode, adapterId, polyglotMode, command, launchArgs);

            return mode switch
            {
                DownstreamConnectionMode.Stdio => await RunStdioModeAsync(command, launchArgs, adapterId, polyglotMode, cancellationToken),
                DownstreamConnectionMode.Callback => await RunCallbackModeAsync(command, commandArgs, adapterId, polyglotMode, cancellationToken),
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
        _logFileWriter?.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] {message}");
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

    private async Task<int> RunStdioModeAsync(string command, string launchArgs, string adapterId, bool polyglotMode, CancellationToken cancellationToken)
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
                disposeOnComplete: null,
                cancellationToken);
        }
        finally
        {
            await stderrTask;
        }
    }

    private async Task<int> RunCallbackModeAsync(string command, string[] commandArgs, string adapterId, bool polyglotMode, CancellationToken cancellationToken)
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
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Text;
using Aspire.Hosting.Dcp.Process;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Dcp;

internal sealed class DcpHostService : IHostedLifecycleService, IAsyncDisposable
{
    private const int LoggingSocketConnectionBacklog = 3;
    private readonly ApplicationExecutor _appExecutor;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private readonly DcpOptions _dcpOptions;
    private readonly DistributedApplicationExecutionContext _executionContext;
    private readonly IDcpDependencyCheckService _dependencyCheckService;
    private readonly Locations _locations;
    private readonly CancellationTokenSource _shutdownCts = new();
    private Task? _logProcessorTask;
    private IAsyncDisposable? _dcpRunDisposable;

    // These environment variables should never be inherited by DCP from app host.
    private static readonly string[] s_doNotInheritEnvironmentVars =
    {
        "ASPNETCORE_URLS",
        "DOTNET_LAUNCH_PROFILE",
        "ASPNETCORE_ENVIRONMENT",
        "DOTNET_ENVIRONMENT"
    };

    public DcpHostService(
        ILoggerFactory loggerFactory,
        IOptions<DcpOptions> dcpOptions,
        DistributedApplicationExecutionContext executionContext,
        ApplicationExecutor appExecutor,
        IDcpDependencyCheckService dependencyCheckService,
        Locations locations)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<DcpHostService>();
        _dcpOptions = dcpOptions.Value;
        _executionContext = executionContext;
        _appExecutor = appExecutor;
        _dependencyCheckService = dependencyCheckService;
        _locations = locations;
    }

    private bool IsSupported => !_executionContext.IsPublishMode;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (!IsSupported)
        {
            return;
        }

        await _dependencyCheckService.EnsureDcpDependenciesAsync(cancellationToken).ConfigureAwait(false);

        EnsureDcpHostRunning();
        await _appExecutor.RunApplicationAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _shutdownCts.Cancel();
        if (_logProcessorTask is { } task)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in logging socket processor.");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_dcpRunDisposable is null)
        {
            return;
        }

        await _dcpRunDisposable.DisposeAsync().ConfigureAwait(false);
        _dcpRunDisposable = null;
    }

    private void EnsureDcpHostRunning()
    {
        AspireEventSource.Instance.DcpApiServerLaunchStart();

        try
        {
            var dcpProcessSpec = CreateDcpProcessSpec(_locations);

            // Enable Unix Domain Socket based log streaming from DCP
            try
            {
                AspireEventSource.Instance.DcpLogSocketCreateStart();
                var loggingSocket = CreateLoggingSocket(_locations.DcpLogSocket);
                loggingSocket.Listen(LoggingSocketConnectionBacklog);

                dcpProcessSpec.EnvironmentVariables.Add("DCP_LOG_SOCKET", _locations.DcpLogSocket);

                _logProcessorTask = Task.Run(() => StartLoggingSocketAsync(loggingSocket));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enable orchestration logging.");
            }
            finally
            {
                AspireEventSource.Instance.DcpLogSocketCreateStop();
            }

            (_, _dcpRunDisposable) = ProcessUtil.Run(dcpProcessSpec);
        }
        finally
        {
            AspireEventSource.Instance.DcpApiServerLaunchStop();
        }

    }

    private ProcessSpec CreateDcpProcessSpec(Locations locations)
    {
        var dcpExePath = _dcpOptions.CliPath;
        if (!File.Exists(dcpExePath))
        {
            throw new FileNotFoundException($"The Aspire application host is not installed at \"{dcpExePath}\". The application cannot be run without it.", dcpExePath);
        }

        var arguments = $"start-apiserver --monitor {Environment.ProcessId} --detach --kubeconfig \"{locations.DcpKubeconfigPath}\"";
        if (!string.IsNullOrEmpty(_dcpOptions.ContainerRuntime))
        {
            arguments += $" --container-runtime \"{_dcpOptions.ContainerRuntime}\"";
        }

        ProcessSpec dcpProcessSpec = new ProcessSpec(dcpExePath)
        {
            WorkingDirectory = Directory.GetCurrentDirectory(),
            Arguments = arguments,
            OnOutputData = Console.Out.Write,
            OnErrorData = Console.Error.Write,
            InheritEnv = false,
        };

        _logger.LogInformation("Starting DCP with arguments: {Arguments}", dcpProcessSpec.Arguments);

        foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
        {
            var key = de.Key?.ToString();
            var val = de.Value?.ToString();
            if (key is not null && val is not null && !s_doNotInheritEnvironmentVars.Contains(key))
            {
                dcpProcessSpec.EnvironmentVariables.Add(key, val);
            }
        }

        if (!string.IsNullOrEmpty(_dcpOptions.ExtensionsPath))
        {
            dcpProcessSpec.EnvironmentVariables.Add("DCP_EXTENSIONS_PATH", _dcpOptions.ExtensionsPath);
        }

        if (!string.IsNullOrEmpty(_dcpOptions.BinPath))
        {
            dcpProcessSpec.EnvironmentVariables.Add("DCP_BIN_PATH", _dcpOptions.BinPath);
        }

        // Set an environment variable to contain session info that should be deleted when DCP is done
        // Currently this contains the Unix socket for logging and the kubeconfig
        dcpProcessSpec.EnvironmentVariables.Add("DCP_SESSION_FOLDER", locations.DcpSessionDir);
        return dcpProcessSpec;
    }

    private static Socket CreateLoggingSocket(string socketPath)
    {
        string? directoryName = Path.GetDirectoryName(socketPath);
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

        Socket socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        socket.Bind(new UnixDomainSocketEndPoint(socketPath));

        return socket;
    }

    private async Task StartLoggingSocketAsync(Socket socket)
    {
        List<Task> outputLoggers = [];
        while (!_shutdownCts.IsCancellationRequested)
        {
            try
            {
                Socket acceptedSocket = await socket.AcceptAsync(_shutdownCts.Token).ConfigureAwait(false);
                outputLoggers.Add(Task.Run(() => LogSocketOutputAsync(acceptedSocket, _shutdownCts.Token)));
            }
            catch
            {
                // Suppress exceptions reading logs from DCP controllers
            }
        }

        await Task.WhenAll(outputLoggers).ConfigureAwait(false);
        socket.Dispose();
    }

    private async Task LogSocketOutputAsync(Socket socket, CancellationToken cancellationToken)
    {
        using var stream = new NetworkStream(socket, ownsSocket: true);
        using var _ = cancellationToken.Register(s => ((NetworkStream)s!).Close(), stream);
        var reader = PipeReader.Create(stream);

        // Logger cache to avoid creating a new string per log line, for a few categories
        var loggerCache = new Dictionary<int, ILogger>();

        (ILogger, LogLevel, string message) GetLogInfo(ReadOnlySpan<byte> line)
        {
            // The log format is
            // <date>\t<level>\t<category>\t<log message>
            // e.g. 2023-09-19T20:40:50.509-0700      info    dcpctrl.ServiceReconciler       service /apigateway is now in state Ready       {"ServiceName": {"name":"apigateway"}}

            var tab = line.IndexOf((byte)'\t');
            var date = line[..tab];
            line = line[(tab + 1)..];
            tab = line.IndexOf((byte)'\t');
            var level = line[..tab];
            line = line[(tab + 1)..];
            tab = line.IndexOf((byte)'\t');
            var category = line[..tab];
            line = line[(tab + 1)..];
            var message = line;

            var logLevel = LogLevel.Information;

            if (level.SequenceEqual("info"u8))
            {
                logLevel = LogLevel.Information;
            }
            else if (level.SequenceEqual("error"u8))
            {
                logLevel = LogLevel.Error;
            }
            else if (level.SequenceEqual("warning"u8))
            {
                logLevel = LogLevel.Warning;
            }
            else if (level.SequenceEqual("debug"u8))
            {
                logLevel = LogLevel.Debug;
            }
            else if (level.SequenceEqual("trace"u8))
            {
                logLevel = LogLevel.Trace;
            }

            var hash = new HashCode();
            hash.AddBytes(category);
            var hashValue = hash.ToHashCode();

            if (!loggerCache.TryGetValue(hashValue, out var logger))
            {
                // loggerFactory.CreateLogger internally caches, but we may as well cache the logger as well as the string
                // for the lifetime of this socket
                loggerCache[hashValue] = logger = _loggerFactory.CreateLogger($"Aspire.Hosting.Dcp.{Encoding.UTF8.GetString(category)}");
            }

            return (logger, logLevel, Encoding.UTF8.GetString(message));
        }

        try
        {
            void LogLines(in ReadOnlySequence<byte> buffer, out SequencePosition position)
            {
                var seq = new SequenceReader<byte>(buffer);
                while (seq.TryReadTo(out ReadOnlySpan<byte> line, (byte)'\n'))
                {
                    var (logger, logLevel, message) = GetLogInfo(line);

                    logger.Log(logLevel, 0, message, null, static (value, ex) => value);
                }

                position = seq.Position;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await reader.ReadAsync(CancellationToken.None).ConfigureAwait(false);

                if (result.IsCompleted || result.IsCanceled)
                {
                    break;
                }

                LogLines(result.Buffer, out var position);

                reader.AdvanceTo(position, result.Buffer.End);
            }
        }
        catch
        {
            // Suppress exceptions reading logs from DCP controllers
        }
        finally
        {
            reader.Complete();
        }
    }

    public Task StartedAsync(CancellationToken _)
    {
        AspireEventSource.Instance.DcpHostStartupStop();
        return Task.CompletedTask;
    }

    public Task StartingAsync(CancellationToken cancellationToken)
    {
        AspireEventSource.Instance.DcpHostStartupStart();
        return Task.CompletedTask;
    }

    public Task StoppedAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StoppingAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

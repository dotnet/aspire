// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Text;
using Aspire.Dashboard;
using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dashboard;
using Aspire.Hosting.Dcp.Process;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Dcp;

internal sealed class DcpHostService : IHostedLifecycleService, IAsyncDisposable
{
    private const int LoggingSocketConnectionBacklog = 3;
    private readonly ApplicationExecutor _appExecutor;
    private readonly DistributedApplicationModel _applicationModel;
    private IAsyncDisposable? _dcpRunDisposable;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private readonly DashboardWebApplication _dashboard;
    private readonly IOptions<PublishingOptions> _publishingOptions;

    public DcpHostService(DistributedApplicationModel applicationModel, ILoggerFactory loggerFactory, IOptions<PublishingOptions> publishingOptions, ApplicationExecutor appExecutor)
    {
        _applicationModel = applicationModel;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<DcpHostService>();
        _publishingOptions = publishingOptions;
        _appExecutor = appExecutor;

        _dashboard = new DashboardWebApplication(serviceCollection =>
        {
            serviceCollection.AddSingleton(_applicationModel);
            serviceCollection.AddSingleton<IDashboardViewModelService, DashboardViewModelService>();
        });
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_publishingOptions.Value.Publisher is not null  && _publishingOptions.Value.Publisher != "dcp")
        {
            return;
        }

        EnsureDcpHostRunning();
        await _appExecutor.RunApplicationAsync(cancellationToken).ConfigureAwait(false);
        await _dashboard.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_publishingOptions.Value.Publisher != "dcp" || _publishingOptions.Value.Publisher is not null)
        {
            return;
        }

        await _dashboard.StopAsync(cancellationToken).ConfigureAwait(false);
        await _appExecutor.StopApplicationAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (_dcpRunDisposable is null)
        {
            return;
        }

        await _appExecutor.StopApplicationAsync().ConfigureAwait(false);
        await _dcpRunDisposable.DisposeAsync().ConfigureAwait(false);
        _dcpRunDisposable = null;
    }

    private void EnsureDcpHostRunning()
    {
        AspireEventSource.Instance.DcpApiServerLaunchStart();

        try
        {
            var dcpProcessSpec = CreateDcpProcessSpec();

            // Enable Unix Domain Socket based log streaming from DCP
            try
            {
                AspireEventSource.Instance.DcpLogSocketCreateStart();
                Socket loggingSocket = CreateLoggingSocket(Locations.DcpLogSocket);
                loggingSocket.Listen(LoggingSocketConnectionBacklog);

                dcpProcessSpec.EnvironmentVariables.Add("DCP_LOG_SOCKET", Locations.DcpLogSocket);

                _ = Task.Run(() => StartLoggingSocketAsync(loggingSocket), CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to enable orchestration logging: {ex}");
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

    private ProcessSpec CreateDcpProcessSpec()
    {
        string dcpExePath = Locations.DcpCliPath;
        if (!File.Exists(dcpExePath))
        {
            throw new FileNotFoundException("The Aspire application host is not installed. The application cannot be run without it.", dcpExePath);
        }

        ProcessSpec dcpProcessSpec = new ProcessSpec(dcpExePath)
        {
            WorkingDirectory = Directory.GetCurrentDirectory(),
            Arguments = $"start-apiserver --monitor {Environment.ProcessId} --detach --kubeconfig \"{Locations.DcpKubeconfigPath}\"",
            OnOutputData = Console.Out.Write,
            OnErrorData = Console.Error.Write,
        };

        _logger.LogInformation("Starting DCP with arguments: {Arguments}", dcpProcessSpec.Arguments);

        foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
        {
            var key = de.Key?.ToString();
            var val = de.Value?.ToString();
            if (key is not null && val is not null)
            {
                dcpProcessSpec.EnvironmentVariables.Add(key, val);
            }
        }

        if (!string.IsNullOrEmpty(Locations.DcpExtensionsPath))
        {
            dcpProcessSpec.EnvironmentVariables.Add("DCP_EXTENSIONS_PATH", Locations.DcpExtensionsPath);
        }

        if (!string.IsNullOrEmpty(Locations.DcpBinPath))
        {
            dcpProcessSpec.EnvironmentVariables.Add("DCP_BIN_PATH", Locations.DcpBinPath);
        }

        // Set an environment variable to contain session info that should be deleted when DCP is done
        // Currently this contains the Unix socket for logging and the kubeconfig
        dcpProcessSpec.EnvironmentVariables.Add("DCP_SESSION_FOLDER", Locations.DcpSessionDir);
        return dcpProcessSpec;
    }

    private static Socket CreateLoggingSocket(string socketPath)
    {
        string? directoryName = Path.GetDirectoryName(socketPath);
        if (!string.IsNullOrEmpty(directoryName))
        {
            Directory.CreateDirectory(directoryName);
        }

        Socket socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        socket.Bind(new UnixDomainSocketEndPoint(socketPath));

        return socket;
    }

    private async Task StartLoggingSocketAsync(Socket socket)
    {
        while (true)
        {
            try
            {
                Socket acceptedSocket = await socket.AcceptAsync().ConfigureAwait(false);
                _ = Task.Run(() => LogSocketOutputAsync(acceptedSocket), CancellationToken.None);
            }
            catch
            {
                // Suppress exceptions reading logs from DCP controllers
            }
        }
    }

    private async Task LogSocketOutputAsync(Socket socket)
    {
        var reader = PipeReader.Create(new NetworkStream(socket));

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
                loggerCache[hashValue] = logger = _loggerFactory.CreateLogger(Encoding.UTF8.GetString(category));
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

            while (true)
            {
                var result = await reader.ReadAsync().ConfigureAwait(false);

                if (result.IsCompleted)
                {
                    break;
                }

                LogLines(result.Buffer, out var position);

                reader.AdvanceTo(position);
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

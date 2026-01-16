// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Projects;

/// <summary>
/// Implementation of <see cref="IAppHostServerSession"/> that manages an AppHost server process.
/// </summary>
internal sealed class AppHostServerSession : IAppHostServerSession
{
    private readonly ILogger _logger;
    private readonly Process _serverProcess;
    private readonly OutputCollector _output;
    private readonly string _socketPath;
    private IAppHostRpcClient? _rpcClient;
    private bool _disposed;

    internal AppHostServerSession(
        Process serverProcess,
        OutputCollector output,
        string socketPath,
        ILogger logger)
    {
        _serverProcess = serverProcess;
        _output = output;
        _socketPath = socketPath;
        _logger = logger;
    }

    /// <inheritdoc />
    public string SocketPath => _socketPath;

    /// <inheritdoc />
    public Process ServerProcess => _serverProcess;

    /// <inheritdoc />
    public OutputCollector Output => _output;

    /// <inheritdoc />
    public async Task<IAppHostRpcClient> GetRpcClientAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(AppHostServerSession));

        return _rpcClient ??= await AppHostRpcClient.ConnectAsync(_socketPath, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_rpcClient != null)
        {
            await _rpcClient.DisposeAsync();
            _rpcClient = null;
        }

        if (!_serverProcess.HasExited)
        {
            try
            {
                _serverProcess.Kill(entireProcessTree: true);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error killing AppHost server process");
            }
        }

        _serverProcess.Dispose();
    }
}

/// <summary>
/// Factory for creating <see cref="IAppHostServerSession"/> instances.
/// </summary>
internal sealed class AppHostServerSessionFactory : IAppHostServerSessionFactory
{
    private readonly IAppHostServerProjectFactory _projectFactory;
    private readonly ILogger<AppHostServerSession> _logger;

    public AppHostServerSessionFactory(
        IAppHostServerProjectFactory projectFactory,
        ILogger<AppHostServerSession> logger)
    {
        _projectFactory = projectFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AppHostServerSessionResult> CreateAsync(
        string appHostPath,
        string sdkVersion,
        IEnumerable<(string PackageId, string Version)> packages,
        Dictionary<string, string>? launchSettingsEnvVars,
        bool debug,
        CancellationToken cancellationToken)
    {
        var appHostServerProject = _projectFactory.Create(appHostPath);
        var socketPath = appHostServerProject.GetSocketPath();

        // Create project files and get channel info
        var (_, channelName) = await appHostServerProject.CreateProjectFilesAsync(sdkVersion, packages, cancellationToken);

        // Build the project
        var (buildSuccess, buildOutput) = await appHostServerProject.BuildAsync(cancellationToken);
        if (!buildSuccess)
        {
            return new AppHostServerSessionResult(
                Success: false,
                Session: null,
                BuildOutput: buildOutput,
                ChannelName: channelName);
        }

        // Start the server process
        var currentPid = Environment.ProcessId;
        var (serverProcess, serverOutput) = appHostServerProject.Run(
            socketPath,
            currentPid,
            launchSettingsEnvVars,
            debug: debug);

        // Create the session
        var session = new AppHostServerSession(
            serverProcess,
            serverOutput,
            socketPath,
            _logger);

        return new AppHostServerSessionResult(
            Success: true,
            Session: session,
            BuildOutput: buildOutput,
            ChannelName: channelName);
    }
}

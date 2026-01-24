// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using GitHub.Copilot.SDK;
using Microsoft.Extensions.Options;

namespace Aspire.Dashboard.Model.Assistant.Copilot;

/// <summary>
/// Factory for creating Copilot CLI-based agent connections.
/// </summary>
internal sealed class CopilotAgentConnectionFactory : IAgentConnectionFactory, IAsyncDisposable
{
    private static readonly string[] s_defaultModels = ["gpt-4.1", "gpt-4o", "gpt-4o-mini", "claude-sonnet-4.5"];

    private readonly IOptionsMonitor<AgentOptions> _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<CopilotAgentConnectionFactory> _logger;
    private readonly SemaphoreSlim _clientLock = new(1, 1);
    private readonly SemaphoreSlim _availabilityLock = new(1, 1);

    private CopilotClient? _client;
    private bool? _cliAvailable;
    private bool _disposed;

    public CopilotAgentConnectionFactory(
        IOptionsMonitor<AgentOptions> options,
        ILoggerFactory loggerFactory)
    {
        _options = options;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<CopilotAgentConnectionFactory>();
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.CurrentValue.Enabled)
        {
            return false;
        }

        // Check cached result
        if (_cliAvailable.HasValue)
        {
            return _cliAvailable.Value;
        }

        await _availabilityLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Double-check after acquiring lock
            if (_cliAvailable.HasValue)
            {
                return _cliAvailable.Value;
            }

            _cliAvailable = await CheckCliAvailableAsync(cancellationToken).ConfigureAwait(false);
            return _cliAvailable.Value;
        }
        finally
        {
            _availabilityLock.Release();
        }
    }

    private async Task<bool> CheckCliAvailableAsync(CancellationToken cancellationToken)
    {
        var options = _options.CurrentValue;

        // If using external server, we assume it's available (could add health check later)
        if (options.UseExternalServer)
        {
            _logger.LogInformation("Using external Copilot server at {Url}", options.ExternalServerUrl);
            return true;
        }

        // Check if copilot CLI is available
        var cliPath = options.CliPath ?? "copilot";

        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = cliPath,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();

            // Wait for the process to exit with a timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            try
            {
                await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    process.Kill();
                }
                catch
                {
                    // Ignore kill errors
                }
                _logger.LogWarning("Copilot CLI check timed out");
                return false;
            }

            if (process.ExitCode == 0)
            {
                var version = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Copilot CLI available: {Version}", version.Trim());
                return true;
            }

            var error = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogWarning("Copilot CLI check failed with exit code {ExitCode}: {Error}", process.ExitCode, error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Copilot CLI not available: {Message}", ex.Message);
            return false;
        }
    }

    public async Task<IAgentConnection> CreateConnectionAsync(AgentConnectionConfig config, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var client = await GetOrCreateClientAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Creating agent session with model {Model}", config.Model);

        var sessionConfig = new SessionConfig
        {
            Model = config.Model,
            Streaming = config.Streaming,
            Tools = config.Tools?.ToList()
        };

        if (!string.IsNullOrEmpty(config.SystemMessage))
        {
            sessionConfig.SystemMessage = new SystemMessageConfig
            {
                Mode = SystemMessageMode.Append,
                Content = config.SystemMessage
            };
        }

        cancellationToken.ThrowIfCancellationRequested();
#pragma warning disable CA2016 // SDK doesn't support cancellation tokens
        var session = await client.CreateSessionAsync(sessionConfig).ConfigureAwait(false);
#pragma warning restore CA2016

        _logger.LogInformation("Created agent session {SessionId}", session.SessionId);

        return new CopilotAgentConnection(session, _loggerFactory.CreateLogger<CopilotAgentConnection>());
    }

    public Task<IReadOnlyList<string>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        // For now, return a static list. In the future, we could query the CLI for available models.
        return Task.FromResult<IReadOnlyList<string>>(s_defaultModels);
    }

    private async Task<CopilotClient> GetOrCreateClientAsync(CancellationToken cancellationToken)
    {
        if (_client is not null)
        {
            return _client;
        }

        await _clientLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_client is not null)
            {
                return _client;
            }

            var options = _options.CurrentValue;
            var clientOptions = new CopilotClientOptions
            {
                Logger = _loggerFactory.CreateLogger<CopilotClient>()
            };

            if (!string.IsNullOrEmpty(options.CliPath))
            {
                clientOptions.CliPath = options.CliPath;
            }

            if (options.UseExternalServer)
            {
                _logger.LogInformation("Connecting to external Copilot CLI server at {Url}", options.ExternalServerUrl);
                clientOptions.CliUrl = options.ExternalServerUrl;
                clientOptions.AutoStart = false;
            }
            else
            {
                _logger.LogInformation("Starting local Copilot CLI process");
                clientOptions.AutoStart = true;
                clientOptions.AutoRestart = true;
            }

            _client = new CopilotClient(clientOptions);
            cancellationToken.ThrowIfCancellationRequested();
#pragma warning disable CA2016 // SDK doesn't support cancellation tokens
            await _client.StartAsync().ConfigureAwait(false);
#pragma warning restore CA2016

            _logger.LogInformation("Copilot client started successfully");

            return _client;
        }
        finally
        {
            _clientLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_client is not null)
        {
            try
            {
                await _client.StopAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error stopping Copilot client");
            }

            await _client.DisposeAsync().ConfigureAwait(false);
            _client = null;
        }

        _clientLock.Dispose();
        _availabilityLock.Dispose();

        _logger.LogDebug("Disposed CopilotAgentConnectionFactory");
    }
}

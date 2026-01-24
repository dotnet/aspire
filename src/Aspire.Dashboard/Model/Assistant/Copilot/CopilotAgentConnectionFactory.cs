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
    // Models supported by the Copilot CLI
    private static readonly string[] s_defaultModels =
    [
        "claude-sonnet-4.5",
        "claude-sonnet-4",
        "gpt-5.1-codex",
        "gpt-5.1",
        "gpt-5",
        "gpt-4.1",
        "gemini-3-pro-preview"
    ];

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

        System.IO.File.AppendAllText("/tmp/aspire-agent-events.log", $"{DateTime.Now}: CreateConnectionAsync called with model {config.Model}\n");

        var client = await GetOrCreateClientAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Creating agent session with model {Model}", config.Model);
        System.IO.File.AppendAllText("/tmp/aspire-agent-events.log", $"{DateTime.Now}: Creating session with model {config.Model}\n");

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
        try
        {
#pragma warning disable CA2016 // SDK doesn't support cancellation tokens
            var session = await client.CreateSessionAsync(sessionConfig).ConfigureAwait(false);
#pragma warning restore CA2016

            _logger.LogInformation("Created agent session {SessionId}", session.SessionId);
            System.IO.File.AppendAllText("/tmp/aspire-agent-events.log", $"{DateTime.Now}: Session created: {session.SessionId}\n");

            return new CopilotAgentConnection(session, _loggerFactory.CreateLogger<CopilotAgentConnection>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session");
            System.IO.File.AppendAllText("/tmp/aspire-agent-events.log", $"{DateTime.Now}: ERROR creating session: {ex.Message}\n{ex.StackTrace}\n");
            throw;
        }
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
            _logger.LogInformation("Returning existing Copilot client");
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

            _logger.LogInformation("Creating CopilotClient...");
            System.IO.File.AppendAllText("/tmp/aspire-agent-events.log", $"{DateTime.Now}: Creating CopilotClient\n");
            
            _client = new CopilotClient(clientOptions);
            cancellationToken.ThrowIfCancellationRequested();
            
            _logger.LogInformation("Calling StartAsync...");
            System.IO.File.AppendAllText("/tmp/aspire-agent-events.log", $"{DateTime.Now}: Calling StartAsync\n");
            
#pragma warning disable CA2016 // SDK doesn't support cancellation tokens
            await _client.StartAsync().ConfigureAwait(false);
#pragma warning restore CA2016

            _logger.LogInformation("Copilot client started successfully");
            System.IO.File.AppendAllText("/tmp/aspire-agent-events.log", $"{DateTime.Now}: CopilotClient started successfully\n");

            return _client;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting Copilot client");
            System.IO.File.AppendAllText("/tmp/aspire-agent-events.log", $"{DateTime.Now}: ERROR starting client: {ex.Message}\n");
            throw;
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

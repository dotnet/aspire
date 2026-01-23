// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Net;
using Hex1b;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Terminals;

/// <summary>
/// Background service that manages terminal instances for the Aspire AppHost.
/// Hosts its own WebApplication for WebSocket connections.
/// </summary>
internal sealed class TerminalHost : IHostedService, IAsyncDisposable
{
    private readonly ILogger<TerminalHost> _logger;
    private readonly ConcurrentDictionary<string, ManagedTerminal> _terminals = new();
    private readonly string _terminalsDirectory;
    private readonly TaskCompletionSource<string> _baseUrlTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly SemaphoreSlim _startLock = new(1, 1);

    private WebApplication? _app;
    private string? _baseUrl;
    private bool _disposed;
    private bool _started;

    /// <summary>
    /// Creates a new terminal host.
    /// </summary>
    public TerminalHost(ILogger<TerminalHost> logger)
    {
        _logger = logger;

        // Default terminals directory: ~/.aspire/terminals/
        var aspireHome = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".aspire");
        _terminalsDirectory = Path.Combine(aspireHome, "terminals");
    }

    /// <summary>
    /// Gets the base URL for terminal WebSocket connections once the server is started.
    /// </summary>
    public Task<string> GetBaseUrlAsync(CancellationToken cancellationToken = default)
        => _baseUrlTcs.Task.WaitAsync(cancellationToken);

    /// <summary>
    /// Ensures the terminal host web server is started.
    /// This can be called before the hosted service lifecycle starts.
    /// </summary>
    private async Task EnsureStartedAsync(CancellationToken cancellationToken)
    {
        if (_started)
        {
            return;
        }

        await _startLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_started)
            {
                return;
            }

            await StartInternalAsync(cancellationToken).ConfigureAwait(false);
            _started = true;
        }
        finally
        {
            _startLock.Release();
        }
    }

    /// <summary>
    /// Allocates a new terminal and starts listening for connections.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The allocated terminal information.</returns>
    public async Task<AllocatedTerminal> AllocateTerminalAsync(CancellationToken cancellationToken = default)
    {
        // Ensure the terminal host web server is started
        await EnsureStartedAsync(cancellationToken).ConfigureAwait(false);

        var id = Guid.NewGuid().ToString("N")[..8];
        var socketPath = Path.Combine(_terminalsDirectory, $"{id}.socket");

        // Build the terminal with UDS workload and multiclient WebSocket presentation
        var builder = Hex1bTerminal.CreateBuilder()
            .WithUdsWorkload(socketPath, out var workloadHandle)
            .WithMulticlientWebSocket(out var presentationAdapter);

        var terminal = builder.Build();

        // Set the terminal reference on the presentation adapter so it can send state to new clients
        presentationAdapter.SetTerminal(terminal);

        // Determine URLs - wait for base URL if not yet available
        var baseUrl = _baseUrl ?? await GetBaseUrlAsync(cancellationToken).ConfigureAwait(false);
        var webSocketUrl = $"{baseUrl.Replace("http://", "ws://").Replace("https://", "wss://")}/terminals/{id}";
        var testPageUrl = $"{baseUrl}/terminals/{id}/index.html";

        var allocation = new AllocatedTerminal
        {
            Id = id,
            SocketPath = socketPath,
            WebSocketUrl = webSocketUrl,
            TestPageUrl = testPageUrl
        };

        var managedTerminal = new ManagedTerminal(
            id,
            terminal,
            workloadHandle.Adapter,
            presentationAdapter,
            allocation);

        // Handle terminal completion
        managedTerminal.Completed += OnTerminalCompleted;

        _terminals[id] = managedTerminal;

        _logger.LogDebug("Allocated terminal {TerminalId} at socket {SocketPath}", id, socketPath);

        // Start the terminal
        await managedTerminal.StartAsync(cancellationToken).ConfigureAwait(false);

        return allocation;
    }

    /// <summary>
    /// Gets a managed terminal by ID.
    /// </summary>
    /// <param name="id">The terminal ID.</param>
    /// <returns>The managed terminal, or null if not found.</returns>
    public ManagedTerminal? GetTerminal(string id)
    {
        _terminals.TryGetValue(id, out var terminal);
        return terminal;
    }

    /// <summary>
    /// Gets all active terminal IDs.
    /// </summary>
    public IEnumerable<string> GetTerminalIds() => _terminals.Keys;

    private void OnTerminalCompleted(ManagedTerminal terminal)
    {
        _terminals.TryRemove(terminal.Id, out _);
        _logger.LogDebug("Terminal {TerminalId} completed", terminal.Id);
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Use EnsureStartedAsync to avoid duplicate initialization
        return EnsureStartedAsync(cancellationToken);
    }

    /// <summary>
    /// Internal method that actually starts the terminal host web server.
    /// </summary>
    private async Task StartInternalAsync(CancellationToken cancellationToken)
    {
        // Ensure terminals directory exists
        Directory.CreateDirectory(_terminalsDirectory);

        _logger.LogDebug("TerminalHost starting...");

        // Build and start the WebApplication for terminal WebSocket connections
        try
        {
            var builder = WebApplication.CreateSlimBuilder();

            // Register this TerminalHost so endpoints can access it
            builder.Services.AddSingleton(this);

            // Configure Kestrel to listen on a random port
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Listen(IPAddress.Loopback, port: 0, listenOptions =>
                {
                    // HTTP/1.1 is required for WebSockets
                    listenOptions.Protocols = HttpProtocols.Http1;
                });
            });

            _app = builder.Build();

            // Enable WebSockets
            _app.UseWebSockets();

            // Map terminal endpoints
            _app.MapTerminalEndpoints();

            await _app.StartAsync(cancellationToken).ConfigureAwait(false);

            // Get the actual listening address
            var addressFeature = _app.Services.GetService<IServer>()?.Features.Get<IServerAddressesFeature>();
            if (addressFeature is not null && addressFeature.Addresses.Count > 0)
            {
                _baseUrl = addressFeature.Addresses.First();
                _baseUrlTcs.TrySetResult(_baseUrl);
                _logger.LogDebug("TerminalHost started at {BaseUrl}. Terminals directory: {Directory}", _baseUrl, _terminalsDirectory);
            }
            else
            {
                _baseUrlTcs.TrySetException(new InvalidOperationException("Could not determine terminal server address"));
            }
        }
        catch (Exception ex)
        {
            _baseUrlTcs.TrySetException(ex);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("TerminalHost stopping. Cleaning up {Count} terminals", _terminals.Count);

        // Stop the WebApplication
        if (_app is not null)
        {
            await _app.StopAsync(cancellationToken).ConfigureAwait(false);
        }

        // Stop all terminals
        var stopTasks = _terminals.Values.Select(t => t.StopAsync());
        await Task.WhenAll(stopTasks).ConfigureAwait(false);

        // Dispose all terminals
        foreach (var terminal in _terminals.Values)
        {
            await terminal.DisposeAsync().ConfigureAwait(false);
        }

        _terminals.Clear();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_app is not null)
        {
            await _app.DisposeAsync().ConfigureAwait(false);
        }

        foreach (var terminal in _terminals.Values)
        {
            await terminal.DisposeAsync().ConfigureAwait(false);
        }

        _terminals.Clear();
    }
}

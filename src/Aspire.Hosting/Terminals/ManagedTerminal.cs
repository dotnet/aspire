// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Hex1b;

namespace Aspire.Hosting.Terminals;

/// <summary>
/// Represents a managed terminal instance with its associated adapters and lifecycle.
/// </summary>
internal sealed class ManagedTerminal : IAsyncDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private Task? _runTask;
    private bool _disposed;

    /// <summary>
    /// Creates a new managed terminal.
    /// </summary>
    public ManagedTerminal(
        string id,
        Hex1bTerminal terminal,
        UdsWorkloadAdapter workloadAdapter,
        MulticlientWebSocketPresentationAdapter presentationAdapter,
        AllocatedTerminal allocation)
    {
        Id = id;
        Terminal = terminal;
        WorkloadAdapter = workloadAdapter;
        PresentationAdapter = presentationAdapter;
        Allocation = allocation;
    }

    /// <summary>
    /// Gets the unique identifier for this terminal.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the Hex1b terminal instance.
    /// </summary>
    public Hex1bTerminal Terminal { get; }

    /// <summary>
    /// Gets the UDS workload adapter.
    /// </summary>
    public UdsWorkloadAdapter WorkloadAdapter { get; }

    /// <summary>
    /// Gets the multiclient WebSocket presentation adapter.
    /// </summary>
    public MulticlientWebSocketPresentationAdapter PresentationAdapter { get; }

    /// <summary>
    /// Gets the terminal allocation information.
    /// </summary>
    public AllocatedTerminal Allocation { get; }

    /// <summary>
    /// Gets or sets the running task for this terminal.
    /// </summary>
    public Task? RunTask
    {
        get => _runTask;
        set => _runTask = value;
    }

    /// <summary>
    /// Gets the cancellation token for this terminal.
    /// </summary>
    public CancellationToken CancellationToken => _cts.Token;

    /// <summary>
    /// Raised when the terminal run task completes.
    /// </summary>
    public event Action<ManagedTerminal>? Completed;

    /// <summary>
    /// Starts the terminal.
    /// </summary>
    /// <param name="cancellationToken">Optional external cancellation token.</param>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        // Link external cancellation token
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);

        // Start listening for workload connection
        await WorkloadAdapter.StartListeningAsync(linkedCts.Token).ConfigureAwait(false);

        // Start the terminal run loop in the background
        _runTask = RunTerminalAsync();
    }

    private async Task RunTerminalAsync()
    {
        try
        {
            // Wait for workload to connect before starting the terminal pump
            await WorkloadAdapter.ClientConnectedTask.ConfigureAwait(false);

            // Run the terminal I/O pump
            await Terminal.RunAsync(_cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected when terminal is stopped
        }
        catch (Exception)
        {
            // Log error but don't rethrow
        }
        finally
        {
            Completed?.Invoke(this);
        }
    }

    /// <summary>
    /// Stops the terminal.
    /// </summary>
    public async Task StopAsync()
    {
        if (_disposed)
        {
            return;
        }

        await _cts.CancelAsync().ConfigureAwait(false);

        if (_runTask is not null)
        {
            try
            {
                await _runTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        await StopAsync().ConfigureAwait(false);
        _cts.Dispose();

        await Terminal.DisposeAsync().ConfigureAwait(false);
        await WorkloadAdapter.DisposeAsync().ConfigureAwait(false);
        await PresentationAdapter.DisposeAsync().ConfigureAwait(false);
    }
}

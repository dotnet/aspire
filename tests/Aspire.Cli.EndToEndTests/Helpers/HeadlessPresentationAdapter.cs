// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Hex1b.Terminal;

namespace Aspire.Cli.EndToEndTests.Helpers;

/// <summary>
/// A headless presentation adapter for running Hex1b terminals in CI/test environments
/// without a display. This adapter discards all rendering output but maintains proper
/// terminal state for automation purposes.
/// </summary>
public sealed class HeadlessPresentationAdapter : IHex1bTerminalPresentationAdapter, IDisposable
{
    private readonly int _width;
    private readonly int _height;
    private readonly TerminalCapabilities _capabilities;

    public HeadlessPresentationAdapter(int width = 120, int height = 40)
    {
        _width = width;
        _height = height;
        _capabilities = new TerminalCapabilities();
    }

    public int Width => _width;

    public int Height => _height;

    public TerminalCapabilities Capabilities => _capabilities;

#pragma warning disable CS0067 // Event is never used
    public event Action<int, int>? Resized;

    public event Action? Disconnected;
#pragma warning restore CS0067

    public ValueTask WriteOutputAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        // Discard output in headless mode
        return ValueTask.CompletedTask;
    }

    public async ValueTask<ReadOnlyMemory<byte>> ReadInputAsync(CancellationToken ct = default)
    {
        // No input in headless mode - wait indefinitely (until cancelled)
        try
        {
            await Task.Delay(Timeout.Infinite, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelled
        }

        return ReadOnlyMemory<byte>.Empty;
    }

    public ValueTask FlushAsync(CancellationToken ct = default)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask EnterRawModeAsync(CancellationToken ct = default)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask ExitRawModeAsync(CancellationToken ct = default)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        // No-op for headless mode
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Hex1b.Terminal;

namespace TerminalMcp;

/// <summary>
/// A presentation adapter that captures output but doesn't display it anywhere.
/// Used to enable the terminal's output pump so the screen buffer gets populated.
/// </summary>
internal sealed class CapturingPresentationAdapter : IHex1bTerminalPresentationAdapter
{
    private readonly TaskCompletionSource _disconnected = new();
    private int _width;
    private int _height;

    public CapturingPresentationAdapter(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public int Width => _width;
    public int Height => _height;

    public TerminalCapabilities Capabilities => new()
    {
        SupportsMouse = false,
        Supports256Colors = true,
        SupportsTrueColor = true,
        CellPixelWidth = 10,
        CellPixelHeight = 20
    };

    public event Action<int, int>? Resized;
    public event Action? Disconnected;

    public ValueTask WriteOutputAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        // Discard output - the terminal's screen buffer captures everything
        return ValueTask.CompletedTask;
    }

    public async ValueTask<ReadOnlyMemory<byte>> ReadInputAsync(CancellationToken ct = default)
    {
        // Block until disconnected or cancelled
        try
        {
            await _disconnected.Task.WaitAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        return ReadOnlyMemory<byte>.Empty;
    }

    public ValueTask FlushAsync(CancellationToken ct = default) => ValueTask.CompletedTask;
    public ValueTask EnterRawModeAsync(CancellationToken ct = default) => ValueTask.CompletedTask;
    public ValueTask ExitRawModeAsync(CancellationToken ct = default) => ValueTask.CompletedTask;

    /// <summary>
    /// Updates the dimensions and triggers a resize event.
    /// </summary>
    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
        Resized?.Invoke(width, height);
    }

    public ValueTask DisposeAsync()
    {
        _disconnected.TrySetResult();
        Disconnected?.Invoke();
        return ValueTask.CompletedTask;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Hex1b;
using Hex1b.Automation;

namespace Aspire.Extension.EndToEndTests.Infrastructure.Hex1b;

/// <summary>
/// High-level wrapper around <see cref="DiagnosticsWorkloadAdapter"/> and <see cref="Hex1bTerminal"/>
/// for driving and inspecting a remote terminal session from test code.
/// </summary>
internal sealed class RemoteTerminalSession : IAsyncDisposable
{
    private readonly DiagnosticsWorkloadAdapter _adapter;
    private readonly Hex1bTerminal _terminal;
    private readonly Task<int> _runTask;
    private readonly CancellationTokenSource _cts = new();

    public Hex1bTerminal Terminal => _terminal;

    private RemoteTerminalSession(DiagnosticsWorkloadAdapter adapter, Hex1bTerminal terminal, Task<int> runTask)
    {
        _adapter = adapter;
        _terminal = terminal;
        _runTask = runTask;
    }

    /// <summary>
    /// Connect to a hex1b diagnostics socket and create a headless terminal that mirrors the remote state.
    /// </summary>
    public static async Task<RemoteTerminalSession> ConnectAsync(string socketPath, CancellationToken ct = default)
    {
        var adapter = await DiagnosticsWorkloadAdapter.ConnectAsync(socketPath, ct);

        var terminal = Hex1bTerminal.CreateBuilder()
            .WithWorkload(adapter)
            .WithHeadless()
            .WithDimensions(adapter.RemoteWidth, adapter.RemoteHeight)
            .WithScrollback(5000)
            .Build();

        var runTask = terminal.RunAsync(ct);

        // Give the terminal a moment to process the initial ANSI data
        await Task.Delay(200, ct);

        return new RemoteTerminalSession(adapter, terminal, runTask);
    }

    /// <summary>
    /// Send text as raw input to the remote terminal (keystrokes).
    /// </summary>
    public async Task SendTextAsync(string text, CancellationToken ct = default)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        await _terminal.SendInputAsync(bytes, ct);
    }

    /// <summary>
    /// Send a newline character (Enter key).
    /// </summary>
    public async Task SendEnterAsync(CancellationToken ct = default)
    {
        await SendTextAsync("\r", ct);
    }

    /// <summary>
    /// Send arrow down key (ESC [ B).
    /// </summary>
    public async Task SendArrowDownAsync(CancellationToken ct = default)
    {
        await SendTextAsync("\x1b[B", ct);
    }

    /// <summary>
    /// Send arrow up key (ESC [ A).
    /// </summary>
    public async Task SendArrowUpAsync(CancellationToken ct = default)
    {
        await SendTextAsync("\x1b[A", ct);
    }

    /// <summary>
    /// Get the current screen text from the terminal snapshot.
    /// </summary>
    public string GetScreenText()
    {
        using var snapshot = _terminal.CreateSnapshot();
        return snapshot.GetText();
    }

    /// <summary>
    /// Get non-empty, trimmed lines from the current terminal screen.
    /// </summary>
    public IReadOnlyList<string> GetNonEmptyLines()
    {
        using var snapshot = _terminal.CreateSnapshot();
        return snapshot.GetNonEmptyLines().ToList();
    }

    /// <summary>
    /// Wait for specific text to appear on the terminal screen.
    /// </summary>
    public async Task<bool> WaitForTextAsync(string text, TimeSpan? timeout = null, CancellationToken ct = default)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(30));

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            var screenText = GetScreenText();
            if (screenText.Contains(text, StringComparison.Ordinal))
            {
                return true;
            }

            await Task.Delay(200, ct);
        }

        return false;
    }

    /// <summary>
    /// Wait for any of the specified texts to appear on the terminal screen.
    /// Returns the matching text, or null if timeout.
    /// </summary>
    public async Task<string?> WaitForAnyTextAsync(IEnumerable<string> texts, TimeSpan? timeout = null, CancellationToken ct = default)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(30));
        var textList = texts.ToList();

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            var screenText = GetScreenText();
            foreach (var text in textList)
            {
                if (screenText.Contains(text, StringComparison.Ordinal))
                {
                    return text;
                }
            }

            await Task.Delay(200, ct);
        }

        return null;
    }

    public async ValueTask DisposeAsync()
    {
        await _adapter.DisposeAsync();

        try
        {
            await _runTask.WaitAsync(TimeSpan.FromSeconds(3));
        }
        catch
        {
            // RunAsync may throw when workload disconnects — that's expected
        }

        await _terminal.DisposeAsync();
        _cts.Dispose();
    }
}

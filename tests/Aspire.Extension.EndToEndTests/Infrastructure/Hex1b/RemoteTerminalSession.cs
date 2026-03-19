// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Hex1b;
using Hex1b.Automation;

namespace Aspire.Extension.EndToEndTests.Infrastructure.Hex1b;

/// <summary>
/// Tracks the sequence number for shell prompt detection, synchronized with the
/// CMDCOUNT variable set by the PROMPT_COMMAND trick in the remote shell.
/// </summary>
internal sealed class SequenceCounter
{
    public int Value { get; private set; } = 1;

    public int Increment() => ++Value;
}

/// <summary>
/// High-level wrapper around Hex1b's <see cref="RemoteTerminalWorkloadAdapter"/> and <see cref="Hex1bTerminal"/>
/// for driving and inspecting a remote terminal session from test code.
/// Connects via WebSocket to a hex1b process started with <c>--passthru --port PORT</c>.
/// </summary>
internal sealed class RemoteTerminalSession : IAsyncDisposable
{
    private readonly RemoteTerminalWorkloadAdapter _adapter;
    private readonly Hex1bTerminal _terminal;
    private readonly Task<int> _runTask;
    private readonly CancellationTokenSource _cts = new();

    /// <summary>
    /// Bash PROMPT_COMMAND that sets a structured prompt with command count and exit code.
    /// After each command, the prompt appears as <c>[N OK] $ </c> or <c>[N ERR:code] $ </c>.
    /// </summary>
    private const string PromptSetup =
        "CMDCOUNT=0; PROMPT_COMMAND='s=$?;((CMDCOUNT++));PS1=\"[$CMDCOUNT $([ $s -eq 0 ] && echo OK || echo ERR:$s)] \\$ \"'";

    public Hex1bTerminal Terminal => _terminal;

    private RemoteTerminalSession(RemoteTerminalWorkloadAdapter adapter, Hex1bTerminal terminal, Task<int> runTask)
    {
        _adapter = adapter;
        _terminal = terminal;
        _runTask = runTask;
    }

    /// <summary>
    /// Connect to a hex1b terminal over WebSocket and create a headless terminal that mirrors the remote state.
    /// Retries the WebSocket connection if the server isn't fully ready yet.
    /// </summary>
    public static async Task<RemoteTerminalSession> ConnectAsync(Uri websocketUri, Action<string>? log = null, CancellationToken ct = default)
    {
        log?.Invoke($"Connecting to remote terminal: {websocketUri}");

        var connectDeadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        var attempt = 0;

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            attempt++;

            var terminal = Hex1bTerminal.CreateBuilder()
                .WithRemoteTerminal(websocketUri, out var adapter)
                .WithHeadless()
                .WithScrollback(5000)
                .Build();

            var runTask = terminal.RunAsync(ct);

            // Wait briefly for the connection to either succeed or fault
            await Task.Delay(1000, ct);

            if (runTask.IsFaulted)
            {
                var ex = runTask.Exception?.InnerException;
                log?.Invoke($"[attempt {attempt}] WebSocket connection failed: {ex?.Message}");

                await terminal.DisposeAsync();

                if (DateTime.UtcNow >= connectDeadline)
                {
                    throw new TimeoutException(
                        $"Failed to connect to hex1b WebSocket at {websocketUri} after {attempt} attempts", ex);
                }

                // Back off and retry — the server may not be fully ready
                await Task.Delay(2000, ct);
                continue;
            }

            // Connection succeeded — wait for initial terminal content
            var contentDeadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
            while (DateTime.UtcNow < contentDeadline)
            {
                ct.ThrowIfCancellationRequested();

                if (runTask.IsCompleted)
                {
                    log?.Invoke($"RunAsync completed unexpectedly: Status={runTask.Status}");
                    break;
                }

                using var snapshot = terminal.CreateSnapshot();
                var text = snapshot.GetText();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    log?.Invoke($"Connected on attempt {attempt}, terminal has content");
                    return new RemoteTerminalSession(adapter, terminal, runTask);
                }

                await Task.Delay(250, ct);
            }

            // Even if no content yet, return — some terminals take a while to produce output
            log?.Invoke($"Connected on attempt {attempt} (no initial content yet)");
            return new RemoteTerminalSession(adapter, terminal, runTask);
        }
    }

    /// <summary>
    /// Installs the PROMPT_COMMAND trick and returns a <see cref="SequenceCounter"/> for tracking prompts.
    /// After this call, every completed command produces a prompt like <c>[N OK] $ </c> or <c>[N ERR:code] $ </c>.
    /// </summary>
    public async Task<SequenceCounter> SetupPromptAsync(CancellationToken ct = default)
    {
        // Wait for an initial shell prompt (root@... or ~#)
        await WaitForAnyTextAsync(["# ", "$ "], timeout: TimeSpan.FromSeconds(30), ct: ct);

        var counter = new SequenceCounter();

        await SendTextAsync(PromptSetup + "\r", ct);
        await WaitForSuccessPromptAsync(counter, timeout: TimeSpan.FromSeconds(10), ct: ct);

        return counter;
    }

    /// <summary>
    /// Waits for a shell success prompt matching the current sequence counter value
    /// (<c>[N OK] $ </c>), then increments the counter.
    /// </summary>
    public async Task WaitForSuccessPromptAsync(
        SequenceCounter counter, TimeSpan? timeout = null, CancellationToken ct = default)
    {
        var pattern = $"[{counter.Value} OK] $ ";
        var found = await WaitForTextAsync(pattern, timeout ?? TimeSpan.FromSeconds(120), ct: ct);
        if (!found)
        {
            // Check if we got an error prompt instead
            var errCheck = GetScreenText();
            if (errCheck.Contains($"[{counter.Value} ERR:", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Command failed with non-zero exit code (detected ERR prompt at sequence {counter.Value}). " +
                    $"Screen: {errCheck}");
            }
            throw new TimeoutException($"Timed out waiting for success prompt [{counter.Value} OK] $ ");
        }
        counter.Increment();
    }

    /// <summary>
    /// Waits for any prompt (success or error) matching the current sequence counter,
    /// then increments the counter. Returns true if it was a success prompt.
    /// </summary>
    public async Task<bool> WaitForAnyPromptAsync(
        SequenceCounter counter, TimeSpan? timeout = null, CancellationToken ct = default)
    {
        var successPattern = $"[{counter.Value} OK] $ ";
        var errorPattern = $"[{counter.Value} ERR:";
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(120);
        var deadline = DateTime.UtcNow + effectiveTimeout;

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            var text = GetScreenText();

            if (text.Contains(successPattern, StringComparison.Ordinal))
            {
                counter.Increment();
                return true;
            }
            if (text.Contains(errorPattern, StringComparison.Ordinal))
            {
                counter.Increment();
                return false;
            }

            await Task.Delay(200, ct);
        }

        throw new TimeoutException($"Timed out waiting for any prompt [{counter.Value} OK/ERR] $ ");
    }

    /// <summary>
    /// Waits for the success prompt, but throws immediately if an error prompt is detected.
    /// </summary>
    public async Task WaitForSuccessPromptFailFastAsync(
        SequenceCounter counter, TimeSpan? timeout = null, CancellationToken ct = default)
    {
        var isSuccess = await WaitForAnyPromptAsync(counter, timeout, ct);
        if (!isSuccess)
        {
            throw new InvalidOperationException(
                $"Command failed with non-zero exit code (detected ERR prompt at sequence {counter.Value - 1}). " +
                $"Check the terminal recording for details.");
        }
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
    /// Get the current screen text from the terminal snapshot (visible area only).
    /// </summary>
    public string GetScreenText()
    {
        using var snapshot = _terminal.CreateSnapshot();
        return snapshot.GetText();
    }

    /// <summary>
    /// Get the full terminal text including scrollback buffer.
    /// </summary>
    public string GetFullText(int scrollbackLines = 5000)
    {
        using var snapshot = _terminal.CreateSnapshot(scrollbackLines);
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
    /// Wait for specific text to appear on the terminal screen or scrollback buffer.
    /// </summary>
    public async Task<bool> WaitForTextAsync(string text, TimeSpan? timeout = null, bool includeScrollback = false, CancellationToken ct = default)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(30));

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            var screenText = includeScrollback ? GetFullText() : GetScreenText();
            if (screenText.Contains(text, StringComparison.Ordinal))
            {
                return true;
            }

            await Task.Delay(200, ct);
        }

        return false;
    }

    /// <summary>
    /// Wait for any of the specified texts to appear on the terminal screen or scrollback buffer.
    /// Returns the matching text, or null if timeout.
    /// </summary>
    public async Task<string?> WaitForAnyTextAsync(IEnumerable<string> texts, TimeSpan? timeout = null, bool includeScrollback = false, CancellationToken ct = default)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(30));
        var textList = texts.ToList();

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            var screenText = includeScrollback ? GetFullText() : GetScreenText();
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

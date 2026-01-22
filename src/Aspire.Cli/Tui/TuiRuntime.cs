// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Aspire.Cli.Tui;

/// <summary>
/// Runtime for managing TUI components with live display and input handling.
/// </summary>
internal sealed class TuiRuntime : IDisposable
{
    private readonly IAnsiConsole _console;
    private readonly Channel<ConsoleKeyInfo> _inputChannel;
    private readonly CancellationTokenSource _cts = new();
    private TuiComponent? _rootComponent;
    private LiveDisplayContext? _liveContext;
    private bool _rerenderScheduled;
    private readonly object _renderLock = new();

    public TuiRuntime(IAnsiConsole console)
    {
        _console = console;
        _inputChannel = Channel.CreateUnbounded<ConsoleKeyInfo>();
    }

    /// <summary>
    /// Runs the TUI with the specified root component.
    /// </summary>
    public async Task RunAsync(TuiComponent rootComponent, CancellationToken cancellationToken)
    {
        _rootComponent = rootComponent;
        _rootComponent.Runtime = this;
        _rootComponent.ComponentDidMount();

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);
        var token = linkedCts.Token;

        // Start input reading on background thread
        var inputTask = Task.Run(() => InputLoopAsync(token), token);

        try
        {
            // Run live display on main thread
            await _console.Live(RenderRoot())
                .AutoClear(false)
                .StartAsync(async ctx =>
                {
                    _liveContext = ctx;
                    
                    // Force initial render
                    ctx.Refresh();

                    while (!token.IsCancellationRequested)
                    {
                        // Process pending input
                        while (_inputChannel.Reader.TryRead(out var keyInfo))
                        {
                            if (_rootComponent is IInputHandler handler)
                            {
                                handler.HandleInput(keyInfo);
                            }
                        }

                        // Rerender if needed
                        if (_rerenderScheduled)
                        {
                            lock (_renderLock)
                            {
                                _rerenderScheduled = false;
                                ctx.UpdateTarget(RenderRoot());
                            }
                        }

                        await Task.Delay(50, token); // ~20 FPS
                    }
                });
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
        finally
        {
            _rootComponent.ComponentWillUnmount();
        }
    }

    /// <summary>
    /// Schedules a rerender of the UI.
    /// </summary>
    public void ScheduleRerender()
    {
        _rerenderScheduled = true;
    }

    /// <summary>
    /// Sends a synthetic key press to the input handler.
    /// </summary>
    public void SendInput(ConsoleKeyInfo keyInfo)
    {
        _inputChannel.Writer.TryWrite(keyInfo);
    }

    private IRenderable RenderRoot()
    {
        try
        {
            return _rootComponent?.Render() ?? new Markup("[red]No component[/]");
        }
        catch (Exception ex)
        {
            return new Markup($"[red]Render error: {ex.Message.EscapeMarkup()}[/]");
        }
    }

    private async Task InputLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {
                    var keyInfo = Console.ReadKey(intercept: true);
                    await _inputChannel.Writer.WriteAsync(keyInfo, cancellationToken);
                }

                await Task.Delay(10, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}

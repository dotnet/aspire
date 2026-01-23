// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components;

/// <summary>
/// A terminal UI component that provides an interactive xterm.js terminal connected via WebSocket.
/// </summary>
public sealed partial class TerminalView : IAsyncDisposable
{
    private readonly string _containerId = $"terminal-container-{Guid.NewGuid():N}";
    private readonly string _terminalContentId = $"terminal-content-{Guid.NewGuid():N}";
    
    private IJSObjectReference? _jsModule;
    private DotNetObjectReference<TerminalView>? _dotNetRef;
    private bool _initialized;
    private string? _lastTerminalUrl;
    
    private string _statusText = "Connecting...";
    private string _statusClass = "connecting";

    [Parameter]
    public string? TerminalUrl { get; set; }

    [Inject]
    public required IStringLocalizer<Dashboard.Resources.Resources> ResourcesLoc { get; init; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            _jsModule = await JS.InvokeAsync<IJSObjectReference>("import", "/js/terminal-interop.js");
        }

        // Initialize or update terminal when URL changes
        if (!string.IsNullOrEmpty(TerminalUrl) && _jsModule is not null)
        {
            if (!_initialized)
            {
                await InitializeTerminalAsync();
            }
            else if (_lastTerminalUrl != TerminalUrl)
            {
                // URL changed - update the terminal connection
                await _jsModule.InvokeVoidAsync("updateTerminalUrl", _terminalContentId, TerminalUrl);
                _lastTerminalUrl = TerminalUrl;
            }
        }
    }

    private async Task InitializeTerminalAsync()
    {
        if (_jsModule is null || _dotNetRef is null || string.IsNullOrEmpty(TerminalUrl))
        {
            return;
        }

        var success = await _jsModule.InvokeAsync<bool>("initTerminal", _terminalContentId, TerminalUrl, _dotNetRef);
        if (success)
        {
            _initialized = true;
            _lastTerminalUrl = TerminalUrl;
        }
    }

    [JSInvokable]
    public void UpdateStatus(string status)
    {
        (_statusText, _statusClass) = status switch
        {
            "connected" => (ResourcesLoc[nameof(Dashboard.Resources.Resources.TerminalConnected)], "connected"),
            "disconnected" => (ResourcesLoc[nameof(Dashboard.Resources.Resources.TerminalDisconnected)], "disconnected"),
            "error" => (ResourcesLoc[nameof(Dashboard.Resources.Resources.TerminalDisconnected)], "disconnected"),
            "no-url" => (ResourcesLoc[nameof(Dashboard.Resources.Resources.TerminalWaiting)], "waiting"),
            _ => (ResourcesLoc[nameof(Dashboard.Resources.Resources.TerminalConnecting)], "connecting")
        };
        StateHasChanged();
    }

    /// <summary>
    /// Triggers a resize of the terminal to fit its container and sends resize event to server.
    /// Call this when the terminal becomes visible (e.g., when switching to the Terminal tab).
    /// </summary>
    public async Task FitAsync()
    {
        if (_jsModule is not null && _initialized)
        {
            try
            {
                await _jsModule.InvokeVoidAsync("fitTerminal", _terminalContentId);
            }
            catch (JSDisconnectedException)
            {
                // Circuit disconnected, ignore
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_jsModule is not null && _initialized)
        {
            try
            {
                await _jsModule.InvokeVoidAsync("disposeTerminal", _terminalContentId);
            }
            catch (JSDisconnectedException)
            {
                // Circuit disconnected, ignore
            }
        }

        _dotNetRef?.Dispose();

        if (_jsModule is not null)
        {
            try
            {
                await _jsModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Circuit disconnected, ignore
            }
        }
    }
}

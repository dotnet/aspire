// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Terminals;

/// <summary>
/// Extension methods for adding terminal WebSocket endpoints to a WebApplication.
/// </summary>
internal static class TerminalEndpointExtensions
{
    /// <summary>
    /// Maps terminal-related endpoints to the application.
    /// </summary>
    public static IEndpointRouteBuilder MapTerminalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // WebSocket endpoint for terminal connections
        endpoints.Map("/terminals/{id}", async context =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("WebSocket connection required").ConfigureAwait(false);
                return;
            }

            var terminalHost = context.RequestServices.GetService<TerminalHost>();
            if (terminalHost is null)
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsync("Terminal service not available").ConfigureAwait(false);
                return;
            }

            var id = context.Request.RouteValues["id"]?.ToString();
            if (string.IsNullOrEmpty(id))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Terminal ID required").ConfigureAwait(false);
                return;
            }

            var managedTerminal = terminalHost.GetTerminal(id);
            if (managedTerminal is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsync($"Terminal '{id}' not found").ConfigureAwait(false);
                return;
            }

            using var webSocket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
            var clientId = Guid.NewGuid().ToString("N")[..8];

            await managedTerminal.PresentationAdapter.AddClientAsync(clientId, webSocket, context.RequestAborted).ConfigureAwait(false);
        });

        // Serve xterm.js test page
        endpoints.MapGet("/terminals/{id}/index.html", async context =>
        {
            var terminalHost = context.RequestServices.GetService<TerminalHost>();
            if (terminalHost is null)
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsync("Terminal service not available").ConfigureAwait(false);
                return;
            }

            var id = context.Request.RouteValues["id"]?.ToString();
            if (string.IsNullOrEmpty(id))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Terminal ID required").ConfigureAwait(false);
                return;
            }

            var managedTerminal = terminalHost.GetTerminal(id);
            if (managedTerminal is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsync($"Terminal '{id}' not found").ConfigureAwait(false);
                return;
            }

            // Generate the WebSocket URL
            var scheme = context.Request.Scheme == "https" ? "wss" : "ws";
            var wsUrl = $"{scheme}://{context.Request.Host}/terminals/{id}";

            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync(GenerateTerminalHtml(id, wsUrl)).ConfigureAwait(false);
        });

        return endpoints;
    }

    private static string GenerateTerminalHtml(string terminalId, string wsUrl)
    {
        return $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Aspire Terminal - {{terminalId}}</title>
                <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/@xterm/xterm@5.5.0/css/xterm.min.css">
                <style>
                    * { margin: 0; padding: 0; box-sizing: border-box; }
                    html, body { height: 100%; background: #1e1e1e; }
                    #terminal { height: 100%; width: 100%; }
                    .xterm { height: 100%; }
                    #status {
                        position: fixed;
                        top: 10px;
                        right: 10px;
                        padding: 5px 10px;
                        border-radius: 4px;
                        font-family: monospace;
                        font-size: 12px;
                        z-index: 1000;
                    }
                    .connected { background: #28a745; color: white; }
                    .disconnected { background: #dc3545; color: white; }
                    .connecting { background: #ffc107; color: black; }
                </style>
            </head>
            <body>
                <div id="status" class="connecting">Connecting...</div>
                <div id="terminal"></div>
                
                <script src="https://cdn.jsdelivr.net/npm/@xterm/xterm@5.5.0/lib/xterm.min.js"></script>
                <script src="https://cdn.jsdelivr.net/npm/@xterm/addon-fit@0.10.0/lib/addon-fit.min.js"></script>
                <script src="https://cdn.jsdelivr.net/npm/@xterm/addon-web-links@0.11.0/lib/addon-web-links.min.js"></script>
                <script>
                    const WS_URL = '{{wsUrl}}';
                    const statusEl = document.getElementById('status');
                    
                    // Initialize terminal
                    const terminal = new Terminal({
                        cursorBlink: true,
                        fontSize: 14,
                        fontFamily: 'Consolas, "Courier New", monospace',
                        theme: {
                            background: '#1e1e1e',
                            foreground: '#d4d4d4'
                        }
                    });
                    
                    const fitAddon = new FitAddon.FitAddon();
                    const webLinksAddon = new WebLinksAddon.WebLinksAddon();
                    
                    terminal.loadAddon(fitAddon);
                    terminal.loadAddon(webLinksAddon);
                    terminal.open(document.getElementById('terminal'));
                    fitAddon.fit();
                    
                    // Handle window resize
                    window.addEventListener('resize', () => {
                        fitAddon.fit();
                        sendResize();
                    });
                    
                    // WebSocket connection
                    let ws = null;
                    
                    function connect() {
                        ws = new WebSocket(WS_URL);
                        
                        ws.onopen = () => {
                            statusEl.textContent = 'Connected';
                            statusEl.className = 'connected';
                            sendResize();
                        };
                        
                        ws.onclose = () => {
                            statusEl.textContent = 'Disconnected';
                            statusEl.className = 'disconnected';
                            // Reconnect after delay
                            setTimeout(connect, 2000);
                        };
                        
                        ws.onerror = (err) => {
                            console.error('WebSocket error:', err);
                        };
                        
                        ws.onmessage = (event) => {
                            try {
                                const msg = JSON.parse(event.data);
                                if (msg.type === 'output' && msg.data) {
                                    const bytes = atob(msg.data);
                                    terminal.write(bytes);
                                } else if (msg.type === 'state' && msg.data) {
                                    terminal.clear();
                                    const bytes = atob(msg.data);
                                    terminal.write(bytes);
                                }
                            } catch (e) {
                                // Raw output (legacy support)
                                terminal.write(event.data);
                            }
                        };
                    }
                    
                    function sendResize() {
                        if (ws && ws.readyState === WebSocket.OPEN) {
                            ws.send(JSON.stringify({
                                type: 'resize',
                                cols: terminal.cols,
                                rows: terminal.rows
                            }));
                        }
                    }
                    
                    function sendInput(data) {
                        if (ws && ws.readyState === WebSocket.OPEN) {
                            ws.send(JSON.stringify({
                                type: 'input',
                                data: btoa(data)
                            }));
                        }
                    }
                    
                    // Handle terminal input
                    terminal.onData((data) => {
                        sendInput(data);
                    });
                    
                    // Start connection
                    connect();
                    
                    // Focus terminal
                    terminal.focus();
                </script>
            </body>
            </html>
            """;
    }
}

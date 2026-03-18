// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.IdeSessionServer;

/// <summary>
/// An HTTPS server that provides the IDE session endpoint for DCP communication.
/// Implements the IDE execution protocol as defined in docs/specs/IDE-execution.md.
/// </summary>
internal sealed class SessionServer : IAsyncDisposable
{
    private static readonly string[] s_protocolsSupported = ["2024-03-03", "2024-04-23", "2025-10-01", "2026-02-01"];
    private static readonly string[] s_supportedLaunchConfigurations = ["project", "python"];

    private readonly ILogger _logger;
    private readonly X509Certificate2 _certificate;
    private readonly string _token;
    private readonly TcpListener _listener;
    private readonly ConcurrentDictionary<string, RunSession> _sessions = new();
    private readonly ConcurrentDictionary<string, WebSocket> _webSocketsByDcpId = new();
    private readonly CancellationTokenSource _serverCts = new();
    private Task? _acceptTask;

    /// <summary>
    /// Event raised when a new run session is requested.
    /// </summary>
    public event Func<RunSessionRequest, CancellationToken, Task<RunSessionResult>>? OnRunSessionRequested;

    /// <summary>
    /// Event raised when a run session should be stopped.
    /// </summary>
    public event Func<string, CancellationToken, Task>? OnRunSessionStopped;

    /// <summary>
    /// Gets the connection information for this server.
    /// </summary>
    public SessionServerConnectionInfo ConnectionInfo { get; }

    private SessionServer(
        ILogger logger,
        X509Certificate2 certificate,
        string token,
        TcpListener listener,
        int port)
    {
        _logger = logger;
        _certificate = certificate;
        _token = token;
        _listener = listener;

        ConnectionInfo = new SessionServerConnectionInfo
        {
            Port = port,
            Token = token,
            Certificate = SecurityHelper.GetCertificateBase64(certificate)
        };
    }

    /// <summary>
    /// Creates and starts a new IDE session server.
    /// </summary>
    public static async Task<SessionServer> CreateAsync(ILogger logger, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken; // Reserved for future use
        logger.LogDebug("Creating IDE session server");

        // Generate security credentials
        var certificate = SecurityHelper.CreateSelfSignedCertificate();
        var token = SecurityHelper.GenerateToken();

        // Bind to an OS-assigned port
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        logger.LogInformation("IDE session server listening on port {Port}", port);

        var server = new SessionServer(logger, certificate, token, listener, port);
        server.StartAccepting();

        return server;
    }

    private void StartAccepting()
    {
        _acceptTask = AcceptConnectionsAsync(_serverCts.Token);
    }

    private async Task AcceptConnectionsAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                _ = HandleClientAsync(client, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting connections");
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        try
        {
            using (client)
            {
                var sslStream = new SslStream(client.GetStream(), leaveInnerStreamOpen: false);

                await sslStream.AuthenticateAsServerAsync(
                    _certificate,
                    clientCertificateRequired: false,
                    checkCertificateRevocation: false);

                // Create the StreamReader once and reuse it across requests on this connection
                // to avoid losing buffered data between requests.
                var reader = new StreamReader(sslStream, Encoding.UTF8, leaveOpen: true);

                // Support HTTP/1.1 keep-alive: process multiple requests on the same connection
                while (!cancellationToken.IsCancellationRequested)
                {
                    var keepAlive = await ProcessHttpRequestAsync(sslStream, reader, cancellationToken);
                    if (!keepAlive)
                    {
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error handling client connection");
        }
    }

    /// <summary>
    /// Processes a single HTTP request on the connection.
    /// Returns <c>true</c> if the connection should be kept alive for additional requests,
    /// or <c>false</c> if it should be closed.
    /// </summary>
    private async Task<bool> ProcessHttpRequestAsync(SslStream stream, StreamReader reader, CancellationToken cancellationToken)
    {
        // Read HTTP request line. A null/empty result means the client closed the connection.
        var requestLine = await reader.ReadLineAsync(cancellationToken);

        if (string.IsNullOrEmpty(requestLine))
        {
            return false;
        }

        var parts = requestLine.Split(' ');
        if (parts.Length < 2)
        {
            await SendErrorResponseAsync(stream, 400, "Bad Request", "Invalid request line", cancellationToken);
            return false;
        }

        var method = parts[0];
        var path = parts[1];

        // Strip query string from path for routing
        var queryIndex = path.IndexOf('?');
        var pathWithoutQuery = queryIndex >= 0 ? path[..queryIndex] : path;

        // Read headers
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadLineAsync(cancellationToken) is { } line && !string.IsNullOrEmpty(line))
        {
            var colonIndex = line.IndexOf(':');
            if (colonIndex > 0)
            {
                var name = line[..colonIndex].Trim();
                var value = line[(colonIndex + 1)..].Trim();
                headers[name] = value;
            }
        }

        // Check for WebSocket upgrade (this takes over the connection, so don't keep-alive after)
        if (headers.TryGetValue("Upgrade", out var upgrade) &&
            upgrade.Equals("websocket", StringComparison.OrdinalIgnoreCase) &&
            pathWithoutQuery.StartsWith("/run_session/notify", StringComparison.OrdinalIgnoreCase))
        {
            await HandleWebSocketUpgradeAsync(stream, headers, cancellationToken);
            return false;
        }

        // Read body if Content-Length is present
        string? body = null;
        if (headers.TryGetValue("Content-Length", out var contentLengthStr) &&
            int.TryParse(contentLengthStr, out var contentLength) &&
            contentLength > 0)
        {
            var buffer = new char[contentLength];
            var read = await reader.ReadBlockAsync(buffer.AsMemory(), cancellationToken);
            body = new string(buffer, 0, read);
        }

        // Determine if connection should be kept alive (HTTP/1.1 defaults to keep-alive)
        var keepAlive = true;
        if (headers.TryGetValue("Connection", out var connection) &&
            connection.Equals("close", StringComparison.OrdinalIgnoreCase))
        {
            keepAlive = false;
        }

        // Route request
        await RouteRequestAsync(stream, method, path, headers, body, cancellationToken);

        return keepAlive;
    }

    private async Task RouteRequestAsync(
        SslStream stream,
        string method,
        string path,
        Dictionary<string, string> headers,
        string? body,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Request: {Method} {Path}", method, path);

        // Strip query string from path for routing
        var queryIndex = path.IndexOf('?');
        var pathWithoutQuery = queryIndex >= 0 ? path[..queryIndex] : path;

        // GET /telemetry/enabled - No auth required
        if (method == "GET" && pathWithoutQuery == "/telemetry/enabled")
        {
            var response = new TelemetryEnabledResponse { IsEnabled = false };
            await SendJsonResponseAsync(stream, 200, response, SessionJsonContext.Default.TelemetryEnabledResponse, cancellationToken);
            return;
        }

        // GET /info - Returns protocol capabilities
        if (method == "GET" && pathWithoutQuery == "/info")
        {
            var info = new RunSessionInfo
            {
                ProtocolsSupported = s_protocolsSupported,
                SupportedLaunchConfigurations = s_supportedLaunchConfigurations
            };
            await SendJsonResponseAsync(stream, 200, info, SessionJsonContext.Default.RunSessionInfo, cancellationToken);
            return;
        }

        // All other endpoints require authentication
        if (!ValidateAuth(headers))
        {
            await SendErrorResponseAsync(stream, 401, "Unauthorized", "Invalid or missing authorization", cancellationToken);
            return;
        }

        // PUT /run_session - Create a new run session
        if (method == "PUT" && pathWithoutQuery == "/run_session")
        {
            await HandleCreateSessionAsync(stream, headers, body, cancellationToken);
            return;
        }

        // DELETE /run_session/{id} - Stop a run session
        if (method == "DELETE" && pathWithoutQuery.StartsWith("/run_session/", StringComparison.OrdinalIgnoreCase))
        {
            var sessionId = pathWithoutQuery["/run_session/".Length..];
            await HandleDeleteSessionAsync(stream, sessionId, cancellationToken);
            return;
        }

        await SendErrorResponseAsync(stream, 404, "Not Found", $"Unknown endpoint: {method} {path}", cancellationToken);
    }

    private bool ValidateAuth(Dictionary<string, string> headers)
    {
        if (!headers.TryGetValue("Authorization", out var auth))
        {
            return false;
        }

        if (!auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var token = auth["Bearer ".Length..];
        return SecurityHelper.TimingSafeEquals(token, _token);
    }

    private async Task HandleCreateSessionAsync(
        SslStream stream,
        Dictionary<string, string> headers,
        string? body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(body))
        {
            await SendErrorResponseAsync(stream, 400, "BadRequest", "Missing request body", cancellationToken);
            return;
        }

        if (!headers.TryGetValue("microsoft-developer-dcp-instance-id", out var dcpId))
        {
            await SendErrorResponseAsync(stream, 400, "MissingHeaders", "Missing Microsoft-Developer-DCP-Instance-ID header", cancellationToken);
            return;
        }

        RunSessionPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize(body, SessionJsonContext.Default.RunSessionPayload);
        }
        catch (JsonException ex)
        {
            await SendErrorResponseAsync(stream, 400, "InvalidJson", $"Failed to parse request body: {ex.Message}", cancellationToken);
            return;
        }

        if (payload?.LaunchConfigurations is null || payload.LaunchConfigurations.Length == 0)
        {
            await SendErrorResponseAsync(stream, 400, "InvalidPayload", "Missing launch_configurations", cancellationToken);
            return;
        }

        var runId = GenerateRunId();
        _logger.LogInformation("Creating run session {RunId} for DCP {DcpId}", runId, dcpId);

        // Invoke the handler
        if (OnRunSessionRequested is not null)
        {
            try
            {
                var request = new RunSessionRequest
                {
                    RunId = runId,
                    DcpId = dcpId,
                    Payload = payload
                };

                var result = await OnRunSessionRequested(request, cancellationToken);

                if (!result.Success)
                {
                    await SendErrorResponseAsync(stream, 500, result.ErrorCode ?? "SessionFailed", result.ErrorMessage ?? "Failed to create session", cancellationToken);
                    return;
                }

                // Track the session
                _sessions[runId] = new RunSession { RunId = runId, DcpId = dcpId };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating run session {RunId}", runId);
                await SendErrorResponseAsync(stream, 500, "InternalError", ex.Message, cancellationToken);
                return;
            }
        }

        // Send 201 Created with Location header
        var locationUrl = $"https://{ConnectionInfo.Port}/run_session/{runId}";
        await SendResponseAsync(stream, 201, [("Location", locationUrl)], null, cancellationToken);

        _logger.LogInformation("Run session {RunId} created successfully", runId);
    }

    private async Task HandleDeleteSessionAsync(SslStream stream, string sessionId, CancellationToken cancellationToken)
    {
        if (_sessions.TryRemove(sessionId, out _))
        {
            _logger.LogInformation("Stopping run session {SessionId}", sessionId);

            if (OnRunSessionStopped is not null)
            {
                await OnRunSessionStopped(sessionId, cancellationToken);
            }

            await SendResponseAsync(stream, 200, [], null, cancellationToken);
        }
        else
        {
            await SendResponseAsync(stream, 204, [], null, cancellationToken);
        }
    }

    private async Task HandleWebSocketUpgradeAsync(
        SslStream stream,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken)
    {
        // Validate authorization
        if (!ValidateAuth(headers))
        {
            await SendErrorResponseAsync(stream, 401, "Unauthorized", "Invalid or missing authorization", cancellationToken);
            return;
        }

        // Get the DCP instance ID
        if (!headers.TryGetValue("microsoft-developer-dcp-instance-id", out var dcpId))
        {
            await SendErrorResponseAsync(stream, 400, "MissingHeaders", "Missing Microsoft-Developer-DCP-Instance-ID header", cancellationToken);
            return;
        }

        // Get the Sec-WebSocket-Key for the handshake
        if (!headers.TryGetValue("Sec-WebSocket-Key", out var webSocketKey))
        {
            await SendErrorResponseAsync(stream, 400, "BadRequest", "Missing Sec-WebSocket-Key header", cancellationToken);
            return;
        }

        // Compute the accept key per RFC 6455
        const string webSocketGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        var acceptKey = Convert.ToBase64String(SHA1.HashData(Encoding.UTF8.GetBytes(webSocketKey + webSocketGuid)));

        // Send the WebSocket upgrade response
        var response = new StringBuilder();
        response.Append("HTTP/1.1 101 Switching Protocols\r\n");
        response.Append("Upgrade: websocket\r\n");
        response.Append("Connection: Upgrade\r\n");
        response.Append(CultureInfo.InvariantCulture, $"Sec-WebSocket-Accept: {acceptKey}\r\n");
        response.Append("\r\n");

        var responseBytes = Encoding.UTF8.GetBytes(response.ToString());
        await stream.WriteAsync(responseBytes, cancellationToken);
        await stream.FlushAsync(cancellationToken);

        _logger.LogInformation("WebSocket connection established for DCP {DcpId}", dcpId);

        // Create a WebSocket over the SSL stream
        var webSocket = WebSocket.CreateFromStream(stream, new WebSocketCreationOptions
        {
            IsServer = true,
            KeepAliveInterval = TimeSpan.FromSeconds(30)
        });

        // Store the WebSocket connection
        _webSocketsByDcpId[dcpId] = webSocket;

        // Keep the connection alive by reading messages (DCP doesn't send messages, but we need to detect close)
        try
        {
            var buffer = new byte[1024];
            while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await webSocket.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogDebug("WebSocket close received from DCP {DcpId}", dcpId);
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Goodbye", cancellationToken);
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (WebSocketException ex)
        {
            _logger.LogDebug(ex, "WebSocket error for DCP {DcpId}", dcpId);
        }
        finally
        {
            _webSocketsByDcpId.TryRemove(dcpId, out _);
            _logger.LogDebug("WebSocket connection closed for DCP {DcpId}", dcpId);
        }
    }

    /// <summary>
    /// Sends a notification to the connected DCP instance.
    /// </summary>
    public async Task SendNotificationAsync(string dcpId, RunSessionNotification notification, CancellationToken cancellationToken = default)
    {
        if (_webSocketsByDcpId.TryGetValue(dcpId, out var ws) && ws.State == WebSocketState.Open)
        {
            var json = JsonSerializer.Serialize(notification, SessionJsonContext.Default.RunSessionNotification);
            var bytes = Encoding.UTF8.GetBytes(json + "\n");
            await ws.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
        }
        else
        {
            _logger.LogDebug("No WebSocket connection for DCP {DcpId}, notification dropped", dcpId);
        }
    }

    private static string GenerateRunId()
    {
        return $"run-{Guid.NewGuid():N}";
    }

    private static async Task SendResponseAsync(
        SslStream stream,
        int statusCode,
        IEnumerable<(string Name, string Value)> headers,
        byte[]? body,
        CancellationToken cancellationToken)
    {
        var statusText = statusCode switch
        {
            200 => "OK",
            201 => "Created",
            204 => "No Content",
            400 => "Bad Request",
            401 => "Unauthorized",
            404 => "Not Found",
            500 => "Internal Server Error",
            501 => "Not Implemented",
            _ => "Unknown"
        };

        var responseBuilder = new StringBuilder();
        responseBuilder.Append(CultureInfo.InvariantCulture, $"HTTP/1.1 {statusCode} {statusText}\r\n");

        foreach (var (name, value) in headers)
        {
            responseBuilder.Append(CultureInfo.InvariantCulture, $"{name}: {value}\r\n");
        }

        if (body is not null)
        {
            responseBuilder.Append(CultureInfo.InvariantCulture, $"Content-Length: {body.Length}\r\n");
            responseBuilder.Append("Content-Type: application/json\r\n");
        }
        else
        {
            responseBuilder.Append("Content-Length: 0\r\n");
        }

        responseBuilder.Append("Connection: keep-alive\r\n");
        responseBuilder.Append("\r\n");

        var headerBytes = Encoding.UTF8.GetBytes(responseBuilder.ToString());
        await stream.WriteAsync(headerBytes, cancellationToken);

        if (body is not null)
        {
            await stream.WriteAsync(body, cancellationToken);
        }

        await stream.FlushAsync(cancellationToken);
    }

    private static async Task SendJsonResponseAsync<T>(
        SslStream stream,
        int statusCode,
        T response,
        System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(response, jsonTypeInfo);
        await SendResponseAsync(stream, statusCode, [], json, cancellationToken);
    }

    private static async Task SendErrorResponseAsync(
        SslStream stream,
        int statusCode,
        string code,
        string message,
        CancellationToken cancellationToken)
    {
        var error = new ErrorResponse
        {
            Error = new ErrorDetail
            {
                Code = code,
                Message = message
            }
        };

        var json = JsonSerializer.SerializeToUtf8Bytes(error, SessionJsonContext.Default.ErrorResponse);
        await SendResponseAsync(stream, statusCode, [], json, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        _serverCts.Cancel();
        _listener.Stop();

        if (_acceptTask is not null)
        {
            try
            {
                await _acceptTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        _certificate.Dispose();
        _serverCts.Dispose();
    }
}

/// <summary>
/// Request data for creating a run session.
/// </summary>
internal sealed class RunSessionRequest
{
    public required string RunId { get; init; }
    public required string DcpId { get; init; }
    public required RunSessionPayload Payload { get; init; }
}

/// <summary>
/// Result of creating a run session.
/// </summary>
internal sealed class RunSessionResult
{
    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public static RunSessionResult Succeeded() => new() { Success = true };
    public static RunSessionResult Failed(string code, string message) => new() { Success = false, ErrorCode = code, ErrorMessage = message };
}

/// <summary>
/// Represents an active run session.
/// </summary>
internal sealed class RunSession
{
    public required string RunId { get; init; }
    public required string DcpId { get; init; }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using IAsyncDisposable = System.IAsyncDisposable;

namespace Aspire.Tools.Service;

/// <summary>
/// Implementation of the AspireServerService. A new instance of this service will be created for each
/// each call to IServiceBroker.CreateProxy()
/// </summary>
internal partial class AspireServerService : IAsyncDisposable
{
    public const string DebugSessionPortEnvVar = "DEBUG_SESSION_PORT";
    public const string DebugSessionTokenEnvVar = "DEBUG_SESSION_TOKEN";
    public const string DebugSessionServerCertEnvVar = "DEBUG_SESSION_SERVER_CERTIFICATE";

    public const int PingIntervalInSeconds = 5;

    private readonly IAspireServerEvents _aspireServerEvents;

    private readonly Action<string>? _reporter;

    private readonly string _currentSecret;
    private readonly string _displayName;

    private readonly CancellationTokenSource _shutdownCancellationTokenSource = new();
    private readonly int _port;
    private readonly X509Certificate2 _certificate;
    private readonly string _certificateEncodedBytes;

    private readonly SemaphoreSlim _webSocketAccess = new(1);

    private readonly SocketConnectionManager _socketConnectionManager = new();

    private volatile bool _isDisposed;

    private static readonly char[] s_charSeparator = { ' ' };

    private readonly Task _requestListener;

    public static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false)
        }
    };

    public AspireServerService(IAspireServerEvents aspireServerEvents, string displayName, Action<string>? reporter)
    {
        _aspireServerEvents = aspireServerEvents;
        _reporter = reporter;
        _displayName = displayName;

        _port = SocketUtilities.GetNextAvailablePort();

        // Set up the encryption so we can use it to generate our secret. 
        var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.KeySize = 128;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateKey();
        _currentSecret = Convert.ToBase64String(aes.Key);

        _certificate = CertGenerator.GenerateCert();
        var certBytes = _certificate.Export(X509ContentType.Cert);
        _certificateEncodedBytes = Convert.ToBase64String(certBytes);

        // Kick of the web server.
        _requestListener = StartListeningAsync();
    }

    public async ValueTask DisposeAsync()
    {
        // Shutdown the service:
        _shutdownCancellationTokenSource.Cancel();

        Log("Waiting for server to shutdown ...");

        try
        {
            await _requestListener;
        }
        catch (OperationCanceledException)
        {
            // nop
        }

        _isDisposed = true;

        _socketConnectionManager.Dispose();
        _certificate.Dispose();
        _shutdownCancellationTokenSource.Dispose();
    }

    /// <inheritdoc/>
    public List<KeyValuePair<string, string>> GetServerConnectionEnvironment()
        => 
        [
            new(DebugSessionPortEnvVar, $"localhost:{_port}"),
            new(DebugSessionTokenEnvVar, _currentSecret),
            new(DebugSessionServerCertEnvVar, _certificateEncodedBytes),
        ];

    /// <exception cref="OperationCanceledException"/>
    public ValueTask NotifySessionEndedAsync(string dcpId, string sessionId, int processId, int? exitCode, CancellationToken cancelationToken)
        => SendNotificationAsync(
            new SessionTerminatedNotification()
            {
                NotificationType = NotificationType.SessionTerminated,
                SessionId = sessionId,
                Pid = processId,
                ExitCode = exitCode
            },
            dcpId,
            sessionId,
            cancelationToken);

    /// <exception cref="OperationCanceledException"/>
    public ValueTask NotifySessionStartedAsync(string dcpId, string sessionId, int processId, CancellationToken cancelationToken)
        => SendNotificationAsync(
            new ProcessRestartedNotification()
            {
                NotificationType = NotificationType.ProcessRestarted,
                SessionId = sessionId,
                PID = processId
            },
            dcpId,
            sessionId,
            cancelationToken);

    /// <exception cref="OperationCanceledException"/>
    public ValueTask NotifyLogMessageAsync(string dcpId, string sessionId, bool isStdErr, string data, CancellationToken cancelationToken)
        => SendNotificationAsync(
            new ServiceLogsNotification()
            {
                NotificationType = NotificationType.ServiceLogs,
                SessionId = sessionId,
                IsStdErr = isStdErr,
                LogMessage = data
            },
            dcpId,
            sessionId,
            cancelationToken);

    /// <exception cref="OperationCanceledException"/>
    private async ValueTask SendNotificationAsync<TNotification>(TNotification notification, string dcpId, string sessionId, CancellationToken cancellationToken)
        where TNotification : SessionNotification
    {
        try
        {
            Log($"[#{sessionId}] Sending '{notification.NotificationType}': {notification}");
            var jsonSerialized = JsonSerializer.SerializeToUtf8Bytes(notification, JsonSerializerOptions);
            var success = await SendMessageAsync(dcpId, jsonSerialized, cancellationToken);

            if (!success)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Log($"[#{sessionId}] Failed to send message: Connection not found (dcpId='{dcpId}').");
            }
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                Log($"[#{sessionId}] Failed to send message: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Waits for a connection so that it can get the WebSocket that will be used to send messages tio the client. It accepts messages via Restful http
    /// calls.
    /// </summary>
    private Task StartListeningAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();

        builder.WebHost.ConfigureKestrel(kestrelOptions =>
        {
            kestrelOptions.ListenLocalhost(_port, listenOptions =>
            {
                listenOptions.UseHttps(_certificate);
            });
        });

        if (_reporter != null)
        {
            builder.Logging.ClearProviders();
            builder.Logging.AddProvider(new LoggerProvider(_reporter));
        }

        var app = builder.Build();

        app.MapGet("/", () => _displayName);
        app.MapGet(InfoResponse.Url, GetInfoAsync);

        // Set up the run session endpoints
        var runSessionApi = app.MapGroup(RunSessionRequest.Url);

        runSessionApi.MapPut("/", RunSessionPutAsync);
        runSessionApi.MapDelete("/{sessionId}", RunSessionDeleteAsync);
        runSessionApi.Map(SessionNotification.Url, RunSessionNotifyAsync);

        app.UseWebSockets(new WebSocketOptions
        {
             KeepAliveInterval = TimeSpan.FromSeconds(PingIntervalInSeconds)
        });

        // Run the application async. It will shutdown when the cancel token is signaled
        return app.RunAsync(_shutdownCancellationTokenSource.Token);
    }

    private async Task RunSessionPutAsync(HttpContext context)
    {
        // Check the authentication header
        if (!IsValidAuthentication(context))
        {
            Log("Authorization failure");
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        }
        else
        {
            await HandleStartSessionRequestAsync(context);
        }
    }

    private async Task RunSessionDeleteAsync(HttpContext context, string sessionId)
    {
        // Check the authentication header
        if (!IsValidAuthentication(context))
        {
            Log("Authorization failure");
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        }
        else
        {
            await HandleStopSessionRequestAsync(context, sessionId);
        }
    }

    private async Task GetInfoAsync(HttpContext context)
    {
        // Check the authentication header
        if (!IsValidAuthentication(context))
        {
            Log("Authorization failure");
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        }
        else
        {
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            await context.Response.WriteAsJsonAsync(InfoResponse.Instance, JsonSerializerOptions, _shutdownCancellationTokenSource.Token);
        }
    }

    private async Task RunSessionNotifyAsync(HttpContext context)
    {
        // Check the authentication header
        if (!IsValidAuthentication(context))
        {
            Log("Authorization failure");
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            return;
        }
        else if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var socketTcs = new TaskCompletionSource();

        // Track this connection.
        _socketConnectionManager.AddSocketConnection(webSocket, socketTcs,  context.GetDcpId(), context.RequestAborted);

        // We must keep the middleware pipeline alive for the duration of the socket
        await socketTcs.Task;
    }

    private void Log(string message)
    {
        _reporter?.Invoke(message);
    }

    private bool IsValidAuthentication(HttpContext context)
    {
        // Check the authentication header
        var authHeader = context.Request.Headers.Authorization;
        if (authHeader.Count == 1)
        {
            var authTokens = authHeader[0]!.Split(s_charSeparator, StringSplitOptions.RemoveEmptyEntries);

            return authTokens.Length == 2 &&
                   string.Equals(authTokens[0], "Bearer", StringComparison.Ordinal) &&
                   string.Equals(authTokens[1], _currentSecret, StringComparison.Ordinal);
        }

        return false;
    }

    private async Task HandleStartSessionRequestAsync(HttpContext context)
    {
        string? projectPath = null;

        try
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(AspireServerService), "Received 'PUT /run_session' request after the service has been disposed.");
            }

            // Get the project launch request data
            var projectLaunchRequest = await context.GetProjectLaunchInformationAsync(_shutdownCancellationTokenSource.Token);
            if (projectLaunchRequest == null)
            {
                // Unknown or unsupported version
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            projectPath = projectLaunchRequest.ProjectPath;

            var sessionId = await _aspireServerEvents.StartProjectAsync(context.GetDcpId(), projectLaunchRequest, _shutdownCancellationTokenSource.Token);

            context.Response.StatusCode = (int)HttpStatusCode.Created;
            context.Response.Headers.Location = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}/{sessionId}";
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            Log($"Failed to start project{(projectPath == null ? "" : $" '{projectPath}'")}: {e}");

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await WriteResponseTextAsync(context.Response, e, context.GetApiVersion() is not null);
        }
    }

    private async Task WriteResponseTextAsync(HttpResponse response, Exception ex, bool useRichErrorResponse)
    {
        byte[] errorResponse;
        if (useRichErrorResponse)
        {
            // If the exception is a webtools one, use the failure bucket strings as the error Code
            string? errorCode = null;

            var error = new ErrorResponse()
            {
                Error = new ErrorDetail { ErrorCode = errorCode, Message = ex.GetMessageFromException() }
            };

            await response.WriteAsJsonAsync(error, JsonSerializerOptions, _shutdownCancellationTokenSource.Token);
        }
        else
        {
            errorResponse = Encoding.UTF8.GetBytes(ex.GetMessageFromException());
            response.ContentType = "text/plain";
            response.ContentLength = errorResponse.Length;
            await response.WriteAsync(ex.GetMessageFromException(), _shutdownCancellationTokenSource.Token);
        }
    }

    private async ValueTask<bool> SendMessageAsync(string dcpId, byte[] messageBytes, CancellationToken cancellationToken)
    {
        // Find the connection for the passed in dcpId
        WebSocketConnection? connection = _socketConnectionManager.GetSocketConnection(dcpId);
        if (connection is null)
        {
            return false;
        }

        var success = false;
        try
        {
            using var cancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, _shutdownCancellationTokenSource.Token, connection.HttpRequestAborted);

            await _webSocketAccess.WaitAsync(cancelTokenSource.Token);
            await connection.Socket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, endOfMessage: true, cancelTokenSource.Token);

            success = true;
        }
        finally
        {
            if (!success)
            {
                // If the connection throws it almost certainly means the client has gone away, so clean up that connection
                _socketConnectionManager.RemoveSocketConnection(connection);
            }

            _webSocketAccess.Release();
        }

        return success;
    }

    private async ValueTask HandleStopSessionRequestAsync(HttpContext context, string sessionId)
    {
        try
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(AspireServerService), "Received 'DELETE /run_session' request after the service has been disposed.");
            }

            var sessionExists = await _aspireServerEvents.StopSessionAsync(context.GetDcpId(), sessionId, _shutdownCancellationTokenSource.Token);
            context.Response.StatusCode = (int)(sessionExists ? HttpStatusCode.OK : HttpStatusCode.NoContent);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            Log($"[#{sessionId}] Failed to stop: {e}");

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await WriteResponseTextAsync(context.Response, e, context.GetApiVersion() is not null);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net.Sockets;
using System.Text.Json;
using Aspire.Cli.Backchannel;
using Aspire.Cli.IdeSessionServer;
using Aspire.Cli.Projects;
using Aspire.DebugAdapter.Types;
using Aspire.Hosting;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable IDE0060 // Remove unused parameter - parameters are part of the middleware API contract

namespace Aspire.Cli.DebugAdapter;

/// <summary>
/// A debug adapter protocol middleware that bridges communication between an upstream
/// debug adapter client (IDE) and a downstream debug adapter server (debugger).
/// Requests, responses, and events are forwarded bidirectionally with optional interception.
/// </summary>
internal sealed class AspireDebugAdapterMiddleware : Aspire.DebugAdapter.Protocol.DebugAdapterMiddleware
{
    // Polyglot mode support
    private IAppHostProjectFactory? _appHostProjectFactory;
    private ILanguageDiscovery? _languageDiscovery;
    private PreparedAppHost? _preparedAppHost;

    // Backchannel support for dashboard URLs
    private IServiceProvider? _serviceProvider;
    private string? _backchannelSocketPath;
    private TaskCompletionSource<IAppHostCliBackchannel>? _backchannelCompletionSource;
    private IAppHostCliBackchannel? _backchannel;
    private CancellationTokenSource? _backchannelCts;

    // IDE session server support
    private SessionServer? _ideSessionServer;
    private bool _ideSessionServerEventSent;

    /// <summary>
    /// The adapter ID to use for the downstream debugger.
    /// </summary>
    private string? _downstreamAdapterId;

    /// <summary>
    /// Sets the adapter ID to use when forwarding initialize requests to the downstream debugger.
    /// Common values include "coreclr" for .NET, "python" for Python, "node" for Node.js, etc.
    /// </summary>
    public void SetDownstreamAdapterId(string adapterId)
    {
        _downstreamAdapterId = adapterId;
    }

    /// <summary>
    /// Enables polyglot mode for debugging non-.NET app hosts.
    /// When enabled, the middleware will start an AppHost server on launch requests
    /// and inject the required environment variables into the launch configuration.
    /// </summary>
    /// <param name="appHostProjectFactory">Factory for creating AppHost project handlers.</param>
    /// <param name="languageDiscovery">Service for discovering and resolving languages.</param>
    public void SetPolyglotMode(IAppHostProjectFactory appHostProjectFactory, ILanguageDiscovery languageDiscovery)
    {
        _appHostProjectFactory = appHostProjectFactory;
        _languageDiscovery = languageDiscovery;
    }

    /// <summary>
    /// Enables backchannel support for retrieving dashboard URLs from the AppHost.
    /// </summary>
    /// <param name="serviceProvider">Service provider for resolving IAppHostCliBackchannel instances.</param>
    public void SetBackchannelSupport(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Enables IDE session server mode.
    /// When enabled, the middleware will emit an aspire/ideSessionServer event with the server connection info.
    /// </summary>
    /// <param name="ideSessionServer">The IDE session server instance.</param>
    public void SetIdeSessionServer(SessionServer ideSessionServer)
    {
        _ideSessionServer = ideSessionServer;

        // Wire up the run session handler to emit DAP events
        _ideSessionServer.OnRunSessionRequested += HandleRunSessionRequestAsync;
        _ideSessionServer.OnRunSessionStopped += HandleRunSessionStoppedAsync;
    }

    /// <summary>
    /// Handles a run session request from DCP by emitting a DAP event to the IDE.
    /// </summary>
    private async Task<RunSessionResult> HandleRunSessionRequestAsync(
        RunSessionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Serialize launch configurations as raw JSON elements to pass through to the IDE.
            // The IDE is responsible for interpreting different configuration types (project, python, etc.).
            var launchConfigElements = request.Payload.LaunchConfigurations
                .Select(config => JsonSerializer.SerializeToElement(config, SessionJsonContext.Default.LaunchConfiguration))
                .ToArray();

            // Build environment dictionary (allowing nullable values to flow through)
            Dictionary<string, string?>? env = null;
            if (request.Payload.Env is not null)
            {
                env = request.Payload.Env.ToDictionary(e => e.Name, e => e.Value);
            }

            // Emit the run session event to the IDE
            var evt = new AspireRunSessionEvent
            {
                Body = new AspireRunSessionEventBody
                {
                    SessionId = request.RunId,
                    DcpId = request.DcpId,
                    LaunchConfigurations = launchConfigElements,
                    Env = env,
                    Args = request.Payload.Args,
                    DebugBridgeSocketPath = request.Payload.DebugBridgeSocketPath,
                    BridgeToken = _ideSessionServer?.ConnectionInfo.Token,
                    DebugSessionId = request.Payload.DebugSessionId
                }
            };

            await ClientConnection.SendEventAsync(evt, cancellationToken).ConfigureAwait(false);
            Log($"Sent aspire/runSession event: sessionId={request.RunId}, launchConfigs={launchConfigElements.Length}");

            // Return success immediately - the IDE handles the session asynchronously
            return IdeSessionServer.RunSessionResult.Succeeded();
        }
        catch (ObjectDisposedException)
        {
            Log($"DAP connection already closed, cannot emit aspire/runSession event for sessionId={request.RunId}");
            return IdeSessionServer.RunSessionResult.Failed("ConnectionClosed", "DAP connection is already closed");
        }
        catch (Exception ex)
        {
            Log($"Failed to emit aspire/runSession event: {ex}");
            return IdeSessionServer.RunSessionResult.Failed("EventFailed", ex.Message);
        }
    }

    /// <summary>
    /// Handles a run session stop request from DCP.
    /// </summary>
    private async Task HandleRunSessionStoppedAsync(string sessionId, CancellationToken cancellationToken)
    {
        try
        {
            var evt = new AspireStopSessionEvent
            {
                Body = new AspireStopSessionEventBody
                {
                    SessionId = sessionId
                }
            };

            await ClientConnection.SendEventAsync(evt, cancellationToken).ConfigureAwait(false);
            Log($"Sent aspire/stopSession event: sessionId={sessionId}");
        }
        catch (ObjectDisposedException)
        {
            Log($"DAP connection already closed, cannot emit aspire/stopSession event for sessionId={sessionId}");
        }
        catch (Exception ex)
        {
            Log($"Failed to emit aspire/stopSession event: {ex}");
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnHostResponseAsync(ResponseMessage response, CancellationToken cancellationToken)
    {
        Log($"Host response: {response.CommandName} (seq={response.Seq}, request_seq={response.RequestSeq}, success={response.Success})");

        // After the initialize response, send the IDE session server event if configured
        if (response.CommandName == "initialize" && response.Success && _ideSessionServer is not null && !_ideSessionServerEventSent)
        {
            _ideSessionServerEventSent = true;
            try
            {
                var evt = new AspireIdeSessionServerEvent
                {
                    Body = new AspireIdeSessionServerEventBody
                    {
                        Port = _ideSessionServer.ConnectionInfo.Port,
                        Token = _ideSessionServer.ConnectionInfo.Token,
                        Certificate = _ideSessionServer.ConnectionInfo.Certificate
                    }
                };
                await ClientConnection.SendEventAsync(evt, cancellationToken).ConfigureAwait(false);
                Log($"Sent aspire/ideSessionServer event: port={_ideSessionServer.ConnectionInfo.Port}");
            }
            catch (Exception ex)
            {
                Log($"Failed to send aspire/ideSessionServer event: {ex}");
            }
        }

        return true;
    }

    /// <inheritdoc/>
    protected override Task<bool> OnHostEventAsync(EventMessage evt, CancellationToken cancellationToken)
    {
        Log($"Host event: {evt.EventName} (seq={evt.Seq})");
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    protected override Task<bool> OnHostRequestAsync(RequestMessage request, CancellationToken cancellationToken)
    {
        Log($"Host request (reverse): {request.CommandName} (seq={request.Seq}) raw={request.RawJson}");
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    protected override Task OnUnknownMessageAsync(ProtocolMessage message, string source, CancellationToken cancellationToken)
    {
        Log($"Unknown message from {source}: type={message.Type} (seq={message.Seq}) raw={message.RawJson}");
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnClientRequestAsync(RequestMessage request, CancellationToken cancellationToken)
    {
        Log($"Client request: {request.CommandName} (seq={request.Seq})");

        switch (request.CommandName)
        {
            case "initialize":
                return await HandleInitializeRequestAsync(request, cancellationToken).ConfigureAwait(false);

            case "launch":
                return await HandleLaunchRequestAsync(request, cancellationToken).ConfigureAwait(false);

            case "attach":
                return await HandleAttachRequestAsync(request, cancellationToken).ConfigureAwait(false);

            case "disconnect":
                return await HandleDisconnectRequestAsync(request, cancellationToken).ConfigureAwait(false);

            case "aspire/notification":
                return await HandleAspireNotificationRequestAsync(request, cancellationToken).ConfigureAwait(false);

            default:
                // Forward all other requests unchanged
                Log($"Client request (unknown command): {request.CommandName} (seq={request.Seq}) raw={request.RawJson}");
                return true;
        }
    }

    private Task<bool> HandleInitializeRequestAsync(RequestMessage request, CancellationToken cancellationToken)
    {
        // Rewrite the adapter ID if one was configured for the downstream debugger
        if (_downstreamAdapterId is not null && request is InitializeRequest { Arguments: not null } initRequest)
        {
            initRequest.Arguments.AdapterID = _downstreamAdapterId;
        }

        // Forward the request
        return Task.FromResult(true);
    }

    private async Task<bool> HandleLaunchRequestAsync(RequestMessage request, CancellationToken cancellationToken)
    {
        if (request is not LaunchRequest launchRequest || launchRequest.Arguments is null)
        {
            Log($"Launch request not recognized as LaunchRequest (actual type: {request.GetType().Name}), forwarding as-is");
            return true;
        }

        Log($"Launch request received");

        // Extract inner configuration if present
        ExtractInnerConfiguration(launchRequest.Arguments);

        // Generate backchannel socket path for dashboard URL retrieval (used by both polyglot and .NET modes)
        string? backchannelSocketPath = null;
        if (_serviceProvider is not null)
        {
            try
            {
                backchannelSocketPath = BackchannelSocketHelper.GetBackchannelSocketPath();
                _backchannelSocketPath = backchannelSocketPath;
                _backchannelCompletionSource = new TaskCompletionSource<IAppHostCliBackchannel>();
                _backchannelCts = new CancellationTokenSource();
                Log($"Generated backchannel socket path: {backchannelSocketPath}");
            }
            catch (Exception ex)
            {
                Log($"Failed to generate backchannel socket path: {ex}");
                // Non-fatal - continue without backchannel support
            }
        }

        // Build IDE session server environment variables (used by both polyglot server process and .NET launch config)
        Dictionary<string, string>? ideSessionEnvVars = null;
        if (_ideSessionServer is not null)
        {
            // Determine run mode from the DAP noDebug flag
            var runMode = launchRequest.Arguments.NoDebug == true ? "NoDebug" : "Debug";

            ideSessionEnvVars = new Dictionary<string, string>
            {
                ["DEBUG_SESSION_PORT"] = _ideSessionServer.ConnectionInfo.Port.ToString(CultureInfo.InvariantCulture),
                ["DEBUG_SESSION_TOKEN"] = _ideSessionServer.ConnectionInfo.Token,
                ["DEBUG_SESSION_SERVER_CERTIFICATE"] = _ideSessionServer.ConnectionInfo.Certificate,
                ["DEBUG_SESSION_RUN_MODE"] = runMode,
                // JSON capabilities info for DCP
                ["DEBUG_SESSION_INFO"] = "{\"protocols_supported\":[\"2024-03-03\",\"2024-04-23\",\"2025-10-01\",\"2026-02-01\"],\"supported_launch_configurations\":[\"project\"]}"
            };
        }

        // Handle polyglot mode: prepare AppHost server and inject environment variables
        var isPolyglotMode = _appHostProjectFactory is not null && _languageDiscovery is not null;
        if (isPolyglotMode)
        {
            try
            {
                // In polyglot mode, the backchannel and IDE session server env vars go to the
                // AppHost *server* process (where DCP runs), not the guest process.
                await PreparePolyglotAndInjectEnvAsync(launchRequest.Arguments, backchannelSocketPath, ideSessionEnvVars, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log($"Failed to prepare polyglot AppHost: {ex}");
                // Send error response and don't forward
                var errorResponse = new LaunchResponse
                {
                    Success = false,
                    Message = $"Failed to prepare Aspire AppHost server: {ex.Message}"
                };
                await ClientConnection.SendResponseAsync(errorResponse, request.Seq, cancellationToken).ConfigureAwait(false);
                return false;
            }
        }
        else
        {
            // In .NET mode, inject backchannel and IDE session server env vars into the launch
            // configuration directly since the launched process IS the AppHost.

            if (backchannelSocketPath is not null)
            {
                try
                {
                    InjectEnvironmentVariables(launchRequest.Arguments, new Dictionary<string, string>
                    {
                        [KnownConfigNames.UnixSocketPath] = backchannelSocketPath
                    });
                    Log($"Injected backchannel socket path into launch configuration: {backchannelSocketPath}");
                }
                catch (Exception ex)
                {
                    Log($"Failed to inject backchannel socket path: {ex}");
                    // Non-fatal - continue without backchannel support
                }
            }

            if (ideSessionEnvVars is not null)
            {
                try
                {
                    InjectEnvironmentVariables(launchRequest.Arguments, ideSessionEnvVars);
                    Log($"Injected IDE session server info: {_ideSessionServer!.ConnectionInfo.Port}");
                }
                catch (Exception ex)
                {
                    Log($"Failed to inject IDE session server info: {ex}");
                    // Non-fatal - continue without IDE session server
                }
            }

        }

        // Start backchannel connection after forwarding launch request
        if (_serviceProvider is not null && _backchannelSocketPath is not null)
        {
            StartBackchannelConnectionAsync(cancellationToken);
        }

        // Log the final launch configuration being forwarded to the host adapter
        if (launchRequest.Arguments.ExtensionData is { } extData)
        {
            var keys = string.Join(", ", extData.Keys);
            Log($"Forwarding launch request to host adapter with config keys: [{keys}]");

            // Log the program path specifically since it's critical for launch
            if (extData.TryGetValue("program", out var program))
            {
                Log($"Launch program: {program}");
            }
        }

        // Forward the modified request
        return true;
    }

    private static Task<bool> HandleAttachRequestAsync(RequestMessage request, CancellationToken cancellationToken)
    {
        if (request is AttachRequest attachRequest && attachRequest.Arguments is not null)
        {
            ExtractInnerConfiguration(attachRequest.Arguments);
        }

        return Task.FromResult(true);
    }

    private async Task<bool> HandleDisconnectRequestAsync(RequestMessage request, CancellationToken cancellationToken)
    {
        // Cancel and clean up backchannel connection
        if (_backchannelCts is not null)
        {
            Log("Cancelling backchannel connection...");
            try
            {
                await _backchannelCts.CancelAsync().ConfigureAwait(false);
                _backchannelCts.Dispose();
            }
            catch (Exception ex)
            {
                Log($"Error cancelling backchannel: {ex}");
            }
            _backchannelCts = null;
            _backchannel = null;
            _backchannelCompletionSource = null;
            _backchannelSocketPath = null;
        }

        // Clean up polyglot AppHost server if it was started
        if (_preparedAppHost is not null)
        {
            Log("Cleaning up polyglot AppHost server...");
            try
            {
                await _preparedAppHost.DisposeAsync().ConfigureAwait(false);
                Log("Polyglot AppHost server cleaned up");
            }
            catch (Exception ex)
            {
                Log($"Error cleaning up polyglot AppHost server: {ex}");
            }
            _preparedAppHost = null;
        }

        // Forward the request
        return true;
    }

    /// <summary>
    /// Handles an aspire/notification custom DAP request from the IDE.
    /// The IDE sends notifications (serviceLogs, processRestarted, sessionTerminated, etc.)
    /// that need to be forwarded to DCP via the IDE session server's WebSocket connection.
    /// This is used in non-bridge DAP mode where child debug sessions are owned by the IDE
    /// but DCP's WebSocket is connected to the CLI's session server.
    /// </summary>
    private async Task<bool> HandleAspireNotificationRequestAsync(RequestMessage request, CancellationToken cancellationToken)
    {
        Log($"Received aspire/notification request (seq={request.Seq})");

        if (_ideSessionServer is null)
        {
            Log("IDE session server not configured, cannot forward notification");
            var errorResponse = new ResponseMessage
            {
                Type = "response",
                RequestSeq = request.Seq,
                Success = false,
                Message = "IDE session server not configured"
            };
            await ClientConnection.SendResponseAsync(errorResponse, request.Seq, cancellationToken).ConfigureAwait(false);
            return false;
        }

        try
        {
            // Parse the notification from the request arguments
            if (request.Arguments is not JsonElement argsElement)
            {
                Log("aspire/notification request has no arguments");
                var errorResponse = new ResponseMessage
                {
                    Type = "response",
                    RequestSeq = request.Seq,
                    Success = false,
                    Message = "Missing notification arguments"
                };
                await ClientConnection.SendResponseAsync(errorResponse, request.Seq, cancellationToken).ConfigureAwait(false);
                return false;
            }

            // Extract dcpId and the notification payload
            if (!argsElement.TryGetProperty("dcpId", out var dcpIdElement) || dcpIdElement.ValueKind != JsonValueKind.String)
            {
                Log("aspire/notification request missing dcpId");
                var errorResponse = new ResponseMessage
                {
                    Type = "response",
                    RequestSeq = request.Seq,
                    Success = false,
                    Message = "Missing dcpId in notification arguments"
                };
                await ClientConnection.SendResponseAsync(errorResponse, request.Seq, cancellationToken).ConfigureAwait(false);
                return false;
            }

            var dcpId = dcpIdElement.GetString()!;

            if (!argsElement.TryGetProperty("notification", out var notificationElement))
            {
                Log("aspire/notification request missing notification payload");
                var errorResponse = new ResponseMessage
                {
                    Type = "response",
                    RequestSeq = request.Seq,
                    Success = false,
                    Message = "Missing notification payload"
                };
                await ClientConnection.SendResponseAsync(errorResponse, request.Seq, cancellationToken).ConfigureAwait(false);
                return false;
            }

            // Deserialize the notification
            var notification = notificationElement.Deserialize(SessionJsonContext.Default.RunSessionNotification);
            if (notification is null)
            {
                Log("Failed to deserialize notification payload");
                var errorResponse = new ResponseMessage
                {
                    Type = "response",
                    RequestSeq = request.Seq,
                    Success = false,
                    Message = "Failed to deserialize notification"
                };
                await ClientConnection.SendResponseAsync(errorResponse, request.Seq, cancellationToken).ConfigureAwait(false);
                return false;
            }

            Log($"Forwarding {notification.NotificationType} notification to DCP {dcpId} for session {notification.SessionId}");
            await _ideSessionServer.SendNotificationAsync(dcpId, notification, cancellationToken).ConfigureAwait(false);

            // Send success response
            var successResponse = new ResponseMessage
            {
                Type = "response",
                RequestSeq = request.Seq,
                Success = true
            };
            await ClientConnection.SendResponseAsync(successResponse, request.Seq, cancellationToken).ConfigureAwait(false);
            return false; // We handled the request, don't forward to downstream adapter
        }
        catch (Exception ex)
        {
            Log($"Error handling aspire/notification: {ex}");
            var errorResponse = new ResponseMessage
            {
                Type = "response",
                RequestSeq = request.Seq,
                Success = false,
                Message = $"Error forwarding notification: {ex.Message}"
            };
            await ClientConnection.SendResponseAsync(errorResponse, request.Seq, cancellationToken).ConfigureAwait(false);
            return false;
        }
    }

    #region Aspire Configuration Extraction

    private const string ConfigurationPropertyName = "configuration";

    /// <summary>
    /// Properties that must be stripped from ExtensionData after extracting the inner configuration.
    /// These either collide with typed properties on the arguments class (causing duplicate JSON keys
    /// which can crash native DAP parsers like vsdbg) or are VS Code-only concepts that the host
    /// adapter does not understand.
    /// </summary>
    private static readonly HashSet<string> s_propertiesToStrip = new(StringComparer.Ordinal)
    {
        "noDebug",      // Typed property on LaunchRequestArguments — duplicate key would crash vsdbg
        "__restart",    // Typed property on LaunchRequestArguments
        "type",         // VS Code debug configuration concept (e.g. "coreclr")
        "request",      // VS Code debug configuration concept (e.g. "launch")
        "name",         // VS Code debug session display name
    };

    /// <summary>
    /// Extracts the inner configuration from an Aspire debug configuration.
    /// If the configuration contains an inner "configuration" property, that is used instead.
    /// Properties that collide with typed members or are VS Code-only are stripped to avoid
    /// duplicate JSON keys and unknown-property issues in the downstream adapter.
    /// </summary>
    private static void ExtractInnerConfiguration(LaunchRequestArguments args)
    {
        if (args.ExtensionData is null)
        {
            return;
        }

        if (args.ExtensionData.TryGetValue(ConfigurationPropertyName, out var configElement) &&
            configElement.ValueKind == JsonValueKind.Object)
        {
            // Replace the extension data with the inner configuration properties,
            // skipping any that would collide with typed properties or are VS Code-only.
            var newExtensionData = new Dictionary<string, JsonElement>();
            foreach (var property in configElement.EnumerateObject())
            {
                if (!s_propertiesToStrip.Contains(property.Name))
                {
                    newExtensionData[property.Name] = property.Value;
                }
            }
            args.ExtensionData = newExtensionData;
        }
    }

    /// <summary>
    /// Extracts the inner configuration from an Aspire debug configuration.
    /// If the configuration contains an inner "configuration" property, that is used instead.
    /// Properties that collide with typed members or are VS Code-only are stripped.
    /// </summary>
    private static void ExtractInnerConfiguration(AttachRequestArguments args)
    {
        if (args.ExtensionData is null)
        {
            return;
        }

        if (args.ExtensionData.TryGetValue(ConfigurationPropertyName, out var configElement) &&
            configElement.ValueKind == JsonValueKind.Object)
        {
            // Replace the extension data with the inner configuration properties,
            // skipping any that would collide with typed properties or are VS Code-only.
            var newExtensionData = new Dictionary<string, JsonElement>();
            foreach (var property in configElement.EnumerateObject())
            {
                if (!s_propertiesToStrip.Contains(property.Name))
                {
                    newExtensionData[property.Name] = property.Value;
                }
            }
            args.ExtensionData = newExtensionData;
        }
    }

    #endregion

    #region Polyglot Mode Support

    /// <summary>
    /// Prepares the polyglot AppHost server and injects environment variables into the launch configuration.
    /// </summary>
    private async Task PreparePolyglotAndInjectEnvAsync(LaunchRequestArguments args, string? backchannelSocketPath, Dictionary<string, string>? serverEnvironmentVariables, CancellationToken cancellationToken)
    {
        // Get the program path from the launch configuration
        string? programPath = null;
        if (args.ExtensionData?.TryGetValue("program", out var programElement) == true &&
            programElement.ValueKind == JsonValueKind.String)
        {
            programPath = programElement.GetString();
        }

        if (string.IsNullOrEmpty(programPath))
        {
            Log("No 'program' property in launch configuration, skipping polyglot preparation");
            return;
        }

        Log($"Polyglot mode: program path is {programPath}");

        // Determine working directory: use cwd if present, otherwise use program's directory
        string workingDirectory;
        if (args.ExtensionData?.TryGetValue("cwd", out var cwdElement) == true &&
            cwdElement.ValueKind == JsonValueKind.String &&
            cwdElement.GetString() is string cwd &&
            !string.IsNullOrEmpty(cwd))
        {
            workingDirectory = cwd;
        }
        else
        {
            workingDirectory = Path.GetDirectoryName(programPath) ?? Environment.CurrentDirectory;
        }

        Log($"Polyglot mode: working directory is {workingDirectory}");

        // Detect the language from the program file
        var appHostFile = new FileInfo(programPath);
        var language = _languageDiscovery!.GetLanguageByFile(appHostFile);
        if (language is null)
        {
            Log($"Could not detect language for file: {programPath}");
            throw new InvalidOperationException($"Could not detect language for file: {appHostFile.Name}");
        }

        Log($"Polyglot mode: detected language {language.DisplayName}");

        // Get the appropriate project handler
        var project = _appHostProjectFactory!.GetProject(language);
        if (project is not GuestAppHostProject guestProject)
        {
            Log($"Project handler is not a GuestAppHostProject: {project.GetType().Name}");
            throw new InvalidOperationException($"Unexpected project type for polyglot debugging: {project.GetType().Name}");
        }

        // Prepare the AppHost server
        Log("Preparing polyglot AppHost server...");

        if (serverEnvironmentVariables is not null)
        {
            Log($"[ENV DIAG] Server env vars being passed to PrepareAsync ({serverEnvironmentVariables.Count}):");
            foreach (var (key, value) in serverEnvironmentVariables)
            {
                Log($"[ENV DIAG]   input: {key} = {value}");
            }
        }
        else
        {
            Log("[ENV DIAG] No server env vars (null)");
        }

        _preparedAppHost = await guestProject.PrepareAsync(appHostFile, backchannelSocketPath, serverEnvironmentVariables, Log, cancellationToken).ConfigureAwait(false);
        Log($"Polyglot AppHost server prepared. Socket: {_preparedAppHost.SocketPath}");

        // Log the final environment variables that were applied to the AppHost server process
        Log($"[ENV DIAG] AppHost server was launched with {_preparedAppHost.ServerLaunchEnvironmentVariables.Count} env vars:");
        foreach (var (key, value) in _preparedAppHost.ServerLaunchEnvironmentVariables)
        {
            Log($"[ENV DIAG]   server: {key} = {value}");
        }

        // Inject the environment variables into the launch configuration
        InjectEnvironmentVariables(args, _preparedAppHost.EnvironmentVariables);
    }

    /// <summary>
    /// Injects environment variables into the launch configuration's "env" property.
    /// </summary>
    private void InjectEnvironmentVariables(LaunchRequestArguments args, Dictionary<string, string> envVars)
    {
        Log($"Injecting {envVars.Count} environment variables into launch configuration");

        args.ExtensionData ??= new Dictionary<string, JsonElement>();

        // Get existing env object or create new one
        Dictionary<string, JsonElement> envObject;
        if (args.ExtensionData.TryGetValue("env", out var existingEnv) &&
            existingEnv.ValueKind == JsonValueKind.Object)
        {
            envObject = new Dictionary<string, JsonElement>();
            foreach (var property in existingEnv.EnumerateObject())
            {
                envObject[property.Name] = property.Value;
            }
        }
        else
        {
            envObject = new Dictionary<string, JsonElement>();
        }

        // Add the environment variables
        foreach (var (key, value) in envVars)
        {
            Log($"  Setting env.{key} = {value}");
            envObject[key] = JsonSerializer.SerializeToElement(value, DebugAdapterJsonContext.Default.String);
        }

        // Serialize the env object back
        args.ExtensionData["env"] = JsonSerializer.SerializeToElement(envObject, DebugAdapterJsonContext.Default.DictionaryStringJsonElement);
    }

    #endregion

    #region Backchannel Support

    /// <summary>
    /// Starts connecting to the AppHost backchannel in the background.
    /// </summary>
    private void StartBackchannelConnectionAsync(CancellationToken cancellationToken)
    {
        if (_serviceProvider is null || _backchannelSocketPath is null || _backchannelCompletionSource is null || _backchannelCts is null)
        {
            return;
        }

        var backchannel = _serviceProvider.GetRequiredService<IAppHostCliBackchannel>();
        var socketPath = _backchannelSocketPath;
        var completionSource = _backchannelCompletionSource;
        var backchannelToken = _backchannelCts.Token;

        _ = Task.Run(async () =>
        {
            Log($"Starting backchannel connection to {socketPath}");

            var connectionAttempts = 0;
            while (!backchannelToken.IsCancellationRequested)
            {
                try
                {
                    Log($"Attempting to connect to AppHost backchannel at {socketPath} (attempt {++connectionAttempts})");
                    await backchannel.ConnectAsync(socketPath, backchannelToken).ConfigureAwait(false);
                    _backchannel = backchannel;
                    completionSource.TrySetResult(backchannel);
                    Log($"Connected to AppHost backchannel at {socketPath}");

                    // Retrieve and emit dashboard URLs
                    await RetrieveAndEmitDashboardUrlsAsync(backchannel, backchannelToken).ConfigureAwait(false);
                    return;
                }
                catch (SocketException)
                {
                    // Slow down polling after initial attempts
                    if (connectionAttempts > 10)
                    {
                        await Task.Delay(1000, backchannelToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await Task.Delay(50, backchannelToken).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    Log("Backchannel connection cancelled");
                    completionSource.TrySetCanceled(backchannelToken);
                    return;
                }
                catch (Exception ex)
                {
                    Log($"Failed to connect to backchannel: {ex}");
                    completionSource.TrySetException(ex);
                    return;
                }
            }
        }, backchannelToken);
    }

    /// <summary>
    /// Retrieves dashboard URLs from the AppHost and emits them as DAP events.
    /// </summary>
    private async Task RetrieveAndEmitDashboardUrlsAsync(IAppHostCliBackchannel backchannel, CancellationToken cancellationToken)
    {
        try
        {
            Log("Retrieving dashboard URLs from AppHost");
            var dashboardUrls = await backchannel.GetDashboardUrlsAsync(cancellationToken).ConfigureAwait(false);

            if (dashboardUrls.DashboardHealthy && !string.IsNullOrEmpty(dashboardUrls.BaseUrlWithLoginToken))
            {
                Log($"Dashboard URL: {dashboardUrls.BaseUrlWithLoginToken}");

                // Send output event with dashboard URL (visible in debug console)
                await ClientConnection.SendEventAsync(new OutputEvent
                {
                    Body = new OutputEventBody
                    {
                        Category = OutputEventBody.CategoryValues.Console,
                        Output = $"Dashboard: {dashboardUrls.BaseUrlWithLoginToken}\n"
                    }
                }, cancellationToken).ConfigureAwait(false);

                // Send custom aspire/dashboard event for extension to handle browser launch
                await ClientConnection.SendEventAsync(new AspireDashboardEvent
                {
                    Body = new AspireDashboardEventBody
                    {
                        BaseUrlWithLoginToken = dashboardUrls.BaseUrlWithLoginToken,
                        CodespacesUrlWithLoginToken = dashboardUrls.CodespacesUrlWithLoginToken,
                        DashboardHealthy = dashboardUrls.DashboardHealthy
                    }
                }, cancellationToken).ConfigureAwait(false);
                Log("Sent aspire/dashboard event to upstream client");
            }
            else
            {
                Log("Dashboard not healthy or URL not available");
            }
        }
        catch (Exception ex)
        {
            Log($"Failed to retrieve dashboard URLs: {ex}");
        }
    }

    #endregion
}

/// <summary>
/// AOT-compatible JSON serializer context for Debug Adapter middleware.
/// </summary>
[System.Text.Json.Serialization.JsonSerializable(typeof(string))]
[System.Text.Json.Serialization.JsonSerializable(typeof(Dictionary<string, JsonElement>))]
internal sealed partial class DebugAdapterJsonContext : System.Text.Json.Serialization.JsonSerializerContext
{
}

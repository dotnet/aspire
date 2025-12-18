// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Aspire.Hosting.Mcp.Bridge;

/// <summary>
/// Service that proxies HTTP MCP requests to a stdio-based MCP server.
/// </summary>
internal sealed class StdioMcpProxyService : IHostedService, IAsyncDisposable
{
    private readonly ILogger<StdioMcpProxyService> _logger;
    private readonly IConfiguration _configuration;
    private readonly SemaphoreSlim _clientLock = new(1, 1);
    private McpClient? _mcpClient;
    private bool _disposed;

    /// <summary>
    /// Gets a value indicating whether the MCP client has been successfully started.
    /// </summary>
    public bool IsStarted => _mcpClient != null;

    public StdioMcpProxyService(ILogger<StdioMcpProxyService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Handles an HTTP MCP request by proxying it to the stdio MCP server.
    /// </summary>
    public async Task HandleRequestAsync(HttpContext context, CancellationToken cancellationToken)
    {
        // Ensure client is initialized
        var client = await GetOrCreateClientAsync(cancellationToken).ConfigureAwait(false);

        // Read the JSON-RPC request
        JsonElement request;
        try
        {
            request = await JsonSerializer.DeserializeAsync(context.Request.Body, McpBridgeJsonContext.Default.JsonElement, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON in request");
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new JsonRpcErrorResponse
            {
                Error = new JsonRpcError { Code = -32700, Message = "Parse error" },
                Id = null
            }, McpBridgeJsonContext.Default.JsonRpcErrorResponse, cancellationToken: cancellationToken).ConfigureAwait(false);
            return;
        }

        // Extract method and params
        var method = request.GetProperty("method").GetString();
        var id = request.TryGetProperty("id", out var idProp) ? idProp : (JsonElement?)null;

        _logger.LogDebug("Proxying MCP request: {Method}", method);

        try
        {
            object? result = method switch
            {
                // Protocol lifecycle
                "initialize" => new InitializeResult
                {
                    ProtocolVersion = "2024-11-05",
                    ServerInfo = new ServerInfo { Name = "aspire-mcp-bridge", Version = "1.0.0" },
                    Capabilities = new ServerCapabilities { Tools = new ToolsCapability() }
                },
                "ping" => new PingResult(),

                // Notifications (no response needed, return null)
                "notifications/initialized" or
                "notifications/cancelled" or
                "notifications/progress" or
                "notifications/message" or
                "notifications/resources/updated" or
                "notifications/resources/list_changed" or
                "notifications/tools/list_changed" or
                "notifications/prompts/list_changed" => null,

                // Tools - proxy to underlying server
                // Wrap in ListToolsResult which has { "tools": [...] } structure
                "tools/list" => new ListToolsResult { Tools = (await client.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false)).Select(t => t.ProtocolTool).ToList() },
                "tools/call" when request.TryGetProperty("params", out var toolParams) =>
                    await HandleToolCallAsync(client, toolParams, cancellationToken).ConfigureAwait(false),

                // Resources - proxy to underlying server  
                // Wrap in ListResourcesResult which has { "resources": [...] } structure
                "resources/list" => new ListResourcesResult { Resources = (await client.ListResourcesAsync(cancellationToken: cancellationToken).ConfigureAwait(false)).Select(r => r.ProtocolResource).ToList() },
                "resources/templates/list" => new ListResourceTemplatesResult { ResourceTemplates = (await client.ListResourceTemplatesAsync(cancellationToken: cancellationToken).ConfigureAwait(false)).Select(t => t.ProtocolResourceTemplate).ToList() },
                "resources/read" when request.TryGetProperty("params", out var resourceParams) =>
                    await HandleResourceReadAsync(client, resourceParams, cancellationToken).ConfigureAwait(false),

                // Prompts - proxy to underlying server
                // Wrap in ListPromptsResult which has { "prompts": [...] } structure
                "prompts/list" => new ListPromptsResult { Prompts = (await client.ListPromptsAsync(cancellationToken: cancellationToken).ConfigureAwait(false)).Select(p => p.ProtocolPrompt).ToList() },
                "prompts/get" when request.TryGetProperty("params", out var promptParams) =>
                    await HandlePromptGetAsync(client, promptParams, cancellationToken).ConfigureAwait(false),

                // Logging
                "logging/setLevel" => new EmptyResult(), // Acknowledge but don't change logging

                // Unknown method
                _ => throw new InvalidOperationException($"Unsupported method: {method}")
            };

            // Send successful response - preserve the original ID type (string, number, or null)
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new JsonRpcSuccessResponse
            {
                Result = result,
                Id = id.HasValue ? GetIdValue(id.Value) : null
            }, McpBridgeJsonContext.Default.JsonRpcSuccessResponse, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MCP request");
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new JsonRpcErrorResponse
            {
                Error = new JsonRpcError
                {
                    Code = -32603,
                    Message = $"Internal error: {ex.Message}"
                },
                Id = id.HasValue ? GetIdValue(id.Value) : null
            }, McpBridgeJsonContext.Default.JsonRpcErrorResponse, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Extracts the ID value from a JsonElement, preserving its original type (string, number, or null).
    /// </summary>
    private static object? GetIdValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var longVal) => longVal,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.Null => null,
            _ => element.GetRawText() // Fallback
        };
    }

    private static async Task<CallToolResult> HandleToolCallAsync(McpClient client, JsonElement paramsElement, CancellationToken cancellationToken)
    {
        var toolName = paramsElement.GetProperty("name").GetString() ?? throw new InvalidOperationException("Tool name is required");
        
        // Convert JsonElement to dictionary for AOT compatibility
        IReadOnlyDictionary<string, object?>? arguments = null;
        if (paramsElement.TryGetProperty("arguments", out var argsElement))
        {
            var dict = new Dictionary<string, object?>();
            foreach (var property in argsElement.EnumerateObject())
            {
                dict[property.Name] = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString(),
                    JsonValueKind.Number => property.Value.TryGetInt64(out var i64) ? i64 : property.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => property.Value
                };
            }
            arguments = dict;
        }

        return await client.CallToolAsync(toolName, arguments, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private static async Task<ReadResourceResult> HandleResourceReadAsync(McpClient client, JsonElement paramsElement, CancellationToken cancellationToken)
    {
        var uri = paramsElement.GetProperty("uri").GetString() ?? throw new InvalidOperationException("Resource URI is required");
        return await client.ReadResourceAsync(uri, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private static async Task<GetPromptResult> HandlePromptGetAsync(McpClient client, JsonElement paramsElement, CancellationToken cancellationToken)
    {
        var name = paramsElement.GetProperty("name").GetString() ?? throw new InvalidOperationException("Prompt name is required");

        // Convert JsonElement to dictionary for AOT compatibility
        IReadOnlyDictionary<string, object?>? arguments = null;
        if (paramsElement.TryGetProperty("arguments", out var argsElement))
        {
            var dict = new Dictionary<string, object?>();
            foreach (var property in argsElement.EnumerateObject())
            {
                dict[property.Name] = property.Value.GetString();
            }
            arguments = dict;
        }

        return await client.GetPromptAsync(name, arguments, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private async Task<McpClient> GetOrCreateClientAsync(CancellationToken cancellationToken)
    {
        if (_mcpClient != null)
        {
            return _mcpClient;
        }

        await _clientLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_mcpClient != null)
            {
                return _mcpClient;
            }

            var command = _configuration["MCP_SERVER_COMMAND"]
                ?? throw new InvalidOperationException("MCP_SERVER_COMMAND environment variable is required");

            var argsString = _configuration["MCP_SERVER_ARGS"];
            var args = string.IsNullOrWhiteSpace(argsString)
                ? new List<string>()
                : argsString.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();

            var workingDirectory = _configuration["MCP_SERVER_WORKING_DIRECTORY"];

            // Collect environment variables and dynamic arguments to pass to the child process
            // Variables prefixed with MCP_PROC_ENV_ are passed as environment variables with the prefix stripped
            // Variables prefixed with MCP_PROC_ARG_ are appended as command-line arguments (--arg-name value)
            var processEnvironment = new Dictionary<string, string>();
            foreach (var kvp in _configuration.AsEnumerable())
            {
                if (kvp.Key.StartsWith("MCP_PROC_ENV_", StringComparison.OrdinalIgnoreCase) && kvp.Value != null)
                {
                    var envVarName = kvp.Key.Substring("MCP_PROC_ENV_".Length);
                    processEnvironment[envVarName] = kvp.Value;
                    _logger.LogInformation("Passing environment variable to MCP server: {Name}={Value}", envVarName, envVarName.Contains("PASSWORD", StringComparison.OrdinalIgnoreCase) || envVarName.Contains("SECRET", StringComparison.OrdinalIgnoreCase) ? "***" : kvp.Value);
                }
                else if (kvp.Key.StartsWith("MCP_PROC_ARG_", StringComparison.OrdinalIgnoreCase) && kvp.Value != null)
                {
                    // Convert env var name to argument name: MCP_PROC_ARG_URL -> --url
                    var argName = "--" + kvp.Key.Substring("MCP_PROC_ARG_".Length).ToLowerInvariant().Replace("_", "-");
                    args.Add(argName);
                    args.Add(kvp.Value);
                    var isSensitive = argName.Contains("password", StringComparison.OrdinalIgnoreCase) || argName.Contains("secret", StringComparison.OrdinalIgnoreCase);
                    _logger.LogInformation("Passing argument to MCP server: {Name} {Value}", argName, isSensitive ? "***" : kvp.Value);
                }
            }

            _logger.LogInformation("Starting stdio MCP server: {Command} {Args}", command, string.Join(" ", args));
            if (!string.IsNullOrEmpty(workingDirectory))
            {
                _logger.LogInformation("Working directory: {WorkingDirectory}", workingDirectory);
            }

            StdioClientTransport transport;
            try
            {
                var transportOptions = new StdioClientTransportOptions
                {
                    Command = command,
                    Arguments = args,
                    WorkingDirectory = workingDirectory,
                    Name = "MCP Bridge Server"
                };
                
                // Add environment variables to be passed to the process
                if (transportOptions.EnvironmentVariables is not null)
                {
                    foreach (var envVar in processEnvironment)
                    {
                        transportOptions.EnvironmentVariables[envVar.Key] = envVar.Value;
                    }
                }
                
                transport = new StdioClientTransport(transportOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create stdio transport for command: {Command}", command);
                throw;
            }

            try
            {
                _mcpClient = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to MCP server. Command: {Command}, Args: {Args}", command, string.Join(" ", args));
                throw;
            }

            _logger.LogInformation("Successfully connected to stdio MCP server");

            return _mcpClient;
        }
        finally
        {
            _clientLock.Release();
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing MCP client at startup...");
        await GetOrCreateClientAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_mcpClient != null)
        {
            await _mcpClient.DisposeAsync().ConfigureAwait(false);
        }
        _clientLock.Dispose();
    }
}

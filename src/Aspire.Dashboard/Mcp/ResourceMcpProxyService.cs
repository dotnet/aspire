// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Text.Json;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Utils;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Aspire.Dashboard.Mcp;

/// <summary>
/// Discovers MCP-capable resources and proxies their tools through the Aspire MCP server.
/// </summary>
internal sealed class ResourceMcpProxyService : IAsyncDisposable
{
    private readonly IDashboardClient _dashboardClient;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ResourceMcpProxyService> _logger;
    private readonly CancellationTokenSource _cts = new();
    private readonly SemaphoreSlim _startGate = new(1, 1);
    private readonly ConcurrentDictionary<string, ResourceRegistration> _registrations = new(StringComparers.ResourceName);
    private readonly ConcurrentDictionary<string, ProxiedTool> _toolIndex = new(StringComparer.Ordinal);
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerOptions.Default)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private Task? _watchTask;

    public ResourceMcpProxyService(IDashboardClient dashboardClient, ILoggerFactory loggerFactory, ILogger<ResourceMcpProxyService> logger)
    {
        _dashboardClient = dashboardClient;
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    public async Task EnsureStartedAsync()
    {
        if (_watchTask is not null)
        {
            return;
        }

        await _startGate.WaitAsync(_cts.Token).ConfigureAwait(false);
        try
        {
            _watchTask ??= Task.Run(WatchAsync, _cts.Token);
        }
        finally
        {
            _startGate.Release();
        }
    }

    public async Task<IReadOnlyList<Tool>> GetToolsAsync(CancellationToken cancellationToken)
    {
        await EnsureStartedAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        return _toolIndex.Values.Select(t => t.Tool).ToList();
    }

    public async Task<CallToolResult?> TryHandleCallAsync(string? toolName, IReadOnlyDictionary<string, JsonElement>? arguments, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(toolName))
        {
            return null;
        }

        await EnsureStartedAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (!_toolIndex.TryGetValue(toolName, out var proxiedTool))
        {
            return null;
        }

        var args = arguments?.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value) ?? new Dictionary<string, object?>();

        return await proxiedTool.Client.CallToolAsync(
            proxiedTool.RemoteName,
            args,
            progress: null,
            _serializerOptions,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task WatchAsync()
    {
        if (!_dashboardClient.IsEnabled)
        {
            return;
        }

        try
        {
            await _dashboardClient.WhenConnected.ConfigureAwait(false);

            var subscription = await _dashboardClient.SubscribeResourcesAsync(_cts.Token).ConfigureAwait(false);

            foreach (var resource in subscription.InitialState)
            {
                await UpdateResourceAsync(resource, _cts.Token).ConfigureAwait(false);
            }

            await foreach (var changes in subscription.Subscription.WithCancellation(_cts.Token).ConfigureAwait(false))
            {
                foreach (var change in changes)
                {
                    switch (change.ChangeType)
                    {
                        case ResourceViewModelChangeType.Delete:
                            await RemoveResourceAsync(change.Resource.Name).ConfigureAwait(false);
                            break;
                        case ResourceViewModelChangeType.Upsert:
                            await UpdateResourceAsync(change.Resource, _cts.Token).ConfigureAwait(false);
                            break;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error watching resources for MCP endpoints.");
        }
    }

    private async Task UpdateResourceAsync(ResourceViewModel resource, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryGetMcpEndpoints(resource, out var endpoints))
        {
            await RemoveResourceAsync(resource.Name).ConfigureAwait(false);
            return;
        }

        var newRegistration = new ResourceRegistration(resource.Name);

        foreach (var endpoint in endpoints)
        {
            try
            {
                var registration = await CreateClientAsync(resource, endpoint, cancellationToken).ConfigureAwait(false);
                if (registration is not null)
                {
                    newRegistration.Merge(registration);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to connect MCP proxy for resource {ResourceName} at {Endpoint}.", resource.Name, endpoint.Uri);
            }
        }

        // Replace existing registration for the resource.
        if (_registrations.TryRemove(resource.Name, out var existing))
        {
            foreach (var toolName in existing.ToolNames)
            {
                _toolIndex.TryRemove(toolName, out _);
            }

            await existing.DisposeAsync().ConfigureAwait(false);
        }

        foreach (var tool in newRegistration.Tools)
        {
            _toolIndex[tool.Name] = tool;
        }

        _registrations[resource.Name] = newRegistration;
    }

    private async Task<ResourceRegistration?> CreateClientAsync(ResourceViewModel resource, McpEndpointExport endpoint, CancellationToken cancellationToken)
    {
        // Only http transport is supported for now.
        if (!string.Equals(endpoint.Transport, "http", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(endpoint.Transport, "https", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Skipping MCP endpoint {Endpoint} for resource {ResourceName} because transport '{Transport}' is not supported.", endpoint.Uri, resource.Name, endpoint.Transport);
            return null;
        }

        var options = new ModelContextProtocol.Client.HttpClientTransportOptions
        {
            Endpoint = endpoint.Uri,
            Name = $"{resource.Name}-mcp",
            TransportMode = ModelContextProtocol.Client.HttpTransportMode.AutoDetect
        };

        if (!string.IsNullOrEmpty(endpoint.AuthToken))
        {
            options.AdditionalHeaders = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {endpoint.AuthToken}"
            };
        }

        var transport = new ModelContextProtocol.Client.HttpClientTransport(options, _loggerFactory);

        var client = await McpClient.CreateAsync(
            transport,
            new McpClientOptions(),
            _loggerFactory,
            cancellationToken).ConfigureAwait(false);

        var remoteTools = await client.ListToolsAsync(_serializerOptions, cancellationToken).ConfigureAwait(false);

        var registration = new ResourceRegistration(resource.Name);
        foreach (var remoteTool in remoteTools)
        {
            var proxiedTool = CreateProxiedTool(resource, endpoint, remoteTool);
            if (proxiedTool is null)
            {
                continue;
            }

            var toolMapping = new ProxiedTool(proxiedTool.Name, remoteTool.ProtocolTool.Name, proxiedTool, client);
            registration.Tools.Add(toolMapping);
        }

        if (registration.Tools.Count == 0)
        {
            await client.DisposeAsync().ConfigureAwait(false);
            return null;
        }

        registration.Clients.Add(client);

        return registration;
    }

    private static Tool? CreateProxiedTool(ResourceViewModel resource, McpEndpointExport endpoint, McpClientTool remoteTool)
    {
        var tool = remoteTool.ProtocolTool;
        if (string.IsNullOrWhiteSpace(tool.Name))
        {
            return null;
        }

        var namespacePrefix = endpoint.Namespace ?? resource.Name;
        var proxiedName = $"{resource.Name}/{namespacePrefix}/{tool.Name}";

        return new Tool
        {
            Name = proxiedName,
            Title = string.IsNullOrWhiteSpace(tool.Title) ? $"{tool.Name} ({resource.DisplayName})" : $"{tool.Title} ({resource.DisplayName})",
            Description = string.IsNullOrWhiteSpace(tool.Description)
                ? $"Proxy for resource '{resource.DisplayName}' via MCP."
                : $"{tool.Description} (resource: {resource.DisplayName})",
            InputSchema = tool.InputSchema,
            OutputSchema = tool.OutputSchema,
            Annotations = tool.Annotations,
            Icons = tool.Icons,
            Meta = tool.Meta
        };
    }

    private async Task RemoveResourceAsync(string resourceName)
    {
        if (_registrations.TryRemove(resourceName, out var existing))
        {
            foreach (var toolName in existing.ToolNames)
            {
                _toolIndex.TryRemove(toolName, out _);
            }

            await existing.DisposeAsync().ConfigureAwait(false);
        }
    }

    private bool TryGetMcpEndpoints(ResourceViewModel resource, out List<McpEndpointExport> endpoints)
    {
        endpoints = [];

        _logger.LogDebug("Checking resource {ResourceName} for MCP endpoints. Properties: {Properties}",
            resource.Name,
            string.Join(", ", resource.Properties.Select(p => p.Key)));

        if (!resource.Properties.TryGetValue(KnownProperties.Resource.McpEndpoints, out var property))
        {
            _logger.LogDebug("Resource {ResourceName} does not have MCP endpoints property.", resource.Name);
            return false;
        }

        if (!property.Value.TryConvertToString(out var json) || string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<List<McpEndpointExport>>(json, _serializerOptions);
            if (parsed is not null && parsed.Count > 0)
            {
                endpoints = parsed;
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse MCP endpoints for resource {ResourceName}.", resource.Name);
        }

        return false;
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();

        if (_watchTask is not null)
        {
            try
            {
                await _watchTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        foreach (var registration in _registrations.Values)
        {
            await registration.DisposeAsync().ConfigureAwait(false);
        }
    }

    private sealed record McpEndpointExport
    {
        public required Uri Uri { get; init; }
        public required string Transport { get; init; }
        public string? AuthToken { get; init; }
        public string? Namespace { get; init; }
    }

    private sealed class ResourceRegistration(string resourceName) : IAsyncDisposable
    {
        public string ResourceName { get; } = resourceName;

        public List<ProxiedTool> Tools { get; } = [];

        public List<McpClient> Clients { get; } = [];

        public IEnumerable<string> ToolNames => Tools.Select(t => t.Name);

        public void Merge(ResourceRegistration other)
        {
            Tools.AddRange(other.Tools);
            Clients.AddRange(other.Clients);
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var client in Clients)
            {
                await client.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private sealed record ProxiedTool(string Name, string RemoteName, Tool Tool, McpClient Client);
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Cli.Backchannel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Aspire.Cli.Mcp;

/// <summary>
/// Service responsible for refreshing resource-based MCP tools and sending tool list change notifications.
/// </summary>
internal sealed class McpResourceToolRefreshService : IMcpResourceToolRefreshService
{
    private readonly IAuxiliaryBackchannelMonitor _auxiliaryBackchannelMonitor;
    private readonly ILogger _logger;
    private readonly object _lock = new();
    private McpServer? _server;
    private Dictionary<string, ResourceToolEntry> _resourceToolMap = new(StringComparer.Ordinal);
    private bool _invalidated = true;
    private string? _selectedAppHostPath;

    public McpResourceToolRefreshService(
        IAuxiliaryBackchannelMonitor auxiliaryBackchannelMonitor,
        ILogger<McpResourceToolRefreshService> logger)
    {
        _auxiliaryBackchannelMonitor = auxiliaryBackchannelMonitor;
        _logger = logger;
    }

    /// <inheritdoc/>
    public bool TryGetResourceToolMap(out IReadOnlyDictionary<string, ResourceToolEntry> resourceToolMap)
    {
        lock (_lock)
        {
            if (_invalidated || _selectedAppHostPath != _auxiliaryBackchannelMonitor.SelectedAppHostPath)
            {
                resourceToolMap = null!;
                return false;
            }

            resourceToolMap = _resourceToolMap;
            return true;
        }
    }

    /// <inheritdoc/>
    public void InvalidateToolMap()
    {
        lock (_lock)
        {
            _resourceToolMap.Clear();
            _invalidated = true;
        }
    }

    /// <inheritdoc/>
    public void SetMcpServer(McpServer? server)
    {
        _server = server;
    }

    /// <inheritdoc/>
    public async Task SendToolsListChangedNotificationAsync(CancellationToken cancellationToken)
    {
        if (_server is { } server)
        {
            await server.SendNotificationAsync(NotificationMethods.ToolListChangedNotification, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<string, ResourceToolEntry>> RefreshResourceToolMapAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Refreshing resource tool map.");

        var refreshedMap = new Dictionary<string, ResourceToolEntry>(StringComparer.Ordinal);

        string? selectedAppHostPath = null;
        try
        {
            var connection = await AppHostConnectionHelper.GetSelectedConnectionAsync(_auxiliaryBackchannelMonitor, _logger, cancellationToken).ConfigureAwait(false);

            if (connection is not null)
            {
                selectedAppHostPath = connection.AppHostInfo?.AppHostPath;

                var allResources = await connection.GetResourceSnapshotsAsync(cancellationToken).ConfigureAwait(false);
                var resourcesWithTools = allResources.Where(r => r.McpServer is not null).ToList();

                _logger.LogDebug("Resources with MCP tools received: {Count}", resourcesWithTools.Count);

                foreach (var resource in resourcesWithTools)
                {
                    Debug.Assert(resource.McpServer is not null);

                    foreach (var tool in resource.McpServer.Tools)
                    {
                        var exposedName = $"{resource.Name.Replace("-", "_")}_{tool.Name}";
                        refreshedMap[exposedName] = new ResourceToolEntry(resource.Name, tool);

                        _logger.LogDebug("{Tool}: {Description}", exposedName, tool.Description);
                    }
                }
            }
            else
            {
                _logger.LogDebug("Unable to refresh resource tool map because there's no selected connection.");
            }
        }
        catch (Exception ex)
        {
            // Don't fail refresh_tools if resource discovery fails; still emit notification.
            _logger.LogDebug(ex, "Failed to refresh resource MCP tool routing map.");
        }

        lock (_lock)
        {
            _resourceToolMap = refreshedMap;
            _selectedAppHostPath = selectedAppHostPath;
            _invalidated = false;
            return _resourceToolMap;
        }
    }
}

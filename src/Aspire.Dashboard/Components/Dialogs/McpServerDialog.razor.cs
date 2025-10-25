// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Mcp;
using Aspire.Dashboard.Model.Markdown;
using Aspire.Dashboard.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Dialogs;

public partial class McpServerDialog
{
    [CascadingParameter]
    public FluentDialog Dialog { get; set; } = default!;

    [Inject]
    public required IStringLocalizer<ControlsStrings> ControlsStringsLoc { get; init; }

    [Inject]
    public required IStringLocalizer<Resources.Dialogs> Loc { get; init; }

    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    [Inject]
    public required IOptions<DashboardOptions> DashboardOptions { get; init; }

    private MarkdownProcessor _markdownProcessor = default!;
    private string? _mcpServerInstallButtonJson;
    private string? _mcpServerConfigFileJson;
    private string? _mcpUrl;
    private bool _isHttps;
    private McpToolView _activeView;
    private List<McpConfigPropertyViewModel> _mcpConfigProperties = [];

    protected override void OnInitialized()
    {
        _markdownProcessor = new MarkdownProcessor(ControlsStringsLoc, MarkdownHelpers.SafeUrlSchemes, []);
        if ((DashboardOptions.Value.Mcp.PublicUrl ?? DashboardOptions.Value.Mcp.EndpointUrl) is { Length: > 0 } mcpUrl)
        {
            var uri = new Uri(baseUri: new Uri(mcpUrl), relativeUri: "/mcp");

            _mcpUrl = uri.ToString();
            _isHttps = uri.Scheme == "https";
        }

        if (McpEnabled)
        {
            (_mcpServerInstallButtonJson, _mcpServerConfigFileJson) = GetMcpServerInstallButtonJson();
            _mcpConfigProperties =
            [
                new McpConfigPropertyViewModel { Name = "name", Value = "aspire-dashboard" },
                new McpConfigPropertyViewModel { Name = "type", Value = "http" },
                new McpConfigPropertyViewModel { Name = "url", Value = _mcpUrl }
            ];

            if (DashboardOptions.Value.Mcp.AuthMode == McpAuthMode.ApiKey)
            {
                _mcpConfigProperties.Add(new McpConfigPropertyViewModel { Name = $"{McpApiKeyAuthenticationHandler.ApiKeyHeaderName} (header)", Value = DashboardOptions.Value.Mcp.PrimaryApiKey! });
            }
        }
        else
        {
            throw new InvalidOperationException("MCP server is not enabled or configured.");
        }
    }

    [MemberNotNullWhen(true, nameof(_mcpServerInstallButtonJson))]
    [MemberNotNullWhen(true, nameof(_mcpUrl))]
    private bool McpEnabled => !DashboardOptions.Value.Mcp.Disabled.GetValueOrDefault() && !string.IsNullOrEmpty(_mcpUrl);

    private (string InstallButtonJson, string ConfigFileJson) GetMcpServerInstallButtonJson()
    {
        Debug.Assert(_mcpUrl != null);

        Dictionary<string, string>? headers = null;

        if (DashboardOptions.Value.Mcp.AuthMode == McpAuthMode.ApiKey)
        {
            headers = new Dictionary<string, string>
            {
                [McpApiKeyAuthenticationHandler.ApiKeyHeaderName] = DashboardOptions.Value.Mcp.PrimaryApiKey!
            };
        }

        var name = "aspire-dashboard";

        var installButtonJson = JsonSerializer.Serialize(
            new McpInstallButtonServerModel
            {
                Name = name,
                Type = "http",
                Url = _mcpUrl,
                Headers = headers
            },
            McpInstallButtonModelContext.Default.McpInstallButtonServerModel);

        var configFileJson = JsonSerializer.Serialize(
            new McpJsonFileServerModel
            {
                Servers = new()
                {
                    [name] = new()
                    {
                        Type = "http",
                        Url = _mcpUrl,
                        Headers = headers
                    }
                }
            },
            McpConfigFileModelContext.Default.McpJsonFileServerModel);

        return (installButtonJson, configFileJson);
    }

    private Task OnTabChangeAsync(FluentTab newTab)
    {
        var id = newTab.Id?.Substring("tab-".Length);

        if (id is null
            || !Enum.TryParse(typeof(McpToolView), id, out var o)
            || o is not McpToolView viewKind)
        {
            return Task.CompletedTask;
        }

        _activeView = viewKind;
        return Task.CompletedTask;
    }

    private string GetJsonConfigurationMarkdown() =>
        $"""
        ```json
        {_mcpServerConfigFileJson}
        ```
        """;

    public enum McpToolView
    {
        VisualStudio,
        VSCode,
        Other
    }
}

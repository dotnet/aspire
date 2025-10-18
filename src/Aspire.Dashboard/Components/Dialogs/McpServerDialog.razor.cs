// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    private string? _mcpServerJson;
    private string? _mcpUrl;

    protected override void OnInitialized()
    {
        _markdownProcessor = new MarkdownProcessor(ControlsStringsLoc, MarkdownHelpers.SafeUrlSchemes, []);
        _mcpUrl = DashboardOptions.Value.Mcp.PublicUrl ?? DashboardOptions.Value.Mcp.EndpointUrl;

        if (McpEnabled)
        {
            _mcpServerJson = GetMcpServerJson();
        }
    }

    [MemberNotNullWhen(true, nameof(_mcpServerJson))]
    [MemberNotNullWhen(true, nameof(_mcpUrl))]
    private bool McpEnabled => !string.IsNullOrEmpty(_mcpUrl);

    private string GetMcpServerJson()
    {
        Dictionary<string, string>? headers = null;

        if (DashboardOptions.Value.Mcp.AuthMode == McpAuthMode.ApiKey)
        {
            headers = new Dictionary<string, string>
            {
                [McpApiKeyAuthenticationHandler.ApiKeyHeaderName] = DashboardOptions.Value.Mcp.PrimaryApiKey!
            };
        }

        var url = new Uri(baseUri: new Uri(_mcpUrl!), relativeUri: "/mcp").ToString();

        return JsonSerializer.Serialize(
            new McpServerModel
            {
                Name = "aspire-dashboard",
                Type = "http",
                Url = url,
                Headers = headers
            },
            McpServerModelContext.Default.McpServerModel);
    }

    private string GetJsonConfigurationMarkdown() =>
        $"""
        ```json
        {_mcpServerJson}
        ```
        """;
}

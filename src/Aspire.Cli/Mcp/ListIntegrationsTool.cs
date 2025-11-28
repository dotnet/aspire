// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using Aspire.Cli.Packaging;
using ModelContextProtocol.Protocol;
using Semver;

namespace Aspire.Cli.Mcp;

/// <summary>
/// Represents an Aspire hosting integration package.
/// </summary>
internal sealed class Integration
{
    /// <summary>
    /// Gets or sets the friendly name of the integration (e.g., "Redis", "PostgreSQL").
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the NuGet package ID.
    /// </summary>
    [JsonPropertyName("packageId")]
    public required string PackageId { get; set; }

    /// <summary>
    /// Gets or sets the package version.
    /// </summary>
    [JsonPropertyName("version")]
    public required string Version { get; set; }
}

/// <summary>
/// Represents the response from the list_integrations tool.
/// </summary>
internal sealed class ListIntegrationsResponse
{
    /// <summary>
    /// Gets or sets the list of available integrations.
    /// </summary>
    [JsonPropertyName("integrations")]
    public required List<Integration> Integrations { get; set; }
}

/// <summary>
/// MCP tool for listing available Aspire hosting integrations.
/// </summary>
internal sealed class ListIntegrationsTool(IPackagingService packagingService, CliExecutionContext executionContext) : CliMcpTool
{
    public override string Name => "list_integrations";

    public override string Description => "List available Aspire hosting integrations. These are NuGet packages that can be added to an Aspire AppHost project to integrate with various services like databases, message brokers, and cloud services. This tool does not require a running AppHost.";

    public override JsonElement GetInputSchema()
    {
        return JsonDocument.Parse("""
            {
              "type": "object",
              "properties": {},
              "description": "No input parameters required. Returns a list of available Aspire hosting integrations."
            }
            """).RootElement;
    }

    public override async ValueTask<CallToolResult> CallToolAsync(ModelContextProtocol.Client.McpClient mcpClient, IReadOnlyDictionary<string, JsonElement>? arguments, CancellationToken cancellationToken)
    {
        // This tool does not use the MCP client as it operates locally
        _ = mcpClient;
        _ = arguments;

        try
        {
            var allPackages = new List<(string FriendlyName, string PackageId, string Version, string Channel)>();

            var packageChannels = await packagingService.GetChannelsAsync(cancellationToken);

            foreach (var channel in packageChannels)
            {
                var integrationPackages = await channel.GetIntegrationPackagesAsync(executionContext.WorkingDirectory, cancellationToken);

                foreach (var package in integrationPackages)
                {
                    // Extract friendly name from package ID (e.g., "Aspire.Hosting.Redis" -> "Redis")
                    var friendlyName = GetFriendlyName(package.Id);
                    allPackages.Add((friendlyName, package.Id, package.Version, channel.Name));
                }
            }

            // Group by package ID and take the latest version using semantic version comparison
            // Parse version once and include it in the result to avoid redundant parsing
            var packagesWithParsedVersions = allPackages
                .Select(p => (p.FriendlyName, p.PackageId, p.Version, p.Channel, ParsedVersion: SemVersion.TryParse(p.Version, SemVersionStyles.Any, out var v) ? v : null))
                .Where(p => p.ParsedVersion is not null)
                .ToList();

            var distinctPackages = packagesWithParsedVersions
                .GroupBy(p => p.PackageId)
                .Select(g => g.OrderByDescending(p => p.ParsedVersion!, SemVersion.PrecedenceComparer).First())
                .OrderBy(p => p.FriendlyName)
                .ToList();

            var integrations = distinctPackages.Select(p => new Integration
            {
                Name = p.FriendlyName,
                PackageId = p.PackageId,
                Version = p.Version
            }).ToList();

            var response = new ListIntegrationsResponse
            {
                Integrations = integrations
            };

            var jsonContent = JsonSerializer.Serialize(response, JsonSourceGenerationContext.Default.ListIntegrationsResponse);

            return new CallToolResult
            {
                Content = [new TextContentBlock { Text = jsonContent }]
            };
        }
        catch (Exception ex)
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = $"Failed to list integrations: {ex.Message}" }]
            };
        }
    }

    private static string GetFriendlyName(string packageId)
    {
        // Handle CommunityToolkit packages
        if (packageId.StartsWith("CommunityToolkit.Aspire.Hosting.", StringComparison.Ordinal))
        {
            return packageId["CommunityToolkit.Aspire.Hosting.".Length..];
        }

        // Handle Aspire.Hosting packages
        if (packageId.StartsWith("Aspire.Hosting.", StringComparison.Ordinal))
        {
            return packageId["Aspire.Hosting.".Length..];
        }

        return packageId;
    }
}

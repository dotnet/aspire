// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Cli.Mcp;

namespace Aspire.Cli.Tests.Mcp;

public class ListIntegrationsToolTests
{
    [Fact]
    public void ListIntegrationsTool_HasCorrectName()
    {
        var tool = new ListIntegrationsTool(new MockPackagingService(), TestExecutionContextFactory.CreateTestContext());

        Assert.Equal("list_integrations", tool.Name);
    }

    [Fact]
    public void ListIntegrationsTool_HasCorrectDescription()
    {
        var tool = new ListIntegrationsTool(new MockPackagingService(), TestExecutionContextFactory.CreateTestContext());

        Assert.Contains("List available Aspire hosting integrations", tool.Description);
        Assert.Contains("This tool does not require a running AppHost", tool.Description);
    }

    [Fact]
    public void ListIntegrationsTool_GetInputSchema_ReturnsValidSchema()
    {
        var tool = new ListIntegrationsTool(new MockPackagingService(), TestExecutionContextFactory.CreateTestContext());
        var schema = tool.GetInputSchema();

        Assert.Equal(JsonValueKind.Object, schema.ValueKind);
        Assert.True(schema.TryGetProperty("type", out var typeElement));
        Assert.Equal("object", typeElement.GetString());
        Assert.True(schema.TryGetProperty("properties", out var propsElement));
        Assert.Equal(JsonValueKind.Object, propsElement.ValueKind);
    }

    [Fact]
    public async Task ListIntegrationsTool_CallToolAsync_ReturnsNoPackagesMessage_WhenNoPackagesFound()
    {
        var mockPackagingService = new MockPackagingService();
        var tool = new ListIntegrationsTool(mockPackagingService, TestExecutionContextFactory.CreateTestContext());

        var result = await tool.CallToolAsync(null!, null, CancellationToken.None);

        Assert.True(result.IsError is null or false);
        Assert.NotNull(result.Content);
        Assert.Single(result.Content);
        var textContent = result.Content[0] as ModelContextProtocol.Protocol.TextContentBlock;
        Assert.NotNull(textContent);
        Assert.Contains("No Aspire hosting integrations found", textContent.Text);
    }

    [Fact]
    public async Task ListIntegrationsTool_CallToolAsync_ReturnsPackageList_WhenPackagesFound()
    {
        var mockPackagingService = new MockPackagingService(new[]
        {
            new Aspire.Shared.NuGetPackageCli { Id = "Aspire.Hosting.Redis", Version = "9.0.0" },
            new Aspire.Shared.NuGetPackageCli { Id = "Aspire.Hosting.PostgreSQL", Version = "9.0.0" }
        });
        var tool = new ListIntegrationsTool(mockPackagingService, TestExecutionContextFactory.CreateTestContext());

        var result = await tool.CallToolAsync(null!, null, CancellationToken.None);

        Assert.True(result.IsError is null or false);
        Assert.NotNull(result.Content);
        Assert.Single(result.Content);
        var textContent = result.Content[0] as ModelContextProtocol.Protocol.TextContentBlock;
        Assert.NotNull(textContent);
        Assert.Contains("Found 2 Aspire hosting integrations", textContent.Text);
        Assert.Contains("Redis", textContent.Text);
        Assert.Contains("PostgreSQL", textContent.Text);
    }
}

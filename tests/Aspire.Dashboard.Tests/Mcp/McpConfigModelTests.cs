// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Dashboard.Mcp;
using Xunit;

namespace Aspire.Dashboard.Tests.Mcp;

public class McpConfigModelTests
{
    [Fact]
    public void McpJsonFileServerModel_WithApiKey_IncludesInputs()
    {
        // Arrange
        var inputs = new List<McpInputModel>
        {
            new McpInputModel
            {
                Id = "x_mcp_api_key",
                Type = "promptString",
                Description = "Enter x-mcp-api-key",
                Password = true
            }
        };

        var model = new McpJsonFileServerModel
        {
            Inputs = inputs,
            Servers = new()
            {
                ["aspire-dashboard"] = new()
                {
                    Type = "http",
                    Url = "http://localhost:23052/mcp",
                    Headers = new Dictionary<string, string>
                    {
                        ["x-mcp-api-key"] = "${input:x_mcp_api_key}"
                    }
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(model, McpConfigFileModelContext.Default.McpJsonFileServerModel);

        // Assert
        Assert.Contains("\"inputs\"", json);
        Assert.Contains("\"x_mcp_api_key\"", json);
        Assert.Contains("\"promptString\"", json);
        Assert.Contains("\"Enter x-mcp-api-key\"", json);
        Assert.Contains("\"password\": true", json);
        Assert.Contains("${input:x_mcp_api_key}", json);
    }

    [Fact]
    public void McpJsonFileServerModel_WithoutApiKey_NoInputs()
    {
        // Arrange
        var model = new McpJsonFileServerModel
        {
            Inputs = null,
            Servers = new()
            {
                ["aspire-dashboard"] = new()
                {
                    Type = "http",
                    Url = "http://localhost:23052/mcp",
                    Headers = null
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(model, McpConfigFileModelContext.Default.McpJsonFileServerModel);

        // Assert
        Assert.DoesNotContain("\"inputs\"", json);
    }

    [Fact]
    public void McpInstallButtonServerModel_WithApiKey_IncludesInputs()
    {
        // Arrange
        var inputs = new List<McpInputModel>
        {
            new McpInputModel
            {
                Id = "x_mcp_api_key",
                Type = "promptString",
                Description = "Enter x-mcp-api-key",
                Password = true
            }
        };

        var model = new McpInstallButtonServerModel
        {
            Name = "aspire-dashboard",
            Inputs = inputs,
            Type = "http",
            Url = "http://localhost:23052/mcp",
            Headers = new Dictionary<string, string>
            {
                ["x-mcp-api-key"] = "${input:x_mcp_api_key}"
            }
        };

        // Act
        var json = JsonSerializer.Serialize(model, McpInstallButtonModelContext.Default.McpInstallButtonServerModel);

        // Assert
        Assert.Contains("\"inputs\"", json);
        Assert.Contains("\"x_mcp_api_key\"", json);
        Assert.Contains("\"promptString\"", json);
        Assert.Contains("\"Enter x-mcp-api-key\"", json);
        Assert.Contains("\"password\":true", json);
        Assert.Contains("${input:x_mcp_api_key}", json);
        Assert.Contains("\"name\":\"aspire-dashboard\"", json);
    }

    [Fact]
    public void McpInstallButtonServerModel_WithoutApiKey_NoInputs()
    {
        // Arrange
        var model = new McpInstallButtonServerModel
        {
            Name = "aspire-dashboard",
            Inputs = null,
            Type = "http",
            Url = "http://localhost:23052/mcp",
            Headers = null
        };

        // Act
        var json = JsonSerializer.Serialize(model, McpInstallButtonModelContext.Default.McpInstallButtonServerModel);

        // Assert
        Assert.DoesNotContain("\"inputs\"", json);
    }

    [Fact]
    public void McpInputModel_SerializesCorrectly()
    {
        // Arrange
        var input = new McpInputModel
        {
            Id = "x_mcp_api_key",
            Type = "promptString",
            Description = "Enter x-mcp-api-key",
            Password = true
        };

        // Act
        var json = JsonSerializer.Serialize(input, McpInstallButtonModelContext.Default.McpInputModel);

        // Assert
        Assert.Contains("\"id\":\"x_mcp_api_key\"", json);
        Assert.Contains("\"type\":\"promptString\"", json);
        Assert.Contains("\"description\":\"Enter x-mcp-api-key\"", json);
        Assert.Contains("\"password\":true", json);
    }
}

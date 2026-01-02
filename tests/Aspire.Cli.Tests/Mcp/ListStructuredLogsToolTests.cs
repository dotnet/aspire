// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Cli.Mcp;

namespace Aspire.Cli.Tests.Mcp;

public class ListStructuredLogsToolTests
{
    [Fact]
    public void ListStructuredLogsTool_HasCorrectName()
    {
        var tool = new ListStructuredLogsTool();

        Assert.Equal("list_structured_logs", tool.Name);
    }

    [Fact]
    public void ListStructuredLogsTool_HasCorrectDescription()
    {
        var tool = new ListStructuredLogsTool();

        Assert.Contains("structured logs", tool.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ListStructuredLogsTool_GetInputSchema_ReturnsValidSchema()
    {
        var tool = new ListStructuredLogsTool();
        var schema = tool.GetInputSchema();

        Assert.Equal(JsonValueKind.Object, schema.ValueKind);
        Assert.True(schema.TryGetProperty("type", out var typeElement));
        Assert.Equal("object", typeElement.GetString());
        Assert.True(schema.TryGetProperty("properties", out var propsElement));
        Assert.Equal(JsonValueKind.Object, propsElement.ValueKind);
    }

    [Fact]
    public void ListStructuredLogsTool_GetInputSchema_HasResourceNameProperty()
    {
        var tool = new ListStructuredLogsTool();
        var schema = tool.GetInputSchema();

        Assert.True(schema.TryGetProperty("properties", out var propsElement));
        Assert.True(propsElement.TryGetProperty("resourceName", out var resourceNameProperty));
        Assert.True(resourceNameProperty.TryGetProperty("type", out var resourceNameType));
        Assert.Equal("string", resourceNameType.GetString());
        Assert.True(resourceNameProperty.TryGetProperty("description", out var resourceNameDesc));
        Assert.Contains("resource", resourceNameDesc.GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ListStructuredLogsTool_GetInputSchema_HasFiltersProperty()
    {
        var tool = new ListStructuredLogsTool();
        var schema = tool.GetInputSchema();

        Assert.True(schema.TryGetProperty("properties", out var propsElement));
        Assert.True(propsElement.TryGetProperty("filters", out var filtersProperty));
        Assert.True(filtersProperty.TryGetProperty("type", out var filtersType));
        Assert.Equal("string", filtersType.GetString());
        Assert.True(filtersProperty.TryGetProperty("description", out var filtersDesc));
        Assert.Contains("filter", filtersDesc.GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("field", filtersDesc.GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("condition", filtersDesc.GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ListStructuredLogsTool_GetInputSchema_HasSeverityProperty()
    {
        var tool = new ListStructuredLogsTool();
        var schema = tool.GetInputSchema();

        Assert.True(schema.TryGetProperty("properties", out var propsElement));
        Assert.True(propsElement.TryGetProperty("severity", out var severityProperty));
        Assert.True(severityProperty.TryGetProperty("type", out var severityType));
        Assert.Equal("string", severityType.GetString());
        Assert.True(severityProperty.TryGetProperty("description", out var severityDesc));
        Assert.Contains("severity", severityDesc.GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Trace", severityDesc.GetString(), StringComparison.Ordinal);
        Assert.Contains("Debug", severityDesc.GetString(), StringComparison.Ordinal);
        Assert.Contains("Information", severityDesc.GetString(), StringComparison.Ordinal);
        Assert.Contains("Warning", severityDesc.GetString(), StringComparison.Ordinal);
        Assert.Contains("Error", severityDesc.GetString(), StringComparison.Ordinal);
        Assert.Contains("Critical", severityDesc.GetString(), StringComparison.Ordinal);
    }
}

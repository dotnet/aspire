// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Cli.Mcp;

namespace Aspire.Cli.Tests.Mcp;

public class ListMetricsToolTests
{
    [Fact]
    public void ListMetricsTool_HasCorrectName()
    {
        var tool = new ListMetricsTool();

        Assert.Equal("list_metrics", tool.Name);
    }

    [Fact]
    public void ListMetricsTool_HasCorrectDescription()
    {
        var tool = new ListMetricsTool();

        Assert.Contains("metrics", tool.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("instruments", tool.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("resource", tool.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ListMetricsTool_GetInputSchema_ReturnsValidSchema()
    {
        var tool = new ListMetricsTool();
        var schema = tool.GetInputSchema();

        Assert.Equal(JsonValueKind.Object, schema.ValueKind);
        Assert.True(schema.TryGetProperty("type", out var typeElement));
        Assert.Equal("object", typeElement.GetString());
        Assert.True(schema.TryGetProperty("properties", out var propsElement));
        Assert.Equal(JsonValueKind.Object, propsElement.ValueKind);
    }

    [Fact]
    public void ListMetricsTool_GetInputSchema_HasResourceNameProperty()
    {
        var tool = new ListMetricsTool();
        var schema = tool.GetInputSchema();

        Assert.True(schema.TryGetProperty("properties", out var propsElement));
        Assert.True(propsElement.TryGetProperty("resourceName", out var resourceNameProperty));
        Assert.True(resourceNameProperty.TryGetProperty("type", out var resourceNameType));
        Assert.Equal("string", resourceNameType.GetString());
        Assert.True(resourceNameProperty.TryGetProperty("description", out var resourceNameDesc));
        Assert.Contains("resource", resourceNameDesc.GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ListMetricsTool_GetInputSchema_ResourceNameIsRequired()
    {
        var tool = new ListMetricsTool();
        var schema = tool.GetInputSchema();

        Assert.True(schema.TryGetProperty("required", out var requiredElement));
        Assert.Equal(JsonValueKind.Array, requiredElement.ValueKind);

        var requiredFields = requiredElement.EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("resourceName", requiredFields);
    }
}

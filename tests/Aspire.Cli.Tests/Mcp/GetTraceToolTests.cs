// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Cli.Mcp;

namespace Aspire.Cli.Tests.Mcp;

public class GetTraceToolTests
{
    [Fact]
    public void GetTraceTool_HasCorrectName()
    {
        var tool = new GetTraceTool();

        Assert.Equal("get_trace", tool.Name);
    }

    [Fact]
    public void GetTraceTool_HasCorrectDescription()
    {
        var tool = new GetTraceTool();

        Assert.Contains("distributed trace", tool.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("spans", tool.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("by its ID", tool.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetTraceTool_GetInputSchema_ReturnsValidSchema()
    {
        var tool = new GetTraceTool();
        var schema = tool.GetInputSchema();

        Assert.Equal(JsonValueKind.Object, schema.ValueKind);
        Assert.True(schema.TryGetProperty("type", out var typeElement));
        Assert.Equal("object", typeElement.GetString());
        Assert.True(schema.TryGetProperty("properties", out var propsElement));
        Assert.Equal(JsonValueKind.Object, propsElement.ValueKind);
    }

    [Fact]
    public void GetTraceTool_GetInputSchema_HasTraceIdProperty()
    {
        var tool = new GetTraceTool();
        var schema = tool.GetInputSchema();

        Assert.True(schema.TryGetProperty("properties", out var propsElement));
        Assert.True(propsElement.TryGetProperty("traceId", out var traceIdProperty));
        Assert.True(traceIdProperty.TryGetProperty("type", out var traceIdType));
        Assert.Equal("string", traceIdType.GetString());
        Assert.True(traceIdProperty.TryGetProperty("description", out var traceIdDesc));
        Assert.Contains("trace", traceIdDesc.GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetTraceTool_GetInputSchema_TraceIdIsRequired()
    {
        var tool = new GetTraceTool();
        var schema = tool.GetInputSchema();

        Assert.True(schema.TryGetProperty("required", out var requiredElement));
        Assert.Equal(JsonValueKind.Array, requiredElement.ValueKind);

        var requiredFields = requiredElement.EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("traceId", requiredFields);
    }

    [Fact]
    public void GetTraceTool_GetInputSchema_HasNoOtherRequiredFields()
    {
        var tool = new GetTraceTool();
        var schema = tool.GetInputSchema();

        Assert.True(schema.TryGetProperty("required", out var requiredElement));
        var requiredFields = requiredElement.EnumerateArray().Select(e => e.GetString()).ToList();

        // Only traceId should be required
        Assert.Single(requiredFields);
        Assert.Equal("traceId", requiredFields[0]);
    }
}

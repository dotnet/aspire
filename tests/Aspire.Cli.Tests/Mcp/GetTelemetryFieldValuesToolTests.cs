// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Cli.Mcp;

namespace Aspire.Cli.Tests.Mcp;

public class GetTelemetryFieldValuesToolTests
{
    [Fact]
    public void GetTelemetryFieldValuesTool_HasCorrectName()
    {
        var tool = new GetTelemetryFieldValuesTool();

        Assert.Equal("get_telemetry_field_values", tool.Name);
    }

    [Fact]
    public void GetTelemetryFieldValuesTool_HasCorrectDescription()
    {
        var tool = new GetTelemetryFieldValuesTool();

        Assert.Contains("distinct values", tool.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("field", tool.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("list_telemetry_fields", tool.Description);
    }

    [Fact]
    public void GetTelemetryFieldValuesTool_GetInputSchema_ReturnsValidSchema()
    {
        var tool = new GetTelemetryFieldValuesTool();
        var schema = tool.GetInputSchema();

        Assert.Equal(JsonValueKind.Object, schema.ValueKind);
        Assert.True(schema.TryGetProperty("type", out var typeElement));
        Assert.Equal("object", typeElement.GetString());
        Assert.True(schema.TryGetProperty("properties", out var propsElement));
        Assert.Equal(JsonValueKind.Object, propsElement.ValueKind);
    }

    [Fact]
    public void GetTelemetryFieldValuesTool_GetInputSchema_HasFieldNameProperty()
    {
        var tool = new GetTelemetryFieldValuesTool();
        var schema = tool.GetInputSchema();

        Assert.True(schema.TryGetProperty("properties", out var propsElement));
        Assert.True(propsElement.TryGetProperty("fieldName", out var fieldNameProperty));
        Assert.True(fieldNameProperty.TryGetProperty("type", out var fieldNameType));
        Assert.Equal("string", fieldNameType.GetString());
        Assert.True(fieldNameProperty.TryGetProperty("description", out var fieldNameDesc));
        Assert.Contains("field name", fieldNameDesc.GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetTelemetryFieldValuesTool_GetInputSchema_HasTypeProperty()
    {
        var tool = new GetTelemetryFieldValuesTool();
        var schema = tool.GetInputSchema();

        Assert.True(schema.TryGetProperty("properties", out var propsElement));
        Assert.True(propsElement.TryGetProperty("type", out var typeProperty));
        Assert.True(typeProperty.TryGetProperty("type", out var typeType));
        Assert.Equal("string", typeType.GetString());
        Assert.True(typeProperty.TryGetProperty("description", out var typeDesc));
        Assert.Contains("traces", typeDesc.GetString());
        Assert.Contains("logs", typeDesc.GetString());
    }

    [Fact]
    public void GetTelemetryFieldValuesTool_GetInputSchema_HasResourceNameProperty()
    {
        var tool = new GetTelemetryFieldValuesTool();
        var schema = tool.GetInputSchema();

        Assert.True(schema.TryGetProperty("properties", out var propsElement));
        Assert.True(propsElement.TryGetProperty("resourceName", out var resourceNameProperty));
        Assert.True(resourceNameProperty.TryGetProperty("type", out var resourceNameType));
        Assert.Equal("string", resourceNameType.GetString());
        Assert.True(resourceNameProperty.TryGetProperty("description", out var resourceNameDesc));
        Assert.Contains("resource", resourceNameDesc.GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetTelemetryFieldValuesTool_GetInputSchema_HasFieldNameAsRequired()
    {
        var tool = new GetTelemetryFieldValuesTool();
        var schema = tool.GetInputSchema();

        Assert.True(schema.TryGetProperty("required", out var requiredElement));
        Assert.Equal(JsonValueKind.Array, requiredElement.ValueKind);
        var requiredArray = requiredElement.EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("fieldName", requiredArray);
    }
}

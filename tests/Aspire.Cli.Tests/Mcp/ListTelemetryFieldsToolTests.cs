// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Cli.Mcp;

namespace Aspire.Cli.Tests.Mcp;

public class ListTelemetryFieldsToolTests
{
    [Fact]
    public void ListTelemetryFieldsTool_HasCorrectName()
    {
        var tool = new ListTelemetryFieldsTool();

        Assert.Equal("list_telemetry_fields", tool.Name);
    }

    [Fact]
    public void ListTelemetryFieldsTool_HasCorrectDescription()
    {
        var tool = new ListTelemetryFieldsTool();

        Assert.Contains("telemetry fields", tool.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("filtering traces and logs", tool.Description);
    }

    [Fact]
    public void ListTelemetryFieldsTool_GetInputSchema_ReturnsValidSchema()
    {
        var tool = new ListTelemetryFieldsTool();
        var schema = tool.GetInputSchema();

        Assert.Equal(JsonValueKind.Object, schema.ValueKind);
        Assert.True(schema.TryGetProperty("type", out var typeElement));
        Assert.Equal("object", typeElement.GetString());
        Assert.True(schema.TryGetProperty("properties", out var propsElement));
        Assert.Equal(JsonValueKind.Object, propsElement.ValueKind);
    }

    [Fact]
    public void ListTelemetryFieldsTool_GetInputSchema_HasTypeProperty()
    {
        var tool = new ListTelemetryFieldsTool();
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
    public void ListTelemetryFieldsTool_GetInputSchema_HasResourceNameProperty()
    {
        var tool = new ListTelemetryFieldsTool();
        var schema = tool.GetInputSchema();

        Assert.True(schema.TryGetProperty("properties", out var propsElement));
        Assert.True(propsElement.TryGetProperty("resourceName", out var resourceNameProperty));
        Assert.True(resourceNameProperty.TryGetProperty("type", out var resourceNameType));
        Assert.Equal("string", resourceNameType.GetString());
        Assert.True(resourceNameProperty.TryGetProperty("description", out var resourceNameDesc));
        Assert.Contains("resource", resourceNameDesc.GetString(), StringComparison.OrdinalIgnoreCase);
    }
}

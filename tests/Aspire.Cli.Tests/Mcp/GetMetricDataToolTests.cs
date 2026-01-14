// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Cli.Mcp;

namespace Aspire.Cli.Tests.Mcp;

public class GetMetricDataToolTests
{
    [Fact]
    public void GetMetricDataTool_HasCorrectName()
    {
        var tool = new GetMetricDataTool();

        Assert.Equal("get_metric_data", tool.Name);
    }

    [Fact]
    public void GetMetricDataTool_HasCorrectDescription()
    {
        var tool = new GetMetricDataTool();

        Assert.Contains("metric data", tool.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("instrument", tool.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dimensions", tool.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetMetricDataTool_GetInputSchema_ReturnsValidSchema()
    {
        var tool = new GetMetricDataTool();
        var schema = tool.GetInputSchema();

        Assert.Equal(JsonValueKind.Object, schema.ValueKind);
        Assert.True(schema.TryGetProperty("type", out var typeElement));
        Assert.Equal("object", typeElement.GetString());
        Assert.True(schema.TryGetProperty("properties", out var propsElement));
        Assert.Equal(JsonValueKind.Object, propsElement.ValueKind);
    }

    [Fact]
    public void GetMetricDataTool_GetInputSchema_HasResourceNameProperty()
    {
        var tool = new GetMetricDataTool();
        var schema = tool.GetInputSchema();

        Assert.True(schema.TryGetProperty("properties", out var propsElement));
        Assert.True(propsElement.TryGetProperty("resourceName", out var resourceNameProperty));
        Assert.True(resourceNameProperty.TryGetProperty("type", out var resourceNameType));
        Assert.Equal("string", resourceNameType.GetString());
        Assert.True(resourceNameProperty.TryGetProperty("description", out var resourceNameDesc));
        Assert.Contains("resource", resourceNameDesc.GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetMetricDataTool_GetInputSchema_HasMeterNameProperty()
    {
        var tool = new GetMetricDataTool();
        var schema = tool.GetInputSchema();

        Assert.True(schema.TryGetProperty("properties", out var propsElement));
        Assert.True(propsElement.TryGetProperty("meterName", out var meterNameProperty));
        Assert.True(meterNameProperty.TryGetProperty("type", out var meterNameType));
        Assert.Equal("string", meterNameType.GetString());
        Assert.True(meterNameProperty.TryGetProperty("description", out var meterNameDesc));
        Assert.Contains("meter", meterNameDesc.GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetMetricDataTool_GetInputSchema_HasInstrumentNameProperty()
    {
        var tool = new GetMetricDataTool();
        var schema = tool.GetInputSchema();

        Assert.True(schema.TryGetProperty("properties", out var propsElement));
        Assert.True(propsElement.TryGetProperty("instrumentName", out var instrumentNameProperty));
        Assert.True(instrumentNameProperty.TryGetProperty("type", out var instrumentNameType));
        Assert.Equal("string", instrumentNameType.GetString());
        Assert.True(instrumentNameProperty.TryGetProperty("description", out var instrumentNameDesc));
        Assert.Contains("instrument", instrumentNameDesc.GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetMetricDataTool_GetInputSchema_HasDurationProperty()
    {
        var tool = new GetMetricDataTool();
        var schema = tool.GetInputSchema();

        Assert.True(schema.TryGetProperty("properties", out var propsElement));
        Assert.True(propsElement.TryGetProperty("duration", out var durationProperty));
        Assert.True(durationProperty.TryGetProperty("type", out var durationType));
        Assert.Equal("string", durationType.GetString());
        Assert.True(durationProperty.TryGetProperty("description", out var durationDesc));
        var descString = durationDesc.GetString();
        Assert.Contains("5m", descString);
        Assert.Contains("1h", descString);
    }

    [Fact]
    public void GetMetricDataTool_GetInputSchema_RequiredFieldsAreCorrect()
    {
        var tool = new GetMetricDataTool();
        var schema = tool.GetInputSchema();

        Assert.True(schema.TryGetProperty("required", out var requiredElement));
        Assert.Equal(JsonValueKind.Array, requiredElement.ValueKind);

        var requiredFields = requiredElement.EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("resourceName", requiredFields);
        Assert.Contains("meterName", requiredFields);
        Assert.Contains("instrumentName", requiredFields);
        Assert.DoesNotContain("duration", requiredFields); // duration is optional
    }
}

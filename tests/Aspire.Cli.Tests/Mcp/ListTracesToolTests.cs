// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Cli.Mcp;

namespace Aspire.Cli.Tests.Mcp;

public class ListTracesToolTests
{
    [Fact]
    public void ListTracesTool_HasCorrectName()
    {
        var tool = new ListTracesTool();

        Assert.Equal("list_traces", tool.Name);
    }

    [Fact]
    public void ListTracesTool_HasCorrectDescription()
    {
        var tool = new ListTracesTool();

        Assert.Contains("distributed traces", tool.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ListTracesTool_GetInputSchema_ReturnsValidSchema()
    {
        var tool = new ListTracesTool();
        var schema = tool.GetInputSchema();

        Assert.Equal(JsonValueKind.Object, schema.ValueKind);
        Assert.True(schema.TryGetProperty("type", out var typeElement));
        Assert.Equal("object", typeElement.GetString());
        Assert.True(schema.TryGetProperty("properties", out var propsElement));
        Assert.Equal(JsonValueKind.Object, propsElement.ValueKind);
    }

    [Fact]
    public void ListTracesTool_GetInputSchema_HasResourceNameProperty()
    {
        var tool = new ListTracesTool();
        var schema = tool.GetInputSchema();

        Assert.True(schema.TryGetProperty("properties", out var propsElement));
        Assert.True(propsElement.TryGetProperty("resourceName", out var resourceNameProperty));
        Assert.True(resourceNameProperty.TryGetProperty("type", out var resourceNameType));
        Assert.Equal("string", resourceNameType.GetString());
        Assert.True(resourceNameProperty.TryGetProperty("description", out var resourceNameDesc));
        Assert.Contains("resource", resourceNameDesc.GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ListTracesTool_GetInputSchema_HasFiltersProperty()
    {
        var tool = new ListTracesTool();
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
    public void ListTracesTool_GetInputSchema_HasSearchTextProperty()
    {
        var tool = new ListTracesTool();
        var schema = tool.GetInputSchema();

        Assert.True(schema.TryGetProperty("properties", out var propsElement));
        Assert.True(propsElement.TryGetProperty("searchText", out var searchTextProperty));
        Assert.True(searchTextProperty.TryGetProperty("type", out var searchTextType));
        Assert.Equal("string", searchTextType.GetString());
        Assert.True(searchTextProperty.TryGetProperty("description", out var searchTextDesc));
        Assert.Contains("span names", searchTextDesc.GetString(), StringComparison.OrdinalIgnoreCase);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Cli.Mcp;

namespace Aspire.Cli.Tests.Mcp;

public class GetAspireDocsToolTests
{
    [Fact]
    public void GetAspireDocsTool_HasCorrectName()
    {
        var tool = new GetAspireDocsTool();

        Assert.Equal("get_aspire_docs", tool.Name);
    }

    [Fact]
    public void GetAspireDocsTool_HasCorrectDescription()
    {
        var tool = new GetAspireDocsTool();

        Assert.Contains("aspire.dev/llms.txt", tool.Description);
        Assert.Contains("This tool does not require a running AppHost", tool.Description);
    }

    [Fact]
    public void GetAspireDocsTool_GetInputSchema_ReturnsValidSchema()
    {
        var tool = new GetAspireDocsTool();
        var schema = tool.GetInputSchema();

        Assert.Equal(JsonValueKind.Object, schema.ValueKind);
        Assert.True(schema.TryGetProperty("type", out var typeElement));
        Assert.Equal("object", typeElement.GetString());
        Assert.True(schema.TryGetProperty("properties", out var propsElement));
        Assert.Equal(JsonValueKind.Object, propsElement.ValueKind);
    }
}

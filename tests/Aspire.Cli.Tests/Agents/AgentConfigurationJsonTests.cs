// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Cli.Agents;
using Microsoft.AspNetCore.InternalTesting;

namespace Aspire.Cli.Tests.Agents;

public class AgentConfigurationJsonTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ParseObject_FileAndInlineInputsShareJsoncAndBomHandling(bool bom)
    {
        using var context = new AgentConfigurationTestContext(output);
        const string json = """{/* preserved */"nested":{"value":true,},"items":["one",],}""";
        var bytes = Encoding.UTF8.GetBytes(json);
        if (bom)
        {
            bytes = Encoding.UTF8.GetPreamble().Concat(bytes).ToArray();
        }

        var path = Path.Combine(context.Project.FullName, "settings.json");
        await File.WriteAllBytesAsync(path, bytes).DefaultTimeout();

        var inline = AgentConfigurationJson.ParseObject(bytes);
        var file = await AgentConfigurationJson.ReadAsync(path, CancellationToken.None).DefaultTimeout();

        Assert.True(JsonNode.DeepEquals(inline, file));
        Assert.True((bool)inline["nested"]!["value"]!);
        Assert.Equal(["one"], inline["items"]!.AsArray().Select(value => (string)value!));
        Assert.Equal(bytes, await File.ReadAllBytesAsync(path).DefaultTimeout());
    }

    [Theory]
    [InlineData("{broken")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("""{"duplicate":1,"duplicate":2}""")]
    [InlineData("""{"nested":{"duplicate":1,"duplicate":2}}""")]
    [InlineData("""{"items":[{"duplicate":1,"duplicate":2}]}""")]
    [InlineData("""{"skills":{"urls":[],"urls":[]}}""")]
    public async Task ParseObject_FileAndInlineInputsRejectAmbiguousObjectsAtTheBoundary(string json)
    {
        using var context = new AgentConfigurationTestContext(output);
        var bytes = Encoding.UTF8.GetBytes(json);
        var path = Path.Combine(context.Project.FullName, "settings.json");
        await File.WriteAllBytesAsync(path, bytes).DefaultTimeout();

        Exception inline = json switch
        {
            "null" => Assert.Throws<ArgumentNullException>(() => AgentConfigurationJson.ParseObject(bytes)),
            "[]" => Assert.Throws<InvalidOperationException>(() => AgentConfigurationJson.ParseObject(bytes)),
            _ => Assert.ThrowsAny<JsonException>(() => AgentConfigurationJson.ParseObject(bytes))
        };
        var file = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            AgentConfigurationJson.ReadAsync(path, CancellationToken.None)).DefaultTimeout();

        Assert.Equal($"{path}: {inline.Message}", file.Message);
        Assert.Equal(inline.GetType(), file.InnerException?.GetType());
        Assert.Equal(bytes, await File.ReadAllBytesAsync(path).DefaultTimeout());
    }
}

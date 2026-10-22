// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Cli.Agents;
using Microsoft.AspNetCore.InternalTesting;

namespace Aspire.Cli.Tests.Agents;

public class AgentConfigurationTests(ITestOutputHelper output)
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

        var inline = AgentConfiguration.ParseObject(bytes);
        var file = await AgentConfiguration.ReadObjectAsync(path, CancellationToken.None).DefaultTimeout();
        var edit = new AgentConfiguration();
        var tracked = await edit.ReadObjectForEditAsync(path, CancellationToken.None).DefaultTimeout();

        Assert.True(JsonNode.DeepEquals(inline, file));
        Assert.True(JsonNode.DeepEquals(inline, tracked));
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
            "null" => Assert.Throws<ArgumentNullException>(() => AgentConfiguration.ParseObject(bytes)),
            "[]" => Assert.Throws<InvalidOperationException>(() => AgentConfiguration.ParseObject(bytes)),
            _ => Assert.ThrowsAny<JsonException>(() => AgentConfiguration.ParseObject(bytes))
        };
        var file = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            AgentConfiguration.ReadObjectAsync(path, CancellationToken.None)).DefaultTimeout();
        var edit = new AgentConfiguration();
        var tracked = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            edit.ReadObjectForEditAsync(path, CancellationToken.None)).DefaultTimeout();

        Assert.Equal($"{path}: {inline.Message}", file.Message);
        Assert.Equal(inline.GetType(), file.InnerException?.GetType());
        Assert.Equal(file.Message, tracked.Message);
        Assert.Equal(inline.GetType(), tracked.InnerException?.GetType());
        Assert.Equal(bytes, await File.ReadAllBytesAsync(path).DefaultTimeout());
    }

    [Fact]
    public async Task ReadOperations_DoNotCreateMissingDirectories()
    {
        using var context = new AgentConfigurationTestContext(output);
        var directory = Path.Combine(context.Project.FullName, "missing");
        var path = Path.Combine(directory, "settings.json");
        var edit = new AgentConfiguration();

        Assert.Null(await AgentConfiguration.ReadObjectAsync(path, CancellationToken.None).DefaultTimeout());
        Assert.Empty(AgentConfiguration.EnumerateJsonFiles(directory));
        Assert.Null(await edit.ReadBytesForEditAsync(path, CancellationToken.None).DefaultTimeout());
        Assert.Empty(edit.GetJsonFilesForEdit(directory));

        Assert.False(Directory.Exists(directory));
        Assert.Empty(Directory.GetFiles(context.Project.FullName, "*", SearchOption.AllDirectories));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Publish_RequiresDestinationCapturedBySameEdit(bool existed)
    {
        using var context = new AgentConfigurationTestContext(output);
        var path = Path.Combine(context.Project.FullName, "settings.json");
        if (existed)
        {
            await File.WriteAllTextAsync(path, "{}");
        }
        var otherEdit = new AgentConfiguration();
        await otherEdit.ReadBytesForEditAsync(path, CancellationToken.None).DefaultTimeout();
        var edit = new AgentConfiguration();

        await Assert.ThrowsAsync<IOException>(() =>
            edit.PublishAsync(path, new JsonObject(), CancellationToken.None)).DefaultTimeout();

        Assert.Equal(existed ? [path] : [], Directory.GetFiles(context.Project.FullName));
        if (existed)
        {
            Assert.Equal("{}", await File.ReadAllTextAsync(path).DefaultTimeout());
        }
    }

    [Fact]
    public async Task ReadOnlyDiscovery_DoesNotReplaceCapturedInputs()
    {
        using var context = new AgentConfigurationTestContext(output);
        var path = Path.Combine(context.Project.FullName, "settings.json");
        await File.WriteAllTextAsync(path, """{"value":"original"}""");
        var edit = new AgentConfiguration();
        await edit.ReadObjectForEditAsync(path, CancellationToken.None).DefaultTimeout();
        const string changed = """{"value":"external"}""";
        await File.WriteAllTextAsync(path, changed);

        var discovered = await AgentConfiguration.ReadObjectAsync(path, CancellationToken.None).DefaultTimeout();
        var captured = await edit.ReadObjectForEditAsync(path, CancellationToken.None).DefaultTimeout();

        Assert.Equal("external", discovered!["value"]!.GetValue<string>());
        Assert.Equal("original", captured!["value"]!.GetValue<string>());
        await Assert.ThrowsAsync<IOException>(() =>
            edit.PublishAsync(path, new JsonObject(), CancellationToken.None)).DefaultTimeout();
        Assert.Equal(changed, await File.ReadAllTextAsync(path).DefaultTimeout());
        Assert.Equal([path], Directory.GetFiles(context.Project.FullName));
    }
}

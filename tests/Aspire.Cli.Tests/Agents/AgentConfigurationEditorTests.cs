// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using Aspire.Cli.Agents;
using Aspire.Cli.Tests.TestServices;

namespace Aspire.Cli.Tests.Agents;

public class AgentConfigurationEditorTests(ITestOutputHelper output) : IDisposable
{
    private readonly AgentConfigurationTestContext _context = new(output);

    [Theory]
    [InlineData("utf8")]
    [InlineData("utf8-bom")]
    [InlineData("utf16")]
    [InlineData("utf16-be")]
    [InlineData("utf32")]
    [InlineData("utf32-be")]
    public async Task McpEdit_EncodedPinnedConfiguration_PreservesBytesAndTimestamp(string encodingName)
    {
        Encoding encoding = encodingName switch
        {
            "utf8" => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            "utf8-bom" => new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
            "utf16" => Encoding.Unicode,
            "utf16-be" => Encoding.BigEndianUnicode,
            "utf32" => Encoding.UTF32,
            "utf32-be" => new UTF32Encoding(bigEndian: true, byteOrderMark: true),
            _ => throw new ArgumentOutOfRangeException(nameof(encodingName))
        };
        var path = Path.Combine(_context.Project.FullName, ".mcp.json");
        const string original = "{\"preference\":\"caf\u00e9\",\"mcpServers\":{\"aspire\":{\"command\":\"pinned-aspire\"}}}";
        byte[] bytes = [.. encoding.GetPreamble(), .. encoding.GetBytes(original)];
        await File.WriteAllBytesAsync(path, bytes);
        File.SetLastWriteTimeUtc(path, new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var timestamp = File.GetLastWriteTimeUtc(path);
        var edit = McpConfigFileHelper.CreateEdit(
            _context.Copilot, path, "mcpServers", configureServer: true, AgentConfigurationScope.Project);

        var result = Assert.Single(await AgentConfigurationEditor.ApplyAsync([edit], CancellationToken.None));

        Assert.Equal(AgentConfigurationStatus.Unchanged, result.Status);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(path));
        Assert.Equal(timestamp, File.GetLastWriteTimeUtc(path));
        Assert.Equal([(false, false)], result.Changes.Select(change => (change.Edit.Repaired, change.Edit.Added)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task McpAliases_ValidateEveryClientsParserBeforeApplyingChanges(bool strictFirst)
    {
        var path = Path.Combine(_context.Project.FullName, ".mcp.json");
        const string original = """{/* comment */"preference":true,}""";
        await File.WriteAllTextAsync(path, original);
        var clients = strictFirst ? new[] { _context.ClaudeCode, _context.Copilot } : [_context.Copilot, _context.ClaudeCode];
        var edits = clients.Select(client => McpConfigFileHelper.CreateEdit(
            client, path, "mcpServers", configureServer: true, AgentConfigurationScope.Project)).ToArray();

        var result = Assert.Single(await AgentConfigurationEditor.ApplyAsync(edits, CancellationToken.None));

        Assert.Equal(AgentConfigurationStatus.Failed, result.Status);
        Assert.IsType<JsonException>(Assert.IsType<InvalidOperationException>(result.Error).InnerException, exactMatch: false);
        Assert.Equal(clients, result.Environments);
        Assert.Empty(result.Changes);
        Assert.Equal(original, await File.ReadAllTextAsync(path));
        Assert.Equal([path], Directory.EnumerateFiles(_context.Project.FullName));
    }

    [Fact]
    public async Task McpEdit_FailedPublication_DoesNotReportSuccessfulChanges()
    {
        var path = Path.Combine(_context.Project.FullName, ".mcp.json");
        const string original = """{"preference":"preserved"}""";
        await File.WriteAllTextAsync(path, original);
        var edit = McpConfigFileHelper.CreateEdit(
            _context.Copilot, path, "mcpServers", configureServer: true, AgentConfigurationScope.Project);
        var failingEdit = edit with
        {
            ApplyAsync = async (root, reader, token) =>
            {
                var applied = await edit.ApplyAsync(root, reader, token);
                root["failure"] = CallbackJsonConverter.CreateValue(() => throw new IOException("MCP publication failed"));

                return applied;
            }
        };

        var result = Assert.Single(await AgentConfigurationEditor.ApplyAsync([failingEdit], CancellationToken.None));

        Assert.Equal(AgentConfigurationStatus.Failed, result.Status);
        Assert.Equal("MCP publication failed", Assert.IsType<IOException>(result.Error).Message);
        Assert.Empty(result.Changes);
    }

    public void Dispose() => _context.Dispose();
}

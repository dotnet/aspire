// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Dashboard.Model.GenAI;
using Xunit;

namespace Aspire.Dashboard.Tests.Model;

public sealed class GenAIMessageParsingHelperTests
{
    [Fact]
    public void DeserializeArrayIncrementally_CompleteArray_ReturnsFalseForTruncated()
    {
        var json = """[{"type":"text","content":"Hello"},{"type":"text","content":"World"}]""";

        var (items, truncated) = GenAIMessageParsingHelper.DeserializeArrayIncrementally(json, GenAIMessageParsingHelper.ReadMessageParts);

        Assert.False(truncated);
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public void DeserializeArrayIncrementally_TruncatedAfterFirstElement_ReturnsTrueForTruncated()
    {
        // JSON is cut off mid-way through the second element.
        var json = """[{"type":"text","content":"Hello"},{"type":"text","con""";

        var (items, truncated) = GenAIMessageParsingHelper.DeserializeArrayIncrementally(json, GenAIMessageParsingHelper.ReadMessageParts);

        Assert.True(truncated);
        Assert.Single(items);
        var parts = items[0];
        var textPart = Assert.IsType<TextPart>(Assert.Single(parts));
        Assert.Equal("Hello", textPart.Content);
    }

    [Fact]
    public void DeserializeArrayIncrementally_TruncatedMidFirstElement_ReturnsTrueAndEmptyList()
    {
        // JSON is cut off in the middle of the first element.
        var json = """[{"type":"text","con""";

        var (items, truncated) = GenAIMessageParsingHelper.DeserializeArrayIncrementally(json, GenAIMessageParsingHelper.ReadMessageParts);

        Assert.True(truncated);
        Assert.Empty(items);
    }

    [Fact]
    public void DeserializeArrayIncrementally_TruncatedAfterArrayStart_ReturnsTrueAndEmptyList()
    {
        var json = """[""";

        var (items, truncated) = GenAIMessageParsingHelper.DeserializeArrayIncrementally(json, GenAIMessageParsingHelper.ReadMessageParts);

        Assert.True(truncated);
        Assert.Empty(items);
    }

    [Fact]
    public void DeserializeArrayIncrementally_TruncatedAfterComma_ReturnsTrueWithParsedItems()
    {
        // JSON has a trailing comma but no next element.
        var json = """[{"type":"text","content":"Hello"},""";

        var (items, truncated) = GenAIMessageParsingHelper.DeserializeArrayIncrementally(json, GenAIMessageParsingHelper.ReadMessageParts);

        Assert.True(truncated);
        Assert.Single(items);
    }

    [Fact]
    public void ReadChatMessage_TruncatedParts_ReturnsPartsTruncatedTrue()
    {
        // First message completes, second message has truncated parts.
        var json = """[{"role":"system","parts":[{"type":"text","content":"OK"}]},{"role":"user","parts":[{"type":"text","content":"Hello"},{"type":"text","con""";

        var (chatMessages, truncated) = GenAIMessageParsingHelper.DeserializeArrayIncrementally(json, GenAIMessageParsingHelper.ReadChatMessage);

        // The outer array is also truncated because the second message never closes.
        Assert.True(truncated);

        // The first message was fully parsed.
        Assert.Single(chatMessages);
        var (role, parts, partsTruncated) = chatMessages[0];
        Assert.Equal("system", role);
        Assert.False(partsTruncated);
        Assert.Single(parts);
    }

    [Fact]
    public void ReadChatMessage_CompleteParts_ReturnsPartsTruncatedFalse()
    {
        var json = """[{"role":"assistant","parts":[{"type":"text","content":"Hi there"}]}]""";

        var (chatMessages, truncated) = GenAIMessageParsingHelper.DeserializeArrayIncrementally(json, GenAIMessageParsingHelper.ReadChatMessage);

        Assert.False(truncated);
        Assert.Single(chatMessages);

        var (role, parts, partsTruncated) = chatMessages[0];
        Assert.Equal("assistant", role);
        Assert.False(partsTruncated);
        Assert.Single(parts);
        var textPart = Assert.IsType<TextPart>(parts[0]);
        Assert.Equal("Hi there", textPart.Content);
    }

    [Fact]
    public void ReadMessageParts_TextPartWithContentArray_ReturnsMultipleParts()
    {
        var json = """
            [{"type":"text","content":[{"type":"text","text":"part one"},{"type":"text","text":"part two"}]}]
            """;

        var (items, truncated) = GenAIMessageParsingHelper.DeserializeArrayIncrementally(json, GenAIMessageParsingHelper.ReadMessageParts);

        Assert.False(truncated);
        Assert.Single(items);

        var parts = items[0];
        Assert.Equal(2, parts.Count);

        var part1 = Assert.IsType<TextPart>(parts[0]);
        Assert.Equal("part one", part1.Content);

        var part2 = Assert.IsType<TextPart>(parts[1]);
        Assert.Equal("part two", part2.Content);
    }

    [Fact]
    public void ReadMessageParts_TextPartWithContentArray_ToolUsePart()
    {
        var json = """
            [{"type":"text","content":[{"type":"tool_use","id":"call_1","name":"get_weather","input":{"city":"Seattle"}}]}]
            """;

        var (items, truncated) = GenAIMessageParsingHelper.DeserializeArrayIncrementally(json, GenAIMessageParsingHelper.ReadMessageParts);

        Assert.False(truncated);
        Assert.Single(items);

        var parts = items[0];
        var toolPart = Assert.IsType<ToolCallRequestPart>(Assert.Single(parts));
        Assert.Equal("call_1", toolPart.Id);
        Assert.Equal("get_weather", toolPart.Name);
        Assert.NotNull(toolPart.Arguments);
        Assert.Equal("Seattle", toolPart.Arguments["city"]!.GetValue<string>());
    }

    [Fact]
    public void ReadMessageParts_TextPartWithContentArray_ToolResultPart()
    {
        var json = """
            [{"type":"text","content":[{"type":"tool_result","tool_use_id":"call_1","content":"72°F"}]}]
            """;

        var (items, truncated) = GenAIMessageParsingHelper.DeserializeArrayIncrementally(json, GenAIMessageParsingHelper.ReadMessageParts);

        Assert.False(truncated);
        Assert.Single(items);

        var parts = items[0];
        var toolResponse = Assert.IsType<ToolCallResponsePart>(Assert.Single(parts));
        Assert.Equal("call_1", toolResponse.Id);
        Assert.NotNull(toolResponse.Response);
    }

    [Fact]
    public void ReadMessageParts_TextPartWithContentArray_MixedPartTypes()
    {
        var json = """
            [{"type":"text","content":[{"type":"text","text":"Hello"},{"type":"tool_use","id":"t1","name":"search","input":{}},{"type":"unknown_type","data":123}]}]
            """;

        var (items, truncated) = GenAIMessageParsingHelper.DeserializeArrayIncrementally(json, GenAIMessageParsingHelper.ReadMessageParts);

        Assert.False(truncated);
        Assert.Single(items);

        var parts = items[0];
        Assert.Equal(3, parts.Count);
        Assert.IsType<TextPart>(parts[0]);
        Assert.IsType<ToolCallRequestPart>(parts[1]);
        Assert.IsType<GenericPart>(parts[2]);
    }

    [Fact]
    public void ReadMessageParts_TextPartWithStringContent_ReturnsSingleTextPart()
    {
        var json = """[{"type":"text","content":"simple string"}]""";

        var (items, truncated) = GenAIMessageParsingHelper.DeserializeArrayIncrementally(json, GenAIMessageParsingHelper.ReadMessageParts);

        Assert.False(truncated);
        Assert.Single(items);

        var parts = items[0];
        var textPart = Assert.IsType<TextPart>(Assert.Single(parts));
        Assert.Equal("simple string", textPart.Content);
    }

    [Fact]
    public void ReadMessageParts_TextPartWithEmptyContentArray_ReturnsEmptyParts()
    {
        var json = """[{"type":"text","content":[]}]""";

        var (items, truncated) = GenAIMessageParsingHelper.DeserializeArrayIncrementally(json, GenAIMessageParsingHelper.ReadMessageParts);

        Assert.False(truncated);
        Assert.Single(items);

        var parts = items[0];
        Assert.Empty(parts);
    }

    [Fact]
    public void ReadToolResultPart_ContentIsTextArray_JoinsTextIntoResponse()
    {
        var json = """
            [{"type":"text","content":[{"type":"tool_result","tool_use_id":"call_1","content":[{"type":"text","text":"Hello "},{"type":"text","text":"World"}]}]}]
            """;

        var (items, truncated) = GenAIMessageParsingHelper.DeserializeArrayIncrementally(json, GenAIMessageParsingHelper.ReadMessageParts);

        Assert.False(truncated);
        Assert.Single(items);

        var parts = items[0];
        var toolResponse = Assert.IsType<ToolCallResponsePart>(Assert.Single(parts));
        Assert.Equal("call_1", toolResponse.Id);
        Assert.NotNull(toolResponse.Response);
        Assert.Equal("Hello World", toolResponse.Response.GetValue<string>());
    }

    [Fact]
    public void ReadToolResultPart_ContentIsMixedArray_FallsBackToRawJson()
    {
        var json = """
            [{"type":"text","content":[{"type":"tool_result","tool_use_id":"call_2","content":[{"type":"text","text":"Hi"},{"type":"image","url":"pic.png"}]}]}]
            """;

        var (items, truncated) = GenAIMessageParsingHelper.DeserializeArrayIncrementally(json, GenAIMessageParsingHelper.ReadMessageParts);

        Assert.False(truncated);
        Assert.Single(items);

        var parts = items[0];
        var toolResponse = Assert.IsType<ToolCallResponsePart>(Assert.Single(parts));
        Assert.Equal("call_2", toolResponse.Id);
        Assert.NotNull(toolResponse.Response);
        // Since the array contains a non-text item, it falls back to raw JSON.
        Assert.Equal(JsonValueKind.Array, toolResponse.Response.GetValueKind());
    }
}

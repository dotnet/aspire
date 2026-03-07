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

    [Fact]
    public void ReadMessageParts_BlobPart_ParsesAllProperties()
    {
        var json = """[{"type":"blob","mime_type":"image/png","modality":"image","content":"iVBORw0KGgo="}]""";

        var (items, truncated) = GenAIMessageParsingHelper.DeserializeArrayIncrementally(json, GenAIMessageParsingHelper.ReadMessageParts);

        Assert.False(truncated);
        Assert.Single(items);

        var parts = items[0];
        var blobPart = Assert.IsType<BlobPart>(Assert.Single(parts));
        Assert.Equal("blob", blobPart.Type);
        Assert.Equal("image/png", blobPart.MimeType);
        Assert.Equal("image", blobPart.Modality);
        Assert.Equal("iVBORw0KGgo=", blobPart.Content);
    }

    [Fact]
    public void ReadMessageParts_FilePart_ParsesAllProperties()
    {
        var json = """[{"type":"file","mime_type":"application/pdf","modality":"image","file_id":"file-abc123"}]""";

        var (items, truncated) = GenAIMessageParsingHelper.DeserializeArrayIncrementally(json, GenAIMessageParsingHelper.ReadMessageParts);

        Assert.False(truncated);
        Assert.Single(items);

        var parts = items[0];
        var filePart = Assert.IsType<FilePart>(Assert.Single(parts));
        Assert.Equal("file", filePart.Type);
        Assert.Equal("application/pdf", filePart.MimeType);
        Assert.Equal("image", filePart.Modality);
        Assert.Equal("file-abc123", filePart.FileId);
    }

    [Fact]
    public void ReadMessageParts_UriPart_ParsesAllProperties()
    {
        var json = """[{"type":"uri","mime_type":"image/jpeg","modality":"image","uri":"https://example.com/photo.jpg"}]""";

        var (items, truncated) = GenAIMessageParsingHelper.DeserializeArrayIncrementally(json, GenAIMessageParsingHelper.ReadMessageParts);

        Assert.False(truncated);
        Assert.Single(items);

        var parts = items[0];
        var uriPart = Assert.IsType<UriPart>(Assert.Single(parts));
        Assert.Equal("uri", uriPart.Type);
        Assert.Equal("image/jpeg", uriPart.MimeType);
        Assert.Equal("image", uriPart.Modality);
        Assert.Equal("https://example.com/photo.jpg", uriPart.Uri);
    }

    [Fact]
    public void ReadMessageParts_ReasoningPart_ParsesContent()
    {
        var json = """[{"type":"reasoning","content":"Let me think about this step by step..."}]""";

        var (items, truncated) = GenAIMessageParsingHelper.DeserializeArrayIncrementally(json, GenAIMessageParsingHelper.ReadMessageParts);

        Assert.False(truncated);
        Assert.Single(items);

        var parts = items[0];
        var reasoningPart = Assert.IsType<ReasoningPart>(Assert.Single(parts));
        Assert.Equal("reasoning", reasoningPart.Type);
        Assert.Equal("Let me think about this step by step...", reasoningPart.Content);
    }

    [Fact]
    public void ReadMessageParts_ServerToolCallPart_ParsesAllProperties()
    {
        var json = """[{"type":"server_tool_call","id":"stc_1","name":"web_search","server_tool_call":{"type":"web_search","query":"latest news"}}]""";

        var (items, truncated) = GenAIMessageParsingHelper.DeserializeArrayIncrementally(json, GenAIMessageParsingHelper.ReadMessageParts);

        Assert.False(truncated);
        Assert.Single(items);

        var parts = items[0];
        var serverToolCallPart = Assert.IsType<ServerToolCallPart>(Assert.Single(parts));
        Assert.Equal("server_tool_call", serverToolCallPart.Type);
        Assert.Equal("stc_1", serverToolCallPart.Id);
        Assert.Equal("web_search", serverToolCallPart.Name);
        Assert.NotNull(serverToolCallPart.ServerToolCall);
        Assert.Equal("latest news", serverToolCallPart.ServerToolCall["query"]!.GetValue<string>());
    }

    [Fact]
    public void ReadMessageParts_ServerToolCallResponsePart_ParsesAllProperties()
    {
        var json = """[{"type":"server_tool_call_response","id":"stc_1","server_tool_call_response":{"type":"web_search","results":["result1","result2"]}}]""";

        var (items, truncated) = GenAIMessageParsingHelper.DeserializeArrayIncrementally(json, GenAIMessageParsingHelper.ReadMessageParts);

        Assert.False(truncated);
        Assert.Single(items);

        var parts = items[0];
        var serverToolCallResponsePart = Assert.IsType<ServerToolCallResponsePart>(Assert.Single(parts));
        Assert.Equal("server_tool_call_response", serverToolCallResponsePart.Type);
        Assert.Equal("stc_1", serverToolCallResponsePart.Id);
        Assert.NotNull(serverToolCallResponsePart.ServerToolCallResponse);
        Assert.Equal("web_search", serverToolCallResponsePart.ServerToolCallResponse["type"]!.GetValue<string>());
    }

    [Fact]
    public void ReadMessageParts_ContentArray_BlobPart_ParsesCorrectly()
    {
        var json = """
            [{"type":"text","content":[{"type":"blob","mime_type":"audio/mp3","modality":"audio","content":"AAAA"}]}]
            """;

        var (items, truncated) = GenAIMessageParsingHelper.DeserializeArrayIncrementally(json, GenAIMessageParsingHelper.ReadMessageParts);

        Assert.False(truncated);
        Assert.Single(items);

        var parts = items[0];
        var blobPart = Assert.IsType<BlobPart>(Assert.Single(parts));
        Assert.Equal("audio/mp3", blobPart.MimeType);
        Assert.Equal("audio", blobPart.Modality);
        Assert.Equal("AAAA", blobPart.Content);
    }

    [Fact]
    public void ReadMessageParts_MixedNewPartTypes_ParsesCorrectly()
    {
        var json = """
            [
                {"type":"text","content":"Hello"},
                {"type":"reasoning","content":"Thinking..."},
                {"type":"blob","mime_type":"image/png","modality":"image","content":"base64data"},
                {"type":"uri","mime_type":"image/jpeg","modality":"image","uri":"https://example.com/img.jpg"},
                {"type":"file","mime_type":"application/pdf","modality":"image","file_id":"f1"},
                {"type":"server_tool_call","id":"s1","name":"code_interpreter","server_tool_call":{"type":"code_interpreter"}},
                {"type":"server_tool_call_response","id":"s1","server_tool_call_response":{"type":"code_interpreter","output":"42"}}
            ]
            """;

        var (items, truncated) = GenAIMessageParsingHelper.DeserializeArrayIncrementally(json, GenAIMessageParsingHelper.ReadMessageParts);

        Assert.False(truncated);
        Assert.Equal(7, items.Count);

        Assert.Collection(items,
            parts => Assert.IsType<TextPart>(Assert.Single(parts)),
            parts => Assert.IsType<ReasoningPart>(Assert.Single(parts)),
            parts => Assert.IsType<BlobPart>(Assert.Single(parts)),
            parts => Assert.IsType<UriPart>(Assert.Single(parts)),
            parts => Assert.IsType<FilePart>(Assert.Single(parts)),
            parts => Assert.IsType<ServerToolCallPart>(Assert.Single(parts)),
            parts => Assert.IsType<ServerToolCallResponsePart>(Assert.Single(parts)));
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Aspire.Dashboard.Model.GenAI;

/// <summary>
/// Provides truncation-tolerant JSON array deserialization for GenAI message parsing.
/// </summary>
internal static class GenAIMessageParsingHelper
{
    internal delegate T? ReadElement<T>(ref Utf8JsonReader reader);

    internal static (List<T> items, bool truncated) DeserializeArrayIncrementally<T>(string json, ReadElement<T> readElement)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        var readerOptions = new JsonReaderOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };
        var reader = new Utf8JsonReader(bytes, readerOptions);
        return DeserializeArrayIncrementally(ref reader, readElement);
    }

    internal static (List<T> items, bool truncated) DeserializeArrayIncrementally<T>(ref Utf8JsonReader reader, ReadElement<T> readElement)
    {
        var items = new List<T>();

        // Read start of array. Let exceptions propagate for truly invalid JSON.
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Expected a JSON array.");
        }

        while (true)
        {
            bool readSuccess;
            try
            {
                readSuccess = reader.Read();
            }
            catch (JsonException)
            {
                return (items, true);
            }

            if (!readSuccess)
            {
                return (items, true);
            }

            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            try
            {
                var item = readElement(ref reader);
                if (item is not null)
                {
                    items.Add(item);
                }
            }
            catch (JsonException)
            {
                return (items, true);
            }
        }

        return (items, false);
    }

    internal static List<MessagePart> ReadMessageParts(ref Utf8JsonReader reader)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeProp))
        {
            throw new JsonException("Missing 'type' property.");
        }

        var type = typeProp.GetString();

        // For text parts with content as an array, each content item becomes a separate part.
        if (type == MessagePart.TextType &&
            root.TryGetProperty("content", out var contentProp) &&
            contentProp.ValueKind == JsonValueKind.Array)
        {
            return ReadContentArrayParts(contentProp);
        }

        // Single part for non-array cases.
        MessagePart? part = type switch
        {
            MessagePart.TextType => JsonSerializer.Deserialize(root.GetRawText(), GenAIMessagesContext.Default.TextPart),
            MessagePart.ToolCallType => JsonSerializer.Deserialize(root.GetRawText(), GenAIMessagesContext.Default.ToolCallRequestPart),
            MessagePart.ToolCallResponseType => JsonSerializer.Deserialize(root.GetRawText(), GenAIMessagesContext.Default.ToolCallResponsePart),
            MessagePart.BlobType => JsonSerializer.Deserialize(root.GetRawText(), GenAIMessagesContext.Default.BlobPart),
            MessagePart.FileType => JsonSerializer.Deserialize(root.GetRawText(), GenAIMessagesContext.Default.FilePart),
            MessagePart.UriType => JsonSerializer.Deserialize(root.GetRawText(), GenAIMessagesContext.Default.UriPart),
            MessagePart.ReasoningType => JsonSerializer.Deserialize(root.GetRawText(), GenAIMessagesContext.Default.ReasoningPart),
            MessagePart.ServerToolCallType => JsonSerializer.Deserialize(root.GetRawText(), GenAIMessagesContext.Default.ServerToolCallPart),
            MessagePart.ServerToolCallResponseType => JsonSerializer.Deserialize(root.GetRawText(), GenAIMessagesContext.Default.ServerToolCallResponsePart),
            _ => JsonSerializer.Deserialize(root.GetRawText(), GenAIMessagesContext.Default.GenericPart),
        };

        return part is not null ? [part] : [];
    }

    private static List<MessagePart> ReadContentArrayParts(JsonElement contentArray)
    {
        var parts = new List<MessagePart>();
        foreach (var item in contentArray.EnumerateArray())
        {
            var part = ReadContentItem(item);
            if (part is not null)
            {
                parts.Add(part);
            }
        }
        return parts;
    }

    private static MessagePart? ReadContentItem(JsonElement item)
    {
        var type = item.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;

        return type switch
        {
            "text" => new TextPart
            {
                Content = item.TryGetProperty("text", out var textProp) && textProp.ValueKind == JsonValueKind.String
                    ? textProp.GetString()
                    : null
            },
            "tool_use" => ReadToolUsePart(item),
            "tool_result" => ReadToolResultPart(item),
            _ => JsonSerializer.Deserialize(item.GetRawText(), GenAIMessagesContext.Default.GenericPart),
        };
    }

    private static ToolCallRequestPart ReadToolUsePart(JsonElement item)
    {
        var id = item.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String
            ? idProp.GetString()
            : null;

        var name = item.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String
            ? nameProp.GetString()
            : null;

        JsonNode? arguments = item.TryGetProperty("input", out var inputProp)
            ? JsonNode.Parse(inputProp.GetRawText())
            : null;

        return new ToolCallRequestPart { Id = id, Name = name, Arguments = arguments };
    }

    private static ToolCallResponsePart ReadToolResultPart(JsonElement item)
    {
        var id = item.TryGetProperty("tool_use_id", out var idProp) && idProp.ValueKind == JsonValueKind.String
            ? idProp.GetString()
            : null;

        JsonNode? response = item.TryGetProperty("content", out var contentProp)
            ? TryJoinTextContent(contentProp) ?? JsonNode.Parse(contentProp.GetRawText())
            : null;

        return new ToolCallResponsePart { Id = id, Response = response };
    }

    private static JsonNode? TryJoinTextContent(JsonElement contentElement)
    {
        if (contentElement.ValueKind is not JsonValueKind.Array)
        {
            return null;
        }

        var sb = new StringBuilder();
        foreach (var arrayItem in contentElement.EnumerateArray())
        {
            if (arrayItem.ValueKind is not JsonValueKind.Object
                || !arrayItem.TryGetProperty("type", out var typeProp)
                || typeProp.GetString() is not "text"
                || !arrayItem.TryGetProperty("text", out var textProp)
                || textProp.ValueKind is not JsonValueKind.String)
            {
                return null;
            }

            sb.Append(textProp.GetString());
        }

        return JsonValue.Create(sb.ToString());
    }

    internal static (string role, List<MessagePart> parts, bool partsTruncated) ReadChatMessage(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected start of chat message object.");
        }

        string? role = null;
        List<MessagePart>? parts = null;
        var partsTruncated = false;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected property name.");
            }

            var propertyName = reader.GetString();

            switch (propertyName)
            {
                case "role":
                    reader.Read();
                    role = reader.GetString();
                    break;
                case "parts":
                    // DeserializeArrayIncrementally reads the StartArray token itself.
                    (var partsResults, partsTruncated) = DeserializeArrayIncrementally<List<MessagePart>>(ref reader, ReadMessageParts);
                    parts = partsResults.SelectMany(p => p).ToList();
                    break;
                default:
                    reader.Read();
                    reader.TrySkip();
                    break;
            }
        }

        return (role ?? string.Empty, parts ?? [], partsTruncated);
    }
}

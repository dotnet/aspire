// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;

namespace Aspire.Dashboard.Model.GenAI;

/// <summary>
/// Represents text, tool calls, and generic parts.
/// </summary>
[JsonConverter(typeof(MessagePartConverter))]
public abstract class MessagePart
{
    public const string TextType = "text";
    public const string ToolCallType = "tool_call";
    public const string ToolCallResponseType = "tool_call_response";

    public string Type { get; set; } = default!;
}

/// <summary>
/// Represents text content sent to or received from the model.
/// </summary>
public class TextPart : MessagePart
{
    public TextPart()
    {
        Type = TextType;
    }

    public string? Content { get; set; } = default!;
}

/// <summary>
/// Represents a tool call requested by the model.
/// </summary>
public class ToolCallRequestPart : MessagePart
{
    public ToolCallRequestPart()
    {
        Type = ToolCallType;
    }

    public string? Id { get; set; }
    public string? Name { get; set; } = default!;
    public JsonNode? Arguments { get; set; }
}

/// <summary>
/// Represents a tool call result sent to the model.
/// </summary>
public class ToolCallResponsePart : MessagePart
{
    public ToolCallResponsePart()
    {
        Type = ToolCallResponseType;
    }

    public string? Id { get; set; }
    public JsonNode? Response { get; set; } = default!;
}

/// <summary>
/// Represents an arbitrary message part with any type and properties.
/// </summary>
public class GenericPart : MessagePart
{
    // Extensible dynamic properties
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}

/// <summary>
/// A chat message containing a role and parts.
/// </summary>
public class ChatMessage
{
    public string Role { get; set; } = default!;
    public List<MessagePart> Parts { get; set; } = new();
    // Only set on output message.
    public string? FinishReason { get; set; }
}

/// <summary>
/// Represents a tool definition that can be used by the model.
/// </summary>
[DebuggerDisplay("Type = {Type}, Name = {Name}")]
public class ToolDefinition
{
    public string Type { get; set; } = "function";
    public string? Name { get; set; }
    public string? Description { get; set; }
    public OpenApiSchema? Parameters { get; set; }
}

/// <summary>
/// Custom converter to handle polymorphic message parts.
/// </summary>
public class MessagePartConverter : JsonConverter<MessagePart>
{
    public override MessagePart? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        if (!doc.RootElement.TryGetProperty("type", out var typeProp))
        {
            throw new JsonException("Missing 'type' property.");
        }

        var type = typeProp.GetString();
        return type switch
        {
            MessagePart.TextType => JsonSerializer.Deserialize<TextPart>(doc.RootElement.GetRawText(), options),
            MessagePart.ToolCallType => JsonSerializer.Deserialize<ToolCallRequestPart>(doc.RootElement.GetRawText(), options),
            MessagePart.ToolCallResponseType => JsonSerializer.Deserialize<ToolCallResponsePart>(doc.RootElement.GetRawText(), options),
            _ => JsonSerializer.Deserialize<GenericPart>(doc.RootElement.GetRawText(), options),
        };
    }

    public override void Write(Utf8JsonWriter writer, MessagePart value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(MessagePart))]
[JsonSerializable(typeof(TextPart))]
[JsonSerializable(typeof(ToolCallRequestPart))]
[JsonSerializable(typeof(ToolCallResponsePart))]
[JsonSerializable(typeof(GenericPart))]
[JsonSerializable(typeof(ChatMessage))]
[JsonSerializable(typeof(List<ChatMessage>))]
public sealed partial class GenAIMessagesContext : JsonSerializerContext;

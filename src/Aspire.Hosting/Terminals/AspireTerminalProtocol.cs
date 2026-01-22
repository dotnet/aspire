// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aspire.Hosting.Terminals;

/// <summary>
/// Aspire Terminal Protocol (ATP) message types and serialization.
/// </summary>
internal static class AspireTerminalProtocol
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Parses an incoming ATP message from the client.
    /// </summary>
    public static AtpMessage? ParseClientMessage(ReadOnlySpan<byte> data)
    {
        try
        {
            var json = System.Text.Encoding.UTF8.GetString(data);

            // Quick check for message type
            if (json.Contains("\"type\""))
            {
                if (json.Contains("\"input\""))
                {
                    return JsonSerializer.Deserialize<AtpInputMessage>(json, s_jsonOptions);
                }
                else if (json.Contains("\"resize\""))
                {
                    return JsonSerializer.Deserialize<AtpResizeMessage>(json, s_jsonOptions);
                }
                else if (json.Contains("\"ping\""))
                {
                    return new AtpPingMessage();
                }
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Creates an output message for sending terminal data to clients.
    /// </summary>
    public static byte[] CreateOutputMessage(ReadOnlyMemory<byte> data)
    {
        var message = new AtpOutputMessage
        {
            Data = Convert.ToBase64String(data.Span)
        };
        return JsonSerializer.SerializeToUtf8Bytes(message, s_jsonOptions);
    }

    /// <summary>
    /// Creates a state message for sending full terminal state to newly connected clients.
    /// </summary>
    public static byte[] CreateStateMessage(ReadOnlyMemory<byte> state)
    {
        var message = new AtpStateMessage
        {
            Data = Convert.ToBase64String(state.Span)
        };
        return JsonSerializer.SerializeToUtf8Bytes(message, s_jsonOptions);
    }

    /// <summary>
    /// Creates a pong message in response to a ping.
    /// </summary>
    public static byte[] CreatePongMessage()
    {
        return JsonSerializer.SerializeToUtf8Bytes(new AtpPongMessage(), s_jsonOptions);
    }
}

/// <summary>
/// Base class for ATP messages.
/// </summary>
internal abstract class AtpMessage
{
    [JsonPropertyName("type")]
    public abstract string Type { get; }
}

/// <summary>
/// Client → Server: Keyboard/mouse input.
/// </summary>
internal sealed class AtpInputMessage : AtpMessage
{
    public override string Type => "input";

    /// <summary>
    /// Base64-encoded input bytes.
    /// </summary>
    [JsonPropertyName("data")]
    public string? Data { get; set; }

    /// <summary>
    /// Gets the decoded input bytes.
    /// </summary>
    public byte[] GetDecodedData() => string.IsNullOrEmpty(Data) ? [] : Convert.FromBase64String(Data);
}

/// <summary>
/// Client → Server: Terminal resize event.
/// </summary>
internal sealed class AtpResizeMessage : AtpMessage
{
    public override string Type => "resize";

    [JsonPropertyName("cols")]
    public int Cols { get; set; }

    [JsonPropertyName("rows")]
    public int Rows { get; set; }
}

/// <summary>
/// Client → Server: Keep-alive ping.
/// </summary>
internal sealed class AtpPingMessage : AtpMessage
{
    public override string Type => "ping";
}

/// <summary>
/// Server → Client: Terminal output data.
/// </summary>
internal sealed class AtpOutputMessage : AtpMessage
{
    public override string Type => "output";

    /// <summary>
    /// Base64-encoded output bytes.
    /// </summary>
    [JsonPropertyName("data")]
    public string? Data { get; set; }
}

/// <summary>
/// Server → Client: Full terminal state (for new connections).
/// </summary>
internal sealed class AtpStateMessage : AtpMessage
{
    public override string Type => "state";

    /// <summary>
    /// Base64-encoded terminal state.
    /// </summary>
    [JsonPropertyName("data")]
    public string? Data { get; set; }
}

/// <summary>
/// Server → Client: Keep-alive pong.
/// </summary>
internal sealed class AtpPongMessage : AtpMessage
{
    public override string Type => "pong";
}

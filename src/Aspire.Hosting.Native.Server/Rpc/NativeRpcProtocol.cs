// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text;

namespace Aspire.Hosting.Native.Rpc;

internal sealed record NativeRpcRequest(JsonNode? Id, string Method, JsonNode? Parameters, bool IsNotification);

/// <summary>Parses JSON-RPC envelopes and bounded trace context independently of dispatch.</summary>
internal static class NativeRpcProtocol
{
    public static void ValidateAuthenticationToken(string token)
    {
        ArgumentException.ThrowIfNullOrEmpty(token);
        if (Encoding.UTF8.GetByteCount(token) is < 32 or > 256)
        {
            throw new ArgumentException("RPC authentication tokens must contain 32 to 256 UTF-8 bytes.", nameof(token));
        }
    }

    public static NativeRpcRequest ParseRequest(ReadOnlyMemory<byte> payload)
    {
        if (payload.Length > NativeRpcConnection.MaximumRequestBytes)
        {
            throw InvalidRequest();
        }
        using var document = JsonDocument.Parse(payload, new JsonDocumentOptions { MaxDepth = 32 });

        return ParseRequest(document.RootElement);
    }

    public static NativeRpcRequest ParseRequest(JsonElement envelope)
    {
        // JSON-RPC uses {"jsonrpc":"2.0","id":1,"method":"invokeCapability",
        // "params":["assembly/capability",{"context":{"$handle":"...","$type":"..."}}]}.
        // Existing no-argument clients also send "params":null; retain that
        // representation of omitted arguments while rejecting scalar arguments.
        // Duplicate keys at any depth cannot be interpreted as last-write-wins.
        ValidateUniqueProperties(envelope);
        if (envelope.ValueKind != JsonValueKind.Object)
        {
            throw InvalidRequest();
        }
        foreach (var property in envelope.EnumerateObject())
        {
            if (property.Name is not ("jsonrpc" or "id" or "method" or "params" or "traceparent" or "tracestate"))
            {
                throw InvalidRequest();
            }
        }
        var notification = !envelope.TryGetProperty("id", out var id);
        envelope.TryGetProperty("params", out var parameters);
        envelope.TryGetProperty("traceparent", out var parent);
        envelope.TryGetProperty("tracestate", out var state);
        if (!envelope.TryGetProperty("jsonrpc", out var version) || version.ValueKind != JsonValueKind.String || !version.ValueEquals("2.0") ||
            !envelope.TryGetProperty("method", out var methodValue) || methodValue.ValueKind != JsonValueKind.String ||
            methodValue.GetString() is not { } method || string.IsNullOrWhiteSpace(method) || method.Length > 256 ||
            !IsValidId(id) ||
            parameters.ValueKind is not (JsonValueKind.Undefined or JsonValueKind.Null or JsonValueKind.Array or JsonValueKind.Object) ||
            !IsValidTraceField(parent) || !IsValidTraceField(state))
        {
            throw InvalidRequest();
        }

        return new(notification ? null : ToNode(id), method,
            parameters.ValueKind == JsonValueKind.Undefined ? null : ToNode(parameters), notification);
    }

    public static ActivityContext? ReadTraceContext(ReadOnlyMemory<byte> payload, int maximumBytes)
    {
        if (payload.Length > maximumBytes)
        {
            return null;
        }
        try
        {
            using var document = JsonDocument.Parse(payload, new JsonDocumentOptions { MaxDepth = 32 });

            return ReadTraceContext(document.RootElement);
        }
        catch (Exception exception) when (exception is JsonException or RpcFault)
        {
            // Trace propagation is preliminary. ParseRequest still rejects an
            // invalid envelope rather than turning absent context into success.
            return null;
        }
    }

    public static ActivityContext? ReadTraceContext(JsonElement envelope)
    {
        if (envelope.ValueKind != JsonValueKind.Object ||
            !envelope.TryGetProperty("traceparent", out var parent) || parent.ValueKind != JsonValueKind.String)
        {
            return null;
        }
        try
        {
            ValidateUniqueProperties(envelope);
        }
        catch (RpcFault)
        {
            return null;
        }
        string? state = null;
        if (envelope.TryGetProperty("tracestate", out var value))
        {
            if (value.ValueKind != JsonValueKind.String)
            {
                return null;
            }
            state = value.GetString();
        }
        // StreamJsonRpc sends "traceparent":"00-<trace-id>-<span-id>-01",
        // "tracestate":"vendor=value". Never attach oversized/malformed context.
        // https://github.com/microsoft/vs-streamjsonrpc/blob/main/doc/tracecontext.md
        if (parent.GetString() is not { Length: <= 512 } parentValue || state is { Length: > 512 } ||
            !ActivityContext.TryParse(parentValue, state, isRemote: true, out var context))
        {
            return null;
        }

        return context;
    }

    public static JsonNode? ToNode(JsonElement element) => element.ValueKind switch
    {
        // Nodes retain their element rather than owning its document. Clone only
        // the retained subtree so handles and replies survive envelope disposal.
        JsonValueKind.Null => null,
        JsonValueKind.Object => JsonObject.Create(element.Clone()),
        JsonValueKind.Array => JsonArray.Create(element.Clone()),
        _ => JsonValue.Create(element.Clone())
    };

    public static void ValidateUniqueProperties(JsonElement element)
    {
        // {"method":"safe","m\u0065thod":"other"} has two decoded "method" keys.
        // Reject ambiguity before different DOM/typed readers can disagree about
        // authority-bearing fields. RFC 8259 documents inconsistent duplicate handling:
        // https://www.rfc-editor.org/rfc/rfc8259#section-4
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name))
                {
                    throw InvalidRequest();
                }
                ValidateUniqueProperties(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                ValidateUniqueProperties(item);
            }
        }
    }

    private static RpcFault InvalidRequest() => new("INVALID_REQUEST", "The request shape is invalid.");
    private static bool IsValidId(JsonElement id) => id.ValueKind switch
    {
        JsonValueKind.Undefined or JsonValueKind.Null => true,
        JsonValueKind.String => id.GetString()!.Length <= 512,
        JsonValueKind.Number => id.TryGetInt64(out _),
        _ => false
    };
    private static bool IsValidTraceField(JsonElement value) => value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null ||
        value.ValueKind == JsonValueKind.String && value.GetString()!.Length <= 512;
}

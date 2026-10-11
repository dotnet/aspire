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
        // JSON-RPC uses {"jsonrpc":"2.0","id":1,"method":"invokeCapability",
        // "params":["assembly/capability",{"context":{"$handle":"...","$type":"..."}}]}.
        // Duplicate keys at any depth cannot be interpreted as last-write-wins.
        using var document = JsonDocument.Parse(payload, new JsonDocumentOptions { MaxDepth = 32 });
        ValidateUniqueProperties(document.RootElement);
        var request = JsonNode.Parse(document.RootElement.GetRawText()) as JsonObject
            ?? throw InvalidRequest();
        var id = request["id"];
        if (request["jsonrpc"]?.ToJsonString() != "\"2.0\"" ||
            request["method"] is not JsonValue methodValue || !methodValue.TryGetValue<string>(out var method) ||
            !IsValidId(id) || request.Any(property => property.Key is not ("jsonrpc" or "id" or "method" or "params" or "traceparent" or "tracestate")) ||
            request["traceparent"] is not null && (request["traceparent"] is not JsonValue traceParent ||
                !traceParent.TryGetValue<string>(out var parent) || parent.Length > 512) ||
            request["tracestate"] is not null && (request["tracestate"] is not JsonValue traceState ||
                !traceState.TryGetValue<string>(out var state) || state.Length > 512))
        {
            throw InvalidRequest();
        }

        return new(id, method, request["params"], !request.ContainsKey("id"));
    }

    public static ActivityContext? ReadTraceContext(ReadOnlyMemory<byte> payload, int maximumBytes)
    {
        if (payload.Length > maximumBytes || payload.Span.IndexOf("\"traceparent\""u8) < 0)
        {
            return null;
        }
        try
        {
            using var document = JsonDocument.Parse(payload, new JsonDocumentOptions { MaxDepth = 32 });
            ValidateUniqueProperties(document.RootElement);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("traceparent", out var parent) || parent.ValueKind != JsonValueKind.String)
            {
                return null;
            }
            var state = document.RootElement.TryGetProperty("tracestate", out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString() : null;
            // StreamJsonRpc sends "traceparent":"00-<trace-id>-<span-id>-01",
            // "tracestate":"vendor=value". Never attach oversized/malformed context.
            // https://github.com/microsoft/vs-streamjsonrpc/blob/main/doc/tracecontext.md
            if (parent.GetString() is not { Length: <= 512 } parentValue || state is { Length: > 512 } ||
                !ActivityContext.TryParse(parentValue, state, out var context))
            {
                return null;
            }

            return context;
        }
        catch (Exception exception) when (exception is JsonException or RpcFault)
        {
            // Trace propagation is preliminary. ParseRequest still rejects an
            // invalid envelope rather than turning absent context into success.
            return null;
        }
    }

    public static void ValidateUniqueProperties(JsonElement element)
    {
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
    private static bool IsValidId(JsonNode? id) => id is null ||
        id is JsonValue value && (value.TryGetValue<string>(out _) || value.TryGetValue<long>(out _));
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Agents;

/// <summary>
/// JSON shape checks and settings reads for native agent registration.
/// </summary>
internal static class AgentConfigurationJson
{
    public static JsonObject ParseObject(ReadOnlySpan<byte> content)
    {
        // Files and inline settings use the same UTF-8 JSONC contract, including a BOM.
        if (content.StartsWith(new byte[] { 0xef, 0xbb, 0xbf }))
        {
            content = content[3..];
        }

        var parseOptions = ConfigurationHelper.ParseOptions;
        parseOptions.AllowDuplicateProperties = false;
        var root = JsonNode.Parse(content, documentOptions: parseOptions);
        ArgumentNullException.ThrowIfNull(root);

        return root.AsObject();
    }

    public static JsonObject? OptionalObject(JsonObject parent, string key)
    {
        if (!parent.TryGetPropertyValue(key, out var node))
        {
            return null;
        }

        ArgumentNullException.ThrowIfNull(node, key);
        return node.AsObject();
    }

    public static JsonObject Object(JsonObject parent, string key)
    {
        var value = OptionalObject(parent, key);
        if (value is null)
        {
            value = new JsonObject();
            parent[key] = value;
        }

        return value;
    }

    public static JsonArray? OptionalStrings(JsonObject parent, string key)
    {
        if (!parent.TryGetPropertyValue(key, out var node))
        {
            return null;
        }

        ArgumentNullException.ThrowIfNull(node, key);
        return Strings(node.AsArray());
    }

    public static JsonArray Strings(JsonArray array)
    {
        foreach (var value in array)
        {
            ArgumentNullException.ThrowIfNull(value);
            _ = value.GetValue<string>();
        }

        return array;
    }

    public static string? String(JsonNode? node)
        => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    public static bool? Boolean(JsonObject parent, string key)
    {
        if (!parent.TryGetPropertyValue(key, out var node))
        {
            return null;
        }

        ArgumentNullException.ThrowIfNull(node, key);
        return node.GetValue<bool>();
    }

    public static Task<JsonObject?> ReadAsync(string path, CancellationToken cancellationToken)
        => ReadAsync(path, new AgentConfigurationReadContext(), cancellationToken);

    public static async Task<JsonObject?> ReadAsync(
        string path, AgentConfigurationReadContext reads, CancellationToken cancellationToken)
    {
        var bytes = await reads.ReadAsync(path, cancellationToken);
        if (bytes is null)
        {
            return null;
        }

        try
        {
            return ParseObject(bytes);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or ArgumentException)
        {
            throw new InvalidOperationException($"{path}: {ex.Message}", ex);
        }
    }

    public static async Task<IReadOnlyList<JsonObject>> ReadSettingsAsync(
        IEnumerable<string> paths,
        string targetPath,
        AgentConfigurationReadContext reads,
        CancellationToken cancellationToken)
    {
        var settings = new List<JsonObject>();
        foreach (var path in paths.Distinct(AgentPath.Comparer))
        {
            if (!AgentPath.Comparer.Equals(path, targetPath) &&
                await ReadAsync(path, reads, cancellationToken) is { } root)
            {
                settings.Add(root);
            }
        }

        return settings;
    }
}

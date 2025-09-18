// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Dashboard.Components.Controls;
using Aspire.Dashboard.Utils;

namespace Aspire.Dashboard.Model.GenAI;

[DebuggerDisplay("Name = {Name}, Value = {Value}")]
public sealed class GenAIPartPropertyViewModel : IPropertyGridItem
{
    public required string Name { get; set; }
    public required string Value { get; set; }
}

public sealed class GenAIItemPartViewModel
{
    public MessagePart? MessagePart { get; init; }
    public string? ErrorMessage { get; init; }
    public required TextVisualizerViewModel TextVisualizerViewModel { get; init; }
    public List<GenAIPartPropertyViewModel>? AdditionalProperties { get; init; }

    private GenAIItemPartViewModel()
    {
    }

    public static GenAIItemPartViewModel CreateErrorMessage(string errorMessage)
    {
        return new GenAIItemPartViewModel
        {
            ErrorMessage = errorMessage,
            TextVisualizerViewModel = new TextVisualizerViewModel(errorMessage, indentText: false)
        };
    }

    public static GenAIItemPartViewModel CreateMessagePart(MessagePart part)
    {
        return new GenAIItemPartViewModel
        {
            MessagePart = part,
            TextVisualizerViewModel = CreateMessagePartVisualizer(part),
            AdditionalProperties = part is GenericPart genericPart
                ? genericPart.AdditionalProperties?.Select(p => new GenAIPartPropertyViewModel { Name = p.Key, Value = p.Value.ToString() ?? string.Empty }).ToList()
                : null
        };
    }

    private static TextVisualizerViewModel CreateMessagePartVisualizer(MessagePart p)
    {
        if (p is TextPart textPart)
        {
            return new TextVisualizerViewModel(textPart.Content ?? string.Empty, indentText: true, fallbackFormat: DashboardUIHelpers.MarkdownFormat);
        }
        if (p is ToolCallRequestPart toolCallRequestPart)
        {
            return new TextVisualizerViewModel($"{toolCallRequestPart.Name}({toolCallRequestPart.Arguments?.ToJsonString()})", indentText: true, knownFormat: DashboardUIHelpers.JavascriptFormat);
        }
        if (p is ToolCallResponsePart toolCallResponsePart)
        {
            return new TextVisualizerViewModel(toolCallResponsePart.Response?.ToJsonString() ?? string.Empty, indentText: true);
        }

        var additionalProperties = p is GenericPart genericPart ? genericPart.AdditionalProperties ?? [] : [];
        var content = additionalProperties.Count > 0 ? ToJsonObject(additionalProperties).ToJsonString() : string.Empty;

        return new TextVisualizerViewModel(content, indentText: true);
    }

    private static JsonObject ToJsonObject(Dictionary<string, JsonElement> dict)
    {
        var jsonObject = new JsonObject();

        foreach (var kvp in dict)
        {
            jsonObject[kvp.Key] = JsonNode.Parse(kvp.Value.GetRawText());
        }

        return jsonObject;
    }
}

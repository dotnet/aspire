// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Aspire.Dashboard.Otlp.Model;
using Microsoft.FluentUI.AspNetCore.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Aspire.Dashboard.Model;

public class GenAIVisualizerDialogViewModel
{
    public required OtlpSpan Span { get; init; }
    public required List<OtlpLogEntry> LogEntries { get; init; }
    public required string Title { get; init; }
    public required SpanDetailsViewModel SpanDetailsViewModel { get; init; }

    public string? PeerName { get; set; }
    public string? SourceName { get; set; }

    public FluentTreeItem? SelectedTreeItem { get; set; }
    public List<GenAIEventViewModel> Events { get; } = new List<GenAIEventViewModel>();

    public GenAIEventViewModel? SelectedEvent { get; set; }

    public OverviewViewKind OverviewActiveView { get; set; }
    public ContentViewKind ContentActiveView { get; set; }
    public long? SelectedLogEntryId { get; set; }

    public string? ModelName { get; set; }
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
}

public enum OverviewViewKind
{
    InputOutput,
    Details
}

public enum ContentViewKind
{
    Preview,
    Raw
}

[DebuggerDisplay("SpanId = {Span.SpanId}, ResourceName = {ResourceName}, Type = {Type}, InternalId = {InternalId}")]
public class GenAIEventViewModel
{
    private static readonly Icon s_toolCallsIcon = new Icons.Regular.Size16.Code();
    private static readonly Icon s_messageIcon = new Icons.Regular.Size16.Mail();

    private static readonly Icon s_personIcon = new Icons.Filled.Size16.Person();
    private static readonly Icon s_systemIcon = new Icons.Filled.Size16.Laptop();
    private static readonly Icon s_toolIcon = new Icons.Filled.Size20.CodeCircle(); // used in 16px size

    public required long InternalId { get; set; }
    public required OtlpSpan Parent { get; set; }
    public required DateTimeOffset TimeStamp { get; set; }
    public required GenAIEventType Type { get; set; }
    public required object? Body { get; set; }
    public required string ResourceName { get; set; }

    public BadgeDetail GetEventCategory()
    {
        if (Body is AssistantEvent assistantEvent)
        {
            if (assistantEvent.ToolCalls?.Length > 0)
            {
                return new BadgeDetail("Tool calls", "tool-calls", s_toolCallsIcon);
            }
        }
        else if (Body is ChoiceEvent choiceEvent)
        {
            if (choiceEvent.Message?.ToolCalls?.Length > 0)
            {
                return new BadgeDetail("Completion", "completion", s_toolCallsIcon);
            }
            else
            {
                return new BadgeDetail("Completion", "completion", s_messageIcon);
            }
        }

        return new BadgeDetail("Message", "message", s_messageIcon);
    }

    public BadgeDetail GetEventTitle()
    {
        return Type switch
        {
            GenAIEventType.SystemMessage => new BadgeDetail("System", "system", s_systemIcon),
            GenAIEventType.UserMessage => new BadgeDetail("User", "user", s_personIcon),
            GenAIEventType.AssistantMessage or GenAIEventType.Choice => new BadgeDetail("Assistant", "assistant", s_personIcon),
            GenAIEventType.ToolMessage => new BadgeDetail("Tool", "tool", s_toolIcon),
            _ => throw new InvalidOperationException("Unexpected type: " + Type)
        };
    }
}

public record BadgeDetail(string Text, string Class, Icon Icon);

public enum GenAIEventType
{
    SystemMessage,
    UserMessage,
    AssistantMessage,
    ToolMessage,
    Choice
}

public sealed class SystemOrUserEvent
{
    public string? Role { get; set; }
    public string? Content { get; set; }
}

public sealed class AssistantEvent
{
    public string? Content { get; set; }
    public ToolCall[]? ToolCalls { get; set; }
}

public sealed class ToolEvent
{
    public string? Id { get; set; }
    public JsonNode? Content { get; set; }
}

public sealed class ChoiceEvent
{
    public string? FinishReason { get; set; }
    public int Index { get; set; }
    public AssistantEvent? Message { get; set; }
}

public sealed class ToolCall
{
    public string? Id { get; set; }
    public string? Type { get; set; } = "function";
    public ToolCallFunction? Function { get; set; }
}

public sealed class ToolCallFunction
{
    public string? Name { get; set; }
    public JsonNode? Arguments { get; set; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(SystemOrUserEvent))]
[JsonSerializable(typeof(AssistantEvent))]
[JsonSerializable(typeof(ToolEvent))]
[JsonSerializable(typeof(ChoiceEvent))]
public sealed partial class OtelContext : JsonSerializerContext;

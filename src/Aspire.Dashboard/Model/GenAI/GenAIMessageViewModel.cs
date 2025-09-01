// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Dashboard.Otlp.Model;
using Microsoft.FluentUI.AspNetCore.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Aspire.Dashboard.Model.GenAI;

public class GenAIMessagePartViewModel
{
    public required MessagePart MessagePart { get; init; }
    public required TextVisualizerViewModel TextVisualizerViewModel { get; init; }
}

[DebuggerDisplay("SpanId = {Span.SpanId}, ResourceName = {ResourceName}, Type = {Type}, InternalId = {InternalId}")]
public class GenAIMessageViewModel
{
    private static readonly Icon s_toolCallsIcon = new Icons.Regular.Size16.Code();
    private static readonly Icon s_messageIcon = new Icons.Regular.Size16.Mail();

    private static readonly Icon s_personIcon = new Icons.Filled.Size16.Person();
    private static readonly Icon s_systemIcon = new Icons.Filled.Size16.Laptop();
    private static readonly Icon s_toolIcon = new Icons.Filled.Size20.CodeCircle(); // used in 16px size

    public required long? InternalId { get; init; }
    public required OtlpSpan Parent { get; init; }
    public required GenAIEventType Type { get; init; }
    public required List<GenAIMessagePartViewModel> MessageParts { get; init; } = [];
    public required string ResourceName { get; init; }

    public BadgeDetail GetEventCategory()
    {
        if (Type == GenAIEventType.Choice)
        {
            if (MessageParts.Any(p => p.MessagePart.Type == MessagePart.ToolCallType))
            {
                return new BadgeDetail("Completion", "completion", s_toolCallsIcon);
            }
            else
            {
                return new BadgeDetail("Completion", "completion", s_messageIcon);
            }
        }
        if (MessageParts.Any(p => p.MessagePart.Type == MessagePart.ToolCallType))
        {
            return new BadgeDetail("Tool calls", "tool-calls", s_toolCallsIcon);
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

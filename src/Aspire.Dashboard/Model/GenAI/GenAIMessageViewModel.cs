// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Dashboard.Otlp.Model;
using Microsoft.FluentUI.AspNetCore.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Aspire.Dashboard.Model.GenAI;

[DebuggerDisplay("Type = {Type}, Index = {Index}, ResourceName = {ResourceName}")]
public class GenAIMessageViewModel
{
    private static readonly Icon s_toolCallsIcon = new Icons.Regular.Size16.Code();
    private static readonly Icon s_messageIcon = new Icons.Regular.Size16.Mail();

    private static readonly Icon s_personIcon = new Icons.Filled.Size16.Person();
    private static readonly Icon s_systemIcon = new Icons.Filled.Size16.Laptop();
    private static readonly Icon s_toolIcon = new Icons.Filled.Size20.CodeCircle(); // used in 16px size

    public required int Index { get; set; }
    public required long? InternalId { get; init; }
    public required OtlpSpan Parent { get; init; }
    public required GenAIMessageType Type { get; init; }
    public required List<GenAIMessagePartViewModel> MessageParts { get; init; } = [];
    public required string ResourceName { get; init; }

    public BadgeDetail GetEventCategory()
    {
        if (Type == GenAIMessageType.OutputMessage)
        {
            if (MessageParts.Any(p => p.MessagePart.Type == MessagePart.ToolCallType))
            {
                return new BadgeDetail("Tool calls", "output", s_toolCallsIcon);
            }
            else
            {
                return new BadgeDetail("Output", "output", s_messageIcon);
            }
        }
        if (MessageParts.Any(p => p.MessagePart.Type == MessagePart.ToolCallType))
        {
            return new BadgeDetail("Tool calls", "tool-calls", s_toolCallsIcon);
        }

        return new BadgeDetail("Message", "message", s_messageIcon);
    }

    public BadgeDetail GetMessageTitle()
    {
        return Type switch
        {
            GenAIMessageType.SystemMessage => new BadgeDetail("System", "system", s_systemIcon),
            GenAIMessageType.UserMessage => new BadgeDetail("User", "user", s_personIcon),
            GenAIMessageType.AssistantMessage or GenAIMessageType.OutputMessage => new BadgeDetail("Assistant", "assistant", s_personIcon),
            GenAIMessageType.ToolMessage => new BadgeDetail("Tool", "tool", s_toolIcon),
            _ => throw new InvalidOperationException("Unexpected type: " + Type)
        };
    }
}

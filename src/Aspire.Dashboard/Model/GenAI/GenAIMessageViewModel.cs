// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Resources;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Aspire.Dashboard.Model.GenAI;

[DebuggerDisplay("Index = {Index}, Type = {Type}, ResourceName = {ResourceName}")]
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

    public BadgeDetail GetCategoryBadge(IStringLocalizer<Dialogs> loc)
    {
        if (Type == GenAIMessageType.OutputMessage)
        {
            if (MessageParts.Any(p => p.MessagePart.Type == MessagePart.ToolCallType))
            {
                return new BadgeDetail(loc[nameof(Dialogs.GenAIMessageCategoryToolCalls)], "output", s_toolCallsIcon);
            }
            else
            {
                return new BadgeDetail(loc[nameof(Dialogs.GenAIMessageCategoryOutput)], "output", s_messageIcon);
            }
        }
        if (MessageParts.Any(p => p.MessagePart.Type == MessagePart.ToolCallType))
        {
            return new BadgeDetail(loc[nameof(Dialogs.GenAIMessageCategoryToolCalls)], "tool-calls", s_toolCallsIcon);
        }

        return new BadgeDetail(loc[nameof(Dialogs.GenAIMessageCategoryMessage)], "message", s_messageIcon);
    }

    public BadgeDetail GetTitleBadge(IStringLocalizer<Dialogs> loc)
    {
        return Type switch
        {
            GenAIMessageType.SystemMessage => new BadgeDetail(loc[nameof(Dialogs.GenAIMessageTitleSystem)], "system", s_systemIcon),
            GenAIMessageType.UserMessage => new BadgeDetail(loc[nameof(Dialogs.GenAIMessageTitleUser)], "user", s_personIcon),
            GenAIMessageType.AssistantMessage or GenAIMessageType.OutputMessage => new BadgeDetail(loc[nameof(Dialogs.GenAIMessageTitleAssistant)], "assistant", s_personIcon),
            GenAIMessageType.ToolMessage => new BadgeDetail(loc[nameof(Dialogs.GenAIMessageTitleTool)], "tool", s_toolIcon),
            _ => throw new InvalidOperationException("Unexpected type: " + Type)
        };
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Aspire.Dashboard.Components.Controls;

public partial class TreeGenAISelector
{
    private static readonly Icon s_toolCallsIcon = new Icons.Regular.Size16.Code();
    private static readonly Icon s_messageIcon = new Icons.Regular.Size16.Mail();

    private static readonly Icon s_personIcon = new Icons.Filled.Size16.Person();
    private static readonly Icon s_systemIcon = new Icons.Filled.Size16.Laptop();
    private static readonly Icon s_toolIcon = new Icons.Filled.Size20.CodeCircle(); // used in 16px size

    [Parameter, EditorRequired]
    public required Func<Task> HandleSelectedTreeItemChangedAsync { get; set; }

    [Parameter, EditorRequired]
    public required GenAIVisualizerDialogViewModel PageViewModel { get; set; }

    public void OnResourceChanged()
    {
        StateHasChanged();
    }

    private static BadgeDetail GetEventCategory(GenAIEvent e)
    {
        if (e.Body is AssistantEvent assistantEvent)
        {
            if (assistantEvent.ToolCalls?.Length > 0)
            {
                return new BadgeDetail("Tool calls", "tool-calls", s_toolCallsIcon);
            }
        }

        return new BadgeDetail("Message", "message", s_messageIcon);
    }

    private static BadgeDetail GetEventTitle(GenAIEvent e)
    {
        return e.Type switch
        {
            GenAIEventType.SystemMessage => new BadgeDetail("System", "system", s_systemIcon),
            GenAIEventType.UserMessage => new BadgeDetail("User", "user", s_personIcon),
            GenAIEventType.AssistantMessage or GenAIEventType.Choice => new BadgeDetail("Assistant", "assistant", s_personIcon),
            GenAIEventType.ToolMessage => new BadgeDetail("Tool", "tool", s_toolIcon),
            _ => throw new InvalidOperationException("Unexpected type: " + e.Type)
        };
    }

    private record BadgeDetail(string Text, string Class, Icon Icon);
}

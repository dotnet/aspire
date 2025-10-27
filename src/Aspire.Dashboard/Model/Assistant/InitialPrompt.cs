// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Model.Assistant;

public class InitialPrompt
{
    public InitialPrompt(Icon icon, string buttonTitle, string buttonDescription, string chatDisplayText, ChatMessage promptMessage)
        : this(icon, buttonTitle, buttonDescription, (context) => { context.ChatBuilder.AddUserMessage(chatDisplayText, promptMessage.Text); return Task.CompletedTask; })
    {
    }

    public InitialPrompt(Icon icon, string buttonTitle, string buttonDescription, Action<InitializePromptContext> createPrompt)
        : this(icon, buttonTitle, buttonDescription, (context) => { createPrompt(context); return Task.CompletedTask; })
    {
    }

    public InitialPrompt(Icon icon, string buttonTitle, string buttonDescription, Func<InitializePromptContext, Task> createPrompt)
    {
        Icon = icon;
        ButtonTitle = buttonTitle;
        ButtonDescription = buttonDescription;
        CreatePrompt = createPrompt;
    }

    public Icon Icon { get; }
    public string ButtonTitle { get; }
    public string ButtonDescription { get; }
    public Func<InitializePromptContext, Task> CreatePrompt { get; }
}

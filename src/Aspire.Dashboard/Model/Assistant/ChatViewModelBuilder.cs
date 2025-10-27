// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model.Markdown;

namespace Aspire.Dashboard.Model.Assistant;

public sealed class ChatViewModelBuilder(MarkdownProcessor markdownProcessor)
{
    private string? _displayText;
    private string? _promptText;

    public void AddUserMessage(string displayText, string promptText)
    {
        if (_displayText != null)
        {
            throw new InvalidOperationException("User message already added.");
        }

        _displayText = displayText;
        _promptText = promptText;
    }

    public ChatViewModel Build()
    {
        if (_displayText == null || _promptText == null)
        {
            throw new InvalidOperationException("User message not added.");
        }

        var userChatVM = new ChatViewModel(isUserMessage: true)
        {
            PromptText = _promptText
        };
        userChatVM.AppendMarkdown(_displayText, markdownProcessor, suppressSurroundingParagraph: true);

        return userChatVM;
    }
}

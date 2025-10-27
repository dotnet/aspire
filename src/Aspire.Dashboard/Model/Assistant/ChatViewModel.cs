// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Web;
using Aspire.Dashboard.Model.Markdown;
using Microsoft.Extensions.AI;

namespace Aspire.Dashboard.Model.Assistant;

public sealed class ChatViewModel
{
    private static long s_nextId;

    public ChatViewModel(bool isUserMessage)
    {
        Id = Interlocked.Increment(ref s_nextId);
        ElementId = $"chat-message-{Id}";
        IsUserMessage = isUserMessage;
    }

    private string? _markdown;

    public long Id { get; }
    public string ElementId { get; }
    public bool IsUserMessage { get; }

    public bool IsComplete { get; set; }
    public bool IsLiked { get; set; }
    public bool IsDisliked { get; set; }
    public string PromptText { get; set; } = string.Empty;
    public string Html { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public bool IsForbidden { get; set; }
    public DateTime? LimitResetDate { get; set; }

    private readonly List<ChatMessageViewModelBase> _chatMessages = new();

    public int ChatMessageCount => _chatMessages.Count;

    public IEnumerable<ChatMessage> GetChatMessages()
    {
        foreach (var chatMessageViewModel in _chatMessages)
        {
            yield return chatMessageViewModel.GetChatMessage();
        }
    }

    public void AddChatMessage(ChatMessage chatMessage)
    {
        _chatMessages.Add(new StaticChatMessageViewModel(chatMessage));
    }

    public void AddChatMessage(Func<ChatMessage> chatMessageProvider)
    {
        _chatMessages.Add(new DynamicChatMessageViewModel(chatMessageProvider));
    }

    public void AppendMarkdown(string markdown, MarkdownProcessor markdownProcessor, bool inCompleteDocument = false, bool suppressSurroundingParagraph = false)
    {
        _markdown += markdown;
        Html = markdownProcessor.ToHtml(_markdown, inCompleteDocument: inCompleteDocument, suppressSurroundingParagraph: suppressSurroundingParagraph);
    }

    public void SetText(string text)
    {
        _markdown = null;

        // Prevent user injecting HTML into the page.
        var newText = HttpUtility.HtmlEncode(text);

        // New lines in message should be preserved in displayed text.
        newText = newText.Replace("\r\n", "<br />", StringComparison.Ordinal)
            .Replace("\r", "<br />", StringComparison.Ordinal)
            .Replace("\n", "<br />", StringComparison.Ordinal);

        Html = newText;
    }

    public void ClearMarkdown()
    {
        _markdown = null;
        Html = string.Empty;
    }

    private abstract class ChatMessageViewModelBase
    {
        public abstract ChatMessage GetChatMessage();
    }

    private sealed class DynamicChatMessageViewModel : ChatMessageViewModelBase
    {
        private readonly Func<ChatMessage> _chatMessageProvider;

        public DynamicChatMessageViewModel(Func<ChatMessage> chatMessageProvider)
        {
            _chatMessageProvider = chatMessageProvider;
        }

        public override ChatMessage GetChatMessage()
        {
            return _chatMessageProvider();
        }
    }

    private sealed class StaticChatMessageViewModel : ChatMessageViewModelBase
    {
        private readonly ChatMessage _chatMessage;

        public StaticChatMessageViewModel(ChatMessage chatMessage)
        {
            _chatMessage = chatMessage;
        }

        public override ChatMessage GetChatMessage()
        {
            return _chatMessage;
        }
    }
}

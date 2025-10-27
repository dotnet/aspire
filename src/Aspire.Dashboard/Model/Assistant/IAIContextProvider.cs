// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model.Assistant.Ghcp;
using Aspire.Dashboard.Model.Assistant.Prompts;

namespace Aspire.Dashboard.Model.Assistant;

public interface IAIContextProvider
{
    bool Enabled { get; }
    AIContext AddNew(string description, Action<AIContext> configure);
    void Remove(AIContext context);
    AIContext? GetContext();
    IDisposable OnContextChanged(Func<Task> callback);
    IDisposable OnDisplayChanged(Func<Task> callback);
    IceBreakersBuilder IceBreakersBuilder { get; }

    AssistantChatViewModel? AssistantChatViewModel { get; }
    bool ShowAssistantSidebarDialog { get; }
    AssistantChatState? ChatState { get; set; }

    Task LaunchAssistantModelDialogAsync(AssistantChatViewModel viewModel, bool openedForMobileView = false);
    Task LaunchAssistantSidebarAsync(Func<InitializePromptContext, Task> sendInitialPrompt);
    Task LaunchAssistantSidebarAsync(AssistantChatViewModel viewModel);
    Task HideAssistantSidebarAsync();
    Task SetAssistantSidebarAsync(AssistantChatViewModel viewModel);
    Task<GhcpInfoResponse> GetInfoAsync(CancellationToken cancellationToken);
}

public static class AIContextExtensions
{
    public static void EnsureEnabled(this IAIContextProvider aiContextProvider)
    {
        if (!aiContextProvider.Enabled)
        {
            throw new InvalidOperationException("AI is disabled.");
        }
    }
}

public record InitializePromptContext(ChatViewModelBuilder ChatBuilder, AssistantChatDataContext DataContext, IServiceProvider ServiceProvider);
